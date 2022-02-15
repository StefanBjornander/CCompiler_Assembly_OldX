using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class TypeCast {
    public static Symbol naturalCast(List<MiddleCode> codeList, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
    
      if (type.IsArray()) {
        return implicitCast(codeList, symbol, new CCompiler.Type(type.ArrayType));
      }
      else if (type.IsFunction()) {
        return implicitCast(codeList, symbol, new CCompiler.Type(type));
      }

      return symbol;
    }
  
    public static Symbol parameterCast(List<MiddleCode> codeList, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
    
      if (type.IsArrayStringOrFunction()) {
        return symbol;
      }
      else if (type.IsLogical()) {
        return implicitCast(codeList, symbol, CCompiler.Type.SignedIntegerType);
      }
      else if (type.IsChar() || type.IsShort()) {
        if (type.IsSigned()) {
          return implicitCast(codeList, symbol, CCompiler.Type.SignedIntegerType);
        }
        else {
          return implicitCast(codeList, symbol, CCompiler.Type.UnsignedIntegerType);
        }      
      }
      else if (type.IsFloat()) {
        return implicitCast(codeList, symbol, CCompiler.Type.DoubleType);
      }

      return symbol;
    }
  
    public static Symbol parameterCast(List<MiddleCode> codeList, Symbol fromSymbol, CCompiler.Type toType) {
      CCompiler.Type fromType = fromSymbol.Type;
    
      if (fromType.IsArray() && toType.IsPointer() &&
          fromType.ArrayType.Equals(toType.PointerType)) {
        return fromSymbol;
      }
      else if (fromType.IsFunction() && toType.IsPointer() &&
               fromType.Equals(toType.PointerType)) {
        return fromSymbol;
      }
      else if (fromType.IsString() && toType.IsPointer() &&
               toType.PointerType.IsChar()) {
        return fromSymbol;
      }
      else {
        return implicitCast(codeList, fromSymbol, toType);
      }
    }
  
    // int i;
    // double x;
    // x = (double) i + 1;
    // x = i + 1;
  
     public static Symbol implicitCast(List<MiddleCode> codeList, Symbol sourceSymbol, CCompiler.Type targetType) {
      Symbol constantSymbol = ConstantExpression.cast(codeList, sourceSymbol, targetType);

      if (constantSymbol != null) {
        return constantSymbol;
      }

     CCompiler.Type sourceType = sourceSymbol.Type;
   
     if (sourceType.Equals(targetType) ||
         (sourceType.IsFloating() && targetType.IsFloating()) ||
         (sourceType.IsIntegralPointerArrayStringOrFunction() &&
          targetType.IsIntegralPointerArrayStringOrFunction() &&
          (sourceType.ConvertedSize() == targetType.ConvertedSize()))) {
        return sourceSymbol;
      }
      else {
        return explicitCast(codeList, sourceSymbol, targetType);
      }
    }
  
    public static Symbol explicitCast(List<MiddleCode> codeList,
                                      Symbol sourceSymbol, CCompiler.Type targetType) {
      CCompiler.Type sourceType = sourceSymbol.Type;

      if ((sourceType.IsArray() || sourceType.IsString()) && targetType.IsPointer()) {
        Symbol resultSymbol = new Symbol(targetType);
        Start.AddMiddleCode(codeList, MiddleOperator.Assign, resultSymbol, sourceSymbol);
        return resultSymbol;
      }
      else if (sourceType.IsFunction() && targetType.IsPointer()) {
        CCompiler.Type pointerType = new CCompiler.Type(targetType);
        Symbol resultSymbol = new Symbol(pointerType);
        Start.AddMiddleCode(codeList, MiddleOperator.Assign, resultSymbol, sourceSymbol);
        return resultSymbol;
      }
      else if (sourceType.IsLogical() && targetType.IsIntegralOrPointer()) {
        Symbol resultSymbol = new Symbol(targetType);

        Symbol oneSymbol = new Symbol(targetType, BigInteger.One);      
        MiddleCode assignTrue = Start.AddMiddleCode(codeList, MiddleOperator.Assign, resultSymbol, oneSymbol);
        Start.Backpatch(sourceSymbol.trueSet(), assignTrue);
      
        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(codeList, MiddleOperator.Goto, targetCode);

        Symbol zeroSymbol = new Symbol(targetType, BigInteger.Zero);
        MiddleCode assignFalse = Start.AddMiddleCode(codeList, MiddleOperator.Assign, resultSymbol, zeroSymbol);
        Start.Backpatch(sourceSymbol.falseSet(), assignFalse);
      
        codeList.Add(targetCode);
        return resultSymbol;
      }
      else if (sourceType.IsLogical() && targetType.IsFloating()) {
        Symbol resultSymbol = new Symbol(targetType);
        Start.Backpatch(sourceSymbol.trueSet(), Start.AddMiddleCode(codeList, MiddleOperator.PushOne));
        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(codeList, MiddleOperator.Goto, targetCode);
        Start.Backpatch(sourceSymbol.trueSet(), Start.AddMiddleCode(codeList, MiddleOperator.PushZero));
        codeList.Add(targetCode);
        return resultSymbol;
      }
      else if (sourceType.IsIntegralPointerArrayStringOrFunction() && targetType.IsLogical()) {
        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();                      
        Symbol resultSymbol = new Symbol(trueSet, falseSet);
      
        CCompiler.Type zeroType;
      
        if (sourceType.IsArray()) {
          zeroType = new CCompiler.Type(sourceType.ArrayType);
        }
        else if (sourceType.IsString()) {
          zeroType = new CCompiler.Type(CCompiler.Type.SignedCharType);
        }
        else if (sourceType.IsFunction()) {
          zeroType = new CCompiler.Type(sourceType);
        }
        else {
          zeroType = sourceType;
        }
      
        Symbol zeroSymbol = new Symbol(zeroType, BigInteger.Zero);
        trueSet.Add(Start.AddMiddleCode(codeList, MiddleOperator.NotEqual, null, sourceSymbol, zeroSymbol));
        falseSet.Add(Start.AddMiddleCode(codeList, MiddleOperator.Goto));
        return resultSymbol;
      }
      else if (sourceType.IsFloating() && targetType.IsLogical()) {
        ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();                      
        Symbol resultSymbol = new Symbol(trueSet, falseSet);
        Start.AddMiddleCode(codeList, MiddleOperator.PushZero);
        trueSet.Add(Start.AddMiddleCode(codeList, MiddleOperator.NotEqual, null, sourceSymbol));
        falseSet.Add(Start.AddMiddleCode(codeList, MiddleOperator.Goto));
        return resultSymbol;
      }
      else if (sourceType.IsIntegralPointerArrayStringOrFunction() && targetType.IsIntegralOrPointer()) {
        Symbol resultSymbol = new Symbol(targetType);
        MiddleCode convertCode = new MiddleCode(MiddleOperator.IntegralToIntegral, resultSymbol, sourceSymbol);
        codeList.Add(convertCode);
        return resultSymbol;
      }
      else if (sourceType.IsFloating() && targetType.IsFloating()) {
        return (new Symbol(targetType));
      }
      else if (sourceType.IsArithmeticPointerArrayOrFunction() && targetType.IsFloating()) {
        Symbol resultSymbol = new Symbol(targetType);
      
        if (sourceType.Size() == 1) {
          CCompiler.Type tempType = sourceType.IsSigned() ? CCompiler.Type.SignedIntegerType
                                                : CCompiler.Type.UnsignedIntegerType;
          Symbol tempSymbol = new Symbol(tempType);
          Start.AddMiddleCode(codeList, MiddleOperator.IntegralToIntegral, tempSymbol, sourceSymbol);
          Start.AddMiddleCode(codeList, MiddleOperator.IntegralToFloating, resultSymbol, tempSymbol);
        }
        else {
          Start.AddMiddleCode(codeList, MiddleOperator.IntegralToFloating, resultSymbol, sourceSymbol);
        }

        return resultSymbol;
      }
      else if (sourceType.IsFloating() && targetType.IsIntegralOrPointer()) {
        Symbol resultSymbol = new Symbol(targetType);

        if (targetType.Size() == 1) {
          CCompiler.Type tempType = sourceType.IsSigned() ? CCompiler.Type.SignedIntegerType
                                                : CCompiler.Type.UnsignedIntegerType;
          Symbol tempSymbol = new Symbol(tempType);
          MiddleCode tempCode = new MiddleCode(MiddleOperator.FloatingToIntegral, tempSymbol, sourceSymbol);
          codeList.Add(tempCode);
          MiddleCode resultCode = new MiddleCode(MiddleOperator.IntegralToIntegral, resultSymbol, tempSymbol);
          codeList.Add(resultCode);
        }
        else {
          MiddleCode resultCode = new MiddleCode(MiddleOperator.FloatingToIntegral, resultSymbol, sourceSymbol);
          codeList.Add(resultCode);
        }

        return resultSymbol;
      }
      else if (targetType.IsVoid()) {
        return new Symbol(CCompiler.Type.VoidType);
      }
      else {
        Assert.Error("Cannot convert from " + sourceType + " to " + targetType);
        return null;
      }
    }
  
      public static Type MaxType(Type leftType, Type rightType) {
      /*Assert.Warning((leftType.IsSigned() && rightType.IsSigned()) ||
                     (!leftType.IsSigned() && !rightType.IsSigned()),
                     null, Message.Mixing_signed_and_unsigned_types);*/

      if ((leftType.IsFloating() && !rightType.IsFloating()) ||
          ((leftType.Size() == rightType.Size()) && leftType.IsSigned() && !rightType.IsSigned())) {
        return leftType;
      }
      else if ((!leftType.IsFloating() && rightType.IsFloating()) ||
               ((leftType.Size() == rightType.Size()) && !leftType.IsSigned() && rightType.IsSigned())) {
        return rightType;
      }
      else {
        return (leftType.Size() > rightType.Size()) ? leftType : rightType;
      }
    }

/*    public static CCompiler.Type MaxType(CCompiler.Type leftType, CCompiler.Type rightType) {
      if (leftType.IsLogical() && rightType.IsLogical()) {
        return CCompiler.Type.SignedIntegerType;
      }
      else if (leftType.IsLogical() && rightType.IsArithmeticPointerOrArray()) {
        return rightType;
      }
      else if (leftType.IsArithmeticPointerOrArray() && rightType.IsLogical()) {
        return leftType;
      }
      else if (leftType.IsString() && rightType.IsString()) {
        CCompiler.Type charType = new CCompiler.Type(Sort.Signed_Char);
        charType.Constant = true;
        CCompiler.Type pointerType = new CCompiler.Type(charType);
        pointerType.Constant = true;
        return pointerType;
      }
      else if (leftType.IsString() &&
               rightType.IsPointer() && rightType.PointerType.IsChar()) {
        CCompiler.Type pointerType = new CCompiler.Type(rightType.PointerType);
        pointerType.Constant = true;
        return pointerType;
      }
      else if (leftType.IsPointer() && leftType.PointerType.IsChar() &&
               rightType.IsString()) {
        CCompiler.Type pointerType = new CCompiler.Type(leftType.PointerType);
        pointerType.Constant = true;
        return pointerType;
      }
      else if (leftType.IsPointer() && rightType.IsIntegralOrArray()) {
        return leftType;
      }
      else if (leftType.IsIntegralOrArray() && rightType.IsPointer()) {
        return rightType;
      }
      else if (leftType.IsArray() && rightType.IsIntegral()) {
        return new CCompiler.Type(leftType.ArrayType);
      }
      else if (leftType.IsIntegral() && rightType.IsArray()) {
        return new CCompiler.Type(rightType.ArrayType);
      }
      else if (leftType.IsIntegral() && rightType.IsIntegral()) {
        int leftSize = leftType.Size(), rightSize = rightType.Size();

        if (leftSize == rightSize) {
          if (leftType.IsUnsigned() && rightType.IsUnsigned()) {
            return leftType;
          }
          else {
            Assert.Warning((leftType.IsSigned() && rightType.IsSigned()) ||
                           (leftType.IsUnsigned() && rightType.IsUnsigned()),
                           null, "mixing signed and unsigned types");
            return leftType.IsSigned() ? leftType : rightType;
          }
        }
        else {
          return (leftSize >= rightSize) ? leftType : rightType;
        }
      }
      else if (leftType.IsFloating() && rightType.IsFloating()) {
        int leftSize = leftType.Size(), rightSize = rightType.Size();
        return (leftSize >= rightSize) ? leftType : rightType;
      }
      else if (leftType.IsIntegral() && rightType.IsFloating()) {
        return rightType;
      }
      else if (leftType.IsFloating() && rightType.IsIntegral()) {
        return leftType;
      }
      else if (leftType.IsIntegral() && rightType.IsFloating()) {
        return rightType;
      }
      else if (leftType.IsFloating() && rightType.IsIntegral()) {
        return leftType;
      }
      else if (leftType.IsPointer() && leftType.PointerType.IsVoid() &&
               rightType.IsPointer()) {
        return leftType;
      }
      else if (leftType.IsPointer() &&
               rightType.IsPointer() && rightType.PointerType.IsVoid()) {
        return rightType;
      }
      else if (leftType.IsPointer() && rightType.IsPointer() &&
               leftType.PointerType.Size() == rightType.PointerType.Size()) {
        return leftType;
      }
      else if (leftType.IsPointer() && rightType.IsArray()) {
        return leftType;
      }
      else if (leftType.IsArray() && rightType.IsPointer()) {
        return rightType;
      }
    
      return null;
    }*/
  }
}