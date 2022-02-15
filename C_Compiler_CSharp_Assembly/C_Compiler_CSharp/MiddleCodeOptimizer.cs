using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCodeOptimizer {
    private bool m_update;
    private List<MiddleCode> m_middleCodeList;

    public MiddleCodeOptimizer(List<MiddleCode> middleCodeList) {
      m_middleCodeList = middleCodeList;
    }

    public void Optimize() {
      ObjectToIntegerAddresses();

      if (Start.CurrentFunction.Name.Equals("fileexists")) {
        string name = "C:\\Users\\Stefan\\Documents\\A A C_Compiler_Assembler - A 16 bits\\StdIO\\" + Start.CurrentFunction.Name + "2.middlebefore";
        StreamWriter streamWriter = new StreamWriter(name);

        for (int index = 0; index < m_middleCodeList.Count; ++index) {
                  MiddleCode middleCode = m_middleCodeList[index];
          streamWriter.WriteLine(index + ": " + middleCode.ToString());
        }

        streamWriter.Close();
      }

      do {
        m_update = false;
        ClearGotoNextStatements();
        ClearDoubleRelationStatements();
        TraceGotoChains();
        ClearUnreachableCode();
        RemovePushPop();
        //MergePopPushToTop();
        MergeTopPopToPop();
        MergeBinary();
        //MergeDoubleAssign();
        RemoveClearedCode();
      } while (m_update);

      OptimizeRelation();
      OptimizeCommutative();
      OptimizeBinary();
//      CheckIntegralOld();
      CheckIntegral();
//      CheckFloatingOld();
      CheckFloating();
    }

    public void ObjectToIntegerAddresses() {
      IDictionary<MiddleCode,int> addressMap = new Dictionary<MiddleCode,int>();
    
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        addressMap.Add(m_middleCodeList[index], index);
      }
    
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        MiddleCode sourceCode = m_middleCodeList[index];
      
        if (sourceCode.IsGoto() || sourceCode.IsCarry() || sourceCode.IsRelation()) {
          Assert.Error(sourceCode.GetOperand(0) is MiddleCode);
          MiddleCode targetCode = (MiddleCode) sourceCode.GetOperand(0);
          Assert.Error(addressMap.ContainsKey(targetCode));
          sourceCode.SetOperand(0, addressMap[targetCode]);
        }
      }
    
      for (int index = 0; index < m_middleCodeList.Count; ++index) {
        Start.MemberStream.WriteLine(index + ": " + m_middleCodeList[index]);
      }      
    }

  // --------------------------------------------------------------------------

    // 1. goto 2
    // 2. ...
  
    private void ClearGotoNextStatements() {
      for (int index = 1; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode middleCode = m_middleCodeList[index];
      
        if (middleCode.IsRelationCarryOrGoto()) {
          int target = (int) middleCode.GetOperand(0);
  
          if (target == (index + 1)) {
            middleCode.Clear();
            m_update = true;
          }
        }
      }
    }

  // --------------------------------------------------------------------------

    // 1. if a < b goto 3
    // 2. goto 10
  
    // 1. if a >= b goto 10
    // 2. Clear

    public static IDictionary<MiddleOperator, MiddleOperator> InverseMap =
      new Dictionary<MiddleOperator, MiddleOperator>();

    public static IDictionary<MiddleOperator,MiddleOperator>
                    m_swapMap = new Dictionary<MiddleOperator,MiddleOperator>();

    static MiddleCodeOptimizer() {
      InverseMap.Add(MiddleOperator.Equal, MiddleOperator.NotEqual);
      InverseMap.Add(MiddleOperator.NotEqual, MiddleOperator.Equal);
      InverseMap.Add(MiddleOperator.Carry, MiddleOperator.NotCarry);
      InverseMap.Add(MiddleOperator.NotCarry, MiddleOperator.Carry);
      InverseMap.Add(MiddleOperator.SignedLessThan, MiddleOperator.SignedGreaterThanEqual);
      InverseMap.Add(MiddleOperator.SignedLessThanEqual, MiddleOperator.SignedGreaterThan);
      InverseMap.Add(MiddleOperator.SignedGreaterThan, MiddleOperator.SignedLessThanEqual);
      InverseMap.Add(MiddleOperator.SignedGreaterThanEqual, MiddleOperator.SignedLessThan);
      InverseMap.Add(MiddleOperator.UnsignedLessThan, MiddleOperator.UnsignedGreaterThanEqual);
      InverseMap.Add(MiddleOperator.UnsignedLessThanEqual, MiddleOperator.UnsignedGreaterThan);
      InverseMap.Add(MiddleOperator.UnsignedGreaterThan, MiddleOperator.UnsignedLessThanEqual);
      InverseMap.Add(MiddleOperator.UnsignedGreaterThanEqual, MiddleOperator.UnsignedLessThan);

      m_swapMap.Add(MiddleOperator.Equal, MiddleOperator.Equal);
      m_swapMap.Add(MiddleOperator.NotEqual, MiddleOperator.NotEqual);
      m_swapMap.Add(MiddleOperator.SignedLessThan, MiddleOperator.SignedGreaterThan);
      m_swapMap.Add(MiddleOperator.SignedGreaterThan, MiddleOperator.SignedLessThan);
      m_swapMap.Add(MiddleOperator.SignedLessThanEqual, MiddleOperator.SignedGreaterThanEqual);
      m_swapMap.Add(MiddleOperator.SignedGreaterThanEqual, MiddleOperator.SignedLessThanEqual);
      m_swapMap.Add(MiddleOperator.UnsignedLessThan, MiddleOperator.UnsignedGreaterThan);
      m_swapMap.Add(MiddleOperator.UnsignedGreaterThan, MiddleOperator.UnsignedLessThan);
      m_swapMap.Add(MiddleOperator.UnsignedLessThanEqual, MiddleOperator.UnsignedGreaterThanEqual);
      m_swapMap.Add(MiddleOperator.UnsignedGreaterThanEqual, MiddleOperator.UnsignedLessThanEqual);
    }
  
    private void ClearDoubleRelationStatements() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.IsRelation() || thisCode.IsCarry()) && nextCode.IsGoto()) {
          int target1 = (int) thisCode.GetOperand(0),
              target2 = (int) nextCode.GetOperand(0);

          if (target1 == (index + 2)) {
            MiddleOperator operator1 = thisCode.Operator;
            thisCode.Operator = InverseMap[operator1];
            thisCode.SetOperand(0, target2);
            nextCode.Clear();
            m_update = true;
          }
        }
      }
    }

  // --------------------------------------------------------------------------

    private void TraceGotoChains() {
      for (int index = 1; index < m_middleCodeList.Count; ++index) {
        MiddleCode middleCode = m_middleCodeList[index];

        if (middleCode.IsRelationCarryOrGoto()) {
          ISet<int> sourceSet = new HashSet<int>();
          sourceSet.Add(index);
        
          int firstTarget = (int) middleCode.GetOperand(0);
          int finalTarget = TraceGoto(firstTarget, sourceSet);

          if (firstTarget != finalTarget) {
            foreach (int source in sourceSet) {
              MiddleCode sourceCode = m_middleCodeList[source];
              sourceCode.SetOperand(0, finalTarget);
            }

            m_update = true;
          }
        }
      }
    }

    private int TraceGoto(int target, ISet<int> sourceSet) {
      MiddleCode objectCode = m_middleCodeList[target];
    
      if (!sourceSet.Contains(target) && objectCode.IsGoto()) {
        sourceSet.Add(target);
        int nextTarget = (int) objectCode.GetOperand(0);
        return TraceGoto(nextTarget, sourceSet);
      }
      else {
        return target;
      }
    }

  // --------------------------------------------------------------------------

    private void ClearUnreachableCode() {
      ISet<int> visitedSet = new HashSet<int>();
      SearchReachableCode(0, visitedSet);

      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        if (!visitedSet.Contains(index)) {
          m_middleCodeList[index].Clear();
          m_update = true;
        }
      }
    }
    
    private void SearchReachableCode(int index, ISet<int> visitedSet) {
      for (; index < m_middleCodeList.Count; ++index) {
        if (visitedSet.Contains(index)) {
          return;
        }

        visitedSet.Add(index);
        MiddleCode middleCode = m_middleCodeList[index];

        if (middleCode.IsRelation() || middleCode.IsCarry()) {
          int target = (int) middleCode.GetOperand(0);
          SearchReachableCode(target, visitedSet);
        }
        else if (middleCode.IsGoto()) {
          int target = (int) middleCode.GetOperand(0);
          SearchReachableCode(target, visitedSet);
          return;
        }
        else if (/*(middleCode.Operator == MiddleOperator.Exit) ||*/
                 (middleCode.Operator == MiddleOperator.Return)) {
          return;
        }
        else if (middleCode.Operator == MiddleOperator.FunctionEnd) {
          Symbol funcSymbol = (Symbol) middleCode.GetOperand(0);
          Assert.Warning(!funcSymbol.Type.ReturnType.IsVoid(),
                         funcSymbol.Name, "reached the end of a non-void function");
          return;
        }
      }
    }
  
    // Push x + Pop => empty
    public void RemovePushPop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.PushFloat) &&
            (nextCode.Operator == MiddleOperator.PopFloat) &&
            (nextCode.GetOperand(0) == null)) {
          thisCode.Clear();
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // Pop x + Push x => Top x
    public void MergePopPushToTop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.PopFloat) &&
            (nextCode.Operator == MiddleOperator.PushFloat) &&
            (thisCode.GetOperand(0) == nextCode.GetOperand(0))) {
          thisCode.Operator = MiddleOperator.TopFloat;
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // Top x + Pop => Pop x
    public void MergeTopPopToPop() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.TopFloat) &&
            (nextCode.Operator == MiddleOperator.PopFloat) &&
            (nextCode.GetOperand(0) == null)) {
          thisCode.Operator = MiddleOperator.PopFloat;
          nextCode.Clear();
          m_update = true;
        }
      }
    }

    // assign a = b => Pop a
    public void AssignFloat() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode middleCode = m_middleCodeList[index];

        if (middleCode.Operator == MiddleOperator.Assign) {
          Symbol resultSymbol = (Symbol) middleCode.GetOperand(1);
        
          if (resultSymbol.Type.IsFloating()) {
            middleCode.Operator = MiddleOperator.PopFloat;
            m_update = true;
          }
        }
      }
    }

    private void MergeBinary() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        // t = b + c
        // a = t
        // a = b + c

        if ((/*thisCode.IsUnaryX() ||*/ thisCode.IsBinary()) &&
            (nextCode.Operator == MiddleOperator.Assign) &&
            ((Symbol) thisCode.GetOperand(0)).IsTemporary() &&
            thisCode.GetOperand(0).Equals(nextCode.GetOperand(1))) {
          thisCode.SetOperand(0, nextCode.GetOperand(0));
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    // t = a
    // b = t

    // b = a

    private void MergeDoubleAssign() {
      for (int index = 0; index < (m_middleCodeList.Count - 1); ++index) {
        MiddleCode thisCode = m_middleCodeList[index],
                   nextCode = m_middleCodeList[index + 1];

        if ((thisCode.Operator == MiddleOperator.Assign) &&
            (nextCode.Operator == MiddleOperator.Assign) &&
            thisCode.GetOperand(0).Equals(nextCode.GetOperand(1))) {
          thisCode.SetOperand(0, nextCode.GetOperand(0));
          nextCode.Clear();
          m_update = true;
        }
      }
    }
  
    public void RemoveClearedCode() {
      for (int index1 = (m_middleCodeList.Count - 2); index1 > 0;--index1){
        if (m_middleCodeList[index1].Operator == MiddleOperator.Empty) {
          for (int index2 = 0; index2 < m_middleCodeList.Count; ++index2) {
            MiddleCode middleCode = m_middleCodeList[index2];
          
            if (middleCode.IsRelationCarryOrGoto()) {
              int target = (int) middleCode.GetOperand(0);
            
              if (target > index1) {
                middleCode.SetOperand(0, target - 1);
              }
            }
          }
        
          m_middleCodeList.RemoveAt(index1);
        }
      }
    }

    // if 1 < x goto
    // if x > 1 goto
  
    private void OptimizeRelation() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        if (middleCode.IsRelation()) {
          Symbol leftSymbol = (Symbol) middleCode.GetOperand(1),
                 rightSymbol = (Symbol) middleCode.GetOperand(2);
   
          if (leftSymbol.Value is long) {
            //Assert.Error(rightSymbol.Value == null);
            middleCode.Operator = m_swapMap[middleCode.Operator];
            middleCode.SetOperand(1, rightSymbol);
            middleCode.SetOperand(2, leftSymbol);
          }
          else if (leftSymbol.IsStaticOrExtern() && leftSymbol.Type.IsArrayFunctionOrString() &&
                   !(rightSymbol.Value is long))  {
//          else if (leftSymbol.IsStaticOrExtern() && leftSymbol.Type.IsArrayFunctionOrString() &&
//                   !((rightSymbol.Value is long) || (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()))) {
            middleCode.Operator = m_swapMap[middleCode.Operator];
            middleCode.SetOperand(1, rightSymbol);
            middleCode.SetOperand(2, leftSymbol);
          }
        }
      }
    }

    // a = b + c
    // t = b + c
    // a = t
  
    private void OptimizeCommutative() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        if (middleCode.IsCommutative()) {
          Symbol leftSymbol = (Symbol) middleCode.GetOperand(1),
                 rightSymbol = (Symbol) middleCode.GetOperand(2);
   
          if (leftSymbol.Type.IsIntegralPointerArrayOfFunction() &&
              leftSymbol.IsValue()) {
            middleCode.SetOperand(1, rightSymbol);
            middleCode.SetOperand(2, leftSymbol);
          }
        }
      }
    }

    private void OptimizeBinary() {
      foreach (MiddleCode middleCode in m_middleCodeList) {
        MiddleOperator middleOperator = middleCode.Operator;
      
        if (middleOperator == MiddleOperator.BinaryAdd) {
          Symbol resultSymbol = (Symbol) middleCode.GetOperand(0), // i = i + 1
                 leftSymbol = (Symbol) middleCode.GetOperand(1),
                 rightSymbol = (Symbol) middleCode.GetOperand(2);
   
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
              resultSymbol.Equals(leftSymbol)) {
            if (rightSymbol.IsValue() && rightSymbol.Value.Equals(BigInteger.One)) {
              middleCode.Operator = MiddleOperator.Increment;
              middleCode.SetOperand(0, null);
              middleCode.SetOperand(2, null);
              m_update = true;
            }
            else if (rightSymbol.IsValue() && rightSymbol.Value.Equals(BigInteger.Negate(BigInteger.One))) {
              middleCode.Operator = MiddleOperator.Decrement;
              middleCode.SetOperand(0, null);
              middleCode.SetOperand(2, null);
              m_update = true;
            }
          }
        }
        else if (middleOperator == MiddleOperator.BinarySubtract) {
          Symbol resultSymbol = (Symbol) middleCode.GetOperand(0), // i = i - 1
                 leftSymbol = (Symbol) middleCode.GetOperand(1),
                 rightSymbol = (Symbol) middleCode.GetOperand(2);
        
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
              resultSymbol.Equals(leftSymbol)) {
            if (rightSymbol.IsValue() &&
                rightSymbol.Value.Equals(BigInteger.One)) {
              middleCode.Operator = MiddleOperator.Decrement;
              middleCode.SetOperand(0, null);
              middleCode.SetOperand(2, null);
              m_update = true;
            }
            else if (rightSymbol.IsValue() &&
                     rightSymbol.Value.Equals(BigInteger.Negate(BigInteger.One))) {
              middleCode.Operator = MiddleOperator.Increment;
              middleCode.SetOperand(0, null);
              middleCode.SetOperand(2, null);
              m_update = true;
            }
          }
        }
        else if (middleOperator == MiddleOperator.Assign) {
          Symbol resultSymbol = (Symbol) middleCode.GetOperand(0), // i = i;
                 assignSymbol = (Symbol) middleCode.GetOperand(1);
        
          if (resultSymbol.Type.IsIntegralPointerArrayStringOrFunction() &&
             resultSymbol.Equals(assignSymbol)) {
            middleCode.Operator = MiddleOperator.Empty;
            m_update = true;
          }
        }
      }
    }

    private static ISet<Symbol> CloneSet(ISet<Symbol> inSet) {
      ISet<Symbol> outSet = new HashSet<Symbol>();

      foreach (Symbol symbol in inSet) {
        outSet.Add(symbol);
      }

      return outSet;
    }

    private void CheckIntegral() {
      Stack<ISet<Symbol>> integralSetStack = new Stack<ISet<Symbol>>();
      ISet<Symbol> integralSet = new HashSet<Symbol>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader: {
              if (symbol1.IsTemporary() && symbol1.Type.IsPointer()) {
                integralSet.Add(symbol1.AddressSymbol);
              }

              middleCode.SetOperand(1, CloneSet(integralSet));
              integralSetStack.Push(integralSet);
              integralSet = new HashSet<Symbol>();
            }
            break;

/*          case MiddleOperator.Call:
            if (symbol1.Type.IsPointer()) {
              integralSet.Add(symbol1.AddressSymbol);
            }
            break;*/

          case MiddleOperator.PostCall:
            integralSet = integralSetStack.Pop();
            //integralSet = new HashSet<Symbol>();
            break;

          /*case MiddleOperator.GetReturnValue:
            if ((symbol1 != null) && symbol1.IsTemporary() &&
                (symbol1.AddressSymbol == null) &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol1);
            }
            break;*/

          case MiddleOperator.Deref:
            Symbol resultSymbol = (Symbol) middleCode.GetOperand(0);

            if (resultSymbol.Switch) {
              integralSet.Remove(resultSymbol.AddressSymbol);
            }
            else {
              integralSet.Add(resultSymbol.AddressSymbol);
            }
            break;

          case MiddleOperator.Case:
            if (symbol1.AddressSymbol != null) {
              integralSet.Remove(symbol1.AddressSymbol);
            }          
            break;

          case MiddleOperator.CaseEnd:
            integralSet.Remove(symbol0);
            break;

          default:
            if ((symbol0 != null) && symbol0.IsTemporary() &&
                (symbol0.AddressSymbol == null) &&
                symbol0.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol0);
            }

            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol1);
            }

            if ((symbol2 != null) && symbol2.IsTemporary() &&
                symbol2.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol2);
            }

            if ((symbol0 != null) && (symbol0.AddressSymbol != null)) {
              integralSet.Remove(symbol0.AddressSymbol);
            }

            if ((symbol1 != null) && (symbol1.AddressSymbol != null)) {
              integralSet.Remove(symbol1.AddressSymbol);
            }

            if ((symbol2 != null) && (symbol2.AddressSymbol != null)) {
              integralSet.Remove(symbol2.AddressSymbol);
            }
            break;
        }
      }
    }

    /*private void CheckIntegral() {
      ISet<Symbol> integralSet = new HashSet<Symbol>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader: {
              middleCode.SetOperand(1, integralSet);
            }
            break;

          case MiddleOperator.PostCall:
            integralSet = new HashSet<Symbol>();
            break;

          case MiddleOperator.GetReturnValue:
            if ((symbol1 != null) && symbol1.IsTemporary() &&
                (symbol1.AddressSymbol == null) &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol1);
            }
            break;

          default:
            if ((symbol0 != null) && symbol0.IsTemporary() &&
                (symbol0.AddressSymbol == null) &&
                symbol0.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol0);
            }

            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol1);
            }

            if ((symbol2 != null) && symbol2.IsTemporary() &&
                symbol2.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol2);
            }
            break;
        }
      }
    }*/

    private void CheckFloating() {
      int stackSize = 0;

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader:
            middleCode.SetOperand(2, stackSize);
            break;

          case MiddleOperator.PushOne:
          case MiddleOperator.PushZero:
          case MiddleOperator.PushFloat:
          case MiddleOperator.IntegralToFloating:
            ++stackSize;
            break;
          
          case MiddleOperator.GetReturnValue:
            if (symbol0.Type.IsFloating()) {
              ++stackSize;
            }
            break;

          case MiddleOperator.PopFloat:
          case MiddleOperator.FloatingToIntegral:
          case MiddleOperator.DecreaseStack:
            --stackSize;
            break;

          case MiddleOperator.Return:
            stackSize = 0;
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual: {
              if (symbol1.Type.IsFloating()) {
                stackSize -= 2;
              }
            }
            break;

          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract:
          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.Parameter:
            if (symbol1.Type.IsFloating()) {
              --stackSize;
            }
            break;

          case MiddleOperator.Deref:
          case MiddleOperator.TopFloat:
          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.address:
          case MiddleOperator.SetReturnValue:
            break;

          case MiddleOperator.FunctionEnd:
            //Assert.Error(stackSize != 0);
            break;
        }
       
        /*if (Start.CurrentFunction.Name.Equals("freopen")) {
          Console.Out.WriteLine(middleCode.ToString() + " " + stackSize);
        }*/
      }
    }

