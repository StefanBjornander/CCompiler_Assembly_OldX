using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class GenerateDeclaration {
    public static void GenerateFunctionHeader(Specifier specifier, Declarator decl) {
      Storage storage;
      CCompiler.Type specType;
    
      if (specifier != null) {
        specifier.Generate();
        storage = specifier.Storage;
        specType = specifier.Type;
      }
      else {
        storage = Storage.Extern;
        specType = CCompiler.Type.SignedIntegerType;
      }

      decl.Add(specType);
      string funcName = decl.Name;
      CCompiler.Type funcType = decl.Type;
      Assert.Error(funcName != null, "unnamed function definition");
      Assert.Error(funcType.IsFunction(), funcName, "not a function");

      CCompiler.Type returnType = funcType.ReturnType;
      Start.CurrentFunction = new Symbol(funcName, storage, funcType);
      Assert.Error(Start.CurrentFunction.IsStaticOrExtern(),
                   funcName, "a function must be static or extern");
      Start.CurrentTable.AddSymbol(Start.CurrentFunction);

      if (IsMain(Start.CurrentFunction)) {
        Assert.Error(returnType.IsVoid() || returnType.IsInteger(),
                     "main", "main function must return void or integer");
      }

      Start.CurrentTable = new SymbolTable(Start.CurrentTable, Scope.Function);
    }

    public static void CheckFunctionDefinition() {
      CCompiler.Type funcType = Start.CurrentFunction.Type;
    
      if (funcType.IsOldStyleFunction()) {
        List<string> nameList = funcType.GetNameList();
        IDictionary<string,Symbol> entryMap = Start.CurrentTable.EntryMap;
      
        Assert.Error(nameList.Count == entryMap.Count, 
                     Start.CurrentFunction.Name, "unmatched number of parameters"
                     + " in old-style function definition");

        foreach (string name in nameList) {
          Assert.Error(entryMap.ContainsKey(name), name, "undefined parameter");
        }
      }
      else {
        Assert.Error(Start.CurrentTable.EntryMap.Count == 0,
                     "new and old style mixed function definition");
      
        foreach (Symbol paramSymbol in funcType.GetParameterList()) {
          Assert.Error(paramSymbol.IsParameter());
          Start.CurrentTable.AddSymbol(paramSymbol); // XXX
        }
      }
    }

    public static bool IsMain(Symbol symbol) {
      Storage storage = symbol.Storage;
      return (symbol.Name.Equals("main") &&
              ((storage == Storage.Static) || (storage == Storage.Extern)));
    }

    public static void CheckMain(Symbol symbol) {
      List<CCompiler.Type> typeList = symbol.Type.GetTypeList();
      Assert.Error((typeList == null) || (typeList.Count == 0) ||
                    IsMainArgs(symbol), "main", "invalid parameter list");
    }

    public static bool IsMainArgs(Symbol symbol) {
      List<CCompiler.Type> typeList = symbol.Type.GetTypeList();
      return (typeList != null) && (typeList.Count == 2) &&
             typeList[0].IsInteger() &&
             typeList[1].IsPointer() &&
             typeList[1].PointerType.IsPointer() &&
             typeList[1].PointerType.PointerType.IsChar();
    }

    public static void CheckGotoSet() {
      foreach (string label in Start.GotoSet) {
        Assert.Error(Start.LabelSet.Contains(label), label, "undefined label");
      }

      foreach (string label in Start.LabelSet) {
        Assert.Warning(Start.GotoSet.Contains(label), label, "unreferenced label");
      }
    }
  
    public static void GenerateFunctionEnd(Statement statement) {
      //statement.List().Insert(0, new MiddleCode(MiddleOperator.FunctionStart, Start.CurrentFunction));
      MiddleCode nextCode = Start.AddMiddleCode(statement.List(), MiddleOperator.Empty);
      Start.Backpatch(statement.NextSet(), nextCode);
    
      /*if (Start.CurrentFunction.UniqueName.Equals("main") && 
          Start.CurrentFunction.Type.ReturnType.IsVoid()) {
        CCompiler.Type signedShortType = new CCompiler.Type(Sort.Signed_Short_Int);
        Symbol zeroSymbol = new Symbol(signedShortType, BigInteger.Zero);
        Start.AddMiddleCode(statement.List(), MiddleOperator.Exit, null, zeroSymbol);
      }
      else*/ {
        CCompiler.Type signedShortType = new CCompiler.Type(Sort.Signed_Short_Int);
        Symbol zeroSymbol = new Symbol(signedShortType, BigInteger.Zero);
        Start.AddMiddleCode(statement.List(), MiddleOperator.Return, null, zeroSymbol);
      }

      Start.AddMiddleCode(statement.List(), MiddleOperator.FunctionEnd, Start.CurrentFunction);

      if (Start.CurrentFunction.Name.Equals("strftime")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" + Start.CurrentFunction.Name + "2.middlebefore";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.List().Count; ++index) {
          MiddleCode middleCode = statement.List()[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      MiddleCodeOptimizer middleCodeOptimizer = new MiddleCodeOptimizer(statement.List());
      middleCodeOptimizer.Optimize();

      if (Start.CurrentFunction.Name.Equals("strftime")) {
        string name = @"C:\Users\Stefan\Documents\vagrant\homestead\code\code\" + Start.CurrentFunction.Name + "2.middleafter";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < statement.List().Count; ++index) {
          MiddleCode middleCode = statement.List()[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      /*if (Start.CurrentFunction.Name.Equals("exp")) {
        string name = "C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\" + Start.CurrentFunction.Name + ".middle";
        StreamWriter streamWriter = new StreamWriter(name);
        for (int index = 0; index < statement.List().Count; ++index) {
          MiddleCode middleCode = statement.List()[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }
        streamWriter.WriteLine();

        MiddleCodeOptimizer middleCodeOptimizer = new MiddleCodeOptimizer(statement.List());
        middleCodeOptimizer.Optimize();

        for (int index = 0; index < statement.List().Count; ++index) {
          MiddleCode middleCode = statement.List()[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }
        streamWriter.Close(); 
      }
      else {
        MiddleCodeOptimizer middleCodeOptimizer = new MiddleCodeOptimizer(statement.List());
        middleCodeOptimizer.Optimize();
      }*/

      ObjectCodeGenerator objectCodeGenerator = new ObjectCodeGenerator();
    
      if (IsMain(Start.CurrentFunction)) {
        CheckMain(Start.CurrentFunction);
        objectCodeGenerator.AddInitializationCode();

        if (IsMainArgs(Start.CurrentFunction)) {
          objectCodeGenerator.AddArgumentCode();
          Start.CurrentFunction.EntryPoint = AssemblyCode.MainInitializationSize + 
                                             AssemblyCode.MainArgumentSize;
        }      
        else {
          Start.CurrentFunction.EntryPoint = AssemblyCode.MainInitializationSize;
        }
 
        if (Start.CheckStackHeap) {
          objectCodeGenerator.GenerateStackHeapCheckFunction();
        }
      }

      objectCodeGenerator.Generate(statement.List());
      Start.CurrentTable = Start.CurrentTable.ParentTable;
      Start.CurrentFunction = null;
    }

    public static void BackpatchGoto() {
      foreach (KeyValuePair<string,ISet<MiddleCode>> entry in Start.GotoSetMap) {
        string labelName = entry.Key;
        ISet<MiddleCode> gotoSet = entry.Value;
        Assert.Error(Start.LabelMap.ContainsKey(labelName), labelName,
                     "missing goto address");
        Start.Backpatch(gotoSet, Start.LabelMap[labelName]);
      }
    }  

    // ------------------------------------------------------------------------
  
    public static void GenerateStructUnionHeader(string optName, Sort sort) {
      if ((optName != null) && (Start.CurrentTable.LookupTag(optName) == null)) {
        CCompiler.Type newType = new CCompiler.Type(sort, (IDictionary<string, Symbol>) null, (List<Symbol>) null, true);
        Start.CurrentTable.AddTag(optName, newType);
      }
    }
  
    public static CCompiler.Type GenerateStructUnionSpecifier(string optName, Sort sort) {
      IDictionary<string,Symbol> memberMap = null;
      List<Symbol> memberList = null;
 
      if (Start.CurrentTable.EntryMap.Count > 0) {
        memberMap = Start.CurrentTable.EntryMap;
        memberList = Start.CurrentTable.EntryList;
      }
    
      if (optName != null) {
        CCompiler.Type oldType = Start.CurrentTable.LookupTag(optName);
      
        if (oldType != null) {
          Assert.Error(oldType.GetSort() == sort,
                       optName, "tag alReady defined");
          Assert.Error((oldType.MemberMap == null) || (memberMap == null),
                        optName, "duplicate symbol");
        
          if (oldType.MemberMap == null) {
            oldType.MemberMap = memberMap;
          }
        
          return oldType;
        }
        else {
          CCompiler.Type newType = new CCompiler.Type(sort, memberMap, memberList, true);
          Start.CurrentTable.ParentTable.AddTag(optName, newType);
          return newType;
        }      
      }
      else {
        return new CCompiler.Type(sort, memberMap, memberList, true);
      }  
    }

    public static CCompiler.Type LookupStructUnionSpecifier(string name, Sort sort) {
      CCompiler.Type type = Start.CurrentTable.LookupTag(name);
    
      if (type != null) {
        type = new CCompiler.Type(sort, type.MemberMap, type.MemberList, true);
        Assert.Error(type.GetSort() == sort, name, "invalid type");
      }
      else {
        type = new CCompiler.Type(sort, (IDictionary<string, Symbol>) null, (List<Symbol>) null, true);
        Start.CurrentTable.AddTag(name, type);
      }

      return type;
    }
  
    // ------------------------------------------------------------------------
  
    public static Symbol GenerateEnumItem(string itemName, Symbol optInitSymbol) {
      CCompiler.Type itemType = new CCompiler.Type(Sort.Signed_Int);
      itemType.Constant = true;
      BigInteger bigValue;

      if (optInitSymbol != null) {
        Start.EnumValueStack.Pop();
        bigValue = (BigInteger) optInitSymbol.Value;
      }
      else {
        bigValue = Start.EnumValueStack.Pop();
      }
    
      
      Symbol itemSymbol = new Symbol(Symbol.Status.Variable, itemName, Storage.Static, itemType, bigValue);
      Start.CurrentTable.AddSymbol(itemSymbol);
      Start.EnumValueStack.Push(BigInteger.Add(bigValue, BigInteger.One));
      return itemSymbol;
    }
  
    public static Type GenerateEnumSpecifier(string optionalName, ISet<Pair<Symbol,bool>> symbolBoolPairSet) {
      bool enumInit = false;
      ISet<Symbol> symbolSet = new HashSet<Symbol>();

      foreach (Pair<Symbol,bool> pair in symbolBoolPairSet) {
        Symbol symbol = pair.First;
        symbolSet.Add(symbol);
        bool init = pair.Second;
        if (init) {
          enumInit = true;
        }
      }

      Type enumType = new Type(Sort.Enumeration, symbolSet, enumInit);

      if (optionalName != null) {
        Start.CurrentTable.AddTag(optionalName, enumType);
      }

      return enumType;
    }

    public static CCompiler.Type LookupEnum(string name) {
      CCompiler.Type type = Start.CurrentTable.LookupTag(name);
      Assert.Error(type != null, name, "unknown enum");
      Assert.Error(type.IsEnum(), name, "not an enum");
      return type;
      //return new CCompiler.Type(type.GetEnumerationList());
    }

    // ------------------------------------------------------------------------

    public static void GenerateDeclarator(Specifier specifier,
                                          Declarator decl) {
      specifier.Generate();
      Storage storage = specifier.Storage;
      CCompiler.Type specType = specifier.Type;

      decl.Add(specType);
      string name = decl.Name;
      CCompiler.Type type = decl.Type;

      if (type.IsFunction()) {
        Assert.Error((storage == Storage.Static) || (storage == Storage.Extern) || (storage == Storage.Typedef),
                     null, "only static or extern storage allowed for function");
        storage = (storage == Storage.Typedef) ? Storage.Typedef : Storage.Extern;
      }

      Assert.Error(((Start.CurrentTable.Scope != Scope.Struct) &&
                    (Start.CurrentTable.Scope != Scope.Union)) ||
                    ((storage == Storage.Auto)||(storage == Storage.Register)),
             null, "only auto or register storage allowed in struct or union");

      if (type.IsFunction()) {
        Symbol oldSymbol = Start.CurrentTable.LookupSymbol(name);

        if (oldSymbol != null) {
          CCompiler.Type oldType = oldSymbol.Type;
          Assert.Error(oldType.ReturnType.Equals(type.ReturnType),
                       name,"different return type in function redeclaration");

          Assert.Error(oldType.IsEllipse() == type.IsEllipse(), name,
                       "mixing ellipse function parameter in redeclaration");

          List<CCompiler.Type> oldTypeList = oldType.GetTypeList(),
                     newTypeList = type.GetTypeList();

          Assert.Error(((oldTypeList == null) && (oldTypeList == null)) ||
                       ((oldTypeList != null) && (oldTypeList != null) &&
                        oldTypeList.Equals(newTypeList)), name,
                       "different parameter lists in function redeclaration");
        }
      }

      Symbol symbol = new Symbol(name, storage, type);
      Start.CurrentTable.AddSymbol(symbol);
    }
  
    public static List<MiddleCode> GenerateAssignmentDeclarator(Specifier specifier,
                                                    Declarator decl, object init) {
      specifier.Generate();
      Storage storage = specifier.Storage;
      CCompiler.Type specType = specifier.Type;

      decl.Add(specType);
      string name = decl.Name;
      CCompiler.Type type = decl.Type;

      Assert.Error(!type.IsFunction(), null, "function cannot be initialized");
      Assert.Error(storage != Storage.Typedef, name,
                   "typedef cannot be initialized");
      Assert.Error(storage != Storage.Extern, name,
                   "extern cannot be initialized");
      Assert.Error((Start.CurrentTable.Scope != Scope.Struct) &&
                   (Start.CurrentTable.Scope != Scope.Union),
                   name, "struct or union field cannot be initialized");

      List<MiddleCode> codeList = new MyList<MiddleCode>();

      switch (storage) {
        case Storage.Auto:
        case Storage.Register: {
            Symbol symbol = new Symbol(name, storage, type);
            symbol.Offset = Start.CurrentTable.CurrentOffset;
            Assert.Error(Start.CurrentTable.Scope != Scope.Global, name,
                         "auto or register storage in global scope");

            GenerateInitializer.Extra = 0;
            GenerateInitializer.GenerateAuto(codeList, symbol, init);
            Start.CurrentTable.AddSymbol(symbol);
          }
          break;

        case Storage.Private:
        case Storage.Static: {
            List<sbyte> byteList = new MyList<sbyte>();
            IDictionary<int,string> accessMap = new Dictionary<int,string>();
            List<string> textList = new MyList<string>();
            Symbol symbol = new Symbol(Symbol.Status.Variable, name, storage, type, null,
                                       byteList, accessMap, null, null, null, textList);
            symbol.SetAssignable(!type.Constant);
            symbol.SetAddressable(true);
            textList.Add("\n\n" + symbol.UniqueName + ":");
            init = ModifyInitializer.DoInit(symbol.Type, init);
            GenerateInitializer.GenerateStatic(symbol.Type, init, byteList,
                                               accessMap, textList);
            Start.CurrentTable.AddSymbol(symbol);
          }
          break;
      }

      return codeList;
    }

    public static void GenerateBitfieldDeclarator(Specifier specifier,
                                       Declarator decl, Symbol bitsSymbol) {
      specifier.Generate();
      Storage storage = specifier.Storage;
      CCompiler.Type specType = specifier.Type;

      Assert.Error(Start.CurrentTable.Scope == Scope.Struct,
                   bitsSymbol, "bitfields only allowed on structs");

      Assert.Error((storage == Storage.Auto) || (storage == Storage.Register),
             null, "only auto or register storage allowed in struct or union");

      object bitsValue = bitsSymbol.Value;
      int bits = int.Parse(bitsValue.ToString());

      if (decl != null) {
        decl.Add(specType);
        CCompiler.Type type = decl.Type;
        Assert.Error(type.IsIntegral(), type, "non-integral bits expression");      
        Assert.Error((bits >= 1) && (bits <= (8 * type.Size())), bitsValue,
                      "bits value out of range");
      
        if (bits < (8 * type.Size())) {
          type.SetBitfieldMask(bits);
        }
      
        Symbol symbol = new Symbol(decl.Name, storage, type);
        Start.CurrentTable.AddSymbol(symbol);
      }
      else {
        Assert.Error((bits >= 1) && (bits <= (8 * CCompiler.Type.LongSize)), bitsValue,
                     "bits value out of range");
      }
    }
  
    // ------------------------------------------------------------------------
  
    public static Declarator GeneratePointerDeclarator(List<CCompiler.Type> typeList,
                                                       Declarator decl) {
      if (decl == null) {
        decl = new Declarator(null);
      }
    
      foreach (CCompiler.Type type in typeList) {
        CCompiler.Type pointerType = new CCompiler.Type((CCompiler.Type) null);
        pointerType.Constant = type.Constant;
        pointerType.Volatile = type.Volatile;
        decl.Add(pointerType);
      }

      return decl;
    }

    public static Declarator GeneratePointerListDeclarator
                  (List<Pair<bool,bool>> pointerList, Declarator decl) {
      foreach (Pair<bool,bool> pair in pointerList) {
        CCompiler.Type pointerType = new CCompiler.Type((CCompiler.Type) null);
        bool isConstant = pair.First, isVolatile = pair.Second;
        pointerType.Constant = isConstant;
        pointerType.Volatile = isVolatile;
        decl.Add(pointerType);
      }
    
      return decl;
    }
  
    // ------------------------------------------------------------------------

    public static Declarator GenerateArrayType(Declarator decl,
                                               Expression optSizeExpr) {
      if (decl == null) {
        decl = new Declarator(null);
      }
    
      if (optSizeExpr != null) {
        Symbol optSizeSymbol = optSizeExpr.Symbol();
        int arraySize = int.Parse(optSizeSymbol.Value.ToString());
        Assert.Error(arraySize > 0, arraySize, "non-positive array size");
        CCompiler.Type arrayType = new CCompiler.Type(arraySize, null);
        decl.Add(arrayType);
      }
      else {
        CCompiler.Type arrayType = new CCompiler.Type(0, null);
        decl.Add(arrayType);
      }
    
      return decl;
    }
  
    public static Declarator GenerateNewFunctionDeclaration(Declarator decl,
                                        Pair<List<Symbol>,bool> paramPair) {
      if (decl == null) {
        decl = new Declarator(null);
      }
    
      List<Symbol> paramList = paramPair.First;
      bool ellipse = paramPair.Second;

      if (paramList.Count == 0) {
        Assert.Error(!ellipse, "...",
                     "an elliptic function must have at least one parameter");
        paramList = null;
      }
      else if ((paramList.Count == 1) && paramList[0].Type.IsVoid()) {
        Assert.Error(paramList[0].Name == null, paramList[0].Name,
                     "a void parameter cannot be named");
        Assert.Error(!ellipse, "...",
                    "an elliptic function cannot have a void parameter");
        paramList.Clear();
      }
      else {
        ISet<string> nameSet = new HashSet<string>();
        foreach (Symbol symbol in paramList) {
          string name = symbol.Name;
          if (name != null) {
            Assert.Error(nameSet.Add(name),
                         name, "repeated name in parameter list");
          }
        
          CCompiler.Type type = symbol.Type;
          if (type.IsVoid()) {
            Assert.Error(name, "invalid void parameter");
          }
          else if (type.IsArray()) {
            CCompiler.Type pointerType = new CCompiler.Type(type.ArrayType);
            pointerType.Constant = true;
            symbol.Type = pointerType;
          }
          else if (type.IsFunction()) {
            CCompiler.Type pointerType = new CCompiler.Type(type);
            pointerType.Constant = true;
            symbol.Type = pointerType;
          }
        }
      }

      if (paramList != null) {
        decl.Add(new CCompiler.Type(null, paramList, ellipse));
      }
      else {
        List<string> nameList = new MyList<string>();
        decl.Add(new CCompiler.Type(null, nameList));
      }
    
      return decl;
    }
  
    public static Declarator GenerateOldFunctionDeclaration(Declarator decl,
                                                       List<string> nameList) {
      ISet<string> nameSet = new HashSet<string>();
      foreach (string name in nameList) {
        Assert.Error(nameSet.Add(name), name, "repeated name in parameter list");
      }
    
      CCompiler.Type funcType = new CCompiler.Type(null, nameList);
      decl.Add(funcType);
      return decl;
    }
  
    public static Symbol GenerateParameter(Specifier specifier,
                                           Declarator decl) {
      specifier.Generate();
      Storage storage = specifier.Storage;
      CCompiler.Type specType = specifier.Type;

      Assert.Error((storage == Storage.Auto) || (storage == Storage.Register),
                   "parameter storage must be auto or register");
    
      if (decl != null) {
        decl.Add(specType);
        CCompiler.Type type = toParameterType(decl.Type);
        return (new Symbol(Symbol.Status.Parameter, decl.Name, storage, type, null));
      }
      else {
        CCompiler.Type type = toParameterType(specType);
        return (new Symbol(Symbol.Status.Parameter, null, storage, type, null));
      }
    }
  
    private static CCompiler.Type toParameterType(CCompiler.Type type) {
      switch (type.GetSort()) {
        case Sort.Array:
          return new CCompiler.Type(type.ArrayType);

        case Sort.Function:
          return new CCompiler.Type(type);
        
        default:
          return type;
      }
    }
  
    public static CCompiler.Type GenerateTypeName(Specifier specifier,
                                        Declarator decl) {
      specifier.Generate();
      CCompiler.Type specType = specifier.Type;

      if (decl != null) {
        decl.Add(specType);
        return decl.Type;
      }
      else {
        return specType;
      }
    }
  }
}