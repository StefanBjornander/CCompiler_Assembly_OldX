using System;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class Type {
    public static Type SignedIntegerType = new Type(Sort.Signed_Int);
    public static Type UnsignedIntegerType = new Type(Sort.Unsigned_Int);
    public static Type SignedLongIntegerType = new Type(Sort.Signed_Long_Int);
    public static Type UnsignedLongIntegerType = new Type(Sort.Unsigned_Long_Int);
    public static Type DoubleType = new Type(Sort.Double);
    public static Type LongDoubleType = new Type(Sort.Long_Double);
    public static Type SignedCharType = new Type(Sort.Signed_Char);
    public static Type UnsignedCharType = new Type(Sort.Unsigned_Char);
    public static Type StringType = new Type(Sort.StringX);
    public static Type VoidType = new Type(Sort.Void);
    public static Type PointerTypeX = new Type(SignedIntegerType);
    public static Type LogicalType = new Type(Sort.Logical);
  
    // ------------------------------------------------------------------------
  
    private Sort m_sort;
  
    public Sort GetSort() {
      return m_sort;
    }

    public Type(Sort sort) { // arithmetic or logical
      m_sort = sort;
    }
     
    // ------------------------------------------------------------------------
  
/*    private List<Symbol> m_enumerationList;

    public Type(List<Symbol> enumerationList) {
      m_sort = Sort.Enumeration;
      m_enumerationList = enumerationList;
    }
  
    public List<Symbol> GetEnumerationList() {
      return m_enumerationList;
    }*/
  
    // ------------------------------------------------------------------------
  
    private BigInteger? m_bitfieldMask = null;

    public bool IsBitfield() {
      return (m_bitfieldMask != null);
    }

    public BigInteger? GetBitfieldMask() {
      return m_bitfieldMask;
    }

    public void SetBitfieldMask(int bits) {
      m_bitfieldMask = BigInteger.Subtract(BigInteger.Pow(2, bits), BigInteger.One);
    }

    // ------------------------------------------------------------------------
  
    private Type m_pointerType;

    public Type(Type pointerType) {
      m_sort = Sort.Pointer;
      m_pointerType = pointerType;
    }

    public Type PointerType {
      get {
        return m_pointerType;
      }

      set {
        m_pointerType = value;
      }
    }

    // ------------------------------------------------------------------------
  
    private int m_dimension;
    private int m_arraySize;
    private Type m_arrayType;

    public Type(int arraySize, Type arrayType) {
      m_sort = Sort.Array;
      m_arraySize = arraySize;
      m_arrayType = arrayType;
    }

    public int Dimension {
      get {
        return m_dimension;
      }

      set {
        m_dimension = value;
      }
    }

    public int ArraySize {
      get {
        return m_arraySize;
      }

      set {
        m_arraySize = value;
      }
    }

    public Type ArrayType {
      get {
        return m_arrayType;
      }

      set {
        m_arrayType = value;
      }
    }

    public Type GetPointerOrArrayType() {
      return (m_sort == Sort.Pointer) ? m_pointerType : m_arrayType;
    }

    // ------------------------------------------------------------------------

    private Type m_returnType;
    private List<string> m_nameList;
    private List<Symbol> m_parameterList;
    private List<Type> m_typeList;
    private bool m_ellipse;

    public Type(Type returnType, List<string> nameList) {
      m_sort = Sort.Function;
      m_returnType = returnType;
      m_nameList = nameList;
      m_parameterList = null;
      m_ellipse = false;
    
      ISet<string> nameSet = new HashSet<string>();
      foreach (string name in m_nameList) {
        Assert.Error(nameSet.Add(name), name, "duplicate name in parameter list");
      }
    }

    public Type(Type returnType, List<Symbol> parameterList, bool ellipse) {
      m_sort = Sort.Function;
      m_returnType = returnType;
      m_nameList = null;
      m_parameterList = parameterList;
      m_ellipse = ellipse;
      m_typeList = null;
    
      if (parameterList != null) {
        m_typeList = new MyList<Type>();

        foreach (Symbol symbol in parameterList) {
          m_typeList.Add(symbol.Type);
        }
      }
    }

    public bool IsOldStyleFunction() {
      return (m_nameList != null);
    }

    public bool IsNewStyleFunction() {
      return (m_parameterList != null);
    }

    public List<string> GetNameList() {
      return m_nameList;
    }

    public List<Symbol> GetParameterList() {
      return m_parameterList;
    }

    public List<Type> GetTypeList() {
      return m_typeList;
    }

    public Type ReturnType {
      get {
        return m_returnType;
      }

      set {
        m_returnType = value;
      }
    }

    public bool IsEllipse() {
      return m_ellipse;
    }

    // ------------------------------------------------------------------------
  
    private bool m_hasTag;
    private IDictionary<string,Symbol> m_memberMap;
    private List<Symbol> m_memberList;

    public Type(Sort sort, IDictionary<string,Symbol> symbolMap, List<Symbol> symbolList, bool hasTag) {
      m_sort = sort;
      m_hasTag = hasTag;
      m_memberMap = symbolMap;
      m_memberList = symbolList;
    }

    public bool HasTag() {
      return m_hasTag;
    }

    public Symbol LookupMember(string name) {
      return m_memberMap[name];
    }

    public IDictionary<string,Symbol> MemberMap {
      get {
        return m_memberMap;
      }

      set {
        m_memberMap = value;
      }
    }

    public List<Symbol> MemberList {
      get {
        return m_memberList;
      }

      set {
        m_memberList = value;
      }
    }

    // ------------------------------------------------------------------------
  
    private ISet<Symbol> m_memberSet;
    private bool m_enumInit;

    public Type(Sort sort, ISet<Symbol> symbolSet, bool enumInit) {
      m_sort = sort;
      m_memberSet = symbolSet;
      m_enumInit = enumInit;
    }

    public bool EnumInit() {
      return m_enumInit;
    }

    public ISet<Symbol> MemberSet {
      get {
        return m_memberSet;
      }

      set {
        m_memberSet = value;
      }
    }

    // ------------------------------------------------------------------------

    public const int CharSize = 1;
    public const int ShortSize = 1;
    public const int IntegerSize = 2;
    public const int LongSize = 4;
    public const int FloatSize = 4;
    public const int DoubleSize = 8;
    public const int QuarterWordSize = 8;
    public const int PointerSize = 2;

    private static IDictionary<Sort,int> m_sizeMap = new Dictionary<Sort,int>();
    private static IDictionary<Sort,BigInteger> m_maskMap = new Dictionary<Sort,BigInteger>();
  
    static Type() {
      m_sizeMap.Add(Sort.Logical, IntegerSize);
      m_sizeMap.Add(Sort.Signed_Char, CharSize);
      m_sizeMap.Add(Sort.Unsigned_Char, CharSize);
      m_sizeMap.Add(Sort.Signed_Short_Int, ShortSize);
      m_sizeMap.Add(Sort.Unsigned_Short_Int, ShortSize);
      m_sizeMap.Add(Sort.Signed_Int, IntegerSize);
      m_sizeMap.Add(Sort.Unsigned_Int, IntegerSize);
      m_sizeMap.Add(Sort.Signed_Long_Int, LongSize);
      m_sizeMap.Add(Sort.Unsigned_Long_Int, LongSize);
      m_sizeMap.Add(Sort.Float, FloatSize);
      m_sizeMap.Add(Sort.Double, DoubleSize);
      m_sizeMap.Add(Sort.Long_Double, QuarterWordSize);

      m_maskMap.Add(Sort.Unsigned_Char, new BigInteger(255));
      m_maskMap.Add(Sort.Unsigned_Short_Int, new BigInteger(255));
      m_maskMap.Add(Sort.Unsigned_Int, new BigInteger(65536));
      m_maskMap.Add(Sort.Unsigned_Long_Int, new BigInteger(4294967296));
    }
  
    /*private static IDictionary<Sort,BigInteger> m_maskMap = new Dictionary<Sort,BigInteger>();
  
    static Type() {
      m_maskMap.Add(Sort.Unsigned_Char, new BigInteger(255));
      m_maskMap.Add(Sort.Unsigned_Short_Int, new BigInteger(255));
      m_maskMap.Add(Sort.Unsigned_Int, new BigInteger(65536));
      m_maskMap.Add(Sort.Unsigned_Long_Int, new BigInteger(4294967296));
    }*/
  
    public static BigInteger GetMask(Sort sort) {
      return m_maskMap[sort];
    }
    
    public int Size() {
      switch (m_sort) {
        case Sort.Pointer:
        case Sort.StringX:
        case Sort.Function:
          return PointerSize;
        
        case Sort.Array:
          return m_arraySize * m_arrayType.Size();

        case Sort.Struct: {
            int size = 0;

            foreach (Symbol symbol in m_memberMap.Values) {
              size += symbol.Type.Size();
            }
        
            return size;
          }

        case Sort.Union: {
            int size = 0;

            foreach (Symbol symbol in m_memberMap.Values) {
              size = Math.Max(size, symbol.Type.Size());
            }

            return size;
          }

        case Sort.Void:
//        case Sort.Function:
          return 0;
  
        case Sort.Logical:
          return Type.SignedCharType.Size();

        default:
          return m_sizeMap[m_sort];
      }
    }

    public int ArraySizeX() {
      switch (m_sort) {
        case Sort.Array:
          return PointerSize;

        default:
          return Size();
      }
    }

    public int ConvertedSize() {
      switch (m_sort) {
        case Sort.Array:
        case Sort.Function:
          return PointerSize;

        default:
          return Size();
      }
    }
  
    public bool IsComplete() {
      switch (m_sort) {
        case Sort.Array:
          return (m_arraySize > 0);

        case Sort.Struct:
        case Sort.Union:
          return (m_memberMap != null);

        default:
          return true;
      }
    }

    // ------------------------------------------------------------------------
  
    private bool m_constant;
    private bool m_volatile;

    public bool Constant {
      get {
        if (m_constant) {
          return true;
        }  
        else if (IsStructOrUnion() && (m_memberMap != null)) {
          foreach (Symbol symbol in m_memberMap.Values) {
            if (symbol.Type.Constant) {
              return true;
            }
          }
        }
    
        return false;
      }
  
      set {
        m_constant = value;
      }
    }

    public bool Volatile {
      get {
        return m_volatile;
      }

      set {
        m_volatile = value;
      }
    }

    // ------------------------------------------------------------------------
  
    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is Type) {
        Type type = (Type) obj;

        if (m_sort == type.m_sort) {
          if ((m_constant != type.m_constant) ||
              (m_volatile != type.m_volatile)) {
            return false;
          }

          switch (m_sort) {
            case Sort.Pointer:
              return m_pointerType.Equals(type.m_pointerType);

            case Sort.Array:
              return ((m_arraySize == 0) || (type.m_arraySize == 0) ||
                      (m_arraySize == type.m_arraySize)) &&
                      m_arrayType.Equals(type.m_arrayType);

            case Sort.Struct:
            case Sort.Union:
              return ((m_memberMap == null) && (type.m_memberMap == null)) ||
                     ((m_memberMap != null) && (type.m_memberMap != null) &&
                      m_memberMap.Equals(type.m_memberMap));

            case Sort.Function:
              return m_returnType.Equals(type.m_returnType) &&
                     (((m_typeList == null) && (type.m_typeList == null)) ||
                      ((m_typeList != null) && (type.m_typeList != null) &&
                       m_typeList.Equals(type.m_typeList))) &&
                       (m_ellipse == type.m_ellipse);

            default:
              return true;
          }
        }
      }

      return false;
    }

    // ------------------------------------------------------------------------
  
    public bool IsVoid() {
      return (m_sort == Sort.Void);
    }

    public bool IsChar() {
      return (m_sort == Sort.Signed_Char) || (m_sort == Sort.Unsigned_Char);
    }

    public bool IsShort() {
      return (m_sort == Sort.Signed_Short_Int) ||
             (m_sort == Sort.Unsigned_Short_Int);
    }

    public bool IsInteger() {
      return (m_sort == Sort.Signed_Int) || (m_sort == Sort.Unsigned_Int);
    }

    public bool IsIntegral() {
      return IsSigned() || IsUnsigned();
    }

    public bool IsSigned() {
      switch (m_sort) {
        case Sort.Signed_Char:
        case Sort.Signed_Short_Int:
        case Sort.Signed_Int:
        case Sort.Signed_Long_Int:
          return true;

        default:
          return false;
      }
    }

    public bool IsUnsigned() {
      switch (m_sort) {
        case Sort.Unsigned_Char:
        case Sort.Unsigned_Short_Int:
        case Sort.Unsigned_Int:
        case Sort.Unsigned_Long_Int:
          return true;

        default:
          return false;
      }
    }

    public bool IsFloat() {
      return (m_sort == Sort.Float);
    }
  
    public bool IsFloating() {
      switch (m_sort) {
        case Sort.Float:
        case Sort.Double:
        case Sort.Long_Double:
          return true;

        default:
          return false;
      }
    }

    public bool IsLogical() {
      return (m_sort == Sort.Logical);
    }

    public bool IsPointer() {
      return (m_sort == Sort.Pointer);
    }

    public bool IsArray() {
      return (m_sort == Sort.Array);
    }

    public bool IsPointerOrArray() {
      return IsPointer() || IsArray();
    }

    public bool IsFunction() {
      return (m_sort == Sort.Function);
    }

    public bool IsArrayOrFunction() {
      return IsArray() || IsFunction();
    }

    public bool IsPointerArrayStringOrFunction() {
      return IsPointerArrayStringOrFunction() || IsString();
    }

    public bool IsPointerArrayOrFunction() {
      return IsPointer() || IsArrayOrFunction();
    }

    public bool IsString() {
      return (m_sort == Sort.StringX);
    }

    public bool IsArrayFunctionOrString() {
      return IsArrayOrFunction() || IsString();
    }

    public bool IsFunctionPointer() {
      return (m_sort == Sort.Pointer) && m_pointerType.IsFunction();
    }

    public bool IsFunctionOrArray() {
      return IsFunction() || IsArray();
    }

    public bool IsArrayStringOrFunction() {
      return IsFunctionOrArray() || IsString();
    }

    public bool IsFunctionArrayStringStructOrUnion() {
      return IsFunctionOrArray() || IsString() || IsStructOrUnion();
    }

    public bool IsArithmetic() {
      return IsIntegral() || IsFloating();
    }

    public bool IsIntegralOrPointer() {
      return IsIntegral() || IsPointer();
    }

    public bool IsIntegralOrArray() {
      return IsIntegral() || IsArray();
    }

    public bool IsIntegralPointerOrArray() {
      return IsIntegralOrPointer() || IsArray();
    }

    public bool IsIntegralPointerArrayOrString() {
      return IsIntegralPointerOrArray() || IsString();
    }

    public bool IsIntegralPointerArrayStringOrFunction() {
      return IsIntegralPointerArrayOrString() || IsFunction();
    }

    public bool IsIntegralPointerArrayOfFunction() {
      return IsIntegralPointerOrArray() || IsFunction();
    }

    public bool IsArithmeticOrPointer() {
      return IsArithmetic() || IsPointer();
    }

    public bool IsArithmeticPointerOrArray() {
      return IsArithmeticOrPointer() || IsArray();
    }

    public bool IsArithmeticPointerArrayOrFunction() {
      return IsArithmeticPointerOrArray() || IsFunction();
    }

    public bool IsArithmeticPointerArrayStringOrFunction() {
      return IsArithmeticPointerArrayOrFunction() || IsString();
    }

    public bool IsStruct() {
      return (m_sort == Sort.Struct);
    }

    public bool IsUnion() {
      return (m_sort == Sort.Union);
    }
  
    public bool IsStructOrUnion() {
      return IsStruct() || IsUnion();
    }
  
    public bool IsArithmeticPointerStructOrUnion() {
      return IsArithmeticOrPointer() || IsStructOrUnion();
    }
  
    public bool IsEnum() {
      return (m_sort == Sort.Enumeration);
    }

    public override string ToString() {
      return Enum.GetName(typeof(Sort), m_sort).Replace("_", " ").ToLower();
    }  
  }
}