/*    private void CheckIntegral() {
      ISet<Symbol> integralSet = new HashSet<Symbol>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.PreCall: {
              int recordOffset = middleCode.SymbolTable().CurrentOffset;

              foreach (Symbol symbol in integralSet) {
                symbol.Offset = recordOffset;
                recordOffset += symbol.Type.Size();
              }

              middleCode.SetOperand(0, recordOffset);
              middleCode.SetOperand(2, integralSet);
            }
            break;

          case MiddleOperator.PostCallIntegral:
            middleCode.SetOperand(2, integralSet);
            integralSet = new HashSet<Symbol>();
            break;

          case MiddleOperator.GetReturnValue:
            if ((symbol1 != null) && symbol1.IsTemporary() &&
                (symbol1.AddressSymbol == null) &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol1);
            }
            break;

          default:
            if ((symbol0 != null) && symbol0.IsTemporary() &&
                (symbol0.AddressSymbol == null) &&
                symbol0.Type.IsIntegralPointerOrArray()) {
              integralSet.Add(symbol0);
            }

            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol1);
            }

            if ((symbol2 != null) && symbol2.IsTemporary() &&
                symbol2.Type.IsIntegralPointerOrArray()) {
              integralSet.Remove(symbol2);
            }
            break;
        }
      }
    }

    private void CheckFloating() {
      int stackSize = 0, recordOffset = 0;
      bool returnFloat = false;
      Stack<ISet<int>> parameterSetStack = new Stack<ISet<int>>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.FunctionStart:
            break;

          case MiddleOperator.CallHeader:
            parameterSetStack.Push(new HashSet<int>());
            break;
          
          case MiddleOperator.PreCall:
            recordOffset = (int) operand0;
            recordOffset += stackSize * Type.QuarterWordSize;

            foreach (int lineNo in parameterSetStack.Pop()) {
              m_middleCodeList[lineNo].SetOperand(0, recordOffset);
            }

            middleCode.SetOperand(0, recordOffset);
            middleCode.SetOperand(1, stackSize);
            break;

          case MiddleOperator.Call:
            middleCode.SetOperand(0, recordOffset);
            m_middleCodeList[line - 1].SetOperand(0, recordOffset);

            if ((symbol1.Type.IsFunction() && symbol1.Type.ReturnType.IsFloating()) ||
                (symbol1.Type.IsPointer() && symbol1.Type.PointerType.ReturnType.IsFloating())) {
              returnFloat = true;
              Assert.Error((++stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
              //Console.Out.WriteLine("call " + stackSize);
            }
            else {
              returnFloat = false;
            }
            break;

          case MiddleOperator.PostCallFloating:
            middleCode.SetOperand(0, recordOffset);
            middleCode.SetOperand(1, stackSize);
            middleCode.SetOperand(2, returnFloat);
            break;

          case MiddleOperator.PushOne:
          case MiddleOperator.PushZero:
          case MiddleOperator.PushFloat:
            Assert.Error((++stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
            //Console.Out.WriteLine("push float " + stackSize);
            break;

          case MiddleOperator.IntegralToFloating:
            Assert.Error((++stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
            //Console.Out.WriteLine("integral to floating " + stackSize);
            break;
          
          case MiddleOperator.Parameter:
            parameterSetStack.Peek().Add(line);
  
            if (symbol1.Type.IsFloating()) {
              --stackSize;
              //Console.Out.WriteLine("parameter " + stackSize);

              if (stackSize < 0) {
                //int i = 1;
              }
            }
            break;
          
          case MiddleOperator.PopFloat:
            --stackSize;
            //Console.Out.WriteLine("pop float " + stackSize);

            if (stackSize < 0) {
              //int i = 1;
            }
            break;

          case MiddleOperator.FloatingToIntegral:
            --stackSize;
            //Console.Out.WriteLine("floating to integral " + stackSize);

            if (stackSize < 0) {
              //int i = 1;
            }
            break;

          case MiddleOperator.Return:
            stackSize = 0;
            //Console.Out.WriteLine("return " + stackSize);
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual: {
              if (symbol1.Type.IsFloating()) {
                stackSize -= 2;
                //Console.Out.WriteLine("relation " + stackSize);
              }

              if (stackSize < 0) {
                //int i = 1;
              }
            }
            break;

          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract:
          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide: {
              if (symbol1.Type.IsFloating()) {
                --stackSize;
                //Console.Out.WriteLine("arithmetic " + stackSize);
              }

              if (stackSize < 0) {
                //int i = 1;
              }
            }
            break;

          case MiddleOperator.Deref:
          case MiddleOperator.TopFloat:
          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.address:
          case MiddleOperator.SetReturnValue:
          case MiddleOperator.GetReturnValue:
            break;
        }
      }
    }

    /*private void CheckIntegralOld() {
      IDictionary<Symbol,int> integralMap = new Dictionary<Symbol,int>();
      Stack<ISet<int>> parameterSetStack = new Stack<ISet<int>>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];
        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1),
               operand2 = middleCode.GetOperand(2);

        Symbol symbol0 = (operand0 is Symbol) ? ((Symbol) operand0) : null,
               symbol1 = (operand1 is Symbol) ? ((Symbol) operand1) : null,
               symbol2 = (operand2 is Symbol) ? ((Symbol) operand2) : null;

        switch (middleCode.Operator) {
          case MiddleOperator.CallHeader:
            parameterSetStack.Push(new HashSet<int>());
            break;
          
          case MiddleOperator.Parameter:
            parameterSetStack.Peek().Add(line);
          
            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralMap.Remove(symbol1);
              middleCode.SymbolTable().RemoveTemporary(symbol1);
            }
            break;
          
          case MiddleOperator.Call: {
              KeyValuePair<Symbol,int>[] integralArray = new KeyValuePair<Symbol,int>[integralMap.Count];
              integralMap.CopyTo(integralArray, 0);
          
              for (int index = (integralArray.Length - 1); index >= 0; --index) {
                KeyValuePair<Symbol,int> entry = (KeyValuePair<Symbol,int>) integralArray[index];
                Symbol symbol = entry.Key;
                middleCode.SymbolTable().AddSymbol(symbol);
              }
            
              int recordSize = middleCode.SymbolTable().CurrentOffset;
              middleCode.SetOperand(0, recordSize);

              foreach (int lineNo in parameterSetStack.Pop()) {
                m_middleCodeList[lineNo].SetOperand(0, recordSize);
              }
            }
            break;

          case MiddleOperator.PostCall: {
              int recordSize = middleCode.SymbolTable().CurrentOffset;
              middleCode.SetOperand(0, recordSize);
            }
            break;
          
          default:
            if ((symbol0 != null) && symbol0.IsTemporary() &&
                (symbol0.AddressSymbol == null) &&
                symbol0.Type.IsIntegralPointerOrArray()) {
              integralMap[symbol0] = line;
            }

            if ((symbol1 != null) && symbol1.IsTemporary() &&
                symbol1.Type.IsIntegralPointerOrArray()) {
              integralMap.Remove(symbol1);
              middleCode.SymbolTable().RemoveTemporary(symbol1);
            }

            if ((symbol2 != null) && symbol2.IsTemporary() &&
                symbol2.Type.IsIntegralPointerOrArray()) {
              integralMap.Remove(symbol2);
              middleCode.SymbolTable().RemoveTemporary(symbol2);
            }
            break;
        }
      }
    }

    private void CheckFloatingOld() {
      int stackSize = 0;
      Stack<ISet<int>> parameterSetStack = new Stack<ISet<int>>();

      for (int line = 0; line < m_middleCodeList.Count; ++line) {
        MiddleCode middleCode = m_middleCodeList[line];

        object operand0 = middleCode.GetOperand(0),
               operand1 = middleCode.GetOperand(1);

        switch (middleCode.Operator) {
          case MiddleOperator.PushFloat:
            Assert.Error((++stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
            break;

          case MiddleOperator.IntegralToFloating:
            Assert.Error((++stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
            break;

          case MiddleOperator.CallHeader:
            parameterSetStack.Push(new HashSet<int>());
            break;
          
          case MiddleOperator.Parameter: {
              Symbol symbol1 = (Symbol) operand1;
              parameterSetStack.Peek().Add(line);

              if (symbol1.Type.IsFloating()) {
                --stackSize;
              }
            }         
            break;
          
          case MiddleOperator.PopFloat:
          case MiddleOperator.FloatingToIntegral:
            --stackSize;
            break;

          case MiddleOperator.PreCall:
            break;

          case MiddleOperator.Call: {
              MiddleCode postCode = m_middleCodeList[line + 1];
              //middleCode.SetOperand(2, stackSize);
              //postCode.SetOperand(2, stackSize);
              int stackBlock = stackSize * CCompiler.Type.QuarterWordSize;

              { int recordSize = (int) middleCode.GetOperand(0);
                postCode.SetOperand(0, recordSize + stackBlock);
              }
            
              foreach (int lineNo in parameterSetStack.Pop()) {
                int recordSize = (int) m_middleCodeList[lineNo].GetOperand(0);
                m_middleCodeList[lineNo].SetOperand(0, recordSize + stackBlock);
              }

              Symbol calleeSymbol = (Symbol) operand1;

              if (calleeSymbol.Type.IsPointer()) {
                if (calleeSymbol.Type.PointerType.ReturnType.IsFloating()) {
                  ++stackSize;
                }              
              }
              else {
                if (calleeSymbol.Type.ReturnType.IsFloating()) {
                  ++stackSize;
                }
              }

              Assert.Error(stackSize <= Start.FloatingStackMaxSize, "floating stack overflow");
            }
            break;

          case MiddleOperator.Return:
            stackSize = 0;
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual: {
              Symbol symbol1 = (Symbol) operand1;

              if (symbol1.Type.IsFloating()) {
                stackSize -= 2;
              }
            }
            break;

          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract:
          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide: {
              Symbol symbol1 = (Symbol) operand1;

              if (symbol1.Type.IsFloating()) {
                --stackSize;
              }
            }
            break;

          case MiddleOperator.Deref: {
              Symbol symbol0 = (Symbol) operand0;

              if (symbol0.Type.IsFloating()) {
                Assert.Error(++stackSize <= Start.FloatingStackMaxSize, "floating stack overflow");
              }
            }
            break;

          case MiddleOperator.TopFloat:
          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.address:
          case MiddleOperator.SetReturnValue:
          case MiddleOperator.GetReturnValue:
            break;
        }

        Start.AfterCodeStream.WriteLine(line + ": " + middleCode + " <" + stackSize + ">");
      }
    }*/
  }
}

  /*  
    Ej röda dagar:
      Julafton?
      Midsommarafton?
      Nyårsafton?
  
    Röda dagar:
      Juldagen
      Annandag Jul
      Långfredagen
      Annandag påsk

    switch (x + 1) {
      ...
    }
  
    1. $0 = x + 1
    2. if $0 == 1 goto 11
    3. if $0 == 2 goto 12
    4. ...
  
    switch (a < b) {
      ...
    }
  
    11. if a < b goto 21
    12. goto 23
    13. ...

    21. $0 = 1 soft Backpatch
    22. goto 24 soft goto
  
    23. $0 = 0 soft Backpatch
  
    24. if $0 == 0 goto 1
    25. if $0 == 1 goto 3
    25. if $0 == 2 goto 5
    26. ...  
  */