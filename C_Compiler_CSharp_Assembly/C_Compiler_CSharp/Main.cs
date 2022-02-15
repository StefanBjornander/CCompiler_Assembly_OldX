using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic; 

namespace CCompiler {
  public class Start {
    public static Symbol IntegralStorageSymbol, FloatingStorageSymbol;
    public static StreamWriter AssemblyFile;

    public static int FloatingStackMaxSize = 7;
    public static int FunctionHeaderSize = 6;

    public static bool Debug2 = true;
    public static bool SmallWarning = false, LargeWarning = false;
    public static bool CheckStackHeap = false;

    public static int ListInitCount = 0;  
    public static bool LiveSet = false;
    public static int LiveCount = 0;
    public static int ParameterCount = 0;
  
    public static int FileCount = 0, SolidCount = 0, TemporaryCount = 0;
    public static List<string> AssemblerList = new MyList<string>();

    public static int TotalParamOffset = 0;
    public static Stack<IDictionary<BigInteger,MiddleCode>> CaseMapStack = new Stack<IDictionary<BigInteger,MiddleCode>>();
    public static Stack<MiddleCode> DefaultStack = new Stack<MiddleCode>();
    public static ISet<string> LabelSet = new HashSet<string>(), GotoSet = new HashSet<string>();

    public static Stack<Specifier> SpecifierStack = new Stack<Specifier>();
    public static IDictionary<string,Macro> MacroMap = new Dictionary<string,Macro>();
    public static Stack<FileInfo> IncludeStack = new Stack<FileInfo>();
    public static ISet<FileInfo> IncludeSet = new HashSet<FileInfo>();
    public static Stack<Triple<bool,bool,Preprocessor.IfStatus>>
                      IfStack = new Stack<Triple<bool,bool,Preprocessor.IfStatus>>();

    public static int NewlineCount = 0;
    public static bool Whitespace = false;
    public static string AssemblerPrompt2 = "Assembler: ";
    public const int MarginSize2 = 20;

    public const string NumberId = "#";
    public const string TemporaryId = "£";
    public const string SeparatorId = "$";
    public const string FileMarker = "@";

    public static IDictionary<ObjectCodeInfo, sbyte[]> MainArrayMap = new SortedDictionary<ObjectCodeInfo, sbyte[]>(new ObjectCodeComparer()); // ListMapXXX
    //public static IDictionary<ObjectCodeInfo, sbyte[]> MainArrayMap = new Dictionary<ObjectCodeInfo, sbyte[]>(); // ListMapXXX

    //public static string IntegralStorageName = SeparatorId + "IntegralStorage" + NumberId;
    public static string FloatingStorageName = SeparatorId + "FloatingStorage" + NumberId;
    public static string CheckStackHeapFunctionRegular = SeparatorId + "CheckStackHeapRegular";
    public static string CheckStackHeapFunctionEllipse = SeparatorId + "CheckStackHeapEllipse";
    public static string StackHeapError = SeparatorId + "StackHeapError";
    public static string StackHeapErrorEllipse = SeparatorId + "StackHeapErrorEllipse";
    public static string StackOverflowName = SeparatorId + "StackOverflow";
    public static string StackOverflowMessage = "Stack Overflow => Exit -1.";
    public static string PathName = SeparatorId + "Path";
    public static string PathText = "";
    public static string InitCodeName = SeparatorId + "InitCode";
    public static string InitArgsName = SeparatorId + "InitArgs";
    public static string StackTopName = SeparatorId + "StackTop";
  
    public static IDictionary<ObjectCodeInfo,sbyte[]> ByteArrayMap = new Dictionary<ObjectCodeInfo,sbyte[]>();  
    public static int ObjectCodeTableMapSize = 21;

    public static TextWriter ErrorStream = Console.Error;
    public static int Line = 1;
    public static Symbol CurrentFunction = null;
    public static string IncludePath = null, Path = null;

    public static SymbolTable CurrentTable = null;

