using System;
using System.IO;
using System.Text;
//using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Preprocessor {
    public enum IfStatus {If, Else};
  
    //private IDictionary<string,Macro> Start.MacroMap = new Dictionary<string,Macro>();
  
    private StringBuilder m_buffer = new StringBuilder();

    public string GetText() {
      return m_buffer.ToString();
    }  
  
    private void AddToBuffer(List<Token> tokenList) {
      bool first = true;

      foreach (Token token in tokenList) {
        int newlineCount = token.GetNewlineCount();

        if (newlineCount > 0) {      
          for (int count = 0; count < newlineCount; ++count) {
            m_buffer.Append('\n');
            //++Start.Line;
            //Console.Out.WriteLine("1: " + Start.Line);
          }
        
          first = true;
        }

        string text = token.ToString();
      
        if (text.Length > 0) {
          m_buffer.Append((first ? "" : " ") + text);
          first = false;
        }
      }
  
      m_buffer.Append('\n');
      ++Start.Line;
      //Console.Out.WriteLine("2: " + Start.Line);
    }
  
    private void AddNewlinesToBuffer(List<Token> tokenList) {
      foreach (Token token in tokenList) {
        int newlineCount = token.GetNewlineCount();

        if (newlineCount > 0) {      
          for (int count = 0; count < newlineCount; ++count) {
            m_buffer.Append('\n');
            ++Start.Line;
            //Console.Out.WriteLine("3: " + Start.Line);
          }
        }
      }
  
      m_buffer.Append('\n');
      ++Start.Line;
      //Console.Out.WriteLine("4: " + Start.Line);
    }
  
    public void doProcess(FileInfo file) {
      StringBuilder buffer = new StringBuilder();
      ReadFile(file, buffer);
      triGraphs(buffer);
      Start.Path = file.FullName;
      Start.Line = 1;
      traverse(buffer);    

      int stackSize = Start.IfStack.Count;
      List<string> lineList = GenerateLineList(buffer.ToString());

      Start.Line = 1;
      traverseLineList(lineList);
      Assert.Error(Start.IfStack.Count == stackSize,
                   "unbalanced #if and #endif strucure");
    }

    private void ReadFile(FileInfo file, StringBuilder buffer) {
      StreamReader streamReader = new StreamReader(file.FullName);

      string line;
      while ((line = streamReader.ReadLine()) != null) {
        buffer.Append(line.Trim() + "\n");
      }

      streamReader.Close();
    }

    // ------------------------------------------------------------------------
  
    private void triGraphs(StringBuilder buffer) {
      buffer.Replace("?=", "#");
      buffer.Replace("?/", "\\");
      //buffer.Replace("?'", "^");
      buffer.Replace("?(", "[");
      buffer.Replace("?)", "]");
      buffer.Replace("?!", "|");
      buffer.Replace("?<", "{");
      buffer.Replace("?>", "}");
      buffer.Replace("?-", "~");
    }

    // ------------------------------------------------------------------------
  
    public void traverse(StringBuilder buffer) {
      buffer.Append("\0\0\0");

      for (int index = 0; buffer[index] != '\0'; ++index) {
        if (buffer.ToString().Substring(index, 2).Equals("//")) {
          index = traverseLineComment(index, buffer);
        }
        else if (buffer.ToString().Substring(index, 2).Equals("/*")) {
          index = traverseBlockComment(index, buffer);
        }      
        else if (buffer[index] == '\"') {
          index = traverseString(index + 1, buffer, true);
        }
        else if (buffer[index] == '\'') {
          index = traversechar(index + 1, buffer, true);
        }
        else {
          Assert.Error(buffer[index] != '$',
                       Start.SeparatorId, "invalid char");
        }
      }

      buffer.Replace("\0", "");
    }

    private int traverseLineComment(int index, StringBuilder buffer) {
      while ((buffer[index] != '\n') && (buffer[index] != '\0')) {
        buffer[index++] = ' ';
      }
    
      return index;
    }
  
    private int traverseBlockComment(int index, StringBuilder buffer) {
      while (true) {
        if (buffer[index] == '\0') {
          Assert.Error("Unfinished commant block.");
        }
        else if (buffer.ToString().ToString().Substring(index, 2).Equals("*/")) {
          buffer[index] =  ' ';
          buffer[index + 1] =  ' ';
          return (index + 1);
        }
        else if (buffer[index] == '\n') {
          buffer[index++] = '\n';
        }
        else {
          buffer[index++] = ' ';
        }
      }
    }

    public int traverseString(int index, StringBuilder buffer, bool octal) {    
      while (true) {
        switch (buffer[index]) {
          case '\"':
            return index;
          
          case '\n':
            Assert.Error("new line in string.");
            break;
          
          case '\0':
            Assert.Error("unfinished string.");
            break;
          
          case '\\':
            slashToChar(index, buffer);
            break;
        }
      
        if (octal) {
          charToOctal(index, buffer);
          index += 4;
        }
        else {
          ++index;
        }
      }
    }
  
    public int traversechar(int index, StringBuilder buffer,
                                        bool octal) {
      int charCount = 0;
    
      while (true) {
        switch (buffer[index]) {
          case '\'':
            Assert.Error(charCount == 1, "invalid char sequence");
            return index;
          
          case '\n':
            Assert.Error("new line in char.");
            break;
          
          case '\0':
            Assert.Error("unfinished char.");
            break;
          
          case '\\':
            slashToChar(index, buffer);
            break;
        }
      
        if (octal) {
          charToOctal(index, buffer);
          index += 4;
        }
        else {
          ++index;
        }

        ++charCount;
      }
    }

    // ------------------------------------------------------------------------
  
    public static void slashToChar(int index, StringBuilder buffer) {
      char char1 = buffer[index + 1],
           char2 = buffer[index + 2],
           char3 = buffer[index + 3];

      if (IsOctal(char1) && IsOctal(char2) && IsOctal(char3)) {
        int octValue = 64 * charToOctal(char1) +
                        8 * charToOctal(char2) +
                            charToOctal(char3);
        Assert.Error(octValue < 256, "invalid octal sequence");
        buffer.Remove(index, 4);
        buffer.Insert(index, ((char) octValue).ToString());
      }
      else if (IsOctal(char1) && IsOctal(char2)) {
        int octValue = 8 * charToOctal(char1) + charToOctal(char2);
        buffer.Remove(index, 3);
        buffer.Insert(index, ((char) octValue).ToString());
      }
      else if (IsOctal(char1)) {
        int octValue = charToOctal(char1);
        buffer.Remove(index, 2);
        buffer.Insert(index, ((char) octValue).ToString());
      }
      else if ((char1 == 'x') || (char1 == 'X')) {
        if (IsHex(char1) && IsHex(char2)) {
          int hexValue = 16 * charToHex(char1) + charToHex(char2);
          buffer.Remove(index, 3);
          buffer.Insert(index, ((char) hexValue).ToString());
        }
        else if (IsHex(char1)) {
          int hexValue = charToHex(char1);
          buffer.Remove(index, 2);
          buffer.Insert(index, ((char) hexValue).ToString());
        }
        else {
          Assert.Error("invalid hexadecimal sequence");
        }
      }
      else if (m_slashMap.ContainsKey(char1)) {
        buffer.Remove(index, 2);
        buffer.Insert(index, (m_slashMap[char1]).ToString());
      }
      else {
        Assert.Error(buffer[index + 1], "invalid slash sequence");
      }
    }

    private static IDictionary<char,char> m_slashMap = new Dictionary<char,char>();
    private static ISet<string> m_specialMacroSet = new HashSet<string>();

    static Preprocessor() {
      m_slashMap.Add('0','\0');
      m_slashMap.Add('n','\n');
      m_slashMap.Add('t','\t');
      m_slashMap.Add('v', (char) 11);
      m_slashMap.Add('b','\b');
      m_slashMap.Add('r','\r');
      m_slashMap.Add('f','\f');
      m_slashMap.Add('a',(char) 7);
      m_slashMap.Add('\\','\\');
      m_slashMap.Add('?','?');
      m_slashMap.Add('\'','\'');
      m_slashMap.Add('"','\"');

      m_specialMacroSet.Add("__STDC__");
      m_specialMacroSet.Add("__FILE__");
      m_specialMacroSet.Add("__LINE__");
      m_specialMacroSet.Add("__DATE__");
      m_specialMacroSet.Add("__TIME__");
    }

    private static bool IsOctal(char c) {
      return "01234567".Contains(c.ToString());
    }
  
    private static bool IsHex(char c) {
      return "0123456789abcdefABCDEF".Contains(c.ToString());
    }
  
    private static int charToOctal(char c) {
      return "01234567".IndexOf(c);
    }
  
    private static int charToHex(char c) {
      return "0123456789abcdef".IndexOf(c.ToString().ToLower());
    }

    public static string octalText(string text) {
      StringBuilder buffer = new StringBuilder(text);
    
      for (int index = (buffer.Length - 1); index >= 0; --index) {
        int ord = (int) buffer[index];
        if ((ord < 32) || (ord > 122)) {
          charToOctal(index, buffer);
        }
      }
    
      return buffer.ToString();
    }

    public static string textToOctal(string text) {
      StringBuilder buffer = new StringBuilder(text);
    
      for (int index = (text.Length - 1); index >= 0; --index) {
        charToOctal(index, buffer);
      }
    
      return buffer.ToString();
    }
  
    public static void charToOctal(int index, StringBuilder buffer) {
      int asciiValue = buffer[index];
      char firstChar = "01234567"[asciiValue / 64],
           secondChar = "01234567"[(asciiValue % 64) / 8],
           thirdChar = "01234567"[asciiValue % 8];
      string text = "\\" + firstChar.ToString() +
                           secondChar.ToString() +
                           thirdChar.ToString();
      buffer.Remove(index, 1);
      buffer.Insert(index, text);
    }
  
    public static string octalToChar(string text) {
      StringBuilder buffer = new StringBuilder(text);

      for (int index = (text.Length - 1); index >= 0; --index) {
        if (buffer[index] == '\\') {
          Preprocessor.slashToChar(index, buffer);
        }
      }

      return buffer.ToString();
    }  

    private List<string> GenerateLineList(string text) {
      List<string> resultList = new MyList<string>();
      string[] lineArray = text.Split('\n');
    
      for (int index = 0; index < lineArray.Length; ++index) {
        if (lineArray[index].Trim().StartsWith("#")) {
          StringBuilder buffer = new StringBuilder();

          while (true) {
            if (index == lineArray.Length) {
              resultList.Add(buffer.ToString());
              break;
            }

            string trim = lineArray[index].Trim();
          
            if (trim.EndsWith("\\")) {
              buffer.Append(trim.ToString().Substring(0, trim.Length - 1) + "\n");
            }
            else {
              resultList.Add(buffer.ToString() + trim);
              break;
            }
          
            ++index;
          }
        }
        else {
          StringBuilder buffer = new StringBuilder();
          bool first = true;
        
          while (true) {
            if (index == lineArray.Length) {
              resultList.Add(buffer.ToString());
              break;
            }
          
            string trim = lineArray[index].Trim();
          
            if (trim.StartsWith("#") && !trim.StartsWith("##")) {
              resultList.Add(buffer.ToString());
              --index;
              break;
            }
            else {
              buffer.Append((first ? "" : "\n") + trim);
              first = false;
            }
          
            ++index;
          }
        }          
      }

      return resultList;
    }

    private List<Token> toTokenList(string text) {
      byte[] byteArray = Encoding.ASCII.GetBytes(text + "\0");
      MemoryStream memoryStream = new MemoryStream(byteArray);
      CCompiler_Pre.Scanner scanner = new CCompiler_Pre.Scanner(memoryStream);
      List<Token> tokenList = new MyList<Token>();

      try {
        while (true) {
          CCompiler_Pre.Tokens tokenId = (CCompiler_Pre.Tokens) scanner.yylex();
          tokenList.Add(new Token(tokenId, scanner.yylval.name));

          if (tokenId == CCompiler_Pre.Tokens.EOL) {
            break;
          }
        }
      }
      catch(Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
      }

      memoryStream.Close();
      return tokenList;
    }

    private void concatTokens(List<Token> tokenList) {
      for (int index = (tokenList.Count - 2); index >= 0; --index) {
        Token token = tokenList[index];
      
        if (token.GetId() == CCompiler_Pre.Tokens.DOUBLE_SHARP) {
          Token nextToken = tokenList[index + 1];
                
          if (index > 0) {
            Token prevToken = tokenList[index - 1];
          
            if ((prevToken.GetId() == CCompiler_Pre.Tokens.STRING) ||
                (nextToken.GetId() == CCompiler_Pre.Tokens.STRING)) {
              nextToken.AddNewlineCount(token.GetNewlineCount());
              tokenList.RemoveAt(index);
            }
            else {
              string prevText = prevToken.ToString(),
                     nextText = nextToken.ToString();
            
              prevToken.Value = prevText + nextText;
              tokenList[index + 2].AddNewlineCount(token.GetNewlineCount() +
                                                       nextToken.GetNewlineCount());
              tokenList.RemoveAt(index);
              tokenList.RemoveAt(index);
            }
          }
          else {
            nextToken.AddNewlineCount(token.GetNewlineCount());
            tokenList.RemoveAt(index);
          }
        }
      }
    }
  
    private void MergeStrings(List<Token> tokenList) {
      for (int index = (tokenList.Count - 2); index >= 0; --index) {
        Token thisToken = tokenList[index],
              nextToken = tokenList[index + 1];
            
        if ((thisToken.GetId() == CCompiler_Pre.Tokens.STRING) &&
            (nextToken.GetId() == CCompiler_Pre.Tokens.STRING)) {
        
          string thIsText = thisToken.ToString(),
                 nextText = nextToken.ToString();
          string MergeText = thIsText.ToString().Substring(1, thIsText.Length - 2) +
                             nextText.ToString().Substring(1, nextText.Length - 2);
               
          thisToken.Value = "\"" + MergeText + "\"";
          tokenList[index + 2].AddNewlineCount(nextToken.GetNewlineCount());
          tokenList.RemoveAt(index + 1);
        }
      }
    }
  
    private string ToString(List<Token> tokenList) {
      StringBuilder buffer = new StringBuilder();
      bool first = true;
    
      foreach (Token token in tokenList) {
        buffer.Append((first ? "" : " ") + token.ToString());
        first = false;
      }
    
      return buffer.ToString();
    }

    private string toTextString(List<Token> tokenList) {
      StringBuilder buffer = new StringBuilder();
      bool first = true;
    
      foreach (Token token in tokenList) {
        buffer.Append((first ? "" : "\\040") + textToOctal(token.ToString()));
        first = false;
      }
    
      return "\"" + buffer.ToString() + "\"";
    }
  
    private List<Token> cloneList(List<Token> tokenList) {
      List<Token> resultList = new MyList<Token>();
    
      foreach (Token token in tokenList) {
        resultList.Add((Token) token.Clone());
      }
    
      return resultList;
    }
  
    public void traverseLineList(List<string> lineList) {
      for (int index = 0; index < lineList.Count; ++index) {
        string line = lineList[index];
        List<Token> tokenList = toTokenList(line);

        if (tokenList[0].GetId() == CCompiler_Pre.Tokens.SHARP) {
          Token secondToken = tokenList[1];

          if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
              secondToken.ToString().ToLower().Equals("line")) {
            if (IsVisible()) {
              doLine(tokenList);
            }
            else {
              AddNewlinesToBuffer(tokenList);
            }
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("include")) {
            if (IsVisible()) {
              doInclude(tokenList);
            }
            else {
              AddNewlinesToBuffer(tokenList);
            }
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("Error")) {
            if (IsVisible()) {
              Assert.Error(ToString(tokenList.GetRange(1, tokenList.Count - 1)));
            }
            else {
              AddNewlinesToBuffer(tokenList);
            }
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("define")) {
            if (IsVisible()) {
              doDefine(tokenList);
            }

            AddNewlinesToBuffer(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("undef")) {
            if (IsVisible()) {
              doUndef(tokenList);
            }
            else {
              AddNewlinesToBuffer(tokenList);
            }
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("ifdef")) {
            doIfDefined(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("ifndef")) {
            doIfNotDefined(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("if")) {
            doIf(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("elif")) {
            doElseIf(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("else")) {
            doElse(tokenList);
          }
          else if ((secondToken.GetId() == CCompiler_Pre.Tokens.NAME) &&
                   secondToken.ToString().ToLower().Equals("endif")) {
            doEndIf(tokenList);
          }
          else {
          string x = ToString(tokenList);
            Assert.Error(secondToken.GetId() == CCompiler_Pre.Tokens.EOL,
                         ToString(tokenList), "invalid preprocessor directive");
          }
        }
        else {
          if (IsVisible()) {
            SearchForMacros(tokenList, new Stack<string>());
            concatTokens(tokenList);
            MergeStrings(tokenList);
            AddToBuffer(tokenList);
          
          }
          else {
            AddNewlinesToBuffer(tokenList);
          }
        }
      }
    }

    private bool IsVisible() {
      foreach (Triple<bool,bool,IfStatus> ifTriple in Start.IfStack) {
        bool currTrue = ifTriple.First;
      
        if (!currTrue) {
          return false;
        }
      }
      
      return true;
    }

    private void doLine(List<Token> tokenList) {
      int listSize = tokenList.Count;
    
      if ((listSize == 4) || (listSize == 5)) {
        string lineText = tokenList[2].ToString();
        Assert.Error(int.TryParse(lineText, out Start.Line), lineText, "invalid line number");

        if (listSize == 5) {
          Start.Path = tokenList[3].ToString();
        }
      }
      else {
        Assert.Error(listSize == 3, ToString(tokenList),
                     "invalid preprocessor directive");
      }

      m_buffer.Append(Start.SeparatorId + Start.Path + "," +
                      Start.Line + Start.SeparatorId + "\n");
    }

    // ------------------------------------------------------------------------
  
    private void doInclude(List<Token> tokenList) {
      FileInfo includeFile = null;
    
      if ((tokenList[2].GetId() == CCompiler_Pre.Tokens.STRING) &&
          (tokenList[3].GetId() == CCompiler_Pre.Tokens.EOL)) {
        string text = tokenList[2].ToString();
        string file = text.ToString().Substring(1, text.Length - 1);
        includeFile = new FileInfo(Start.IncludePath + file);
      }
      else {
        StringBuilder buffer = new StringBuilder();

        foreach (Token token in tokenList.GetRange(2, tokenList.Count - 2)) {
          buffer.Append(token.ToString());
        }
      
        string text = buffer.ToString();

        if (text.StartsWith("<") && text.EndsWith(">")) {
          string file = text.ToString().Substring(1, text.Length - 2);
          includeFile = new FileInfo(Start.IncludePath + file);
        }
        else {
          Assert.Error(ToString(tokenList), "invalide preprocessor directive");
        }
      }

      Assert.Error(!Start.IncludeStack.Contains(includeFile),
                   "nestled include structure: " + includeFile.FullName);
      Start.IncludeStack.Push(includeFile);
      Start.IncludeSet.Add(includeFile);
      string oldPath = Start.Path;
      int oldLine = Start.Line;
      Start.Path = includeFile.FullName;
      Start.Line = 1;
      m_buffer.Append(Start.SeparatorId + Start.Path + "," +
                      Start.Line + Start.SeparatorId + "\n");
      doProcess(includeFile);
      Start.Line = oldLine + 1;
      Start.Path = oldPath;
      m_buffer.Append(Start.SeparatorId + Start.Path + "," +
                      (Start.Line - 1) + Start.SeparatorId + "\n");
      Start.IncludeStack.Pop();
    }

    // ------------------------------------------------------------------------
  
    public void doDefine(List<Token> tokenList) {
      Assert.Error(tokenList[2].GetId() == CCompiler_Pre.Tokens.NAME,
                   ToString(tokenList), "invalid define directive");
      string name = tokenList[2].ToString();
      Macro macro;

      if ((tokenList[3].GetId() == CCompiler_Pre.Tokens.LEFT_PAREN) &&
          !tokenList[3].HasWhitespace()) {
        int tokenIndex = 4, paramIndex = 0;      
        IDictionary<string,int> paramMap = new Dictionary<string,int>();

        while (true) {
          Token nextToken = tokenList[tokenIndex++];
          Assert.Error(nextToken.GetId() == CCompiler_Pre.Tokens.NAME,
                       nextToken.ToString(), "invalid macro definition");
        
          string param = nextToken.Value.ToString();
          Assert.Error(!paramMap.ContainsKey(param), param,
                       "repeated macro parameter");
          paramMap.Add(param, paramIndex++);

          nextToken = tokenList[tokenIndex++];
        
          if (nextToken.GetId() == CCompiler_Pre.Tokens.COMMA) {
            // Empty.
          }
          else if (nextToken.GetId() == CCompiler_Pre.Tokens.RIGHT_PAREN) {
            break;
          }
          else {
            Assert.Error(nextToken.ToString(), "invalid macro definition");
          }
        }
      
        List<Token> macroList = tokenList.GetRange(tokenIndex, tokenList.Count - tokenIndex);
      
        for (int macroIndex = 0; macroIndex < macroList.Count; ++macroIndex) {
          Token macroToken = macroList[macroIndex];
          if (macroToken.GetId() == CCompiler_Pre.Tokens.NAME) {
            string macroName = macroToken.ToString();
          
            if (paramMap.ContainsKey(macroName)) {
              macroList[macroIndex] = new Token(CCompiler_Pre.Tokens.MARK, paramMap[macroName]);
            }
          }
        }
      
        macro = new Macro(paramMap.Count, macroList);
      }
      else {
        macro = new Macro(0, tokenList.GetRange(3, tokenList.Count - 3));
      }
    
      if (Start.MacroMap.ContainsKey(name)) {
        Macro oldMacro = Start.MacroMap[name];
        Assert.Error(macro.Equals(oldMacro), name,
                     "invalid macro redefinition");
      }

      Start.MacroMap[name] = macro;
      //Start.MacroMap.Add(name, macro);
    }

    public void doUndef(List<Token> tokenList) {
      Assert.Error((tokenList[2].GetId() == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].GetId() == CCompiler_Pre.Tokens.EOL),
                   ToString(tokenList), "invalid undef directive");
      string name = tokenList[2].ToString();
      Assert.Error(Start.MacroMap.ContainsKey(name), name, "macro not defined");
      Start.MacroMap.Remove(name);
    }

    // ------------------------------------------------------------------------

    private void doIf(List<Token> tokenList) {
      bool result = parseExpression(ToString(tokenList.GetRange(2, tokenList.Count - 2)));
      Start.IfStack.Push(new Triple<bool,bool,IfStatus>(result, result, IfStatus.If));
    }

    public static object PreProcessorResult;
  
    private bool parseExpression(string line) {    
      int result = 0;

      try {
        byte[] byteArray = Encoding.ASCII.GetBytes(line.Trim());
        MemoryStream memoryStream = new MemoryStream(byteArray);
        CCompiler_Exp.Scanner expressionScanner = new CCompiler_Exp.Scanner(memoryStream);
        CCompiler_Exp.Parser expressionParser = new CCompiler_Exp.Parser(expressionScanner);
        //CCompiler_Exp.Parser expressionParser = new ExpressionParser(expressionScanner.toLexer());
        Assert.Error(expressionParser.Parse(), "expression parser");
        result = (int) PreProcessorResult;
        memoryStream.Close();
      }
      catch (Exception exception) {
        Console.Out.WriteLine(exception.StackTrace);
        Assert.Error(line, "invalid expression");
      }

      return (result != 0);
    }
  
    private void doIfDefined(List<Token> tokenList) {
      Assert.Error((tokenList[2].GetId() == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].GetId() == CCompiler_Pre.Tokens.EOL),
                   ToString(tokenList), "invalid preprocessor directive");
      string name = tokenList[2].ToString();
      bool result = Start.MacroMap.ContainsKey(name);
      Start.IfStack.Push(new Triple<bool,bool,IfStatus>(result, result, IfStatus.If));
      AddNewlinesToBuffer(tokenList);    
    }

    private void doIfNotDefined(List<Token> tokenList) {
      Assert.Error((tokenList[2].GetId() == CCompiler_Pre.Tokens.NAME) &&
                   (tokenList[3].GetId() == CCompiler_Pre.Tokens.EOL),
                   ToString(tokenList), "invalid preprocessor directive");
      bool result = !Start.MacroMap.ContainsKey((string) tokenList[2].Value);
      Start.IfStack.Push(new Triple<bool,bool,IfStatus>(result, result, IfStatus.If));
      AddNewlinesToBuffer(tokenList);    
    }

    private void doElseIf(List<Token> tokenList) {
      bool result = parseExpression(ToString(tokenList.GetRange(2, tokenList.Count - 2)));
      Assert.Error(Start.IfStack.Count > 0,
                   "#elif without preceeding #if, #ifdef, or #ifndef");
    
      Triple<bool,bool,IfStatus> ifTriple = Start.IfStack.Pop();
      bool //currTrue = ifTriple.First,
              lastTrue = ifTriple.Second;    

      IfStatus lastIf = ifTriple.Third;
      Assert.Error(lastIf == IfStatus.If,
                   "#elif after #else if not allowed");

      if (!lastTrue) {
        Start.IfStack.Push(new Triple<bool,bool,IfStatus>(result, result, IfStatus.If));
      }
      else {
        Start.IfStack.Push(new Triple<bool,bool,IfStatus>(false, true, IfStatus.If));
      }    
      AddNewlinesToBuffer(tokenList);    
    }

    private void doElse(List<Token> tokenList) {
      Assert.Error(tokenList[2].GetId() == CCompiler_Pre.Tokens.EOL,
                   ToString(tokenList), "invalid preprocessor directive");
      Assert.Error(Start.IfStack.Count > 0,
               "#else without preceeding #if, #ifdef, or #ifndef");

      Triple<bool,bool,IfStatus> ifTriple = Start.IfStack.Pop();
      bool lastTrue = ifTriple.Second;

      IfStatus lastIf = ifTriple.Third;
      Assert.Error(lastIf == IfStatus.If,
               "#else after #else if not allowed");

      Start.IfStack.Push(new Triple<bool,bool,IfStatus>(!lastTrue, true /* doesn't matter */,
                                     IfStatus.Else));
      AddNewlinesToBuffer(tokenList);    
    }

    private void doEndIf(List<Token> tokenList) {
      Assert.Error(tokenList[2].GetId() == CCompiler_Pre.Tokens.EOL,
                   tokenList[2].ToString(), "invalid preprocessor directive");
      Assert.Error(Start.IfStack.Count > 0,
               "#endif without preceeding #if, #ifdef, or #ifndef");
      Start.IfStack.Pop();
      AddNewlinesToBuffer(tokenList);    
    }

    // ------------------------------------------------------------------------
  
/*    private static ISet<string> m_specialMacroSet = new HashSet<string>();
  
    static Preprocessor() {
      m_specialMacroSet.Add("__STDC__");
      m_specialMacroSet.Add("__FILE__");
      m_specialMacroSet.Add("__LINE__");
      m_specialMacroSet.Add("__DATE__");
      m_specialMacroSet.Add("__TIME__");
    }*/
      
    private void SearchForMacros(List<Token> tokenList,
                                 Stack<string> nameStack) {
      for (int index = 0; index < tokenList.Count; ++index) {
        Token thisToken = tokenList[index];
        Start.Line += thisToken.GetNewlineCount();
        //Console.Out.WriteLine("5: " + Start.Line);

        if (thisToken.GetId() == CCompiler_Pre.Tokens.NAME) {
          string name = thisToken.ToString();
          int beginNewlineCount = thisToken.GetNewlineCount();
          //Start.Line += beginNewlineCount;

          if (!nameStack.Contains(name) && Start.MacroMap.ContainsKey(name)) {          
            Token nextToken = tokenList[index + 1];

            if ((nextToken.GetId() == CCompiler_Pre.Tokens.LEFT_PAREN) &&
                !nextToken.HasWhitespace()) {
              int countIndex = index + 2, level = 1, totalNewlineCount = 0;
              List<Token> subList = new MyList<Token>();
              List<List<Token>> mainList = new MyList<List<Token>>();
        
              while (true) {
                nextToken = tokenList[countIndex];
                int newlineCount = nextToken.GetNewlineCount();
                totalNewlineCount += newlineCount;
                Start.Line += newlineCount;
                //Console.Out.WriteLine("6: " + Start.Line);
                nextToken.ClearNewlineCount();
              
                Token token = tokenList[countIndex];
                Assert.Error(token.GetId() != CCompiler_Pre.Tokens.EOL,
                             "invalid end of macro call");
              
                switch (token.GetId()) {
                  case CCompiler_Pre.Tokens.LEFT_PAREN:
                    ++level;
                    subList.Add(token);
                    break;
                  
                  case CCompiler_Pre.Tokens.RIGHT_PAREN:
                    if ((--level) > 0) {
                      subList.Add(token);
                    }
                    break;
                  
                  default:
                    if ((level == 1) && (token.GetId() == CCompiler_Pre.Tokens.COMMA)) {
                      Assert.Error(subList.Count > 0, name, "empty macro parameter");
                      SearchForMacros(subList, nameStack); // XXX
                      mainList.Add(subList);
                      subList = new MyList<Token>();
                    }
                    else {
                      subList.Add(token);
                    }
                    break;
                }
              
                if (level == 0) {
                  Assert.Error(subList.Count > 0, name, "empty macro parameter");
                  mainList.Add(subList);
                  break;
                }
              
                ++countIndex;
              }

              Macro macro = Start.MacroMap[name];
              Assert.Error(macro.Parameters() == mainList.Count, name,
                           "invalid number of parameters in macro call");
            
              List<Token> cloneListX = cloneList(macro.TokenList());
            
              for (int macroIndex = (cloneListX.Count - 1); macroIndex >= 0; --macroIndex) {
                Token macroToken = cloneListX[macroIndex];

                if (macroToken.GetId() == CCompiler_Pre.Tokens.MARK) {
                  int markIndex = (int) macroToken.Value;
                  cloneListX.RemoveAt(macroIndex);
                  List<Token> replaceList = cloneList(mainList[markIndex]);
                
                  bool sharp = (macroIndex > 0) &&
                                  (cloneListX[macroIndex - 1].GetId() == CCompiler_Pre.Tokens.SHARP);

                  if (sharp) {
                    string text = toTextString(replaceList);
                    cloneListX.Insert(macroIndex, new Token(CCompiler_Pre.Tokens.STRING, text));
                  }
                  else {
                    cloneListX.InsertRange(macroIndex, replaceList);
                  }
                }
              
                if (macroToken.GetId() == CCompiler_Pre.Tokens.SHARP) {
                  cloneListX.RemoveAt(macroIndex);
                }
              }

              nameStack.Push(name);
              SearchForMacros(cloneListX, nameStack);
              nameStack.Pop();

              for (int RemoveIndex = index; RemoveIndex <= countIndex; ++RemoveIndex) {
                tokenList.RemoveAt(index);
              }
            
              tokenList.InsertRange(index, cloneListX);
              tokenList[index].AddNewlineCount(beginNewlineCount);
              tokenList[index + cloneListX.Count].AddNewlineCount(totalNewlineCount);
              index += cloneListX.Count - 1;
            }
            else {
              Macro macro = Start.MacroMap[name];
              Assert.Error(macro.Parameters() == 0, name,
                           "invalid number of parameters in macro call");
              List<Token> cloneListX = cloneList(macro.TokenList());
              nameStack.Push(name);
              SearchForMacros(cloneListX, nameStack);
              nameStack.Pop();

              tokenList.RemoveAt(index);
              tokenList.InsertRange(index, cloneListX);
              tokenList[index].AddNewlineCount(beginNewlineCount);
              index += cloneListX.Count - 1;
            }
          }
          else if (name.Equals("__STDC__")) {
            tokenList[index] = new Token(CCompiler_Pre.Tokens.TOKEN, 1, beginNewlineCount);
          }
          else if (name.Equals("__FILE__")) {
            string text = "\"" + textToOctal(Start.Path) + "\"";
            tokenList[index] = new Token(CCompiler_Pre.Tokens.TOKEN, text, beginNewlineCount);
          }
          else if (name.Equals("__LINE__")) {
            tokenList[index] = new Token(CCompiler_Pre.Tokens.TOKEN, Start.Line, beginNewlineCount);
          }
          else if (name.Equals("__DATE__")) {
            DateTime dateTime = new DateTime();
            string[] monthArray = {"Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                   "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
            string date = string.Format("\"%s %02d %04d\"", monthArray[dateTime.Month],
                                        dateTime.Day, dateTime.Year);
            tokenList[index] = new Token(CCompiler_Pre.Tokens.TOKEN, date, beginNewlineCount);
          }
          else if (name.Equals("__TIME__")) {
            DateTime dateTime = new DateTime();
            string time = string.Format("\"%02d:%02d:%02d\"", dateTime.Hour,
                                        dateTime.Minute, dateTime.Second);
            tokenList[index] = new Token(CCompiler_Pre.Tokens.TOKEN, time, beginNewlineCount);
          }
        }
      }
    }  
  }
}