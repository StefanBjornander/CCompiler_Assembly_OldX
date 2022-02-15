using System;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  public class GenerateInitializer {
    public static int Extra;

    private static void UpdateExtra(List<MiddleCode> codeList) {
      foreach (MiddleCode middleCode in codeList) {
        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader:
          //case MiddleOperator.ParameterInitSize:
          case MiddleOperator.Parameter:
          case MiddleOperator.Call:
            middleCode.SetOperand(0, ((int)middleCode.GetOperand(0)) + Extra);
            break;

          case MiddleOperator.PostCall:
            middleCode.SetOperand(2, ((int) middleCode.GetOperand(2)) + Extra);
            break;
        }
      }
    }

    private static List<object> TextToCharList(string text) {
      List<object> list = new List<object>();

      foreach (char c in text) {
        Symbol charSymbol = new Symbol(Type.SignedCharType, new BigInteger((int) c));
        list.Add(new Expression(charSymbol, new List<MiddleCode>(), new List<MiddleCode>()));
      }

      Symbol endSymbol = new Symbol(Type.SignedCharType, new BigInteger(0));
      list.Add(new Expression(endSymbol, new List<MiddleCode>(), new List<MiddleCode>()));
      return list;
    }
  
    public static void GenerateStatic(CCompiler.Type toType, object fromInit,
                                      List<sbyte> byteList, IDictionary<int,string> accessMap,
                                      List<string> textList) {
      if (toType.IsArray() && toType.ArrayType.IsChar() &&
          (fromInit is Expression) && ((Expression) fromInit).Symbol().Type.IsString()) {
        fromInit = TextToCharList((string) ((Expression) fromInit).Symbol().Value);
      }
    
      if (fromInit is Expression) {
        Symbol fromSymbol = ((Expression) fromInit).Symbol();

        if (fromSymbol.Type.IsArray() && toType.IsPointer()) {
          accessMap[byteList.Count] = fromSymbol.UniqueName;
          ObjectCodeGenerator.WriteName(fromSymbol.UniqueName);
          byteList.Add((sbyte)0);
          byteList.Add((sbyte) 0);
          textList.Add("\tdw " + fromSymbol.UniqueName + " ; 2");
          //SymbolTable.StaticSet.Add(new StaticSymbol(toSymbol.UniqueName, toSymbol.Type, toSymbol.Value));
        }
        else {
          Symbol initSymbol = TypeCast.implicitCast(null, ((Expression) fromInit).Symbol(), toType);
      
          if (initSymbol.Value is string) {
            accessMap[byteList.Count] = initSymbol.UniqueName;
            ObjectCodeGenerator.WriteName(fromSymbol.UniqueName);
            byteList.Add((sbyte)0);
            byteList.Add((sbyte) 0);
            textList.Add("\tdw " + initSymbol.UniqueName + " ; 2");
            Start.CurrentTable.AddSymbol(initSymbol);
          }
          else if (initSymbol.HasValue()) {
            GenerateByteTextList(initSymbol, byteList, accessMap, textList);
          }
          else {
            GenerateZeroByteTextList(toType.Size(), byteList, textList);
          }
        }
      }
      else {
        List<object> fromList = (List<object>) fromInit;
      
        switch (toType.GetSort()) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              int toByteListSize = byteList.Count + toType.Size();
              Assert.Error(fromList.Count <= toType.ArraySize,
                           toType, "to many initializers");
              Assert.Warning(fromList.Count == toType.ArraySize,
                             toType, "to few initializers");

              CCompiler.Type subType = toType.ArrayType;           
            
              for (int index = 0; index < fromList.Count; ++index) {
                GenerateStatic(subType, fromList[index], byteList, accessMap, textList);
              }

              GenerateZeroByteTextList(toByteListSize - byteList.Count, byteList, textList);
            }          
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;            
              Assert.Error(fromList.Count <= memberMap.Count,
                           toType, "to many initializers");
              Assert.Warning(fromList.Count == memberMap.Count,
                             toType, "to few initializers");
            
              int toByteListSize = byteList.Count + toType.Size();
              KeyValuePair<string,Symbol>[] memberArray = new KeyValuePair<string,Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              for (int index = 0; index < fromList.Count; ++index)
              {
                Symbol memberSymbol = memberArray[index].Value;
                object init = ModifyInitializer.DoInit(memberSymbol.Type, fromList[index]);
                GenerateStatic(memberSymbol.Type, init, byteList, accessMap, textList);
              }

              GenerateZeroByteTextList(toByteListSize - byteList.Count, byteList, textList);
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType,
                           "a union can only be initalized by one value");
              int toByteListSize = byteList.Count + toType.Size();
              IDictionary<string, Symbol> memberMap = toType.MemberMap;
              KeyValuePair<string, Symbol>[] memberArray = new KeyValuePair<string, Symbol>[memberMap.Count]; ;
              memberMap.CopyTo(memberArray, 0);
              Symbol firstSymbol = memberArray[0].Value;
              object init = ModifyInitializer.DoInit(firstSymbol.Type, fromList[0]);
              GenerateStatic(firstSymbol.Type, init, byteList, accessMap, textList);
              GenerateZeroByteTextList(toByteListSize - byteList.Count, byteList, textList);
            }
            break;
        }
      }
    }
  
    public static void GenerateAuto(List<MiddleCode> codeList, Symbol toSymbol, object fromInit) {
      CCompiler.Type toType = toSymbol.Type;
    
      if (toType.IsArray() && toType.ArrayType.IsChar() &&
          (fromInit is Expression) && ((Expression) fromInit).Symbol().Type.IsString()) {
        fromInit = TextToCharList((string) ((Expression) fromInit).Symbol().Value);
      }
    
      if (fromInit is Expression) {
        Expression initExpr = (Expression) fromInit;
        UpdateExtra(initExpr.LongList());
        codeList.AddRange(initExpr.LongList());      
        Symbol initSymbol = TypeCast.implicitCast(codeList, initExpr.Symbol(), toType);
      
        if (toType.IsFloating()) {
          Start.AddMiddleCode(codeList, MiddleOperator.PopFloat, toSymbol);
        }
        else {
          Start.AddMiddleCode(codeList, MiddleOperator.Assign, toSymbol, initSymbol);
        }

        Extra += toType.Size();
      }
      else {
        List<object> fromList = (List<object>) fromInit;
      
        switch (toType.GetSort()) {
          case Sort.Array: {
              if (toType.ArraySize == 0) {
                toType.ArraySize = fromList.Count;
              }

              Assert.Error(fromList.Count <= toType.ArraySize,
                           toType, "to many initializers");
              Assert.Warning(fromList.Count == toType.ArraySize,
                             toType, "to few initializers");
              CCompiler.Type arrayType = toType.ArrayType;
            
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol subSymbol = new Symbol(arrayType);
                subSymbol.Offset = toSymbol.Offset + (index * arrayType.Size());
                subSymbol.Name = toSymbol.Name + "[" + index + "]";
                //subSymbol.Name = toSymbol.UniqueName + Start.SeparatorId + index;
                GenerateAuto(codeList, subSymbol, fromList[index]);
              }
            }
            break;
          
          case Sort.Struct: {
              IDictionary<string,Symbol> memberMap = toType.MemberMap;            
              Assert.Error(fromList.Count <= memberMap.Count,
                           toType, "to many initializers");
              Assert.Warning(fromList.Count == memberMap.Count,
                             toType, "to few initializers");

              KeyValuePair<string, Symbol>[] memberArray = new KeyValuePair<string, Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              
              for (int index = 0; index < fromList.Count; ++index) {
                Symbol memberSymbol = memberArray[index].Value;
                Symbol subSymbol = new Symbol(memberSymbol.Type);
                subSymbol.Name = toSymbol.Name + Start.SeparatorId + memberSymbol.Name;
                subSymbol.Offset = toSymbol.Offset + memberSymbol.Offset;
                GenerateAuto(codeList, subSymbol, fromList[index]);
              }
            }
            break;
          
          case Sort.Union: {
              Assert.Error(fromList.Count == 1, toType,
                           "a union can pnly be initalized by one value");
              IDictionary<string, Symbol> memberMap = toType.MemberMap;

              KeyValuePair<string, Symbol>[] memberArray = new KeyValuePair<string, Symbol>[memberMap.Count];
              memberMap.CopyTo(memberArray, 0);
              Symbol firstSymbol = memberArray[0].Value;
              Symbol subSymbol = new Symbol(firstSymbol.Type);
              subSymbol.Name = toSymbol.Name + Start.SeparatorId + firstSymbol.Name;
              subSymbol.Offset = toSymbol.Offset;
              GenerateAuto(codeList, subSymbol, fromList[0]);
            }
            break;
        }
      }
    }
  
    public static void GenerateByteTextList(Symbol symbol) {
      Assert.Error(symbol.GetTextList().Count == 0);
      symbol.GetTextList().Add("\n\n" + symbol.UniqueName + ":");
      GenerateByteTextList(symbol, symbol.ByteList, symbol.AccessMap, symbol.GetTextList());
    }
  
    public static void GenerateByteTextList(Symbol symbol, List<sbyte> sbyteList,
                                            IDictionary<int,string> accessMap,
                                            List<string> textList) {
      CCompiler.Type type = symbol.Type;
      object value = symbol.Value;
    
      if (type.IsString()) {
        string text = (string) value;
        StringBuilder buffer = new StringBuilder();

        bool isText = false;
        for (int index = 0; index < text.Length; ++index) {
          char c = text[index];
          int asciiValue = (int) c;
          sbyteList.Add((sbyte) asciiValue);

          if ((c != '\"') && (c != '\'') &&
              (asciiValue >= 32) && (asciiValue <= 126)) {
            buffer.Append((!isText ? ((((index == 0) ? "" : ", ")) + "\"") : "")+
                           ((char) asciiValue));
            isText = true;
          }
          else {
            buffer.Append((isText ? "\"" : "") + ((index == 0) ? "" : ", ") +
                           asciiValue);
            isText = false;
          }
        }

        sbyteList.Add((sbyte) 0);
        buffer.Append((isText ? "\"" : "") + (text.Length > 0 ? ", 0" : "0"));
        textList.Add("\tdb " + buffer.ToString() + "\t; " + (text.Length + 1));
      }
      else if (value is string) {
        sbyteList.Add((sbyte) 0);
        sbyteList.Add((sbyte) 0);
        textList.Add("\tdw " + symbol.UniqueName + "\t; " + CCompiler.Type.PointerSize);
      }
      else if (value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) value;
        string name = staticAddress.Name;
        accessMap.Add(sbyteList.Count, name);
        ObjectCodeGenerator.WriteName(name);
        int offset = staticAddress.Offset;
        sbyteList.Add((sbyte) ((short) offset));
        sbyteList.Add((sbyte) (((short) offset) >> 8));
      
        if (offset > 0) {
          textList.Add("\tdw " + name + " + " + offset + "\t; " + CCompiler.Type.PointerSize);
        }
        else if (offset < 0) {
          textList.Add("\tdw " + name + " - " + (-offset) + "\t; " + CCompiler.Type.PointerSize);
        }
        else {
          textList.Add("\tdw " + name + "\t; " + CCompiler.Type.PointerSize);
        }
      }
      else {
        switch (symbol.Type.GetSort()) {
          case Sort.Signed_Short_Int:
          case Sort.Unsigned_Short_Int: {              
              int intValue = int.Parse(value.ToString());
              sbyte byteValue = (sbyte) ((intValue >= 128) ? (intValue - 256) : intValue);
              sbyteList.Add(byteValue);
              textList.Add("\tdb " + byteValue + "\t; " + type.Size());
            }
            break;

          case Sort.Signed_Char:
          case Sort.Unsigned_Char: {
              int asciiValue = int.Parse(value.ToString());
              sbyteList.Add((sbyte) asciiValue);

              if ((asciiValue >= 32) && (asciiValue <= 126)) {
                textList.Add("\tdb '" + ((char) asciiValue) +
                             "'\t; " + type.Size());
              }
              else {
                textList.Add("\tdb " + asciiValue + "\t; " + type.Size());
              }
            }
            break;

          case Sort.Signed_Int:
          case Sort.Unsigned_Int:
          case Sort.Pointer: {
              Assert.Error(value is BigInteger, "value");
              int intValue = int.Parse(value.ToString());
              sbyteList.Add((sbyte) ((short) intValue));
              sbyteList.Add((sbyte) (((short) intValue) >> 8));
              textList.Add("\tdw " + intValue + "\t; " + type.Size());
            }
            break;

          case Sort.Signed_Long_Int:
          case Sort.Unsigned_Long_Int: {
              List<byte> bList = new List<byte>(((BigInteger) value).ToByteArray());

              while (bList.Count < 4) {
                bList.Add((byte) 0);
              }

              sbyteList.Add((sbyte) bList[0]);
              sbyteList.Add((sbyte) bList[1]);
              sbyteList.Add((sbyte) bList[2]);
              sbyteList.Add((sbyte) bList[3]);
              textList.Add("\tdd " + value.ToString() + "\t; " + type.Size());
            }
            break;

          case Sort.Float: {
              float floatValue = (float)((decimal) value);
              byte[] byteArray = BitConverter.GetBytes(floatValue);
            
              foreach (byte b in byteArray) {
                sbyteList.Add((sbyte) b);
              }

              string text = ((decimal) value).ToString();
              string suffix = text.Contains(".") ? "" : ".0";
              textList.Add("\tdd " + text + suffix + "\t; " + type.Size());
            }
            break;

          case Sort.Double:
          case Sort.Long_Double: {
              double doubleValue = (double) ((decimal) value);
              byte[] byteArray = BitConverter.GetBytes(doubleValue);

              foreach (byte b in byteArray) {
                sbyteList.Add((sbyte) b);
              }

              /*if ((Start.CurrentFunction != null) &&
                  Start.CurrentFunction.Name.Equals("asin")) {
                Console.Out.WriteLine(symbol.UniqueName);
                foreach (byte b in byteArray) {
                  Console.Out.Write(b + " ");
                }
  
                if (symbol.UniqueName.Equals("float8$6.2831853071795864768#")) {
                  Console.Out.WriteLine("X");
                }
                else {
                  Console.Out.WriteLine();
                }

                Console.Out.WriteLine();
              }*/

              string text = ((decimal) value).ToString();
              string suffix = text.Contains(".") ? "" : ".0";
              textList.Add("\tdq " + text + suffix+ "\t; " + type.Size());
            }
            break;        
        }
      }
    }

    public static void GenerateZeroByteTextList(Symbol symbol) {
      Assert.Error(symbol.GetTextList().Count == 0);
      symbol.GetTextList().Add("\n\n" + symbol.UniqueName + ":");
      GenerateZeroByteTextList(symbol.Type.Size(), symbol.ByteList,
                               symbol.GetTextList());
    }
  
    public static void GenerateZeroByteTextList(int size, List<sbyte> byteList,
                                                List<string> textList) {
      Assert.Error(size >= 0);
    
      if (size > 0) {
        StringBuilder buffer = new StringBuilder();

        for (int index = 0; index < size; ++index) {
          byteList.Add((sbyte) 0);
          buffer.Append((index == 0) ? "0" : ", 0");
        }

        textList.Add("\tdb " + buffer.ToString() + "\t; " + size);
      }
    }
  }
}