    public static Stack<ISet<MiddleCode>> BreakSetStack = new Stack<ISet<MiddleCode>>(),
                                          ContinueSetStack = new Stack<ISet<MiddleCode>>();
    public static IDictionary<string,ISet<MiddleCode>> GotoSetMap = new Dictionary<string,ISet<MiddleCode>>();
    public static IDictionary<string,MiddleCode> LabelMap = new Dictionary<string,MiddleCode>();

    public static Stack<List<CCompiler.Type>> TypeListStack = new Stack<List<CCompiler.Type>>();
    public static Stack<int> ParameterIndexStack = new Stack<int>();
    public static Stack<int> ParameterOffsetStack = new Stack<int>();

    public static Stack<BigInteger> EnumValueStack = new Stack<BigInteger>();

    public static StreamWriter AssemblyStream;
    public static StreamWriter BeforeCodeStream;
    public static StreamWriter MiddleCodeStream;
    public static StreamWriter AfterCodeStream;
    public static StreamWriter GraphCodeStream;
    public static StreamWriter TrackCodeStream;
    public static StreamWriter ObjectCodeStream;
          
    public static StreamWriter MemberStream;

    public static StreamWriter symbolStream = new StreamWriter("C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\Test2.txt");

    public static void Main(string[] args) {
      System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      ObjectCodeTable.Init();
      args = new string[]{"-r", "-p", /*"-w", "-c",*/ "-l1", "Main", "Malloc", "CType", "ErrNo", "Locale", "Math", "SetJmp",      
                          "Signal", "File", "Temp", "Scanf", "Printf", "StdLib", "Time",
                          "String", "PrintTest", "CharacterTest", "FloatTest", "LimitsTest",
                          "AssertTest", "StringTest", "LocaleTest",
                          "SetJmpTest", "MathTest", "FileTest", "StdIOTest",
                          "SignalTest", "StackTest", "MallocTest",
                          "StdLibTest", "TimeTest"};

      if (args.Length == 0) {
        Console.Error.WriteLine("usage: compiler <filename>");
        Environment.Exit(-1);
      }
    
      List<string> argList = new MyList<string>();
      foreach (string text in args) {
        argList.Add(text);
      }

      bool rebuild = argList.Remove("-r"),
              print = argList.Remove("-p");
      CheckStackHeap = argList.Remove("-c");
      CheckStackHeap = false;
      SmallWarning = argList.Remove("-w");
      LargeWarning = argList.Remove("-W");
      bool noLink = argList.Remove("-nolink");
    
      if (LargeWarning) {
        SmallWarning = LargeWarning;
      }

      foreach (string arg in argList) {
        if (arg.StartsWith("-l")) {
          LiveSet = true;
          LiveCount = int.Parse(arg.Substring(2));
          argList.Remove(arg);
          break;
        }
      }

      PathText = "C:\\D\\" + argList[0] + "A.com";
      IncludePath = Environment.GetEnvironmentVariable("include");
      string pathName = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\";
      //string pathName = "C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\stdio\\";
    
      if (IncludePath == null) {
        IncludePath = pathName;
      }

      bool windows = true, linux = false;

      try {
        MemberStream = new StreamWriter(pathName + "Test.Member");
        bool doLink = false;
        string pathName2 = pathName;

        foreach (string arg in argList) {
          FileInfo sourceFile = new FileInfo(pathName + arg + ".c");
          FileInfo objectFile = new FileInfo(pathName + arg + ".obj");

          Start.AssemblyFile = new StreamWriter(pathName2 + arg + ".asm");
          Start.BeforeCodeStream = new StreamWriter(pathName2 + arg + ".before");
          Start.MiddleCodeStream = new StreamWriter(pathName2 + arg + ".middle");
          Start.AfterCodeStream = new StreamWriter(pathName2 + arg + ".after");
          Start.GraphCodeStream = new StreamWriter(pathName2 + arg + ".graph");
          Start.TrackCodeStream = new StreamWriter(pathName2 + arg + ".track");
          Start.ObjectCodeStream = new StreamWriter(pathName2 + arg + ".object");

          if (rebuild || !IsObjectFileUpToDate(sourceFile, objectFile)) {
            FileInfo preproFile = new FileInfo(pathName + arg + ".p");
            FileInfo middleFile = new FileInfo(pathName + arg + ".mid");

            if (print) {
              Console.Out.WriteLine("Compiling \"" + sourceFile.FullName + "\"."); 
            }
          
            ReadSourceFile(sourceFile, preproFile, objectFile, middleFile, windows, linux);
            doLink = true;
          }

          Start.AssemblyFile.Close();
          Start.BeforeCodeStream.Close();
          Start.MiddleCodeStream.Close();
          Start.AfterCodeStream.Close();
          Start.GraphCodeStream.Close();
          Start.TrackCodeStream.Close();
          Start.ObjectCodeStream.Close();
        }
      
        if (!noLink && doLink) {
          FileInfo comFile = new FileInfo(PathText);
          FileInfo asmFile = new FileInfo(pathName2 + argList[0] + "_Asm.asm");
          Linker linker = new Linker(comFile, asmFile);

          Path = null;
          foreach (string arg in argList) {
            FileInfo objectFile = new FileInfo(pathName + arg + ".obj");

            if (print) {
              Console.Out.WriteLine("Loading \"" + objectFile.FullName + "\".");
            }
          
            ReadObjectFile(objectFile, linker);
          }

          linker.Generate();
        }
        else if (print) {
          Console.Out.WriteLine(pathName + argList[0] +".com is up-to-date.");
        }

        MemberStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(exception.Message);
      }

      symbolStream.WriteLine(Start.TemporaryCount);
      symbolStream.Close();
    }
 
