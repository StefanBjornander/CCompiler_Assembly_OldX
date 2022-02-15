using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class Symbol {
    public enum Status {Variable, Parameter};
    private Status m_status;
  
    protected string m_name, m_uniqueName;
    private Storage m_storage;
    protected CCompiler.Type m_type;
    protected object m_value;  
    private int m_offset, m_entryPoint;

    private Symbol m_addressSymbol;
    private int m_addressOffset;

    private Symbol m_parentSymbol;
    private bool m_assignable, m_Addressable;
  
    private ISet<MiddleCode> m_trueSet, m_falseSet;

    private List<sbyte> m_byteList;
    private IDictionary<int,string> m_accessMap;
    private IDictionary<int,string> m_callMap;
    private ISet<int> m_returnSet;

    private List<string> m_textList;
    private IDictionary<int, int> m_byteToTextMap;

    private static int UniqueNameCount = 0;

    public Symbol() {
      // Empty.
    }

    public Symbol(string name, Storage storage, CCompiler.Type type)
     :this(Status.Variable, name, storage, type, null) {
       // Empty.
    }

    public Symbol(Status status, string name, Storage storage,
                  CCompiler.Type type, object value)
     :this(status, name, storage, type, value, null, null, null, null, null, null) {
      m_assignable = !type.Constant;
      m_Addressable = true;

      if (m_type.IsFunction()) {
        m_byteList = new MyList<sbyte>();
        m_accessMap = new Dictionary<int,string>();
        m_callMap = new Dictionary<int,string>();
        m_returnSet = new HashSet<int>();
        m_byteToTextMap = new Dictionary<int,int>();
        m_textList = new MyList<string>();
      }
      else if (m_storage == Storage.Static) {
        m_byteList = new MyList<sbyte>();
        m_accessMap = new Dictionary<int,string>();
        m_textList = new MyList<string>();
        GenerateInitializer.GenerateZeroByteTextList(this);
        //Start.CurrentTable.StaticSet.Add(this);
      }
    }

    public Symbol(Status status, string name, Storage storage, // full
                  CCompiler.Type type, object value, List<sbyte> byteList,
                  IDictionary<int,string> accessMap, IDictionary<int,string> callMap,
                  ISet<int> returnSet, IDictionary<int,int> byteToTextMap,
                  List<string> textList) {
      m_status = status;
      m_name = name;
      m_storage = storage;
      m_type = type;
      m_value = value;
    
      m_byteList = byteList;
      m_accessMap = accessMap;
      m_callMap = callMap;
      m_returnSet = returnSet;
      m_byteToTextMap = byteToTextMap;
      m_textList = textList;

      if (m_name != null) {
        if (m_name.StartsWith("container") && m_name.EndsWith("bytes#")) {
          m_uniqueName = name;
        }
        else if (!m_name.EndsWith(Start.NumberId) &&
                 ((Start.CurrentTable.Scope == Scope.Global) &&
                  (m_storage != Storage.Private) /*&& (m_storage != Storage.Static)*/)) {
          m_uniqueName = name;
        }
        else /*if ((storage == Storage.Static) || (storage == Storage.Private))*/ {
          m_uniqueName = Start.FileMarker + (UniqueNameCount++) +
                         Start.SeparatorId + name;
        }
      }

      if (m_storage == Storage.Private) {
        m_storage = Storage.Static;
      }

      if (m_storage == Storage.Static) {
        Start.CurrentTable.StaticSet.Add(this);
      }
    }

    public Symbol(ISet<MiddleCode> trueSet, ISet<MiddleCode> falseSet)
     :this(Status.Variable, Start.TemporaryId + "logical" + (Start.TemporaryCount++),
           Storage.Auto, new CCompiler.Type(Sort.Logical), null, null, null, null, null, null, null) {
      m_trueSet = trueSet;
      m_falseSet = falseSet;
    }

    public ISet<MiddleCode> trueSet() {
      return m_trueSet;
    }

    public ISet<MiddleCode> falseSet() {
      return m_falseSet;
    }

    public Symbol(CCompiler.Type type, object value) // value
     :this(Status.Variable, GenerateValueName(type, value), Storage.Static,
           type, value, new MyList<sbyte>(), new Dictionary<int,string>(),
           null, null, null, new MyList<string>()) {
      GenerateInitializer.GenerateByteTextList(this);
    }

    public static string GenerateValueName(CCompiler.Type type, object value) {
      Assert.Error(value != null);

      if (value is string) {
        string text = (string) value;
        StringBuilder buffer = new StringBuilder();

        for (int index = 0; index < text.Length; ++index) {
          if (char.IsLetterOrDigit(text[index]) ||
              (text[index] == '_')) {
            buffer.Append(text[index]);
          }
          else if (text[index] != '\0') {
            int asciiValue = (int) text[index];
            char hex1 = "0123456789ABCDEF"[asciiValue / 16],
                 hex2 = "0123456789ABCDEF"[asciiValue % 16];
            buffer.Append(hex1.ToString() + hex2.ToString());
          }
        }

        //string name = Enum.GetName(typeof(Sort), Sort.StringX);
        return "string_" + buffer.ToString() + Start.NumberId;
      }
      else if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        return "staticaddress" + Start.SeparatorId + staticAddress.Name +
                Start.SeparatorId + staticAddress.Offset + Start.NumberId;
      }
      else if (type.IsArray()) {
        return "Array_" + value.ToString() + Start.NumberId;
        //return "Array_" + Start.NumberId;
      }
      else if (type.IsFloating()) {
        return "float" + type.Size().ToString() + Start.SeparatorId + value.ToString().Replace("-", "minus") + Start.NumberId;
      }
      else if (type.IsLogical()) {
        return "int" + type.Size().ToString() + Start.SeparatorId + ((((bool) value)) ? "1" : "0") + Start.NumberId;
      }
      else {
        return "int" + type.Size().ToString() + Start.SeparatorId + value.ToString().Replace("-", "minus") + Start.NumberId;
      }
/*      else {
        string text;

        if (value is decimal) {
          decimal decValue = (decimal) value;
          text = decValue.ToString().Replace("-", "minus")/*.Replace(",", ".")/;
        }
        else {
          text = value.ToString().Replace("-", "minus")/*.Replace(",", ".")/;
        }

        switch (type.Size()) {
          case 1:
            return "Byte_" + text + Start.NumberId;
          
          case 2:
            return "Word_" + text + Start.NumberId;
          
          case 4:
            return "DWord_" + text + Start.NumberId;
          
          case 8:
            return "QWord_" + text + Start.NumberId;
          
          default:
            Assert.Error("");
            return null;
        }
      }*/
    }

    public Symbol(string name) // function
     :this(Status.Variable, name, Storage.Static, null, null,
           new MyList<sbyte>(), new Dictionary<int,string>(),
           new Dictionary<int,string>(), new HashSet<int>(),
           new Dictionary<int,int>(), new MyList<string>()) {
      m_textList.Add("\n\n" + m_uniqueName + ":");
    }

    private static Symbol m_lastFunction = null;

    public Symbol(CCompiler.Type type) // temporary
     :this(Status.Variable, Start.TemporaryId + "temporary" + (Start.TemporaryCount++),
           Storage.Auto, type, null, null, null, null, null, null, null) {

      if ((Start.CurrentFunction != null) && (Start.CurrentFunction != m_lastFunction)) {
        Start.symbolStream.WriteLine(Start.CurrentFunction.Name + " " + m_name);
        m_lastFunction = Start.CurrentFunction;
      }
    }

    public Register? BaseRegister() {
      switch (m_storage) {
        case Storage.Auto:
        case Storage.Register:
          return !Start.CurrentFunction.Type.IsEllipse() ||
                 (m_status == Status.Parameter) ? AssemblyCode.EllipseRegister
                                                : AssemblyCode.FrameRegister;
        
        default:
          return null;
      }
    }

    public string Name {
      get {
        return m_name;
      }

      set {
        m_name = value;
      }
    }

    public string UniqueName {
      get {
        return m_uniqueName;
      }

      set {
        m_uniqueName = value;
      }
    }

    public Symbol.Status GetStatus() {
      return m_status;
    }

    public CCompiler.Storage Storage {
      get {
        return m_storage;
      }

      set {
        if ((m_storage != Storage.Static) && (value == Storage.Static)) {
          m_byteList = new MyList<sbyte>();
          m_accessMap = new Dictionary<int,string>();
          m_textList = new MyList<string>();
          m_textList.Add("\n\n" + m_uniqueName + ":");
        }
    
        m_storage = value;
      }
    }

    public CCompiler.Type Type {
      get {
        return m_type;
      }

      set {
        m_type = value;
      }
    }

    public int Offset {
      get {
        return m_offset;
      }

      set {
        m_offset = value;
      }
    }

    public int EntryPoint {
      get {
        return m_entryPoint;
      }

      set {
        m_entryPoint = value;
      }
    }
  
    public bool IsRightValue() {
      return true;
    }
  
    public bool IsAutoOrRegister() {
      return (m_storage == Storage.Auto) ||
             (m_storage == Storage.Register);
    }

    public bool IsStatic() {
      return (m_storage == Storage.Static);
    }
  
    public bool IsExtern() {
      return (m_storage == Storage.Extern);
    }
  
    public bool IsStaticOrExtern() {
      return IsStatic() || IsExtern();
    }
  
    public bool IsTypedef() {
      return (m_storage == Storage.Typedef);
    }
  
    public bool IsParameter() {
      return (m_status == Status.Parameter);
    }
          
    public bool IsValue() {
      return m_name.EndsWith(Start.NumberId);
    }

    /*public bool IsTemporary() {
      return (m_name != null) && m_name.StartsWith(Start.TemporaryId);
    }*/

    private bool m_switch = false;

    public bool Switch {
      get {
        return m_switch;
      }

      set {
        m_switch = value;
      }
    }

    public bool IsTemporary() {
      return m_name.StartsWith(Start.TemporaryId);
    }

    public bool IsTemporary2() {
      Symbol symbol = this;
    
      while (symbol != null) {
        if (!symbol.Name.StartsWith(Start.TemporaryId)) {
          return false;
        }
      
        symbol = symbol.ParentSymbol;
      }
    
      return true;
    }

    public bool HasValue() {
      return (m_value != null) && !m_type.IsString();
    }

    public object Value {
      get {
        return m_value;
      }

      set {
        m_value = value;
      }
    }
  
    public BigInteger GetSignedValue() {
      if (m_type.IsUnsigned()) {
        BigInteger intValue = (BigInteger) m_value;

        if (intValue.CompareTo(BigInteger.Zero) < 0) {      
          switch (m_type.Size()) {
            case CCompiler.Type.CharSize:
              return BigInteger.Subtract(intValue, new BigInteger(128));

            case CCompiler.Type.IntegerSize:
              return BigInteger.Subtract(intValue, new BigInteger(32768));

            case CCompiler.Type.LongSize:
              return BigInteger.Subtract(intValue, new BigInteger(2147483648));
          }
        }  
      }

      return ((BigInteger) m_value);
    }
  
    public bool HasAddressSymbol() {
      return (m_addressSymbol != null);
    }

    public Symbol AddressSymbol {
      get {
        return m_addressSymbol;
      }
  
      set {
        m_addressSymbol = value;
      }
    }  

    public int AddressOffset {
      get {
        return m_addressOffset;
      }

      set {
        m_addressOffset = value;
      }
    }

    public Symbol ParentSymbol {
      get {
        return m_parentSymbol;
      }

      set {
        m_parentSymbol = value;
      }
    }

    public List<sbyte> ByteList {
      get {
        return m_byteList;
      }
    }
  
    public IDictionary<int,string> AccessMap {
      get {
        return m_accessMap;
      }
    }
  
    public IDictionary<int,string> CallMap {
      get {
        return m_callMap;
      }
    }  
  
    public ISet<int> ReturnSet {
      get {
        return m_returnSet;
      }
    }
  
    public IDictionary<int,int> ByteToTextMap() {
      return m_byteToTextMap;
    }
  
    public List<string> GetTextList() {
      return m_textList;
    }
  
    public void SetTextList(List<string> textList) {
      m_textList = textList;
    }
  
    public void SetAssignable(bool assignable) {
      m_assignable = assignable;
    }
  
    public bool IsAssignable() {
      if (m_type.IsArrayFunctionOrString() || m_type.Constant) {
        return false;
      }
    
      if (m_type.IsStructOrUnion()) {
        foreach (Symbol memberSymbol in m_type.MemberMap.Values) {
          if (!memberSymbol.IsAssignable()) {
            return false;
          }
        }
      }
    
      return true;
    }

    public void SetAddressable(bool Addressable) {
      m_Addressable = Addressable;
    }
  
    // p = &a[0];

    public bool IsAddressable() {
      return true;
    }
  
    public bool IsAddressableX() {
      if (!IsTemporary() && (m_storage != Storage.Register) &&
          (m_type.GetBitfieldMask() == null)) {
        return true;
      }
    
      return ((m_addressSymbol != null) && m_addressSymbol.IsAddressable());
    }
  
   public override string ToString() {
      if ((m_type.IsIntegralOrPointer() || m_type.IsFloating()) && (m_value != null)) {
        return Value.ToString();
      }
      else if (m_name != null) {
        if (m_addressSymbol != null) {
          return ((m_name != null) ? m_name : "") + " -> " + m_addressSymbol.ToString();
        }
        else {
          return ((m_name != null) ? m_name : "");
        }
      }
      else {
        if (m_addressSymbol != null) {
          return ((m_uniqueName != null) ? m_uniqueName : "") + " -> " + m_addressSymbol.ToString();
        }
        else {
          return ((m_uniqueName != null) ? m_uniqueName : "");
        }
      }
    }

    private static string SimpleNameX(string name) {
      int index = name.LastIndexOf(Start.SeparatorId);
      return (index != -1) ? name.Substring(index + 1).Replace("#", "") : name;
    }

    public void Save(BinaryWriter outStream) /* throw Exception */ {
      outStream.Write(m_uniqueName);
      outStream.Write(m_entryPoint);

      if (m_byteList != null) {
        outStream.Write(m_byteList.Count);
        foreach (sbyte b in m_byteList) {
          outStream.Write(b);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_accessMap != null) {
        outStream.Write(m_accessMap.Count);
        foreach (KeyValuePair<int,string> entry in m_accessMap) {
          outStream.Write(entry.Key);
          outStream.Write(entry.Value);
        }
      }
      else {
        outStream.Write(0);
      }
    
      if (m_callMap != null) {
        outStream.Write(m_callMap.Count);
        foreach (KeyValuePair<int,string> entry in m_callMap) {
          outStream.Write(entry.Key);
          outStream.Write(entry.Value);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_returnSet != null) {
        outStream.Write(m_returnSet.Count);
        foreach (int address in m_returnSet) {
          outStream.Write(address);
        }
      }
      else {
        outStream.Write(0);
      }
    
      if (m_byteToTextMap != null) {
        outStream.Write(m_byteToTextMap.Count);
        foreach (KeyValuePair<int,int> entry in m_byteToTextMap) {
          outStream.Write(entry.Key);
          outStream.Write(entry.Value);
        }
      }
      else {
        outStream.Write(0);
      }

      if (m_textList != null) {    
        outStream.Write(m_textList.Count);
        foreach (string text in m_textList) {
          outStream.Write(text);
        }
      }
      else {
        outStream.Write(0);
      }
    }

    public void Load(BinaryReader inStream) /* throw Exception */ {
      m_uniqueName = inStream.ReadString();
      m_entryPoint = inStream.ReadInt32();

      { m_byteList = new MyList<sbyte>();
        int byteListSize = inStream.ReadInt32();

        for (int index = 0; index < byteListSize; ++index) {
          sbyte b = inStream.ReadSByte();
          m_byteList.Add(b);
        }
      }
    
      { m_accessMap = new Dictionary<int,string>();
        int accessMapSize = inStream.ReadInt32();
        for (int index = 0; index < accessMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_accessMap.Add(address, name);
        }
      }
    
      { m_callMap = new Dictionary<int,string>();
        int callMapSize = inStream.ReadInt32();
        for (int index = 0; index < callMapSize; ++index) {
          int address = inStream.ReadInt32();
          string name = inStream.ReadString();
          m_callMap.Add(address, name);
        }
      }
    
      { m_returnSet = new HashSet<int>();
        int returnSetSize = inStream.ReadInt32();
        for (int index = 0; index < returnSetSize; ++index) {
          int address = inStream.ReadInt32();
          m_returnSet.Add(address);
        }
      }
    
      { m_byteToTextMap = new Dictionary<int,int>();
        int byteToTextMapSize = inStream.ReadInt32();
        for (int index = 0; index < byteToTextMapSize; ++index) {
          int address = inStream.ReadInt32();
          int textLine = inStream.ReadInt32();
          m_byteToTextMap.Add(address, textLine);
        }
      }
    
      { m_textList = new MyList<string>();
        int textListSize = inStream.ReadInt32();
        for (int index = 0; index < textListSize; ++index) {
          string text = inStream.ReadString();
          m_textList.Add(text);
        }
      }
    }
  }
}

    // global space:
    //   int i;        => external linkage
    //   extern int i; => external linkage
    //   static int i; => no external linkage
  
    //   int f(int i);        => external linkage
    //   extern int f(int i); => external linkage
    //   static int f(int i); => no external linkage

    //   int f(int i) {}        => external linkage
    //   extern int f(int i) {} => external linkage
    //   static int f(int i) {} => no external linkage

    //   int x = 1;        => external linkage
    //   static int x = 1; => external linkage
    //   extern int x = 1; => external linkage
  
    // int  f() {}
    // extern int  f() {}
    // static int  f() {}

    // int i;
    // extern int i;      
    // static int i;