using System;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCode {
    private MiddleOperator m_middleOperator;
    private object[] m_operandArray = new object[4];
    private SymbolTable m_symbolTable;

    public MiddleCode(MiddleOperator middleOp)
     :this(middleOp, null, null, null, null) {
        // Empty.
    }
  
    public MiddleCode(MiddleOperator middleOp, object operand1)
     :this(middleOp, operand1, null, null, null) {
        // Empty.
    }
  
    public MiddleCode(MiddleOperator middleOp, object operand1,
                      object operand2)
     :this(middleOp, operand1, operand2, null, null) {
        // Empty.
    }
  
    public MiddleCode(MiddleOperator middleOp, object operand0,
                      object operand1, object operand2)
     :this(middleOp, operand0, operand1, operand2,  null) {
        // Empty.
    }

    public MiddleCode(MiddleOperator middleOp, object operand0,
                      object operand1, object operand2, object operand3) {
      m_middleOperator = middleOp;
      m_operandArray[0] = operand0;
      m_operandArray[1] = operand1;
      m_operandArray[2] = operand2;
      m_operandArray[3] = operand3;
      m_symbolTable = Start.CurrentTable;
    }

    public SymbolTable SymbolTable() {
      return m_symbolTable;
    }

    public MiddleOperator Operator {
      get {
        return m_middleOperator;
      }

      set {
        m_middleOperator = value;
      }
    }

    public object GetOperand(int index) {
      return m_operandArray[index];
    }

    public void SetOperand(int index, object operand) {
      m_operandArray[index] = operand;
    }

    public void Clear() {
      m_middleOperator = MiddleOperator.Empty;
      m_operandArray[0] = null;
      m_operandArray[1] = null;
      m_operandArray[2] = null;
    }

    public bool IsGoto() {
      return (m_middleOperator == MiddleOperator.Goto);
    }

    public bool IsMemory() {
      switch (m_middleOperator) {
        case MiddleOperator.Deref:
        case MiddleOperator.Arrow:
        case MiddleOperator.Index:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsCarry() {
      return IsCarry(m_middleOperator);
    }
  
    public static bool IsCarry(MiddleOperator middleOperator) {
      return (middleOperator == MiddleOperator.Carry) ||
             (middleOperator == MiddleOperator.NotCarry);
    }

    public bool IsRelation() {
      return IsRelation(m_middleOperator);
    }
  
    public static bool IsRelation(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.Case:
        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
        case MiddleOperator.SignedLessThan:
        case MiddleOperator.SignedLessThanEqual:
        case MiddleOperator.SignedGreaterThan:
        case MiddleOperator.SignedGreaterThanEqual:
        case MiddleOperator.UnsignedLessThan:
        case MiddleOperator.UnsignedLessThanEqual:
        case MiddleOperator.UnsignedGreaterThan:
        case MiddleOperator.UnsignedGreaterThanEqual:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsUnary() {
      return IsUnary(m_middleOperator);
    }
  
    public bool IsUnaryX() {
      return IsUnaryX(m_middleOperator);
    }
  
    public static bool IsIncrement(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.Increment:
        case MiddleOperator.Decrement:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsBinaryArithmetic() {
      switch (m_middleOperator) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsModulo() {
      return IsModulo(m_middleOperator);
    }
  
    public static bool IsModulo(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedModulo:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsCommutative() {
      return IsCommutative(m_middleOperator);
    }
  
    public bool IsAssociative() {
      return IsRelation(m_middleOperator);
    }
  
    public static bool IsAssociative(MiddleOperator middleOperator) {
      switch (middleOperator) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.BitwiseIOr:
        case MiddleOperator.BitwiseXOr:
          return true;

        default:
          return false;
      }
    }

    public bool IsStrictlyUnary() {
      return IsStrictlyUnary(m_middleOperator);
    }
  
    public static bool IsStrictlyUnary(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
          return true;
        
        default:
          return false;
      }
    }

    public static bool IsUnaryX(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
          return true;

        default:
          return false;
      }
    }

    public static bool IsUnary(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
          return true;

        case MiddleOperator.LogicalNot:
          return true;
        
        case MiddleOperator.Assign:
        case MiddleOperator.PushFloat:
        case MiddleOperator.TopFloat:
        case MiddleOperator.PopFloat:
        case MiddleOperator.Deref:
        case MiddleOperator.address:
        case MiddleOperator.IntegralToIntegral:
        case MiddleOperator.IntegralToFloating:
        case MiddleOperator.FloatingToIntegral:
        case MiddleOperator.FunctionToPointer:
        case MiddleOperator.ArrayToPointer:
        case MiddleOperator.StringToPointer:
        case MiddleOperator.Parameter:
        case MiddleOperator.EqualZero:
        case MiddleOperator.Increment:
        case MiddleOperator.Decrement:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsPostfix() {
      return IsPostfix(m_middleOperator);
    }
  
    public static bool IsPostfix(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.Increment:
        case MiddleOperator.Decrement:
        case MiddleOperator.EqualZero:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsShift() {
      return IsShift(m_middleOperator);
    }
  
    public static bool IsShift(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return true;
        
        default:
          return false;
      }
    }

    public static bool IsCommutative(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.BitwiseIOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.BitwiseAnd:
          return true;
        
        default:
          return false;
      }
    }
  
    public bool IsBinary() {
      return IsBinary(m_middleOperator);
    }
  
    public static bool IsArithmetic(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
          return true;
        
        default:
          return false;
      }
    }

    public static bool IsBinary(MiddleOperator operatorX) {
      switch (operatorX) {
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
        case MiddleOperator.LogicalOr:
        case MiddleOperator.LogicalAnd:
        case MiddleOperator.BitwiseIOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return true;
        
        default:
          return false;
      }
    }

    public bool IsRelationCarryOrGoto() {
      return IsRelation() || IsCarry() || IsGoto();
    }
  
  /*  public static IDictionary<MiddleOperator,MiddleOperator> InverseMap = new Dictionary<>();

    static {
      InverseMap.Add(MiddleOperator.Equal, MiddleOperator.NotEqual);
      InverseMap.Add(MiddleOperator.NotEqual, MiddleOperator.Equal);
      InverseMap.Add(MiddleOperator.SignedLessThan, MiddleOperator.SignedGreaterThanEqual);
      InverseMap.Add(MiddleOperator.SignedLessThanEqual, MiddleOperator.SignedGreaterThan);
      InverseMap.Add(MiddleOperator.SignedGreaterThan, MiddleOperator.SignedLessThanEqual);
      InverseMap.Add(MiddleOperator.SignedGreaterThanEqual, MiddleOperator.SignedLessThan);
      InverseMap.Add(MiddleOperator.UnsignedLessThan, MiddleOperator.UnsignedGreaterThanEqual);
      InverseMap.Add(MiddleOperator.UnsignedLessThanEqual, MiddleOperator.UnsignedGreaterThan);
      InverseMap.Add(MiddleOperator.UnsignedGreaterThan, MiddleOperator.UnsignedLessThanEqual);
      InverseMap.Add(MiddleOperator.UnsignedGreaterThanEqual, MiddleOperator.UnsignedLessThan);
    }*/

    private static IDictionary<MiddleOperator, string> OpToTextMap = new Dictionary<MiddleOperator, string>();
  
    static MiddleCode() {
      OpToTextMap.Add(MiddleOperator.Assign, "=");
      OpToTextMap.Add(MiddleOperator.BinaryAdd, "+");
      OpToTextMap.Add(MiddleOperator.BinarySubtract, "-");
      OpToTextMap.Add(MiddleOperator.SignedMultiply, "*");
      OpToTextMap.Add(MiddleOperator.SignedDivide, "/");
      OpToTextMap.Add(MiddleOperator.SignedModulo, "%");
      OpToTextMap.Add(MiddleOperator.UnsignedMultiply, "*");
      OpToTextMap.Add(MiddleOperator.UnsignedDivide, "/");
      OpToTextMap.Add(MiddleOperator.UnsignedModulo, "%");
      OpToTextMap.Add(MiddleOperator.BitwiseIOr, "|");
      OpToTextMap.Add(MiddleOperator.BitwiseXOr, "^");
      OpToTextMap.Add(MiddleOperator.BitwiseAnd, "&");
      OpToTextMap.Add(MiddleOperator.ShiftLeft, "<<");
      OpToTextMap.Add(MiddleOperator.ShiftRight, ">>");
      OpToTextMap.Add(MiddleOperator.LogicalOr, "||");
      OpToTextMap.Add(MiddleOperator.LogicalAnd, "&&");
      OpToTextMap.Add(MiddleOperator.Increment, "++");
      OpToTextMap.Add(MiddleOperator.Decrement, "--");
      OpToTextMap.Add(MiddleOperator.EqualZero, "==0");
      OpToTextMap.Add(MiddleOperator.Equal, "==");
      OpToTextMap.Add(MiddleOperator.NotEqual, "!=");
      OpToTextMap.Add(MiddleOperator.SignedLessThan, "<");
      OpToTextMap.Add(MiddleOperator.SignedLessThanEqual, "<=");
      OpToTextMap.Add(MiddleOperator.SignedGreaterThan, ">");
      OpToTextMap.Add(MiddleOperator.SignedGreaterThanEqual, ">=");
      OpToTextMap.Add(MiddleOperator.UnsignedLessThan, "<");
      OpToTextMap.Add(MiddleOperator.UnsignedLessThanEqual, "<=");
      OpToTextMap.Add(MiddleOperator.UnsignedGreaterThan, ">");
      OpToTextMap.Add(MiddleOperator.UnsignedGreaterThanEqual, ">=");
      OpToTextMap.Add(MiddleOperator.UnaryAdd, "+");
      OpToTextMap.Add(MiddleOperator.UnarySubtract, "-");
      OpToTextMap.Add(MiddleOperator.LogicalNot, "!");
      OpToTextMap.Add(MiddleOperator.BitwiseNot, "~");
      OpToTextMap.Add(MiddleOperator.address, "&");
      OpToTextMap.Add(MiddleOperator.Deref, "*");
      OpToTextMap.Add(MiddleOperator.Dot, ".");
      OpToTextMap.Add(MiddleOperator.Arrow, "->");
      OpToTextMap.Add(MiddleOperator.Comma, ",");
      OpToTextMap.Add(MiddleOperator.IntegralToIntegral, "int_to_int ");
      OpToTextMap.Add(MiddleOperator.IntegralToFloating, "int_to_float ");
      OpToTextMap.Add(MiddleOperator.FloatingToIntegral, "float_to_int ");
    }
 
    private string opToText() {
      if (OpToTextMap.ContainsKey(m_middleOperator)) {
        if (IsUnary()) {
          return OpToTextMap[m_middleOperator];
        }
        else {
          return " " + OpToTextMap[m_middleOperator] + " ";
        }
      }
      else {
        return Enum.GetName(typeof(MiddleOperator), m_middleOperator).ToLower() + " ";
      }
    }
  
    private string opToTextX() {
      if (OpToTextMap.ContainsKey(m_middleOperator)) {
        if (IsUnary()) {
          return OpToTextMap[m_middleOperator];
        }
        else {
          return OpToTextMap[m_middleOperator];
        }
      }
      else {
        return Enum.GetName(typeof(MiddleOperator), m_middleOperator).ToLower();
      }
    }
  
    public override string ToString() {
      /*if (m_middleOperator == MiddleOperator.IntegralToIntegral) {
        int toSize = ((Symbol) m_operandArray[0]).Type.Size(),
            fromSize = ((Symbol) m_operandArray[1]).Type.Size();
        return m_middleOperator + " " + fromSize + " -> " +
               toSize + " " + ToString(m_operandArray[0]) +
               ToString(m_operandArray[1]) + ToString(m_operandArray[2]);
            
      }
      else*/ {
        return m_middleOperator + ToString(m_operandArray[0]) +
               ToString(m_operandArray[1]) + ToString(m_operandArray[2]);
      }
    }

    public string ToString(object value) {
      if (value != null) {
        return (" "  + value.ToString().Replace("\n", "\\n"));
      }
      else {
        return "";
      }
    }

    public /*override*/ string ToStringX() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];
           
      Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
             symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
             symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;         
    
      switch (m_middleOperator) {
        case MiddleOperator.FunctionStart:
          return "function start " + operand0;

        case MiddleOperator.FunctionEnd:
          return "function end " + operand0;
        
        case MiddleOperator.CallHeader:
          if (operand1 != null) {
            int setSize = ((ISet<Symbol>) operand1).Count,
                stackSize = (int) operand2;
            return "call header integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize +
                   " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "call header";
          }
        
        /*case MiddleOperator.SaveTemporary:
          return "save temparary " + operand0;*/

        case MiddleOperator.Call:
          if (Start.CurrentFunction.Type.IsEllipse() && symbol1.Type.IsEllipse()) {
            return "call function ellipse-ellipse " + symbol1 + ", extra " + operand2;
          }
          else if (Start.CurrentFunction.Type.IsEllipse()) {
            return "call function ellipse-noellipse " + symbol1;
          }
          else if (symbol1.Type.IsEllipse() ) {
            return "call function noellipse-ellipse " + symbol1 + ", extra " + operand2;
          }
          else {
            return "call function noellipse-noellipse " + symbol1;
          }

        case MiddleOperator.PostCall:
          if (operand0 != null) {
            int setSize = ((ISet<Symbol>) operand0).Count,
                stackSize = (int) operand1;
            return "post call integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize +
                   " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "post call";
          }

        /*case MiddleOperator.PreCall:
          if (operand1 != null) {
            int stackSize = (int) operand1,
                setSize = ((ISet<Symbol>) operand2).Count;
            return "pre call record " + operand0.ToString() + " stack " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize +" integral " + ((setSize != 0) ? "no zero" : "zero") + " " + setSize;
          }
          else {
            return "pre call";
          }

        case MiddleOperator.PostCallIntegral:
          if (operand0 != null) {
            int stackSize = ((ISet<Symbol>)operand2).Count;
            return "post call integral  " + ((stackSize != 0) ? "no zero" : "zero") + " " + stackSize;
          }
          else {
            return "post call integral";
          }

        case MiddleOperator.PostCallFloating:
          if (operand1 != null) {
            int size = (int) operand1;
            return "post call floating record " + operand0.ToString() +
                   " stack " + ((size != 0) ? "no zero" : "zero") + " " + size +
                   " return float" + (((bool) operand2) ? "true" : "false");
          }
          else {
            return "post call floating";
          }*/

        case MiddleOperator.Parameter:
          return "parameter " + operand1 + ", offset " + operand2;

        case MiddleOperator.Return:
         return "return";

        /*case MiddleOperator.Exit:
          return "exit " + operand1;*/
        
        case MiddleOperator.IntegralToIntegral:
        case MiddleOperator.IntegralToFloating:
        case MiddleOperator.FloatingToIntegral:
          return operand0 + " = " + opToText() + operand1 +
                        " (" + Enum.GetName(typeof(Sort), symbol1.Type.GetSort()) +
                        " -> " + Enum.GetName(typeof(Sort), symbol0.Type.GetSort()) + ")";

        case MiddleOperator.UnaryAdd:
        case MiddleOperator.UnarySubtract:
        case MiddleOperator.BitwiseNot:
        case MiddleOperator.address:        
          return operand0 + " = " + opToText() + operand1;

        case MiddleOperator.Carry:
        case MiddleOperator.NotCarry:
          return "if " + opToText() + "goto " + operand0;

        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
        case MiddleOperator.SignedLessThan:
        case MiddleOperator.SignedLessThanEqual:
        case MiddleOperator.SignedGreaterThan:
        case MiddleOperator.SignedGreaterThanEqual:
        case MiddleOperator.UnsignedLessThan:
        case MiddleOperator.UnsignedLessThanEqual:
        case MiddleOperator.UnsignedGreaterThan:
        case MiddleOperator.UnsignedGreaterThanEqual:
          if (operand0 is int) {
            return "if " + operand1 + opToText() + operand2 + " goto " + operand0;
          }
          else if (operand0 is MiddleCode) {
            return "if " + operand1 + opToText() + operand2 + " goto <MiddleCode>";
          }
          else {
            return "if " + operand1 + opToText() + operand2 + " goto <null>";
          }

        case MiddleOperator.Goto:
          if (operand0 is int) {
            return "goto " + operand0;
          }
          else if (operand0 is MiddleCode) {
            return "goto <MiddleCode>";
          }
          else {
            return "goto <null>";
          }

        case MiddleOperator.Empty:
          return "empty";

        case MiddleOperator.Assign:
          return operand0 + " " + opToText() + " " + operand1;
  
        case MiddleOperator.BinaryAdd:
        case MiddleOperator.BinarySubtract:
        case MiddleOperator.SignedMultiply:
        case MiddleOperator.SignedDivide:
        case MiddleOperator.SignedModulo:
        case MiddleOperator.UnsignedMultiply:
        case MiddleOperator.UnsignedDivide:
        case MiddleOperator.UnsignedModulo:
        case MiddleOperator.BitwiseAnd:
        case MiddleOperator.BitwiseIOr:
        case MiddleOperator.BitwiseXOr:
        case MiddleOperator.ShiftLeft:
        case MiddleOperator.ShiftRight:
          return operand0 + " = " + operand1 + opToText() + operand2;

        case MiddleOperator.Case:
          return "case " + operand1.ToString() + " == " + operand2.ToString() + " goto " + operand0.ToString();

        case MiddleOperator.CaseEnd:
          return "case end " + operand0.ToString();

        case MiddleOperator.Increment:
        case MiddleOperator.Decrement:
          return opToText() + operand1;
        
        case MiddleOperator.AssignRegister:
        //case MiddleOperator.SaveFromRegister:
          return operand0 + " = " + operand1;
        
        case MiddleOperator.Interrupt:
          return "interrupt " + operand0;
        
        case MiddleOperator.GetReturnValue:
          return operand0 + " = return_value";
        
        case MiddleOperator.SetReturnValue:
          return "return_value = " + operand1;
        
        case MiddleOperator.Deref:
          if (((int) operand2) != 0) {
            return operand0 + " = *" + operand1 + ", offset " + operand2;
          }
          else {
            return operand0 + " = *" + operand1;
          }

        case MiddleOperator.DecreaseStack:
          return "decrease stack";

        case MiddleOperator.PushZero:
          return "push 0";

        case MiddleOperator.PushOne:
          return "push 1";

        case MiddleOperator.PushFloat:
          return "push float " + operand0.ToString();
        
        case MiddleOperator.PopFloat:
          if (operand0 != null) {
            return "pop float " + operand0;
          }
          else {
            return "pop float empty X";
          }

        case MiddleOperator.PopEmpty:
          return "Pop empty";

        case MiddleOperator.TopFloat:
          return "top float " + operand0;

        case MiddleOperator.InspectRegister:
          return operand0 + " = " + operand1;

        case MiddleOperator.InspectFlagByte:
          return operand0 + " = flagbyte";

        case MiddleOperator.SystemInit:
          return "system init " + operand0;

        case MiddleOperator.SystemParameter:
          return "system parameter " + operand0 + ", " + operand1 + ", " + operand2;

        case MiddleOperator.SystemCall:
          return "system call " + operand0;

        case MiddleOperator.JumpRegister:
          return "jump to " + operand1;

        case MiddleOperator.ClearRegisters:
          return "clear registers";

        case MiddleOperator.CheckTrackMapFloatStack:
          return "check track map float stack";

        case MiddleOperator.AssignInitSize:
          return "assign init size";

        case MiddleOperator.ParameterInitSize:
          return "parameter init size";
      }

      Assert.Error(false);
      return null;
    }
  }
}