    public static void ReadObjectFile(FileInfo objectFile, Linker linker) {
      try {
        BinaryReader dataInputStream = new BinaryReader(File.OpenRead(objectFile.FullName));
    
        int includeSetSize = dataInputStream.ReadInt32();
        for (int count = 0; count < includeSetSize; ++count) {
          IncludeSet.Add(new FileInfo(dataInputStream.ReadString()));
        }

        int linkerSetSize = dataInputStream.ReadInt32();
        for (int count = 0; count < linkerSetSize; ++count) {
          Symbol symbol = new Symbol();
          symbol.Load(dataInputStream);
          linker.Add(symbol);
        }

        FileCount++;
        dataInputStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(exception.Message);
      }
    }
  
    public static void ReadSourceFile(FileInfo sourceFile, FileInfo preproFile,
                                      FileInfo objectFile, FileInfo middleFile,
                                      bool windows, bool linux) {
      MacroMap.Clear();

      if (windows) {
        MacroMap.Add("__WINDOWS__", new Macro(0, new List<Token>()));
      }

      if (linux) {
        MacroMap.Add("__LINUX__", new Macro(0, new List<Token>()));
      }

      IncludeSet.Clear();
      Preprocessor preprocessor = new Preprocessor();
      preprocessor.doProcess(sourceFile);
    
      Assert.Error(IfStack.Count == 0,
                   "#if, #ifdef, or #ifndef without matching #endif");
    
      StreamWriter preproStream = File.CreateText(preproFile.FullName);
      preproStream.Write(preprocessor.GetText());
      preproStream.Close();

      byte[] byteArray = Encoding.ASCII.GetBytes(preprocessor.GetText());
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Main.Scanner scanner = new CCompiler_Main.Scanner(memoryStream);

      try {
        SymbolTable.TableCount = 0;
        Start.CurrentTable = new SymbolTable(null, Scope.Global);
      
        /*IntegralStorageSymbol = new Symbol(IntegralStorageName, Storage.Static, CCompiler.Type.UnsignedLongIntegerType);
        FloatingStorageSymbol = new Symbol(FloatingStorageName, Storage.Static, CCompiler.Type.UnsignedLongIntegerType);
        CurrentTable.AddSymbol(IntegralStorageSymbol);
        CurrentTable.AddSymbol(FloatingStorageSymbol);*/
      
        Start.Path = sourceFile.FullName;
        Start.Line = 1;
        CCompiler_Main.Parser parser = new CCompiler_Main.Parser(scanner);
        Assert.Error(parser.Parse(), "parse");
      }
      catch (IOException ioException) {
        Assert.Error(ioException.StackTrace, " Syntax Error");
      }

      BinaryWriter binaryWriter = new BinaryWriter(File.Open(objectFile.FullName, FileMode.Create));
      binaryWriter.Write(IncludeSet.Count);

      foreach (FileInfo includeFile in IncludeSet) {
        binaryWriter.Write(includeFile.FullName);
      }

      ISet<Symbol> staticSet = CurrentTable.StaticSet;
      binaryWriter.Write(staticSet.Count);
    
      foreach (Symbol symbol in staticSet) {
        symbol.Save(binaryWriter);
      }

      binaryWriter.Close();
    }

