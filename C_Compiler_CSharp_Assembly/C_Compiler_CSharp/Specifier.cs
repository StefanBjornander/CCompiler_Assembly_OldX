using System.Collections.Generic;

namespace CCompiler {
  public class Specifier {
    private static IDictionary<int,Storage> m_maskToStorageMap = new Dictionary<int,Storage>();
    private static IDictionary<int,Sort> m_maskToSortMap = new Dictionary<int,Sort>();
    private List<object> m_specList = new MyList<object>();
    private Storage m_storage;
    private CCompiler.Type m_type = null;
  
    static Specifier() {
      m_maskToStorageMap.Add(Mask.Auto, Storage.Auto);
      m_maskToStorageMap.Add(Mask.Register, Storage.Register);
      m_maskToStorageMap.Add(Mask.Extern, Storage.Extern);
      m_maskToStorageMap.Add(Mask.Static, Storage.Static);
      m_maskToStorageMap.Add(Mask.Typedef, Storage.Typedef);
      m_maskToStorageMap.Add(Mask.Private, Storage.Private);

      m_maskToSortMap.Add(Mask.Void, Sort.Void);
      m_maskToSortMap.Add(Mask.Char, Sort.Signed_Char);
      m_maskToSortMap.Add(Mask.SignedChar, Sort.Signed_Char);
      m_maskToSortMap.Add(Mask.UnsignedChar, Sort.Unsigned_Char);
      m_maskToSortMap.Add(Mask.Short, Sort.Signed_Short_Int);
      m_maskToSortMap.Add(Mask.ShortInt, Sort.Signed_Short_Int);
      m_maskToSortMap.Add(Mask.SignedShort, Sort.Signed_Short_Int);
      m_maskToSortMap.Add(Mask.SignedShortInt, Sort.Signed_Short_Int);
      m_maskToSortMap.Add(Mask.UnsignedShort, Sort.Unsigned_Short_Int);
      m_maskToSortMap.Add(Mask.UnsignedShortInt, Sort.Unsigned_Short_Int);
      m_maskToSortMap.Add(Mask.Int, Sort.Signed_Int);
      m_maskToSortMap.Add(Mask.Signed, Sort.Signed_Int);
      m_maskToSortMap.Add(Mask.SignedInt, Sort.Signed_Int);
      m_maskToSortMap.Add(Mask.Unsigned, Sort.Unsigned_Int);
      m_maskToSortMap.Add(Mask.UnsignedInt, Sort.Unsigned_Int);
      m_maskToSortMap.Add(Mask.Long, Sort.Signed_Long_Int);
      m_maskToSortMap.Add(Mask.LongInt, Sort.Signed_Long_Int);
      m_maskToSortMap.Add(Mask.SignedLong, Sort.Signed_Long_Int);
      m_maskToSortMap.Add(Mask.SignedLongInt, Sort.Signed_Long_Int);
      m_maskToSortMap.Add(Mask.UnsignedLong, Sort.Unsigned_Long_Int);
      m_maskToSortMap.Add(Mask.UnsignedLongInt, Sort.Unsigned_Long_Int);
      m_maskToSortMap.Add(Mask.Float, Sort.Float);
      m_maskToSortMap.Add(Mask.Double, Sort.Double);
      m_maskToSortMap.Add(Mask.LongDouble, Sort.Long_Double);
    }

    public void Add(object obj) {
      m_specList.Add(obj);
    }

    public void Generate() {
      int totalMask = 0;
      CCompiler.Type compoundType = null;
    
      foreach (object obj in m_specList) {
        if (obj is int) {
          int mask = (int) obj;
          Assert.Error((totalMask & mask) == 0, Mask.MaskToNameMap[mask], // 16384
                       "keyword defined twice");
          totalMask |= mask;
        }
        else if (obj is CCompiler.Type) {
          Assert.Error(compoundType == null, "invalid type specifier sequence");
          compoundType = (CCompiler.Type) obj;
        }
      }
    
      int storageMask = totalMask & Mask.StorageMask;
      if (storageMask != 0) {
        Assert.Error(m_maskToStorageMap.ContainsKey(storageMask),
                     "invalid storage specifier sequence");
        m_storage = m_maskToStorageMap[storageMask];
      }
      else if (Start.ParameterCount > 0) {
        m_storage = Storage.Auto;
      }
      else if (Start.CurrentTable.Scope == Scope.Global) {
        m_storage = Storage.Static;
      }
      else {
        m_storage = Storage.Auto;
      }
    
      if ((compoundType != null) && compoundType.IsEnum()) {
        Assert.Error((m_storage != Storage.Extern) || !compoundType.EnumInit(),
                     compoundType, "extern enumeration cannot be initialized");

        foreach (Symbol member in compoundType.MemberSet) {
          if (member.Storage == Storage.Static) {
            SymbolTable.m_staticSet.Remove(member);
          }

/*          if (Start.Path.Contains("File.h") && member.Name.Equals("WRITE")) {
            int i = 1;
          }*/

          member.Storage = m_storage;

          if (member.Storage == Storage.Static) {
            SymbolTable.m_staticSet.Add(member);
          }
        }
      }

/*      if ((compoundType != null) && compoundType.IsEnum()) {
        List<Symbol> symbolList = compoundType.GetEnumerationList();

        foreach (Symbol itemSymbol in symbolList) {
          itemSymbol.Storage = m_storage;
        }
      }*/
    
      bool isConstant = (totalMask & Mask.Constant) != 0;
      bool isVolatile = (totalMask & Mask.Volatile) != 0;
    
      Sort? sort = null;
      int sortMask = totalMask & Mask.SortMask;

      if (sortMask != 0) {
        Assert.Error(m_maskToSortMap.ContainsKey(sortMask),
                     "invalid type specifier sequence");
        sort = m_maskToSortMap[sortMask];
      }

      if ((compoundType != null) && (sort == null)) {
        compoundType.Constant = (compoundType.Constant || isConstant);
        compoundType.Volatile = (compoundType.Volatile || isVolatile);
        m_type = compoundType;
      }
      else if ((compoundType == null) && (sort != null)) {
        m_type = new CCompiler.Type(sort.Value);
        m_type.Constant = isConstant;
        m_type.Volatile = isVolatile;
      }
      else if ((compoundType == null) && (sort == null)) {
        m_type = new CCompiler.Type(Sort.Signed_Int);
        m_type.Constant = isConstant;
        m_type.Volatile = isVolatile;
      }
      else {
        Assert.Error("invalid type specification sequence");
      }
    }
 
    public CCompiler.Storage Storage {
      get {
        return m_storage;
      }
    }
 
    public CCompiler.Type Type {
      get {
        return m_type;
      }
    }
  
    public CCompiler.Type GenerateQualifiers() {
      CCompiler.Type type = new CCompiler.Type((CCompiler.Type) null);

      if (m_specList != null) {
        int totalMask = 0;
      
        foreach (object obj in m_specList) {
          if (obj is int) {
            int mask = (int) obj;
            Assert.Error((totalMask & mask) == 0, "keyword defined twice");
            Assert.Error((mask & Mask.QualifierMask) != 0,
                         "invalid specification in pointer qualifier list");
            totalMask |= mask;
          }
          else  {
            //Assert.Error("invalid specification in pointer qualifier list");
          }
        }

        type.Constant = ((totalMask & Mask.Constant) != 0);
        type.Volatile = ((totalMask & Mask.Volatile) != 0);
      }
    
      return type;
    }
  }
}