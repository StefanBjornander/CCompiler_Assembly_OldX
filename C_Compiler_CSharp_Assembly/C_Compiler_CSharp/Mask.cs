using System.Collections.Generic;

namespace CCompiler {
  public class Mask {
    public const int StorageMask = 0x0000FFF;
    public const int Auto        = 0x0000010;
    public const int Register    = 0x0000020;
    public const int Static      = 0x0000040;
    public const int Extern      = 0x0000080;
    public const int Typedef     = 0x0000100;
    public const int Private     = 0x0000200;

    public const int QualifierMask = 0x000F000;
    public const int Constant      = 0x0001000;
    public const int Volatile      = 0x0002000;

    public const int SortMask  = 0xFFF0000;
    public const int Signed    = 0x0010000;
    public const int Unsigned  = 0x0020000;
    public const int Char      = 0x0040000;
  //  public const int WChar_T   = 0x0080000;
    public const int Short     = 0x0100000;
    public const int Int       = 0x0200000;
    public const int Long      = 0x0400000;
    public const int Float     = 0x0800000;
    public const int Double    = 0x1000000;
    public const int Void      = 0x2000000;

    public const int SignedChar = Signed | Char;
    public const int UnsignedChar = Unsigned | Char;
    public const int ShortInt = Short | Int;
    public const int SignedShort = Signed | Short;
    public const int SignedShortInt = Signed | Short | Int;
    public const int UnsignedShort = Unsigned | Short;
    public const int UnsignedShortInt = Unsigned | Short | Int;
    public const int SignedInt = Signed | Int;
    public const int UnsignedInt = Unsigned | Int;
    public const int LongInt = Long | Int;
    public const int SignedLong = Signed | Long;
    public const int SignedLongInt = Signed | Long | Int;
    public const int UnsignedLong = Unsigned | Long;
    public const int UnsignedLongInt = Unsigned | Long | Int;
    public const int LongDouble = Long | Double;
  
    public static IDictionary<int,string> MaskToNameMap = new Dictionary<int,string>();

    static Mask() {
      MaskToNameMap.Add(Auto, "auto");
      MaskToNameMap.Add(Register, "register");
      MaskToNameMap.Add(Static, "static");
      MaskToNameMap.Add(Extern, "extern");
      MaskToNameMap.Add(Typedef, "typedef");
      MaskToNameMap.Add(Private, "private");
      MaskToNameMap.Add(Constant, "constant");
      MaskToNameMap.Add(Volatile, "volatile");
      MaskToNameMap.Add(Signed, "signed");
      MaskToNameMap.Add(Unsigned, "unsigned");
      MaskToNameMap.Add(Char, "char");
  //    MaskToNameMap.Add(WChar_T, "wchar_t");
      MaskToNameMap.Add(Short, "short");
      MaskToNameMap.Add(Int, "int");
      MaskToNameMap.Add(Long, "long");
      MaskToNameMap.Add(Float, "float");
      MaskToNameMap.Add(Double, "double");
      MaskToNameMap.Add(Void, "void");
    }    
  }
}