    public static bool IsObjectFileUpToDate(FileInfo sourceFile, FileInfo objectFile) {
      if (!objectFile.Exists || (sourceFile.LastWriteTime > objectFile.LastWriteTime)) {
        return false;
      }
      
      try {
        BinaryReader objectReader = new BinaryReader(File.OpenRead(objectFile.FullName));
        bool result = true;

        int TraceSize = objectReader.ReadInt32();
        for (int count = 0; count < TraceSize; ++count) {
          FileInfo TraceFile = new FileInfo(objectReader.ReadString());

          if (TraceFile.LastWriteTime > objectFile.LastWriteTime) {
            result = false;
            break;
          }
        }

        objectReader.Close();
        return result;
      }
      catch (IOException ioException) {
        Console.Out.WriteLine(ioException.StackTrace);
        return false;
      }
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op) {
      return AddMiddleCode(codeList, new MiddleCode(op, null, null, null));
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op, object operand1) {
      return AddMiddleCode(codeList, new MiddleCode(op, operand1, null, null));
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op, object operand1,
                                    object operand2) {
      return AddMiddleCode(codeList, new MiddleCode(op, operand1, operand2, null));
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op, object operand1,
                                    object operand2, object operand3) {
      return AddMiddleCode(codeList, new MiddleCode(op, operand1, operand2, operand3));
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleOperator op, object operand1,
                                    object operand2, object operand3, object operand4) {
      return AddMiddleCode(codeList, new MiddleCode(op, operand1, operand2, operand3, operand4));
    }

    public static MiddleCode AddMiddleCode(List<MiddleCode> codeList, MiddleCode middleCode) {
      codeList.Add(middleCode);
      return middleCode;
    }

    public static void Backpatch(MiddleCode source, List<MiddleCode> list) {
      if (list.Count == 0) {
        list.Add(new MiddleCode(MiddleOperator.Empty));
      }
    
      Backpatch(source, list[0]);
    }

    public static void Backpatch(MiddleCode source, MiddleCode target) {
      if (source.GetOperand(0) == null) {
        source.SetOperand(0, target);
      }
      else {
        Assert.Error(target.Equals(source.GetOperand(0)));
      }
    }

    public static void Backpatch(ISet<MiddleCode> sourceSet, List<MiddleCode> list) {
      if (sourceSet != null) {
        foreach (MiddleCode source in sourceSet) {
          Backpatch(source, list);
        }
      }
    }  

    public static void Backpatch(ISet<MiddleCode> sourceSet, MiddleCode target) {
      if (sourceSet != null) {
        foreach (MiddleCode source in sourceSet) {
          Backpatch(source, target);
        }
      }
    }  
  }
}

namespace CCompiler_Main {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Exp {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}

namespace CCompiler_Pre {
  public partial class Parser : QUT.Gppg.ShiftReduceParser<ValueType, QUT.Gppg.LexLocation> {
    public Parser(Scanner scanner)
     :base(scanner) {
      // Empty.
    }
  }
}