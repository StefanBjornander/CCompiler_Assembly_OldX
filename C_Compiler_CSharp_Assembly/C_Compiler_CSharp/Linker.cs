using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Linker {
    private int m_totalSize = 256;
  
    private IDictionary<string,Symbol> m_globalMap = new Dictionary<string,Symbol>();
    private List<Symbol> m_globalList = new MyList<Symbol>();
    private IDictionary<string,int> m_addressMap = new Dictionary<string,int>(),
                                    m_entryMap = new Dictionary<string,int>();
    private FileInfo m_comFile, m_asmFile;
  
    public Linker(FileInfo comFile, FileInfo asmFile) {
      m_comFile = comFile;
      m_asmFile = asmFile;
    }
  
    public void Add(Symbol symbol) {
      string name = symbol.UniqueName;
      Assert.Error(!m_globalMap.ContainsKey(name) || name.EndsWith(Start.NumberId),
                   SimpleName(name), "duplicate global identifier");
      m_globalMap[name] = symbol;
    }

    public void Generate() {
      m_globalMap.Add(Start.PathName, GeneratePathSymbol());

      { Symbol mainInfo;
        Assert.Error(m_globalMap.TryGetValue("main", out mainInfo),
                     "main", "function missing");
        GenerateTrace(mainInfo);
      }

      StreamWriter streamWriter = new StreamWriter("C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\Linker2.debug");
      foreach (Symbol symbol in m_globalList) {
        streamWriter.WriteLine(symbol.UniqueName.Replace("\n", "\\n"));
      }
      streamWriter.Close();

      /*{ Symbol symbol = m_globalMap[Start.PathName];
        m_globalList.Add(symbol);
        m_addressMap.Add(symbol.UniqueName, m_totalSize);
        m_entryMap.Add(symbol.UniqueName, m_totalSize + symbol.EntryPoint);
        m_totalSize += symbol.ByteList.Count;
      }*/

      m_addressMap.Add(Start.StackTopName, m_totalSize);
    
      foreach (Symbol symbol in m_globalList) {
        List<sbyte> byteList = symbol.ByteList;
        int startAddress = m_addressMap[symbol.UniqueName];
        GenerateAccess(symbol.AccessMap, byteList);
        GenerateCall(startAddress, symbol.CallMap, byteList,
                     symbol.GetTextList(), symbol.ByteToTextMap());
        GenerateReturn(startAddress, symbol.ReturnSet, byteList);
      }

      foreach (Symbol symbol in m_globalList) {
        int startAddress = m_addressMap[symbol.UniqueName];
        SplitTextList(symbol.GetTextList());
        GenerateTextAddress(startAddress, symbol.ByteList,
                            symbol.GetTextList());
      }
    
      { Console.Out.WriteLine("Generating \"" + m_comFile.FullName + "\".");
        StreamWriter s = new StreamWriter("c:\\d\\y");
        m_comFile.Delete();
        BinaryWriter comStream = new BinaryWriter(File.OpenWrite(m_comFile.FullName));

        foreach (Symbol symbol in m_globalList) {
          s.WriteLine(symbol.UniqueName);
          foreach (sbyte b in symbol.ByteList) {
            comStream.Write(b);
          }
        }

        s.Close();
        comStream.Close();
      }
    
      { Console.Out.WriteLine("Generating \"" + m_asmFile.FullName + "\".");
        m_asmFile.Delete();
        StreamWriter asmStream = new StreamWriter(m_asmFile.FullName);
        asmStream.WriteLine("\torg 100h");

        foreach (Symbol symbol in m_globalList) {
          foreach (string text in symbol.GetTextList()) {
            if (text.Length > 0) {
              asmStream.WriteLine(text);
            }
          }
        }
      
        asmStream.WriteLine("\n" + Start.StackTopName + ":");
        asmStream.WriteLine("x" + m_totalSize + ":");
        asmStream.Close();
      }    
    }
  
    private Symbol GeneratePathSymbol() {
      List<sbyte> byteList = new MyList<sbyte>();
      IDictionary<int,string> accessMap = new Dictionary<int,string>();
      List<string> textList = new MyList<string>();

      Symbol symbol = new Symbol(Symbol.Status.Variable, Start.PathName, Storage.Static, CCompiler.Type.StringType, @"C:\D\Main.com",
                                 byteList, accessMap, new Dictionary<int,string>(), new HashSet<int>(), new Dictionary<int,int>(), textList);
      GenerateInitializer.GenerateByteTextList(symbol);
      return symbol;
    }
  
    private void GenerateTrace(Symbol symbol) {
      if (!symbol.UniqueName.Equals(Start.PathName) && !m_globalList.Contains(symbol)) {
        m_globalList.Add(symbol);
        m_addressMap.Add(symbol.UniqueName, m_totalSize);
        m_entryMap.Add(symbol.UniqueName, m_totalSize + symbol.EntryPoint);
        m_totalSize += symbol.ByteList.Count;
      
        ISet<string> accessNameSet =
          new HashSet<string>(symbol.AccessMap.Values);
        foreach (string accessName in accessNameSet) {
          if (!accessName.Equals(Start.StackTopName)) {
            Symbol accessSymbol = m_globalMap[accessName];
            Assert.Error(accessSymbol != null, "Linkage",
                         "missing external variable \"" + SimpleName(accessName)+
                         "\" in \"" + SimpleName(symbol.UniqueName) +"\"");
            GenerateTrace(accessSymbol);
          }
        }

        ISet<string> callNameSet = new HashSet<string>(symbol.CallMap.Values);
        foreach (string callName in callNameSet) {
          Symbol funcSymbol = m_globalMap[callName];
          Assert.Error(funcSymbol != null, callName, "missing extern function \""+
                       SimpleName(callName) + "\" in \"" +
                       SimpleName(symbol.UniqueName) + "\"");
          GenerateTrace(funcSymbol);
        }      
      }
    }

    private void GenerateAccess(IDictionary<int,string> accessMap,
                                List<sbyte> byteList) {
      foreach (KeyValuePair<int,string> entry in accessMap) {
        string name = entry.Value;

        if (name.Equals(Start.PathName)) {
          Symbol symbol = m_globalMap[Start.PathName];
          m_globalList.Add(symbol);
          m_addressMap.Add(symbol.UniqueName, m_totalSize);
          m_entryMap.Add(symbol.UniqueName, m_totalSize + symbol.EntryPoint);
          m_totalSize += symbol.ByteList.Count;
        }
      }

      foreach (KeyValuePair<int,string> entry in accessMap) {
        int address = entry.Key;
        string name = entry.Value;

        int oldLowByte = CheckValue(byteList[address]),
            oldHighByte = CheckValue(byteList[address + 1]);

        int oldTarget = (oldHighByte << 8) + oldLowByte;
        int newTarget = oldTarget + m_addressMap[name];

        sbyte newLowByte = (sbyte) newTarget,
              newHighByte = (sbyte) (newTarget >> 8);

        byteList[address] = newLowByte;
        byteList[address + 1] = newHighByte;
      }
    }
  
    private static int CheckValue(int i) {
      return (i < 0) ? (i + 256) : i;
    }

    private void GenerateCall(int startAddress, IDictionary<int,string> callMap,
                              List<sbyte> byteList, List<string> textList,
                              IDictionary<int,int> byteToTextMap) {
      foreach (KeyValuePair<int,string> entry in callMap) {
        int address = entry.Key;
        int callerAddress = startAddress + address + 2;
        int calleeAddress = m_entryMap[entry.Value];
        int relativeAddress = calleeAddress - callerAddress;

        if (relativeAddress == -129) {
          byteList[address - 1] = (sbyte) AssemblyCode.ShortJumpOperator;
          byteList[address] = (sbyte) -128;
          byteList[address + 1] = (sbyte) AssemblyCode.NopOperator;
        }
        else if (relativeAddress == 127) { // XXX
          int baseAddress = startAddress + address;

          //if (baseAddress < 10834) {
          //if (baseAddress < 10532) {
          //if (baseAddress < 11540) {
          //if (baseAddress < 11638) {
          //if (baseAddress < 11768) {
          //if (baseAddress < 11790) {
          //if (baseAddress <= 12322) {
          //if (baseAddress <= 12522) {
          //if (baseAddress <= 12525) {
          //if (baseAddress < 11507) {
          //if (baseAddress < 11485) {
          //if (baseAddress < 11501) {
          //if (baseAddress < 11449) {
          //if (baseAddress <= 11488) {
          //if (baseAddress <= 11492) {
          if (baseAddress <= 12849) {
            byteList[address - 1] = (sbyte) AssemblyCode.NopOperator;
            byteList[address] = (sbyte) AssemblyCode.ShortJumpOperator;
            byteList[address + 1] = (sbyte) relativeAddress;
        
            int textLine = byteToTextMap[address];
            Assert.Error(textLine < textList.Count, "text line");
        
            string text = textList[textLine];
            textList[textLine] = "\tnop\t; 1\n" + text.Replace("\t; 3", "\t; 2");
          }
          else {
            byteList[address] = (sbyte) relativeAddress;
            byteList[address + 1] = (sbyte)(relativeAddress >> 8);
          }
        }
        else if ((relativeAddress >= -128) && (relativeAddress <= 126)) {
          byteList[address - 1] = (sbyte) AssemblyCode.NopOperator;
          byteList[address] = (sbyte) AssemblyCode.ShortJumpOperator;
          byteList[address + 1] = (sbyte) relativeAddress;
        
          int textLine = byteToTextMap[address];
          Assert.Error(textLine < textList.Count, "text line");
        
          string text = textList[textLine];
          textList[textLine] = "\tnop\t; 1\n" + text.Replace("\t; 3", "\t; 2");
        }
        else {
          byteList[address] = (sbyte) relativeAddress;
          byteList[address + 1] = (sbyte)(relativeAddress >> 8);
        }
      }
    }
  
    private void GenerateReturn(int startAddress, ISet<int> returnSet,
                                List<sbyte> byteList) {
      foreach (int address in returnSet) {
        int relativeLowByte = CheckValue(byteList[address]),
            relativeHighByte = CheckValue(byteList[address + 1]);
          
        int relativeAddress = (relativeHighByte << 8) + relativeLowByte;
        int globalAddress = startAddress + address + relativeAddress;
      
        sbyte globalLowByte = (sbyte) globalAddress;
        sbyte globaHighByte = (sbyte) (globalAddress >> 8);

        byteList[address] = globalLowByte;
        byteList[address + 1] = globaHighByte;
      }
    }

    private void GenerateTextAddress(int address, List<sbyte> byteList,
                                     List<string> textList) {
      int byteIndex = 0;
    
      for (int line = 0; line < textList.Count; ++line) {
        string text = textList[line];
      
        if ((text.Length > 0) && !text.Contains(":\t;") &&
            !text.StartsWith("\t;") && !text.EndsWith(":")) {
          StringBuilder buffer = new StringBuilder(text);
          buffer.Insert(0, "x" + address + ":");

          int semiIndex = buffer.ToString().LastIndexOf("; "), size;
          string subText = buffer.ToString().Substring(semiIndex + 2);
          Assert.Error(int.TryParse(subText, out size), text);

          buffer.Append(":");
          for (int index = 0; index < size; ++index) {
            buffer.Append(" " + ((byte) byteList[byteIndex++]));
          }

          textList[line] = buffer.ToString();
          address += size;
        }
      }
    
      Assert.Error(byteIndex == byteList.Count, "linker sbyte list");
    }

    private void SplitTextList(List<string> textList) {
      for (int index = (textList.Count - 1); index >= 0; --index) {
        string text = textList[index];

        int newLineIndex = text.IndexOf("\n");
        if ((newLineIndex != -1) && !text.EndsWith("\n")) {
          textList[index] = text.Substring(0, newLineIndex);
          textList.Insert(index + 1, text.Substring(newLineIndex + 1));
        }
      }
    }
  
    string SimpleName(string name) {
      int index = name.LastIndexOf(Start.SeparatorId);
      return ((index != -1)) ? name.Substring(0, index) : name;
    }  
  }
}