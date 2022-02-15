using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class ConstantExpression {
    public static Symbol Generate(List<MiddleCode> codeList, MiddleOperator operatorX,
                                  Symbol leftSymbol, Symbol rightSymbol) {
      CCompiler.Type leftType = leftSymbol.Type, rightType = rightSymbol.Type;
    
      if (leftSymbol.HasValue() && rightSymbol.HasValue()) {
        if (leftType.IsLogical() && rightType.IsLogical()) {
          return GenerateLogical(operatorX, leftSymbol, rightSymbol);
        }
        else if ((leftType.IsLogical() || leftType.IsIntegral()) &&
                 (rightType.IsLogical() || rightType.IsIntegral())) {
          return GenerateIntegral(operatorX, leftSymbol, rightSymbol);
        }
        else if ((leftType.IsLogical() || rightType.IsArithmetic()) &&
                 (rightType.IsLogical() || rightType.IsArithmetic())) {
          return GenerateFloating(codeList, operatorX, leftSymbol, rightSymbol);
        }
      }
    
      return null;
    }
  
    public static Symbol GenerateIntegral(MiddleOperator operatorX,
                                          Symbol leftSymbol, Symbol rightSymbol){
      BigInteger leftValue, rightValue, resultValue = BigInteger.Zero;
    
      if (leftSymbol.Type.IsLogical()) {
        leftValue = new BigInteger(((bool) leftSymbol.Value) ? 1 : 0);
      }
      else {
        leftValue = (BigInteger) leftSymbol.Value;
      }
    
      if (rightSymbol.Type.IsLogical()) {
        rightValue = new BigInteger(((bool) rightSymbol.Value) ? 1 : 0);
      }
      else {
        rightValue = (BigInteger) rightSymbol.Value;
      }

      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
          resultValue = leftValue + rightValue;
          break;
        
        case MiddleOperator.BinarySubtract:
          resultValue = leftValue - rightValue;
          break;
        
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.UnsignedMultiply:
          resultValue = leftValue * rightValue;
          break;
        
        case MiddleOperator.SignedDivide:
        case MiddleOperator.UnsignedDivide:
          resultValue = leftValue / rightValue;
          break;
        
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedModulo:
          resultValue = leftValue % rightValue;
          break;
        
        case MiddleOperator.BitwiseIOr:
          resultValue = leftValue | rightValue;
          break;
        
        case MiddleOperator.BitwiseXOr:
          resultValue = leftValue ^ rightValue;
          break;
        
        case MiddleOperator.BitwiseAnd:
          resultValue = leftValue & rightValue;
          break;
        
        case MiddleOperator.ShiftLeft:
          resultValue = leftValue << ((int) rightValue);
          break;
        
        case MiddleOperator.ShiftRight:
          resultValue = leftValue >> ((int) rightValue);
          break;
        
        case MiddleOperator.Equal:
          resultValue = (leftValue == rightValue) ? BigInteger.One : BigInteger.Zero;
          break;

        case MiddleOperator.NotEqual:
          resultValue = (leftValue != rightValue) ? BigInteger.One : BigInteger.Zero;
          break;

        case MiddleOperator.SignedLessThan:
        case MiddleOperator.UnsignedLessThan:
          resultValue = (leftValue < rightValue) ? BigInteger.One : BigInteger.Zero;
          break;

        case MiddleOperator.SignedLessThanEqual:
        case MiddleOperator.UnsignedLessThanEqual:
          resultValue = (leftValue <= rightValue) ? BigInteger.One : BigInteger.Zero;
          break;

        case MiddleOperator.SignedGreaterThan:
        case MiddleOperator.UnsignedGreaterThan:
          resultValue = (leftValue > rightValue) ? BigInteger.One : BigInteger.Zero;
          break;

        case MiddleOperator.SignedGreaterThanEqual:
        case MiddleOperator.UnsignedGreaterThanEqual:
          resultValue = (leftValue >= rightValue) ? BigInteger.One : BigInteger.Zero;
          break;
      }
    
      CCompiler.Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
      return (new Symbol(maxType, CheckUnsigned(resultValue, maxType)));
    }
  
    public static object CheckUnsigned(object value, CCompiler.Type type) {
      if (type.IsUnsigned()) {
        BigInteger intValue = (BigInteger) value;

        if (intValue < BigInteger.Zero) {
          switch (type.Size()) {
            case 1:
              return (intValue + 256);

            case 2:
              return (intValue + 65536);

            case 4:
              return (intValue + 4294967296);
          }
        }

        /*if (intValue < BigInteger.Zero) {
          return (intValue + Type.GetMask(type)) {
          }
          object x = (intValue ^ CCompiler.Type.GetMask(type.GetSort())) + BigInteger.One;
          return (intValue ^ CCompiler.Type.GetMask(type.GetSort())) + BigInteger.One;
        }*/
      }
    
      return value;
    }
  
    public static Symbol GenerateFloating(List<MiddleCode> codeList, MiddleOperator operatorX,
                                          Symbol leftSymbol, Symbol rightSymbol){
      CCompiler.Type leftType = leftSymbol.Type, rightType = rightSymbol.Type;
      decimal leftValue, rightValue, resultValue = 0;

      if (leftSymbol.Type.IsLogical()) {
        leftValue = ((bool) leftSymbol.Value) ? 1 : 0;
      }
      else if (leftSymbol.Type.IsIntegral()) {
        leftValue = (decimal) ((BigInteger) leftSymbol.Value);
      }
      else {
        Start.AddMiddleCode(codeList, MiddleOperator.PopFloat);
        leftValue = (decimal) leftSymbol.Value;
      }

      if (rightSymbol.Type.IsLogical()) {
        rightValue = (decimal) (((bool) rightSymbol.Value) ? 1 : 0);
      }
      if (rightSymbol.Type.IsIntegral()) {
        rightValue = decimal.Parse(rightSymbol.Value.ToString());
      }
      else {
        Start.AddMiddleCode(codeList, MiddleOperator.PopFloat);
        rightValue = decimal.Parse(rightSymbol.Value.ToString());
      }
  
      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
          resultValue = ((decimal) leftValue) + ((decimal) rightValue);
          break;
        
        case MiddleOperator.BinarySubtract:
          resultValue = ((decimal) leftValue) - ((decimal) rightValue);
          break;
        
        case MiddleOperator.SignedMultiply:
          resultValue = ((decimal) leftValue) * ((decimal) rightValue);
          break;
        
        case MiddleOperator.SignedDivide:
          resultValue = ((decimal) leftValue) / ((decimal) rightValue);
          break;

        case MiddleOperator.LogicalOr:
          resultValue = ((((decimal) leftValue) != 0) || (((decimal) rightValue) != 0)) ? 1 : 0;
          break;

        case MiddleOperator.LogicalAnd:
          resultValue = ((((decimal) leftValue) != 0) && (((decimal) rightValue) != 0)) ? 1 : 0;
          break;

        case MiddleOperator.Equal:
          resultValue = (((decimal) leftValue) == ((decimal) rightValue)) ? 1 : 0;
          break;

        case MiddleOperator.NotEqual:
          resultValue = (((decimal) leftValue) != ((decimal) rightValue)) ? 1 : 0;
          break;

        case MiddleOperator.SignedLessThan:
          resultValue = (((decimal) leftValue) < ((decimal) rightValue)) ? 1 : 0;
          break;

        case MiddleOperator.SignedLessThanEqual:
          resultValue = (((decimal) leftValue) <= ((decimal) rightValue)) ? 1 : 0;
          break;

        case MiddleOperator.SignedGreaterThan:
          resultValue = (((decimal) leftValue) > ((decimal) rightValue)) ? 1 : 0;
          break;

        case MiddleOperator.SignedGreaterThanEqual:
          resultValue = (((decimal) leftValue) >= ((decimal) rightValue)) ? 1 : 0;
          break;
      }

      CCompiler.Type maxType = TypeCast.MaxType(leftType, rightType);
      Symbol resultSymbol = new Symbol(maxType, resultValue);
      Start.CurrentTable.AddSymbol(resultSymbol);
      PushFloat(codeList, resultSymbol);
      return resultSymbol;
    }
  
    public static Symbol GenerateLogical(MiddleOperator operatorX,
                                         Symbol leftSymbol, Symbol rightSymbol){
      bool leftValue = (bool ) leftSymbol.Value,
           rightValue = (bool) rightSymbol.Value, resultValue = true;
  
      switch (operatorX) {
        case MiddleOperator.LogicalOr:
          resultValue = leftValue || rightValue;
          break;
        
        case MiddleOperator.LogicalAnd:
          resultValue = leftValue && rightValue;
          break;

        case MiddleOperator.Equal:
          resultValue = (leftValue.CompareTo(rightValue) == 0);
          break;

        case MiddleOperator.NotEqual:
          resultValue = (leftValue.CompareTo(rightValue) != 0);
          break;

      }

      return (new Symbol(CCompiler.Type.LogicalType, resultValue));
    }
  
    public static Symbol Generate(List<MiddleCode> codeList,
                                  MiddleOperator operatorX, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
    
      if (symbol.HasValue()) {
        if (type.IsLogical()) {
          return GenerateLogical(operatorX, symbol);
        }
        else if (type.IsIntegral()) {
          return GenerateIntegral(operatorX, symbol);
        }
        else if (type.IsFloating()) {
          return GenerateFloating(codeList, operatorX, symbol);
        }
      }
    
      return null;
    }
  
    public static Symbol GenerateIntegral(MiddleOperator operatorX, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
      BigInteger value = (BigInteger) symbol.Value, resultValue = BigInteger.Zero;
    
      switch (operatorX) {
        case MiddleOperator.UnaryAdd:
          resultValue = value;
          break;
        
        case MiddleOperator.UnarySubtract:
          resultValue = BigInteger.Negate(value);
          break;

        case MiddleOperator.LogicalNot:
          resultValue = value.Equals(BigInteger.Zero) ? BigInteger.One : BigInteger.Zero;
          break;
        
        case MiddleOperator.BitwiseNot:
          //resultValue = !value;
          //resultValue = BigInteger.BitwiseNot(value); XXX
          break;
      }
    
      return (new Symbol(type, CheckUnsigned(resultValue, type)));
    }    

    public static Symbol GenerateFloating(List<MiddleCode> codeList,
                                          MiddleOperator operatorX, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
      decimal value = (decimal) symbol.Value;
      Symbol resultSymbol = null;
    
      switch (operatorX) {
        case MiddleOperator.UnaryAdd:
          resultSymbol = new Symbol(type, value);
          break;

        case MiddleOperator.UnarySubtract: {
            Start.AddMiddleCode(codeList, MiddleOperator.PopFloat);
            resultSymbol = new Symbol(type, -value);
            Start.CurrentTable.AddSymbol(resultSymbol);
            PushFloat(codeList, resultSymbol);
          }
          break;
      }
    
      return resultSymbol;
    }
  
    public static Symbol GenerateLogical(MiddleOperator operatorX, Symbol symbol) {
      bool value = (bool) symbol.Value;
    
      switch (operatorX) {
        case MiddleOperator.LogicalNot:
          return (new Symbol(CCompiler.Type.LogicalType, !value));
      }
    
      return null;
    }
  
    public static Symbol cast(List<MiddleCode> codeList,
                              Symbol fromSymbol, CCompiler.Type toType) {
      CCompiler.Type fromType = fromSymbol.Type;
      object fromValue = fromSymbol.Value;

      if ((fromValue is BigInteger) || (fromValue is decimal) || (fromValue is bool)) {
        if ((fromType.IsIntegralOrPointer() && toType.IsIntegralOrPointer()) ||
            (fromType.IsFloating() && toType.IsFloating())) {
          return new Symbol(toType, CheckUnsigned(fromValue, toType));
        }
        else if (fromType.IsLogical() && toType.IsLogical()) {
          return new Symbol(toType, CheckUnsigned(fromValue, toType));
        }
        else if (fromType.IsIntegralOrPointer() && toType.IsFloating()) {
          decimal toValue = decimal.Parse(fromValue.ToString());
          Symbol resultSymbol = new Symbol(toType, toValue);
          Start.CurrentTable.AddSymbol(resultSymbol);
          PushFloat(codeList, resultSymbol);
          return resultSymbol;
        }
        else if (fromType.IsIntegralOrPointer() && toType.IsLogical()) {
          object toValue = !fromValue.Equals(BigInteger.Zero);
          return new Symbol(toType, toValue);
        }
        else if (fromType.IsFloating() && toType.IsIntegralOrPointer()) {
          Start.AddMiddleCode(codeList, MiddleOperator.PopFloat);
          object toValue = decimal.Parse(fromValue.ToString());
          return new Symbol(toType, CheckUnsigned(toValue, toType));
        }
        else if (fromType.IsFloating() && toType.IsLogical()) {
          Start.AddMiddleCode(codeList, MiddleOperator.PopFloat);
          object toValue = fromValue.Equals((decimal) 0);
          return new Symbol(toType, toValue);
        }
        else if (fromType.IsLogical() && toType.IsIntegralOrPointer()) {
          object toValue = ((bool) fromValue) ? BigInteger.One : BigInteger.Zero;
          return new Symbol(toType, CheckUnsigned(toValue, toType));
        }
        else if (fromType.IsLogical() && toType.IsFloating()) {        
          if ((bool) fromValue) {
            Start.AddMiddleCode(codeList, MiddleOperator.PushOne);
            return (new Symbol(toType, (decimal) 1));
          }
          else {
            Start.AddMiddleCode(codeList, MiddleOperator.PushZero);
            return (new Symbol(toType, (decimal) 0));
          }
        }
      }

      return null;
    }    

    private static void PushFloat(List<MiddleCode> codeList, Symbol symbol) {
      object value = symbol.Value;
    
      if (value.Equals((decimal) 0)) {
        Start.AddMiddleCode(codeList, MiddleOperator.PushZero);
      }
      else if (value.Equals((decimal) 1)) {
        Start.AddMiddleCode(codeList, MiddleOperator.PushOne);
      }
      else {
        Start.AddMiddleCode(codeList, MiddleOperator.PushFloat, symbol);
      }
    }
  }
}