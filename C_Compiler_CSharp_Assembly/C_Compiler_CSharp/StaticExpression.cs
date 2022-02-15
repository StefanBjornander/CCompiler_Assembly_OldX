using System.Numerics;

namespace CCompiler {
  public class StaticExpression {
    public static Symbol Generate(MiddleOperator operatorX, Symbol leftSymbol, Symbol rightSymbol) {
      CCompiler.Type leftType = leftSymbol.Type, rightType = rightSymbol.Type;
      object leftValue = leftSymbol.Value, rightValue = rightSymbol.Value;
    
      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
          if (leftType.IsPointer() && !leftType.PointerType.IsVoid() &&
              (leftValue is StaticAddress) &&
              rightType.IsIntegral() && rightSymbol.HasValue()) {
            StaticAddress staticAddress = (StaticAddress) leftValue;
            int indexValue = int.Parse(rightValue.ToString());
            int resultOffset = indexValue * leftType.PointerType.Size();
            object resultValue = new StaticAddress(staticAddress.Name,
                                     staticAddress.Offset + resultOffset);
            return new Symbol(leftType, resultValue);
          }

          if (leftType.IsIntegral() && leftSymbol.HasValue() &&
              (rightValue is StaticAddress) &&
              rightType.IsPointer() && !rightType.PointerType.IsVoid()) {
            StaticAddress staticAddress = (StaticAddress) rightValue;
            int indexValue = int.Parse(leftValue.ToString()); 
            int resultOffset = indexValue * rightType.PointerType.Size();
            object resultValue = new StaticAddress(staticAddress.Name,
                                     staticAddress.Offset + resultOffset);
            return new Symbol(rightType, resultValue);
          }

          if (leftType.IsArray() && rightType.IsIntegral() &&
              rightSymbol.HasValue()) {
                int indexValue = int.Parse(rightValue.ToString());
            int resultOffset = indexValue * leftType.ArrayType.Size();
            object resultValue = new StaticAddress(leftSymbol.UniqueName, resultOffset);
            return new Symbol(leftType, resultValue);
          }

          if (leftType.IsIntegral() && leftSymbol.HasValue() && rightType.IsArray()) {
            int indexValue = int.Parse(leftValue.ToString());
            int resultOffset = indexValue * rightType.ArrayType.Size();
            object resultValue = new StaticAddress(rightSymbol.UniqueName, resultOffset);
            return new Symbol(rightType, resultValue);
          }
          break;

        case MiddleOperator.BinarySubtract:
          if (leftType.IsPointer() && !leftType.PointerType.IsVoid() &&
              (leftValue is StaticAddress) &&
              rightType.IsIntegral() && rightSymbol.HasValue()) {
            StaticAddress staticAddress = (StaticAddress) leftValue;
            int indexValue = int.Parse(rightValue.ToString());
            int resultOffset = indexValue * leftType.PointerType.Size();
            object resultValue = new StaticAddress(staticAddress.Name,
                                     staticAddress.Offset - resultOffset);
            return new Symbol(leftType, resultValue);
          }

          if (leftType.IsArray() && rightType.IsIntegral() && rightSymbol.HasValue()) {
            int indexValue = int.Parse(rightValue.ToString()); 
            int resultOffset = indexValue * leftType.ArrayType.Size();
            object resultValue = new StaticAddress(leftSymbol.UniqueName, -resultOffset);
            return new Symbol(leftType, resultValue);
          }
          break;

        case MiddleOperator.Index:
          if (leftSymbol.IsStaticOrExtern() && leftType.IsArray() && (rightValue is BigInteger)) { // a[2]
            int resultOffset = ((int) ((BigInteger) rightValue)) * leftType.ArrayType.Size();
            StaticValue resultValue = new StaticValue(leftSymbol.UniqueName, resultOffset);
            Symbol resultSymbol = new Symbol(leftType, resultValue);
            resultSymbol.SetAddressable(true);
            resultSymbol.SetAssignable(true);
            /*resultSymbol.Addressable = !leftSymbol.IsRegister() && !leftType.ArrayType.IsBitfield();
            resultSymbol.Assignable = !leftType.ArrayType.IsConstantRecursive() &&
                                      !leftType.ArrayType.IsArrayFunctionOrString();*/
            return resultSymbol;
          }
          else if ((leftValue is BigInteger) && rightSymbol.IsStaticOrExtern() && rightType.IsArray()) { // [2]a
            int resultOffset = ((int) ((BigInteger) leftValue)) * rightType.ArrayType.Size();
            StaticValue resultValue = new StaticValue(rightSymbol.UniqueName, resultOffset);
            Symbol resultSymbol = new Symbol(rightType, resultValue);
            resultSymbol.SetAddressable(true);
            resultSymbol.SetAssignable(true);
            /*resultSymbol.Addressable = !leftSymbol.IsRegister() && !leftType.ArrayType.IsBitfield();
            resultSymbol.Assignable = !leftType.ArrayType.IsConstantRecursive() &&
                                      !leftType.ArrayType.IsArrayFunctionOrString();*/
            return resultSymbol;
          }
          break;


        case MiddleOperator.Dot:
          if (leftSymbol.IsStaticOrExtern()) {
            object resultValue =
              new StaticValue(leftSymbol.UniqueName, rightSymbol.Offset);
            return new Symbol(leftType, resultValue);
          }
          break;
      }
    
      return null;
    }  

    public static Symbol Generate(MiddleOperator operatorX, Symbol symbol) {
      CCompiler.Type type = symbol.Type;
    
      switch (operatorX) {
        case MiddleOperator.address:
          if (symbol.IsStaticOrExtern()) {
            CCompiler.Type pointerType = new CCompiler.Type(type);
            StaticAddress staticAddress = new StaticAddress(symbol.UniqueName, 0);
            //resultSymbol.Value = staticAddress;
            Symbol resultSymbol = new Symbol(pointerType, staticAddress);
            return resultSymbol;
          }
        
          if (symbol.HasValue() && (symbol.Value is StaticValue)) {
            CCompiler.Type pointerType = new CCompiler.Type(type);
            StaticValue staticValue = (StaticValue) symbol.Value;
            StaticAddress staticAddress =
              new StaticAddress(staticValue.Name, staticValue.Offset());
            //resultSymbol.Value = staticAddress;
            Symbol resultSymbol = new Symbol(pointerType, staticAddress);
            return resultSymbol;
          }
          break;
        
        case MiddleOperator.Deref:
          if (type.IsPointer() && !type.PointerType.IsVoid() && symbol.IsStaticOrExtern()) {
            Symbol resultSymbol = new Symbol(type.PointerType);
            StaticValue staticValue = new StaticValue(symbol.UniqueName, 0);
            resultSymbol.Value = staticValue;
            return resultSymbol;
          }

          if (symbol.HasValue() && (symbol.Value is StaticAddress)) {
            Symbol resultSymbol = new Symbol(type.PointerType);
            StaticAddress staticAddress = (StaticAddress) symbol.Value;
            StaticValue staticValue =
              new StaticValue(staticAddress.Name, staticAddress.Offset);
            resultSymbol.Value = staticAddress;
            return resultSymbol;
          }
          break;
      }
    
      return null;
    }  
  }
}