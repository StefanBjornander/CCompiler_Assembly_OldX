using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class ObjectCodeGenerator {
    public IDictionary<Symbol,Track> m_trackMap = new Dictionary<Symbol,Track>();
    public ISet<Track> m_trackSet = new HashSet<Track>(); // ListSetXXX
    private IDictionary<int,int> m_middleToObjectMap = new Dictionary<int,int>();
//    private IDictionary<int,int> m_objectToByteMap = new Dictionary<int,int>();
    public List<AssemblyCode> m_assemblyCodeList;
  
    private int m_stackSize = 0;
    //private bool m_returnFloat = false;
    private Stack<int> m_topStack = new Stack<int>();
    private Stack<int> m_recordStack = new Stack<int>();
    private Stack<IDictionary<Symbol,Track>> m_trackMapStack = new Stack<IDictionary<Symbol,Track>>();
    private Stack<IDictionary<Track,int>> m_postMapStack = new Stack<IDictionary<Track,int>>();

    public void Generate(List<MiddleCode> middleCodeList) {
      GenerateObjectCodeList(middleCodeList);
      GenerateTrackSet();

      for(int index = 0; index < m_assemblyCodeList.Count; ++index) {
        string text = m_assemblyCodeList[index].ToString();

        if (text.Length > 0) {
          Start.TrackCodeStream.WriteLine(index + ":\t" + text);
        }
      }
    
      Start.GraphCodeStream.WriteLine(Start.CurrentFunction.Name);
      new RegisterAllocator(m_trackSet, m_assemblyCodeList);

      for(int index = 0; index < m_assemblyCodeList.Count; ++index) {
        string text = m_assemblyCodeList[index].ToString();

        if (text.Length > 0) {
          Start.ObjectCodeStream.WriteLine(index + ":\t" + text);
        }
      }
    
      ObjectCodeOptimizer optimizer = new ObjectCodeOptimizer(m_assemblyCodeList);
      optimizer.Optimize();
      GenerateJumpInfo();
      GenerateTargetByteList(Start.CurrentFunction.ByteList, Start.CurrentFunction.AccessMap, 
                             Start.CurrentFunction.CallMap, Start.CurrentFunction.ReturnSet,
                             Start.CurrentFunction.ByteToTextMap(), Start.CurrentFunction.GetTextList());
    }
  
    public void AddAssemblyCode(AssemblyCode objectCode) {
      m_assemblyCodeList.Add(objectCode);
    }

    public void AddAssemblyCode(AssemblyOperator operatorX) {
      AddAssemblyCode(operatorX, null, null, null);
    }
  
    public void AddAssemblyCode(AssemblyOperator operatorX, object operand0) {
      AddAssemblyCode(operatorX, operand0, null, null);
    }
  
    public void AddAssemblyCode(AssemblyOperator operatorX, object operand0,
                              object operand1) {
      AddAssemblyCode(operatorX, operand0, operand1, null);
    }
  
    public void AddAssemblyCode(AssemblyOperator operatorX, object operand0,
                              object operand1, object operand2) {
      m_assemblyCodeList.Add(new AssemblyCode(operatorX, operand0, operand1, operand2));
    }
  
    public void SetObjectCode(int index, AssemblyOperator operatorX) {
      SetObjectCode(index, operatorX, null, null, null);
    }

    public void SetObjectCode(int index, AssemblyOperator operatorX, object operand0)
    {
      SetObjectCode(index, operatorX, operand0, null);
    }

    public void SetObjectCode(int index, AssemblyOperator operatorX, object operand0,
                              object operand1) {
      SetObjectCode(index, operatorX, operand0, operand1, null);
    }

    public void SetObjectCode(int index, AssemblyOperator operatorX, object operand0,
                              object operand1, object operand2) {
      m_assemblyCodeList[index] = new AssemblyCode(operatorX, operand0, operand1, operand2);
    }
  
    private void GenerateTrackSet() {
      for (int index = 0; index < m_assemblyCodeList.Count; ++index) {
        AssemblyCode objectCode = m_assemblyCodeList[index];
     
        object operand0 = objectCode.GetOperand(0),
               operand1 = objectCode.GetOperand(1),
               operand2 = objectCode.GetOperand(2);

        if (objectCode.Operator == AssemblyOperator.set_track_size) {
          Track track = (Track) operand0;
        
          if (operand1 is int) {
            int size = (int) operand1;
            track.Size = size;
            objectCode.Operator = AssemblyOperator.empty;
          }
          else {
            int size = ((Track) operand1).Size;
            track.Size = size;
            objectCode.Operator = AssemblyOperator.empty;
          }
        }
        else {
          CheckTrack(operand0, 0, index);
          CheckTrack(operand1, 1, index);
          CheckTrack(operand2, 2, index);
        }
      }
    }

    private void CheckTrack(object operand, int position, int index) {
      if (operand is Track) {
        Track track = (Track) operand;
      
        if (!m_trackSet.Contains(track)) {
          m_trackSet.Add(track);
        }
      
        track.AddCode(position, index);
      }
    }

    public void GenerateObjectCodeList(List<MiddleCode> middleCodeList) {
      if (m_assemblyCodeList == null) {
        m_assemblyCodeList = new MyList<AssemblyCode>();
      }
    
      for (int middleIndex = 0; middleIndex < middleCodeList.Count; ++middleIndex) {
        AddAssemblyCode(AssemblyOperator.new_code, middleIndex);
        MiddleCode middleCode = middleCodeList[middleIndex];
        Start.CurrentTable = middleCode.SymbolTable();
        MiddleOperator middleOp = middleCode.Operator;

        string middleText = middleCode.ToString();

        /*if (middleCode.ToString().Equals("BinarySubtract £temporary6138 p list")) {
          int i = 1;
        }*/

        if (middleCode.Operator == MiddleOperator.FunctionStart) {
          middleText += ", " + Start.CurrentTable.ToString();
        }

        string label = Start.CurrentFunction.UniqueName +
                       ((middleIndex > 0) ? (Start.SeparatorId + middleIndex) : "");
        AddAssemblyCode(AssemblyOperator.label, label, middleText, null);

        switch (middleCode.Operator) {
          case MiddleOperator.FunctionStart:
            //GenerateFunctionStart(middleCode, index);
            break;

          case MiddleOperator.CallHeader:
            GenerateCallHeader(middleCode);
            break;

/*          case MiddleOperator.PreCall:
                      GeneratePreCall(middleCode);
                      break;*/

          case MiddleOperator.Call:
            GenerateFunctionCall(middleCode, middleIndex);
            break;

          case MiddleOperator.PostCall:
            GenerateFunctionPostCall(middleCode);
            break;

/*          case MiddleOperator.PostCallIntegral:
            GeneratePostCallIntegral(middleCode);
            break;

          case MiddleOperator.PostCallFloating:
            GeneratePostCallFloating(middleCode);
            break;*/

          case MiddleOperator.Return:
            GenerateReturn(middleCode);
            break;

          /*case MiddleOperator.Exit:
            GenerateExit(middleCode);
            break;*/

          case MiddleOperator.Goto:
            GenerateGoto(middleCode);
            break;

          case MiddleOperator.AssignRegister:
            GenerateLoadToRegister(middleCode);
            break;

          /*case MiddleOperator.SaveFromRegister:
            GenerateSaveFromRegister(middleCode);
            break;

          case MiddleOperator.LoadStatusByte:
            GenerateLoadStatusByte(middleCode);
            break;*/
          
          case MiddleOperator.InspectRegister:
            GenerateInspectRegister(middleCode);
            break;

          case MiddleOperator.InspectFlagByte:
            GenerateFlagByte(middleCode);
            break;

          /*case MiddleOperator.SaveTemporary:
            GenerateSaveTemporary(middleCode);
            break;*/

          case MiddleOperator.JumpRegister:
            GenerateJumpRegister(middleCode);
            break;

          case MiddleOperator.Interrupt:
            GenerateInterrupt(middleCode);
            break;

          case MiddleOperator.Assign: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsStructOrUnion()) {
                GenerateStructUnionAssign(middleCode, middleIndex);
              }
              else  {
                GenerateIntegralAssign(middleCode);
              }
            }
            break;
        
          case MiddleOperator.BitwiseAnd:
          case MiddleOperator.BitwiseIOr:
          case MiddleOperator.BitwiseXOr:
          case MiddleOperator.ShiftLeft:
          case MiddleOperator.ShiftRight:
            GenerateIntegralAdditionBitwiseShift(middleCode);
            break;
          
          case MiddleOperator.BinaryAdd:
          case MiddleOperator.BinarySubtract: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsFloating()) {
                GenerateFloatingBinary(middleCode);
              }
              else  {
                GenerateIntegralAdditionBitwiseShift(middleCode);
              }
            }
            break;

          case MiddleOperator.SignedMultiply:
          case MiddleOperator.SignedDivide:
          case MiddleOperator.SignedModulo:
          case MiddleOperator.UnsignedMultiply:
          case MiddleOperator.UnsignedDivide:
          case MiddleOperator.UnsignedModulo: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsFloating()) {
                GenerateFloatingBinary(middleCode);
              }
              else  {
                GenerateIntegralMultiply(middleCode);
              }
            }
            break;

          case MiddleOperator.Carry:
          case MiddleOperator.NotCarry:
            GenerateCarryExpression(middleCode);
            break;

          case MiddleOperator.Equal:
          case MiddleOperator.NotEqual:
          case MiddleOperator.SignedLessThan:
          case MiddleOperator.SignedLessThanEqual:
          case MiddleOperator.SignedGreaterThan:
          case MiddleOperator.SignedGreaterThanEqual:
          case MiddleOperator.UnsignedLessThan:
          case MiddleOperator.UnsignedLessThanEqual:
          case MiddleOperator.UnsignedGreaterThan:
          case MiddleOperator.UnsignedGreaterThanEqual: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsFloating()) {
                GenerateFloatingRelation(middleCode, middleIndex);
              }
              else  {
                GenerateIntegralRelation(middleCode, middleIndex);
              }
            }
            break;

          case MiddleOperator.Case:
            GenerateCase(middleCode);
            break;

          case MiddleOperator.CaseEnd:
            GenerateCaseEnd(middleCode);
            break;

          case MiddleOperator.Increment:
          case MiddleOperator.Decrement:
            GenerateIntegralIncrementDecrement(middleCode);
            break;

          case MiddleOperator.UnaryAdd:
          case MiddleOperator.UnarySubtract:
          case MiddleOperator.BitwiseNot: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsFloating()) {
                GenerateFloatingUnary(middleCode);
              }
              else  {
                GenerateIntegralUnary(middleCode);
              }
            }
            break;
          
          case MiddleOperator.Dot:
            // Empty.
            break;
          
          case MiddleOperator.address:
            GenerateAddress(middleCode);
            break;

          case MiddleOperator.Deref:
            GenerateDeref(middleCode);
            break;

          case MiddleOperator.DecreaseStack:
            Assert.Error((--m_stackSize) >= 0);
            break;

          case MiddleOperator.CheckTrackMapFloatStack:
            Assert.Error((m_trackMap.Count == 0) && (m_stackSize == 0));
            break;

          case MiddleOperator.PushZero:
            GeneratePushZero(middleCode);
            break;

          case MiddleOperator.PushOne:
            GeneratePushOne(middleCode);
            break;

          case MiddleOperator.PushFloat:
            GeneratePushFloat(middleCode);
            break;

          case MiddleOperator.TopFloat:
            GenerateTopFloat(middleCode);
            break;
          
          case MiddleOperator.PopFloat:
            GeneratePopFloat(middleCode);
            break;

          case MiddleOperator.PopEmpty:
            GeneratePopEmpty(middleCode);
            break;

          case MiddleOperator.IntegralToIntegral:
            GenerateIntegralToIntegral(middleCode, middleIndex);
            break;

          case MiddleOperator.IntegralToFloating:
            GenerateIntegralToFloating(middleCode);
            break;

          case MiddleOperator.FloatingToIntegral:
            GenerateFloatingToIntegral(middleCode);
            break;

          case MiddleOperator.Parameter: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);

              if (symbol.Type.IsFloating()) {
                GenerateFloatingParameter(middleCode);
              }
              else if (symbol.Type.IsStructOrUnion()) {
                GenerateStructUnionParameter(middleCode, middleIndex);
              }
              else  {
                GenerateIntegralParameter(middleCode);
              }
            }
            break;
          
          case MiddleOperator.GetReturnValue: {
              Symbol symbol = (Symbol) middleCode.GetOperand(0);

              if (symbol.Type.IsStructOrUnion()) {
                GenerateStructUnionGetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating()) {
                Assert.Error((++m_stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
              }
              else {
                GenerateIntegralGetReturnValue(middleCode);
              }
            }
            break;
          
          case MiddleOperator.SetReturnValue: {
              Symbol symbol = (Symbol) middleCode.GetOperand(1);
            
              if (symbol.Type.IsStructOrUnion()) {
                GenerateStructUnionSetReturnValue(middleCode);
              }
              else if (symbol.Type.IsFloating()) {
                Assert.Error((--m_stackSize) == 0);
              }
              else {
                GenerateIntegralSetReturnValue(middleCode);
              }
            }
            break;

          case MiddleOperator.ClearRegisters:
          case MiddleOperator.FunctionEnd:
          case MiddleOperator.Empty:
            break;

          case MiddleOperator.SystemInit:
            GenerateSystemInit(middleCode);
            break;

          case MiddleOperator.SystemParameter:
            GenerateSystemParameter(middleCode);
            break;

          case MiddleOperator.SystemCall:
            GenerateSystemCall(middleCode, middleIndex);
            break;

          case MiddleOperator.AssignInitSize:
          case MiddleOperator.ParameterInitSize:
            break;
          
          default:
            Assert.Error("object code switch default: " +
                         Enum.GetName(typeof(MiddleOperator), middleOp));
            break;
        }

        /*if (Start.CurrentFunction.Name.Equals("exp")) {
          Console.Out.WriteLine(middleCode.ToString() + ": " + m_stackSize);
        }*/
      }

      if (m_middleToObjectMap != null) {
        for (int assemblyIndex = 0; assemblyIndex < m_assemblyCodeList.Count; ++assemblyIndex) {
          AssemblyCode assemblyCode = m_assemblyCodeList[assemblyIndex];

          if (assemblyCode.Operator == AssemblyOperator.new_code) {
            int middleIndex = (int) assemblyCode.GetOperand(0);
            m_middleToObjectMap.Add(middleIndex, assemblyIndex);
            assemblyCode.Operator = AssemblyOperator.empty;
          }
        }
      }
    }

    // ------------------------------------------------------------------------

    private Track SetPointer(Track track, Symbol symbol) {
      if (track.Pointer) {
        return track;
      }

      if (track.Register == null) {
        track.Pointer = true;
        return track;
      }

      if (!RegisterAllocator.m_pointerRegisterSet.Contains(track.Register.Value)) {
        Track newTrack = new Track(symbol);
        AddAssemblyCode(AssemblyOperator.mov, newTrack, track.Register);
        m_trackMap[symbol] = newTrack;
        m_trackSet.Add(newTrack);
        newTrack.Pointer = true;
        return newTrack;
      }
      else {
        return track;
      }
    }

    public void AddToTrackMap(Symbol symbol, Track track) {
      Assert.Error(symbol != null);

      if (symbol.IsTemporary()) {
        m_trackMap[symbol] = track;
      }

      m_trackSet.Add(track);
    }
  
    /*public void RemoveFromTrackMap(Symbol symbol) {
      Assert.Error(symbol != null);

      if (symbol.IsTemporary()) {
        m_trackMap.Remove(symbol);
      }
    }*/

    public Register BaseRegister(Symbol symbol) {
      Assert.Error((symbol == null) || symbol.IsAutoOrRegister());
    
      if (Start.CurrentFunction.Type.IsEllipse() && ((symbol == null) || !symbol.IsParameter())) {
        return AssemblyCode.EllipseRegister;
      }
      else {
        return AssemblyCode.FrameRegister;
      }
    }

    public void GenerateFunctionStart(MiddleCode middleCode, int index) {
      if (Start.CheckStackHeap) {
        AddAssemblyCode(AssemblyOperator.comment, "Check Stack and Heap");
        AddAssemblyCode(AssemblyOperator.mov, Register.ax,
                      Start.CurrentTable.CurrentOffset);
        AddAssemblyCode(AssemblyOperator.register_return, Register.bx, index + 1);

        if (Start.CurrentFunction.Type.IsEllipse()) {
          AddAssemblyCode(AssemblyOperator.call, Start.CheckStackHeapFunctionEllipse);
        }
        else {
          AddAssemblyCode(AssemblyOperator.call, Start.CheckStackHeapFunctionRegular);
        }
      }
    }

    private Type m_returnType = null;
    private Track m_returnTrack = null;

    public void GenerateCallHeader(MiddleCode middleCode) {
      ISet<Symbol> integralSet = (ISet<Symbol>) middleCode.GetOperand(1);
      Assert.Error(integralSet.SequenceEqual(m_trackMap.Keys), "integral set != track map.keys");
      int stackSize = (int) middleCode.GetOperand(2);
      Assert.Error(stackSize == m_stackSize, "stack size");

      Register baseRegister = BaseRegister(null);
      int recordOffset = (int) middleCode.GetOperand(0), recordSize = 0;
      //int recordOffset = middleCode.SymbolTable().CurrentOffset;

      IDictionary<Track,int> postMap = new Dictionary<Track,int>();
      foreach (KeyValuePair<Symbol,Track> pair in m_trackMap) {
        Symbol symbol = pair.Key;
        //symbol.Offset = recordOffset + recordSize;
        Track track = pair.Value;
        AddAssemblyCode(AssemblyOperator.mov, baseRegister, recordOffset + recordSize, track);
        postMap[track] = recordOffset + recordSize;
        recordSize += symbol.Type.Size();
      }

      int totalSize = 0;
      foreach (int size in m_topStack) {
        totalSize += size;
      }

      for (int count = 0; count < (stackSize - totalSize); ++count) {
        AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister, totalSize * Type.QuarterWordSize +
                        recordOffset + recordSize);
        recordSize += Type.QuarterWordSize;
      }

      m_topStack.Push(stackSize - totalSize);
      m_recordStack.Push(recordSize);
      m_trackMapStack.Push(m_trackMap);
      m_postMapStack.Push(postMap);
      m_trackMap = new Dictionary<Symbol, Track>();
    }

    public void GenerateFunctionCall(MiddleCode middleCode, int index) {
      int recordSize = ((int) middleCode.GetOperand(0)) + ((int) middleCode.GetOperand(2));

      foreach (int size in m_recordStack) {
        recordSize += size;
      }

      /*int totalSize = 0;
      foreach (int size in m_topStack) {
        totalSize += size;
      }

      if (m_topStack.Count > 1) {
        int i = 1;
      }

      if (totalSize == 2) {
        int i = 1;
      }

      recordSize += totalSize * Type.QuarterWordSize;*/

      Symbol calleeSymbol = (Symbol) middleCode.GetOperand(1);
      int extraSize = (int) middleCode.GetOperand(3);

      if (Start.CurrentFunction.Name.Equals("atan") &&
          calleeSymbol.Name.Equals("sqrt")) {
        recordSize += 8;
      }

      Type calleeType = calleeSymbol.Type.IsFunction() ? calleeSymbol.Type :
                        calleeSymbol.Type.PointerType;

      bool callerEllipse = Start.CurrentFunction.Type.IsEllipse(),
           calleeEllipse = calleeType.IsEllipse();

      Register frameRegister = callerEllipse ? AssemblyCode.EllipseRegister : AssemblyCode.FrameRegister;               

      AddAssemblyCode(AssemblyOperator.address_return, frameRegister,
                      recordSize + SymbolTable.ReturnAddressOffset, index + 1);

      AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                      recordSize + SymbolTable.RegularFrameOffset,
                      AssemblyCode.FrameRegister);

      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.mov, frameRegister,
                        recordSize + SymbolTable.EllipseFrameOffset,
                        AssemblyCode.EllipseRegister);
      }

      Track jumpTrack = null;
      if (!calleeSymbol.Type.IsFunction()) {
        jumpTrack = LoadValueToRegister(calleeSymbol);
      }

      AddAssemblyCode(AssemblyOperator.add, frameRegister, // add di, 10
                      recordSize);

      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister, // mov bp, di
                        AssemblyCode.EllipseRegister);
      }
      else {
        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                          AssemblyCode.FrameRegister);
        }
      }

      if (calleeEllipse && (extraSize > 0)) {
        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,
                        extraSize);
      }

/*      if (callerEllipse) {
        AddAssemblyCode(AssemblyOperator.address_return, AssemblyCode.EllipseRegister,
                      recordSize + SymbolTable.ReturnAddressOffset, index + 1);

        AddAssemblyCode(AssemblyOperator.mov,AssemblyCode.EllipseRegister,
                             recordSize + SymbolTable.RegularFrameOffset,
                             AssemblyCode.FrameRegister);
        AddAssemblyCode(AssemblyOperator.mov,AssemblyCode.EllipseRegister,
                             recordSize + SymbolTable.EllipseFrameOffset,
                             AssemblyCode.EllipseRegister);

        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister, AssemblyCode.EllipseRegister, null);

        if (!calleeSymbol.Type.IsFunction()) {
          jumpTrack = LoadValueToRegister(calleeSymbol);
        }

        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.FrameRegister, recordSize, null);

        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,
                                         recordSize + extraSize, null);
        }
      }
      else {
        AddAssemblyCode(AssemblyOperator.address_return, AssemblyCode.FrameRegister,
                      recordSize + SymbolTable.ReturnAddressOffset, index + 1);

        AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                                       recordSize + SymbolTable.RegularFrameOffset,
                                       AssemblyCode.FrameRegister);

        if (!calleeSymbol.Type.IsFunction()) {
          jumpTrack = LoadValueToRegister(calleeSymbol);
        }

        AddAssemblyCode(AssemblyOperator.add, AssemblyCode.FrameRegister, recordSize, null);

        if (calleeEllipse) {
          AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                                         AssemblyCode.FrameRegister);
          Assert.Error(extraSize >= 0);
          if (extraSize > 0) {
            AddAssemblyCode(AssemblyOperator.add, AssemblyCode.EllipseRegister,  extraSize);
          }
        }
      }*/

      if (calleeSymbol.Type.IsFunction()) {
        AddAssemblyCode(AssemblyOperator.call, calleeSymbol.UniqueName, null, null);
        m_returnType = calleeSymbol.Type.ReturnType;
        //m_returnFloat = calleeSymbol.Type.ReturnType.IsFloating();
      }
      else {
        AddAssemblyCode(AssemblyOperator.long_jmp, jumpTrack, null, null);
        m_returnType = calleeSymbol.Type.PointerType.ReturnType;
        //m_returnFloat = calleeSymbol.Type.PointerType.ReturnType.IsFloating();
      }            
    }
  
    public void GenerateFunctionPostCall(MiddleCode middleCode) {
      Register baseRegister = BaseRegister(null);
      m_trackMap = m_trackMapStack.Pop();
      IDictionary<Track,int> postMap = m_postMapStack.Pop();

      if (m_returnType.IsIntegralOrPointer() || m_returnType.IsStructOrUnion()) {
        m_returnTrack = null;

        foreach (KeyValuePair<Track,int> pair in postMap) {
          Track track = pair.Key;

          if (AssemblyCode.RegisterOverlap(track.Register, AssemblyCode.ReturnValueRegister)) {
            m_returnTrack = new Track(m_returnType);
            m_trackSet.Add(m_returnTrack);
            Register returnRegister = AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, m_returnType.Size());
            AddAssemblyCode(AssemblyOperator.mov, m_returnTrack, returnRegister);
          }

          int offset = pair.Value;
          AddAssemblyCode(AssemblyOperator.mov, track, baseRegister, offset);
        }

        if (m_returnTrack == null) {
          m_returnTrack = new Track(m_returnType);
          m_returnTrack.Register = AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, m_returnType.Size());
        }
      }
      else {
        foreach (KeyValuePair<Track,int> pair in postMap) {
          Track track = pair.Key;
          int offset = pair.Value;
          AddAssemblyCode(AssemblyOperator.mov, track, baseRegister, offset);
        }
      }

      /*foreach (KeyValuePair<Symbol,Track> pair in m_trackMap) {
        Symbol symbol = pair.Key;
        Track track = pair.Value;
        AddAssemblyCode(ObjectOperator.mov, track, baseRegister, symbol.Offset);
      }*/

      int topSize = m_topStack.Pop();
      if (topSize > 0) {
        int recordOffset = (int) middleCode.GetOperand(2);
        int recordSize = (int) m_recordStack.Pop();

        if (Start.CurrentFunction.Name.Equals("atan") && (topSize == 2)) {
          recordSize += 8;
        }

        if (m_returnType.IsFloating()) {
          AddAssemblyCode(AssemblyOperator.fstp_qword, baseRegister, recordOffset + recordSize);
        }

        int currentOffset = recordOffset + recordSize;
        for (int count = 0; count < topSize; ++count) {
          currentOffset -= Type.QuarterWordSize;
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister, currentOffset);
        }

        if (m_returnType.IsFloating()) {
          AddAssemblyCode(AssemblyOperator.fld_qword, baseRegister, recordOffset + recordSize);
        }
      }
      else {
        m_recordStack.Pop();
      }
    }

    public Track LoadValueToRegister(Symbol symbol) {
      return LoadValueToRegister(symbol, null);
    }

    public Track LoadValueToRegister(Symbol symbol, Register? register) {
      if (register != null) {
        CheckRegister(symbol, register);
      }
    
      if (m_trackMap.ContainsKey(symbol)) {
        Track track = m_trackMap[symbol];
        m_trackMap.Remove(symbol);
      
        if (register != null) {
          if (track.Register == null) {
            if (register == track.getForbidRegister()) {
              Track newTrack = new Track(symbol);
              newTrack.Size = track.Size;
              newTrack.Pointer = track.Pointer;
              AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
              newTrack.Register = register;
              m_trackMap.Add(symbol, newTrack);
              track = newTrack;
            }
            else {
              track.Register = register;
            }

            foreach (Track twinTrack in track.TwinTrackSet) {
              Assert.Error((twinTrack.Register == null) || twinTrack.Register.Equals(register));
              twinTrack.Register = register;
            }
          }
          else {
            Register fromRegister =
              AssemblyCode.RegisterToSize(track.Register, symbol.Type.Size());
        
            if (register != fromRegister) {
              Track newTrack = new Track(symbol, register.Value);
              AddToTrackMap(symbol, newTrack);
              AddAssemblyCode(AssemblyOperator.mov, newTrack, track);
              track = newTrack;
            }
          }
        }

        return track;
      }
      else {
        Track track = new Track(symbol, register);
        AddToTrackMap(symbol, track);
        LoadValueToTrack(track, symbol);
        return track;
      }
    }
  
    private void LoadValueToTrack(Track track, Symbol symbol) {
      if (symbol.HasValue() && (symbol.Value is BigInteger)) {
        int intValue = int.Parse(symbol.Value.ToString());
        AddAssemblyCode(AssemblyOperator.mov, track, intValue);
      }
      else if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        AddAssemblyCode(AssemblyOperator.mov, track, staticAddress.Name);

        if (staticAddress.Offset > 0) {
          AddAssemblyCode(AssemblyOperator.add, track, staticAddress.Offset);
        }
      }
      else if (symbol.Type.IsFunctionArrayStringStructOrUnion()) {
        if (symbol.IsAutoOrRegister()) {
          AddAssemblyCode(AssemblyOperator.mov, track, BaseRegister(symbol));
          AddAssemblyCode(AssemblyOperator.add, track, symbol.Offset);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, track, symbol.UniqueName);
        }
      }
      else if (symbol.Switch) {
        AddAssemblyCode(AssemblyOperator.mov, track, BaseRegister(symbol), symbol.Offset);
        m_trackMap.Remove(symbol);
      }
      else if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        //addressTrack = SetPointer(addressTrack);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        AddAssemblyCode(AssemblyOperator.mov, track, addressTrack, symbol.AddressOffset);
      }
      else if (symbol.IsAutoOrRegister()) {
        AddAssemblyCode(AssemblyOperator.mov, track, BaseRegister(symbol), symbol.Offset);
        m_trackMap.Remove(symbol);
      }
      else if (symbol.Value is StaticAddress) {
        StaticAddress staticAddress = (StaticAddress) symbol.Value;
        AddAssemblyCode(AssemblyOperator.mov, track, staticAddress.Name, staticAddress.Offset);
        m_trackMap.Remove(symbol);
      }
      else {
        AddAssemblyCode(AssemblyOperator.mov, track, symbol.UniqueName, symbol.Offset);
      }
    }
  
    private int FindFirstTrackLine(Track track) {
      for (int index = 0; index < m_assemblyCodeList.Count; ++index) {
        AssemblyCode assemblyCode = m_assemblyCodeList[index];

        if (track.Equals(assemblyCode.GetOperand(0)) ||
            track.Equals(assemblyCode.GetOperand(1)) ||
            track.Equals(assemblyCode.GetOperand(2))) {
          return index;
        }    
      }

      Assert.Error(false);
      return 0;
    }

    private int FindLastTrackLine(Track track) {
      for (int index = m_assemblyCodeList.Count - 1; index >= 0; --index) {
        AssemblyCode assemblyCode = m_assemblyCodeList[index];

        if (track.Equals(assemblyCode.GetOperand(0)) ||
            track.Equals(assemblyCode.GetOperand(1)) ||
            track.Equals(assemblyCode.GetOperand(2))) {
          return index;
        }    
      }

      Assert.Error(false);
      return 0;
    }

    public void CheckRegister(Symbol symbol, Register? register) {
      foreach (KeyValuePair<Symbol,Track> entry in m_trackMap) {
        Symbol oldSymbol = entry.Key;
        Track oldTrack = entry.Value;

        if (!oldSymbol.Equals(symbol) &&
            AssemblyCode.RegisterOverlap(register, oldTrack.Register)) {
          Track newTrack = new Track(oldSymbol);
          m_trackMap[oldSymbol] = newTrack;
          m_trackSet.Add(newTrack);

          int lastOldTrackLine = FindLastTrackLine(oldTrack);
          AssemblyCode setCode = new AssemblyCode(AssemblyOperator.set_track_size, newTrack, oldTrack);
          AssemblyCode movCode = new AssemblyCode(AssemblyOperator.mov, newTrack, oldTrack);
          m_assemblyCodeList.Insert(lastOldTrackLine + 1, setCode);
          m_assemblyCodeList.Insert(lastOldTrackLine + 2, movCode);
          break;
        }
      }
    }

    public void CheckRegisterX(Symbol symbol, Register? register) {
      foreach (KeyValuePair<Symbol,Track> entry in m_trackMap) {
        if (AssemblyCode.RegisterOverlap(register, entry.Value.Register)) {
          Symbol oldSymbol = entry.Key;

          if (!oldSymbol.Equals(symbol)) {
            Track oldTrack = entry.Value;

            Track previousTrack = oldTrack.PreviousTrack;
            
            //Assert.Error(previousTrack != null);
            if (previousTrack != null) {
              Assert.Error(oldTrack.FirstLine != -1);
              Assert.Error((m_assemblyCodeList[oldTrack.FirstLine].Operator == AssemblyOperator.empty) &&
                           (m_assemblyCodeList[oldTrack.FirstLine + 1].Operator == AssemblyOperator.empty));

              m_assemblyCodeList[oldTrack.FirstLine] = new AssemblyCode(AssemblyOperator.set_track_size, oldTrack, previousTrack, null);
              m_assemblyCodeList[oldTrack.FirstLine + 1] = new AssemblyCode(AssemblyOperator.mov, oldTrack, previousTrack, null);

              //AddAssemblyCode(ObjectOperator.set_track_size, newTrack, oldTrack);
              //AddAssemblyCode(ObjectOperator.mov, newTrack, oldTrack);
              //m_trackMap[oldSymbol] = previousTrack;
              oldTrack.Register = null;
              oldTrack.PreviousTrack = null;
            }
            else {
              m_trackMap.Remove(oldSymbol);
            }
            break;
          }
        }
      }
    }
  
/*    public void GeneratePostCall(MiddleCode middleCode) {
      int record = (int) middleCode.GetOperand(0);
      Symbol calleeSymbol = (Symbol) middleCode.GetOperand(1);
      int stackSize = (int) middleCode.GetOperand(3);

      if (stackSize > 0) {
        Symbol returnSymbol = null;

        if (calleeSymbol.Type.ReturnType.IsFloating()) {
          returnSymbol = new Symbol(Type.LongDoubleType);
          returnSymbol.Offset = record;
          PopTopSymbol(returnSymbol, TopOrPop.Pop);
          //PopSymbol(returnSymbol);
        }

        for (int stackIndex = 0; stackIndex < stackSize; ++stackIndex) {
          Symbol stackSymbol = new Symbol(Type.LongDoubleType);
          stackSymbol.Offset = record - ((stackIndex + 1) * Type.QuarterWordSize);
          PushSymbol(stackSymbol);
        }

        if (calleeSymbol.Type.ReturnType.IsFloating()) {
          PushSymbol(returnSymbol);
        }
      }

      m_trackMap.Clear();
    }*/

    private int returnCount = 0;

    public void GenerateReturn(MiddleCode middleCode) {
      if (Start.CurrentFunction.UniqueName.Equals("main")) {
        AddAssemblyCode(AssemblyOperator.cmp_word, AssemblyCode.FrameRegister,
                        SymbolTable.ReturnAddressOffset, 0);

        string labelText = "return_label$" + (returnCount++);
        AssemblyCode jumpCode = new AssemblyCode(AssemblyOperator.short_je, null, null, labelText);
        AddAssemblyCode(jumpCode);
        GenerateReturn();
        jumpCode.SetOperand(1, m_assemblyCodeList.Count);
        AddAssemblyCode(AssemblyOperator.label, labelText);
        GenerateExit(middleCode);
      }
      else {
        GenerateReturn();
      }
    }

    public void GenerateReturn() {
      Assert.Error(m_stackSize == 0);
      Track track = new Track(Type.UnsignedIntegerType, null);
      AddAssemblyCode(AssemblyOperator.mov, track,
                    AssemblyCode.FrameRegister, SymbolTable.ReturnAddressOffset);                
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.EllipseRegister,
                    AssemblyCode.FrameRegister, SymbolTable.EllipseFrameOffset);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister,
                    AssemblyCode.FrameRegister, SymbolTable.RegularFrameOffset);
      AddAssemblyCode(AssemblyOperator.jmp, track);
    }
  
    public void GenerateExit(MiddleCode middleCode) {
      Symbol exitSymbol = (Symbol) middleCode.GetOperand(0);

      if (exitSymbol == null) {
        AddAssemblyCode(AssemblyOperator.mov, Register.al, 0x00);
      }
      else {
        LoadValueToRegister(exitSymbol, Register.al);
        m_trackMap.Remove(exitSymbol);
      }

      AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C);
      AddAssemblyCode(AssemblyOperator.interrupt, 33);
    }

    public void GenerateGoto(MiddleCode middleCode) {
      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, middleCode.GetOperand(0));
    }

    public void GenerateLoadToRegister(MiddleCode middleCode) {
      Register register = (Register) middleCode.GetOperand(0);
      Symbol symbol = (Symbol) middleCode.GetOperand(1);
      LoadValueToRegister(symbol, register);
      m_trackMap.Remove(symbol);
    }

    public void GenerateSaveFromRegister(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Register register = (Register) middleCode.GetOperand(1);
      Track track = new Track(symbol, register);
      AddToTrackMap(symbol, track);
      SaveFromRegister(track, symbol);
      m_trackMap.Remove(symbol);
    }
  
    /*public void GenerateLoadStatusByte(MiddleCode middleCode) {
      AddAssemblyCode(ObjectOperator.lahf);
    }*/

    public void GenerateInspectRegister(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Register register = (Register) middleCode.GetOperand(1);
      Track track = new Track(symbol, register);
      m_trackSet.Add(track);
      m_trackMap.Add(symbol, track);
    }

    public void GenerateFlagByte(MiddleCode middleCode) {
      AddAssemblyCode(AssemblyOperator.lahf);
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Assert.Error(symbol.Type.Size() == 1);
      Track track = new Track(symbol, Register.ah);
      m_trackSet.Add(track);
      m_trackMap.Add(symbol, track);
    }

    public void GenerateSystemInit(MiddleCode middleCode) {
      String name = (String) middleCode.GetOperand(0);
      SystemCode.GenerateInit(name, this);
    }

    public void GenerateSystemParameter(MiddleCode middleCode) {
      String name = (String) middleCode.GetOperand(0);
      int index = (int) middleCode.GetOperand(1);
      Symbol argSymbol = (Symbol) middleCode.GetOperand(2);
      SystemCode.GenerateParameter(name, index, argSymbol, this);
    }

    public void GenerateSystemCall(MiddleCode middleCode, int middleIndex) {
      String name = (String) middleCode.GetOperand(0);
      Symbol returnSymbol = (Symbol) middleCode.GetOperand(1);
      SystemCode.GenerateCall(name, returnSymbol, this, middleIndex);
    }

    /*public void GenerateFlagByte(MiddleCode middleCode) {
      AddAssemblyCode(ObjectOperator.lahf);
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Assert.Error(symbol.Type.Size() == 1);
      AddAssemblyCode(ObjectOperator.mov, Base(symbol), Offset(symbol), Register.ah);
    }*/

/*    public void GenerateSaveFromFlagByte(MiddleCode middleCode) {
      AddAssemblyCode(ObjectOperator.sahf);
      //Symbol symbol = (Symbol) middleCode.GetOperand(0);
      Symbol symbol = new Symbol(Type.UnsignedCharType);
      //Assert.Error(symbol.Type.Size() == 1);
      Track track = new Track(symbol, Register.ah);
      AddToTrackMap(symbol, track);
      //AddAssemblyCode(ObjectOperator.mov, Base(symbol), Offset(symbol), Register.ah);
    }*/

    public void GenerateSaveTemporary(MiddleCode middleCode) {
      Symbol tempSymbol = (Symbol) middleCode.GetOperand(0);
      Track tempTrack = m_trackMap[tempSymbol];
      m_trackMap.Remove(tempSymbol);
      SaveFromRegister(tempTrack, tempSymbol);
    }
  
    public void GenerateJumpRegister(MiddleCode middleCode) {
      Register jumpRegister = (Register) middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.jmp, jumpRegister);
    }

    public void GenerateInterrupt(MiddleCode middleCode) {
      int intValue = int.Parse(middleCode.GetOperand(0).ToString());
      AddAssemblyCode(AssemblyOperator.interrupt, intValue);
      m_trackMap.Clear();
    }

    public void GenerateCarryExpression(MiddleCode middleCode) {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, middleCode.GetOperand(0));
    }

    public void GenerateCase(MiddleCode middleCode) {
      Symbol switchSymbol = (Symbol) middleCode.GetOperand(1);
      Track switchTrack;

      if (m_trackMap.ContainsKey(switchSymbol)) {
        switchTrack = m_trackMap[switchSymbol];
      }
      else {
        switchTrack = new Track(switchSymbol);
        m_trackMap.Add(switchSymbol, switchTrack);
        m_trackSet.Add(switchTrack);
        AddAssemblyCode(AssemblyOperator.mov, switchTrack, Base(switchSymbol), Offset(switchSymbol));
      }

      Symbol caseSymbol = (Symbol) middleCode.GetOperand(2);
      int caseValue = (int) ((BigInteger) caseSymbol.Value);
      AddAssemblyCode(AssemblyOperator.cmp, switchTrack, caseValue); // cmp ax, 123
      //m_trackMap.Remove(symbol);
      int target = (int) middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.long_je, null, null, target);
    }


    public void GenerateCaseX(MiddleCode middleCode) {
      Symbol switchSymbol = (Symbol) middleCode.GetOperand(1);
      Track track = null;
      m_trackMap.TryGetValue(switchSymbol, out track);
      Symbol caseSymbol = (Symbol) middleCode.GetOperand(2);
      BigInteger caseValue = (BigInteger) caseSymbol.Value;

      if (track != null) {
        AddAssemblyCode(AssemblyOperator.cmp, track, (int) caseValue); // cmp ax, 123
        //m_trackMap.Remove(symbol);
      }
      else {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.cmp, switchSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(switchSymbol), Offset(switchSymbol), (int) caseValue);
      }

      int target = (int) middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.long_je, null, null, target);
    }

    public void GenerateCaseEnd(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      m_trackMap.Remove(symbol);
    }

    // x += y; => x = x + y;
    // x += 1; => x = x + 1; => ++x;
    // t1 = x + y;
  
    public void GenerateIntegralAssign(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0),
             assignSymbol = (Symbol) middleCode.GetOperand(1);
      GenerateAssignParameter(middleCode.Operator, resultSymbol, assignSymbol);
    }
  
    public void SaveFromRegister(Track track, Symbol symbol) {
      if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        AddAssemblyCode(AssemblyOperator.mov, addressTrack, symbol.AddressOffset, track);
      }
      else if (symbol.IsAutoOrRegister()) {
        AddAssemblyCode(AssemblyOperator.mov, BaseRegister(symbol), symbol.Offset, track);
      }
      else {
        AddAssemblyCode(AssemblyOperator.mov, symbol.UniqueName, symbol.Offset, track);
      }
    }

    public static IDictionary<MiddleOperator,AssemblyOperator>
                           m_middleToIntegralBinaryTargetMap =
                           new Dictionary<MiddleOperator,AssemblyOperator>();

    public static IDictionary<int,Register> LeftMultiplyMap = new Dictionary<int,Register>();

    public static IDictionary<Pair<MiddleOperator, int>, Register>
                    ResultMultiplyMap = new Dictionary<Pair<MiddleOperator, int>, Register>();

    public static IDictionary<Pair<MiddleOperator, int>, Register>
                    ExtraMultiplyMap = new Dictionary<Pair<MiddleOperator, int>, Register>();

    public static IDictionary<MiddleOperator, AssemblyOperator>
                    m_middleToIntegralUnaryTargetMap = new Dictionary<MiddleOperator, AssemblyOperator>();

    public static IDictionary<Sort, AssemblyOperator> m_floatPushMap = new Dictionary<Sort, AssemblyOperator>();

    public static IDictionary<Sort,AssemblyOperator> m_floatTopMap = new Dictionary<Sort,AssemblyOperator>(),
                                                  m_floatPopMap = new Dictionary<Sort,AssemblyOperator>();

    public static IDictionary<Pair<int, int>, int>
                    m_maskMap = new Dictionary<Pair<int, int>, int>();

    public static IDictionary<MiddleOperator,AssemblyOperator>
                    m_middleToFloatingBinaryTargetMap = new Dictionary<MiddleOperator,AssemblyOperator>();

    public static IDictionary<MiddleOperator,AssemblyOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,AssemblyOperator>();

    public static IDictionary<MiddleOperator,AssemblyOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,AssemblyOperator>();

    //public static IDictionary<AssemblyOperator,AssemblyOperator>
    //                m_inverseMap = new Dictionary<AssemblyOperator,AssemblyOperator>();

    static ObjectCodeGenerator() {
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Assign, AssemblyOperator.mov);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Parameter, AssemblyOperator.mov);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Compare, AssemblyOperator.cmp);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.add);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.sub);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseAnd, AssemblyOperator.and);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseIOr, AssemblyOperator.or);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.BitwiseXOr, AssemblyOperator.xor);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.ShiftLeft, AssemblyOperator.shl);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.ShiftRight, AssemblyOperator.shr);

      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Equal, AssemblyOperator.long_je);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.long_jne);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.Carry, AssemblyOperator.long_jc);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.NotCarry, AssemblyOperator.long_jnc);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.long_jl);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedLessThanEqual,AssemblyOperator.long_jle);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.long_jg);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.long_jge);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedLessThan, AssemblyOperator.long_jb);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedLessThanEqual, AssemblyOperator.long_jbe);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedGreaterThan, AssemblyOperator.long_ja);
      m_middleToIntegralBinaryTargetMap.
        Add(MiddleOperator.UnsignedGreaterThanEqual, AssemblyOperator.long_jae);

      LeftMultiplyMap.Add(Type.ShortSize, Register.al);
      LeftMultiplyMap.Add(Type.IntegerSize, Register.ax);
      LeftMultiplyMap.Add(Type.LongSize, Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.ShortSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.ShortSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);

      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.edx);

      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.edx);

      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator, int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);

      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd, AssemblyOperator.empty);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot, AssemblyOperator.not);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.neg);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Increment, AssemblyOperator.inc);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Decrement, AssemblyOperator.dec);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.imul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedModulo, AssemblyOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedMultiply, AssemblyOperator.mul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedDivide, AssemblyOperator.div);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedModulo, AssemblyOperator.div);

      m_floatPushMap.Add(Sort.Signed_Int, AssemblyOperator.fild_word);
      m_floatPushMap.Add(Sort.Unsigned_Int, AssemblyOperator.fild_word);
      m_floatPushMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fild_dword);
      m_floatPushMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fild_dword);
      m_floatPushMap.Add(Sort.Float, AssemblyOperator.fld_dword);
      m_floatPushMap.Add(Sort.Double, AssemblyOperator.fld_qword);
      m_floatPushMap.Add(Sort.Long_Double, AssemblyOperator.fld_qword);

      m_floatTopMap.Add(Sort.Signed_Int, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Unsigned_Int, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Pointer, AssemblyOperator.fist_word);
      m_floatTopMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fist_dword);
      m_floatTopMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fist_dword);
      m_floatTopMap.Add(Sort.Float, AssemblyOperator.fst_dword);
      m_floatTopMap.Add(Sort.Double, AssemblyOperator.fst_qword);
      m_floatTopMap.Add(Sort.Long_Double, AssemblyOperator.fst_qword);
  
      m_floatPopMap.Add(Sort.Signed_Int, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Unsigned_Int, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Pointer, AssemblyOperator.fistp_word);
      m_floatPopMap.Add(Sort.Signed_Long_Int, AssemblyOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Unsigned_Long_Int, AssemblyOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Float, AssemblyOperator.fstp_dword);
      m_floatPopMap.Add(Sort.Double, AssemblyOperator.fstp_qword);
      m_floatPopMap.Add(Sort.Long_Double, AssemblyOperator.fstp_qword);    

      m_maskMap.Add(new Pair<int,int>(1,2), 0x000000FF);
      m_maskMap.Add(new Pair<int,int>(1,4), 0x000000FF);
      m_maskMap.Add(new Pair<int,int>(2,4), 0x0000FFFF);  

      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, AssemblyOperator.fadd);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, AssemblyOperator.fsub);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedMultiply, AssemblyOperator.fmul);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedDivide, AssemblyOperator.fdiv);

      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, AssemblyOperator.long_je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, AssemblyOperator.long_jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, AssemblyOperator.long_ja);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, AssemblyOperator.long_jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, AssemblyOperator.long_jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, AssemblyOperator.long_jbe);

      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,AssemblyOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,AssemblyOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, AssemblyOperator.fchs);

      /*m_inverseMap.Add(AssemblyOperator.je, AssemblyOperator.jne);
      m_inverseMap.Add(AssemblyOperator.jne, AssemblyOperator.je);
      m_inverseMap.Add(AssemblyOperator.jl, AssemblyOperator.jge);
      m_inverseMap.Add(AssemblyOperator.jle, AssemblyOperator.jg);
      m_inverseMap.Add(AssemblyOperator.jg, AssemblyOperator.jle);
      m_inverseMap.Add(AssemblyOperator.jge, AssemblyOperator.jl);
      m_inverseMap.Add(AssemblyOperator.ja, AssemblyOperator.jbe);
      m_inverseMap.Add(AssemblyOperator.jae, AssemblyOperator.jb);
      m_inverseMap.Add(AssemblyOperator.jb, AssemblyOperator.jae);
      m_inverseMap.Add(AssemblyOperator.jbe, AssemblyOperator.ja);*/
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    public void GenerateFloatingRelation(MiddleCode middleCode, int index) {
      int target = (int) middleCode.Operand(0);
      AddAssemblyCode(ObjectOperator.fcompp);
      AddAssemblyCode(ObjectOperator.fstsw, Register.ax);
      AddAssemblyCode(ObjectOperator.sahf);
      ObjectOperator objectOperator =
        m_middleToFloatingRelationTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, index + 1);
      AddAssemblyCode(ObjectOperator.long_jmp, null, null, target);
    }

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }*/

    public void GenerateFloatingRelation(MiddleCode middleCode, int index) {
      Assert.Error((m_stackSize -= 2) >= 0);
      int target = (int) middleCode.GetOperand(0);
      AddAssemblyCode(AssemblyOperator.fcompp);
      AddAssemblyCode(AssemblyOperator.fstsw, Register.ax);
      AddAssemblyCode(AssemblyOperator.sahf);
      AssemblyOperator objectOperator = m_middleToFloatingRelationTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, target);
//      AddAssemblyCode(objectOperator, null, null, index + 1);
//      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, target);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }*/

    public void GenerateIntegralAdditionBitwiseShift(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0),
             leftSymbol = (Symbol) middleCode.GetOperand(1),
             rightSymbol = (Symbol) middleCode.GetOperand(2);
      
      if (resultSymbol.Equals(leftSymbol) && !resultSymbol.IsTemporary()) {
        GenerateCompoundIntegralBinary(middleCode.Operator, leftSymbol, rightSymbol);
      }
      else {
        //GenerateIntegralBinary(middleCode.Operator, resultSymbol, leftSymbol, rightSymbol);
        GenerateSimpleIntegralBinary(middleCode.Operator, resultSymbol, leftSymbol, rightSymbol);
      }
    }
  
    public void GenerateCompoundIntegralBinary(MiddleOperator middleOperator,
                                               Symbol leftSymbol, Symbol rightSymbol) {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleOperator];
      //Assert.Error(!m_trackMap.ContainsKey(leftSymbol));

      if (leftSymbol.AddressSymbol != null) {
        Track leftaddressTrack = LoadValueToRegister(leftSymbol.AddressSymbol);

        if (rightSymbol.Value is BigInteger) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(objectOperator, leftSymbol.Type.Size());
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(sizeOperator, leftaddressTrack, leftSymbol.AddressOffset, rightValue);
        }
        else {
          Track rightTrack;

          if (MiddleCode.IsShift(middleOperator)) {
            rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
          }
          else {
            rightTrack = LoadValueToRegister(rightSymbol);
          }

          AddAssemblyCode(objectOperator, leftaddressTrack, leftSymbol.AddressOffset, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }

        m_trackMap.Remove(leftSymbol.AddressSymbol);
      }
      else if (leftSymbol.IsStaticOrExtern()) {
        if (rightSymbol.Value is BigInteger) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(objectOperator, leftSymbol.Type.Size());
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(sizeOperator, leftSymbol.UniqueName, 0, rightValue);
        }
        else {
          Track rightTrack;

          if (MiddleCode.IsShift(middleOperator)) {
            rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
          }
          else {
            rightTrack = LoadValueToRegister(rightSymbol);
          }

          AddAssemblyCode(objectOperator, leftSymbol.UniqueName, 0, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
      }
      else { // leftSymbol.IsAutoOrRegister()
        if (rightSymbol.Value is BigInteger) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(objectOperator, leftSymbol.Type.Size());
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(sizeOperator, BaseRegister(leftSymbol), leftSymbol.Offset, rightValue);
        }
        else {
          Track rightTrack;

          if (MiddleCode.IsShift(middleOperator)) {
            rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
          }
          else {
            rightTrack = LoadValueToRegister(rightSymbol);
          }

          AddAssemblyCode(objectOperator, BaseRegister(leftSymbol), leftSymbol.Offset, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
      }
    }

    public void GenerateSimpleIntegralBinary(MiddleOperator middleOperator, Symbol resultSymbol,
                                             Symbol leftSymbol, Symbol rightSymbol) {
      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleOperator];
      Track leftTrack = LoadValueToRegister(leftSymbol), rightTrack;

      if (rightSymbol.Value is BigInteger) {
        int rightValue = (int) ((BigInteger) rightSymbol.Value);
        AddAssemblyCode(objectOperator, leftTrack, rightValue);
      }
      else if (m_trackMap.TryGetValue(rightSymbol, out rightTrack)) {
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        m_trackMap.Remove(rightSymbol);
      }
      else if (MiddleCode.IsShift(middleOperator)) {
        rightTrack = LoadValueToRegister(rightSymbol, AssemblyCode.ShiftRegister);
        AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        m_trackMap.Remove(rightSymbol);
      }
      else if (rightSymbol.IsStaticOrExtern()) {
        if (rightSymbol.Type.IsArrayFunctionOrString()) {
          AddAssemblyCode(objectOperator, leftTrack, rightSymbol.UniqueName);
        }
        else {
          AddAssemblyCode(objectOperator, leftTrack, rightSymbol.UniqueName, 0);
        }
      }
      else if (rightSymbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(rightSymbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, rightSymbol.AddressSymbol);
        AddAssemblyCode(objectOperator, leftTrack, addressTrack, rightSymbol.AddressOffset);
      }
      else if (rightSymbol.Type.IsArray()) {
        if ((middleOperator == MiddleOperator.BinaryAdd) ||
            (middleOperator == MiddleOperator.BinarySubtract)) {
          AddAssemblyCode(objectOperator, leftTrack, BaseRegister(rightSymbol));

          if (rightSymbol.Offset != 0) {
            AddAssemblyCode(objectOperator, leftTrack, rightSymbol.Offset);
          }
        }
        else {
          rightTrack = new Track(rightSymbol);
          m_trackSet.Add(rightTrack);
          AddAssemblyCode(AssemblyOperator.mov, rightTrack, BaseRegister(rightSymbol));
          AddAssemblyCode(AssemblyOperator.add, rightTrack, rightSymbol.Offset);
          AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        }
      }
      else { // rightSymbol.isAutoOrRegister()
        AddAssemblyCode(objectOperator, leftTrack, BaseRegister(rightSymbol), rightSymbol.Offset);
      }

      if (resultSymbol.IsTemporary() && (resultSymbol.AddressSymbol == null)) {
        AddToTrackMap(resultSymbol, leftTrack);
      }
      else {
        SaveFromRegister(leftTrack, resultSymbol);
      }

      m_trackMap.Remove(leftSymbol);
    }

    /*public void GenerateIntegralBinary(MiddleOperator middleOperator, Symbol resultSymbol, Symbol leftSymbol, Symbol rightSymbol) {
      if (resultSymbol.UniqueName.Equals("int2$48#")) {
        int i = 1;
      }

      if (leftSymbol.UniqueName.Equals("int2$48#")) {
        int i = 1;
      }

      if (rightSymbol.UniqueName.Equals("int2$48#")) {
        int i = 1;
      }

      ObjectOperator objectOperator =
        m_middleToIntegralBinaryTargetMap[middleOperator];
      
      Track leftTrack = null;
    
      if ((resultSymbol != null) && !resultSymbol.Equals(leftSymbol)) {
        leftTrack = LoadValueToRegister(leftSymbol);
      }
      else {
        leftTrack = m_trackMap.ContainsKey(leftSymbol) ? m_trackMap[leftSymbol] : null;
      }
    
      if (leftTrack != null) {
        if (rightSymbol.Type.IsIntegralOrPointer() && rightSymbol.IsValue()) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(objectOperator, leftTrack, rightValue);
        }
        else if (rightSymbol.Type.IsString()) {
          AddAssemblyCode(objectOperator, leftTrack, rightSymbol.UniqueName);
        }
        else if (m_trackMap.ContainsKey(rightSymbol)) {
          Track rightTrack = m_trackMap[rightSymbol];
          AddAssemblyCode(objectOperator, leftTrack, rightTrack);
        
          if (MiddleCode.IsShift(middleOperator)) {
            rightTrack.Register = ObjectCode.ShiftRegister;
          }
        
          m_trackMap.Remove(rightSymbol);
        }
        else if (MiddleCode.IsShift(middleOperator)) {
          Track rightTrack = LoadValueToRegister(rightSymbol, ObjectCode.ShiftRegister);
          AddAssemblyCode(objectOperator, leftTrack, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
        else if ((rightSymbol.AddressSymbol != null) || rightSymbol.IsAutoOrRegister()) {
          object rightBase;
          int rightOffset;
        
          if (rightSymbol.AddressSymbol != null) {
            Track rightTrack = LoadValueToRegister(rightSymbol.AddressSymbol);
            rightTrack = SetPointer(rightTrack, rightSymbol.AddressSymbol);
            rightBase = rightTrack;
            rightOffset = rightSymbol.AddressOffset;
          }
          else {
            rightBase = BaseRegister(rightSymbol);
            rightOffset = rightSymbol.Offset;
          }

          if (rightSymbol.Type.IsArray()) {
            if (MiddleCode.IsAssociative(middleOperator)) {
              AddAssemblyCode(objectOperator, leftTrack, rightBase);
              AddAssemblyCode(objectOperator, leftTrack, rightOffset);
            }
            else if (MiddleCode.IsRelation(middleOperator)) {
              Track rightTrack = LoadValueToRegister(rightSymbol);
              AddAssemblyCode(objectOperator, leftTrack, rightTrack);
              m_trackMap.Remove(rightSymbol);
            }
            else {
              //int i = 1;
            }
          }
          else {
            AddAssemblyCode(objectOperator, leftTrack, rightBase, rightOffset);
          }
        }
        else {
          if (rightSymbol.Type.IsArray()) {
            AddAssemblyCode(objectOperator, leftTrack, rightSymbol.UniqueName);
          }
          else {
            AddAssemblyCode(objectOperator, leftTrack, rightSymbol.UniqueName, 0);
          }
        }
      
        m_trackMap.Remove(leftSymbol);

        if (resultSymbol != null) {
          if (resultSymbol.IsTemporary() && (resultSymbol.AddressSymbol == null)) {
            AddToTrackMap(resultSymbol, leftTrack);
          }
          else {
            SaveFromRegister(leftTrack, resultSymbol);
          }
        }
      }
      else {
        Assert.Error((resultSymbol == null) || resultSymbol.Equals(leftSymbol));
        object leftBase;
        int leftOffset;

        if (leftSymbol.AddressSymbol != null) {
          leftTrack = LoadValueToRegister(leftSymbol.AddressSymbol);
          //leftTrack.SetPointer();
          leftTrack = SetPointer(leftTrack, leftSymbol.AddressSymbol);
          leftBase = leftTrack;
          leftOffset = leftSymbol.AddressOffset;
        }
        else if (leftSymbol.IsAutoOrRegister()) {
          leftBase = BaseRegister(leftSymbol);
          leftOffset = leftSymbol.Offset;
        }
        else {
          leftBase = leftSymbol.UniqueName;
          leftOffset = 0;
        }
      
        ObjectOperator sizeOperator =
          ObjectCode.OperatorToSize(objectOperator, leftSymbol.Type.Size());
     
        if (rightSymbol.Type.IsIntegralOrPointer() && rightSymbol.IsValue()) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(sizeOperator, leftBase, leftOffset, rightValue);
        }
        else if (rightSymbol.Type.IsString()) {
          AddAssemblyCode(sizeOperator, leftBase, leftOffset, rightSymbol.UniqueName);
        }
        else if (m_trackMap.ContainsKey(rightSymbol)) {
          Track rightTrack = m_trackMap[rightSymbol];
          AddAssemblyCode(objectOperator, leftBase, leftOffset, rightTrack);
        
          if (MiddleCode.IsShift(middleOperator)) {
            rightTrack.Register = ObjectCode.ShiftRegister;
          }
        
          m_trackMap.Remove(rightSymbol);
        }
        else if (MiddleCode.IsShift(middleOperator)) {
          Track rightTrack = LoadValueToRegister(rightSymbol, ObjectCode.ShiftRegister);
          AddAssemblyCode(objectOperator, leftBase, leftOffset, rightTrack);
          m_trackMap.Remove(rightSymbol);
        }
        else if ((rightSymbol.AddressSymbol != null) || rightSymbol.IsAutoOrRegister()) {
          object rightBase;
          int rightOffset;
        
          if (rightSymbol.AddressSymbol != null) {
            Track rightTrack = LoadValueToRegister(rightSymbol.AddressSymbol);
            rightTrack = SetPointer(rightTrack, rightSymbol.AddressSymbol);
            //rightTrack.SetPointer();
            rightBase = rightTrack;
            rightOffset = rightSymbol.AddressOffset;
          }
          else {
            rightBase = BaseRegister(rightSymbol);
            rightOffset = rightSymbol.Offset;
          }

          if (rightSymbol.Type.IsArray()) {
            if (MiddleCode.IsAssociative(middleOperator)) {
              AddAssemblyCode(objectOperator, leftBase, leftOffset, rightBase);
              AddAssemblyCode(sizeOperator, leftBase, leftOffset, rightOffset);
            }
            else if (MiddleCode.IsRelation(middleOperator)) {
              Track rightTrack = LoadValueToRegister(rightSymbol);
              AddAssemblyCode(objectOperator, leftBase, leftOffset, rightTrack);
              m_trackMap.Remove(rightSymbol);
            }
            else {
              //int i = 1;
            }
          }
          else {
            Track rightTrack = new Track(rightSymbol);
            m_trackSet.Add(rightTrack);
            AddAssemblyCode(ObjectOperator.mov, rightTrack, rightBase, rightOffset);
            AddAssemblyCode(objectOperator, leftBase, leftOffset, rightTrack);
          }
        }
        else {
          if (rightSymbol.Type.IsArray()) {
            AddAssemblyCode(sizeOperator, leftBase, leftOffset, rightSymbol.UniqueName);
          }
          else {
            Track rightTrack = LoadValueToRegister(rightSymbol);
            AddAssemblyCode(objectOperator, leftBase, leftOffset, rightTrack);
          }
        }
      }
    }*/

    public void GenerateIntegralRelationBinary(Symbol leftSymbol, Symbol rightSymbol) {
      Track leftTrack = null, rightTrack = null;
      m_trackMap.TryGetValue(leftSymbol, out leftTrack);
      m_trackMap.TryGetValue(rightSymbol, out rightTrack);

      if ((leftTrack == null) && (rightTrack == null)) {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(AssemblyOperator.cmp, leftSymbol.Type.Size());

        if (!leftSymbol.Switch && (leftSymbol.AddressSymbol != null)) {
          if ((rightSymbol.Value is BigInteger) ||
              (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString())) {
            Track leftaddressTrack = LoadValueToRegister(leftSymbol.AddressSymbol);
            leftaddressTrack = SetPointer(leftaddressTrack, leftSymbol.AddressSymbol);

            if (rightSymbol.Value is BigInteger) {
              int rightValue = int.Parse(rightSymbol.Value.ToString());
              AddAssemblyCode(sizeOperator, leftaddressTrack, leftSymbol.AddressOffset, rightValue);
              return;
            }
            else if (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()) {
              AddAssemblyCode(sizeOperator, leftaddressTrack, leftSymbol.AddressOffset, rightSymbol.UniqueName);
              return;
            }
          }
        }
        else if (leftSymbol.IsStaticOrExtern() && !leftSymbol.Type.IsArrayFunctionOrString()) {
          if (rightSymbol.Value is BigInteger) {
            int rightValue = int.Parse(rightSymbol.Value.ToString());
            AddAssemblyCode(sizeOperator, leftSymbol.UniqueName, 0, rightValue);
            return;
          }
          else if (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()) {
            AddAssemblyCode(sizeOperator, leftSymbol.UniqueName, 0, rightSymbol.UniqueName);
            return;
          }
        }
        else if (leftSymbol.IsAutoOrRegister()) {
          if (rightSymbol.Value is BigInteger) {
            int rightValue = int.Parse(rightSymbol.Value.ToString());
            AddAssemblyCode(sizeOperator, BaseRegister(leftSymbol),leftSymbol.Offset, rightValue);
            return;
          }
          else if (rightSymbol.IsStaticOrExtern() && rightSymbol.Type.IsArrayFunctionOrString()) {
            AddAssemblyCode(sizeOperator, BaseRegister(leftSymbol), leftSymbol.Offset, rightSymbol.UniqueName);
            return;
          }
        }

        //leftTrack = LoadValueToRegister(leftSymbol);
        rightTrack = LoadValueToRegister(rightSymbol);

        /*if (rightSymbol.IsAutoOrRegister() && rightSymbol.Type.IsArray()) {
          rightTrack = LoadValueToRegister(rightSymbol);
        }
        else {
          leftTrack = LoadValueToRegister(leftSymbol);
        }*/
      }

      if (leftTrack != null) {
        if (rightSymbol.Value is BigInteger) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightValue); // cmp ax, 123
        }
        else if (rightTrack != null) {
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack); // cmp ax, bx
          m_trackMap.Remove(rightSymbol);
        }
        else if (rightSymbol.IsStaticOrExtern()) {
          if (rightSymbol.Type.IsArrayFunctionOrString()) {
            AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightSymbol.UniqueName); // cmp ax, Text
          }
          else {
            AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightSymbol.UniqueName, 0); // cmp ax, [Text]
          }
        }
        else if (rightSymbol.Type.IsArray()) {
          Assert.Error(!m_trackMap.ContainsKey(rightSymbol));
          rightTrack = LoadValueToRegister(rightSymbol);            // mov bx, bp; add bx, 2
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack); // cmp ax, bx
          m_trackMap.Remove(rightSymbol);
        }
        else if (!rightSymbol.Switch && (rightSymbol.AddressSymbol != null)) {
          Track rightaddressTrack = LoadValueToRegister(rightSymbol.AddressSymbol);
          rightaddressTrack = SetPointer(rightaddressTrack, rightSymbol.AddressSymbol);
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightaddressTrack, rightSymbol.AddressOffset); // cmp ax, [bx + 2]
          m_trackMap.Remove(rightSymbol.AddressSymbol);
        }
        else {
          Assert.Error(rightSymbol.IsAutoOrRegister());
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, BaseRegister(rightSymbol), rightSymbol.Offset); // cmp ax, [bp + 2]
        }
      
        m_trackMap.Remove(leftSymbol);
      }
      else { // rightTrack != null
        Assert.Error(!(leftSymbol.Value is BigInteger));
        if (leftSymbol.IsStaticOrExtern()) {
          if (leftSymbol.Type.IsArrayFunctionOrString()) {
            leftTrack = LoadValueToRegister(leftSymbol);
            AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack); // cmp Text, bx
            m_trackMap.Remove(leftSymbol);
          }
          else {
            AddAssemblyCode(AssemblyOperator.cmp, leftSymbol.UniqueName, 0, rightTrack); // cmp [Text], bx
          }
        }
        else if (leftSymbol.Type.IsArray()) {
          leftTrack = LoadValueToRegister(leftSymbol);
          AddAssemblyCode(AssemblyOperator.cmp, leftTrack, rightTrack); // mov ax, bp; add ax, 2; cmp ax, bx
          m_trackMap.Remove(leftSymbol);
        }
        else if (leftSymbol.AddressSymbol != null) {
          Track leftaddressTrack = LoadValueToRegister(leftSymbol.AddressSymbol); // cmp [ax + 2], bx
          AddAssemblyCode(AssemblyOperator.cmp, leftaddressTrack, leftSymbol.AddressOffset, rightTrack);
          m_trackMap.Remove(leftSymbol.AddressSymbol);
        }
        else {
          Assert.Error(leftSymbol.IsAutoOrRegister());
          AddAssemblyCode(AssemblyOperator.cmp, BaseRegister(leftSymbol), leftSymbol.Offset, rightTrack);  // cmp [ax + 2], bx
        }

        m_trackMap.Remove(rightSymbol);
      }
    }

    /*public static IDictionary<int,Register> LeftMultiplyMap = new Dictionary<int,Register>();

    static ObjectCodeGenerator() {
      LeftMultiplyMap.Add(Type.ShortSize, Register.al);
      LeftMultiplyMap.Add(Type.IntegerSize, Register.ax);
      LeftMultiplyMap.Add(Type.LongSize, Register.eax);
    }
  
    public static IDictionary<Pair<MiddleOperator,int>,Register>
                    ResultMultiplyMap = new Dictionary<Pair<MiddleOperator,int>,Register>();

    static ObjectCodeGenerator() {
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply,Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply,Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.eax);

      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.eax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.ShortSize),
                            Register.al);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.ax);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.eax);
    
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.ShortSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.ShortSize),
                            Register.ah);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ResultMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);
    }
  
    public static IDictionary<Pair<MiddleOperator,int>,Register>
                    ExtraMultiplyMap = new Dictionary<Pair<MiddleOperator,int>,Register>();
  
    static ObjectCodeGenerator() {
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply,Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedMultiply, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply,Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedMultiply, Type.LongSize),
                            Register.edx);
    
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedDivide, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedDivide, Type.LongSize),
                            Register.edx);

      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.SignedModulo, Type.LongSize),
                            Register.edx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.ShortSize),
                            Register.ah);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.IntegerSize),
                            Register.dx);
      ExtraMultiplyMap.Add(new Pair<MiddleOperator,int>(MiddleOperator.UnsignedModulo, Type.LongSize),
                            Register.edx);
    }*/
  
    private object Base(Symbol symbol) {
      if (/*!symbol.Switch &&*/ (symbol.AddressSymbol != null)) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        m_trackMap.Remove(symbol.AddressSymbol);
        return addressTrack;
      }
      else if (symbol.IsStaticOrExtern()) {
        return symbol.UniqueName;
      }
      else { //resultSymbol.IsAutoOrRegister()
        return BaseRegister(symbol);
      }
    }

    private object Offset(Symbol symbol) {
      if (/*!symbol.Switch &&*/ (symbol.AddressSymbol != null)) {
        return symbol.AddressOffset;
      }
      else if (symbol.IsStaticOrExtern()) {
        return symbol.Offset;
      }
      else { //resultSymbol.IsAutoOrRegister()
        return symbol.Offset;
      }
    }

    public void GenerateAssignParameter(MiddleOperator middleOp, Symbol resultSymbol,
                                        Symbol assignSymbol) {
      if ((middleOp == MiddleOperator.Assign) && resultSymbol.IsTemporary() && (resultSymbol.AddressSymbol == null)) {
        Track resultTrack;

        if (m_trackMap.TryGetValue(resultSymbol, out resultTrack)) {
          m_trackMap.Remove(resultSymbol);
          Track twinTrack = new Track(resultSymbol, resultTrack.Register);
          //resultTrack.TwinTrack = twinTrack;
          //twinTrack.TwinTrack = resultTrack;
          resultTrack.TwinTrackSet.UnionWith(twinTrack.TwinTrackSet);
          twinTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
          resultTrack.TwinTrackSet.Add(twinTrack);
          twinTrack.TwinTrackSet.Add(resultTrack);
          Assert.Error(resultTrack.Register == twinTrack.Register);
          resultTrack = twinTrack;
        }
        else {
          resultTrack = new Track(resultSymbol);
          m_trackSet.Add(resultTrack);
        }

        m_trackMap.Add(resultSymbol, resultTrack);

        Track assignTrack;
        if (m_trackMap.TryGetValue(assignSymbol, out assignTrack)) {
          //AddAssemblyCode(ObjectOperator.mov, resultTrack, m_trackMap[assignSymbol]);
          m_trackMap.Remove(assignSymbol);
          m_trackMap[resultSymbol] = assignTrack;
          resultTrack.TwinTrackSet.UnionWith(assignTrack.TwinTrackSet);
          assignTrack.TwinTrackSet.UnionWith(resultTrack.TwinTrackSet);
          resultTrack.TwinTrackSet.Add(assignTrack);
          assignTrack.TwinTrackSet.Add(resultTrack);
        }
        else if (assignSymbol.Value is BigInteger) {
          int assignValue = (int) ((BigInteger)assignSymbol.Value);
          AddAssemblyCode(AssemblyOperator.mov, resultTrack, assignValue);
        }
        else if (assignSymbol.IsStaticOrExtern() && assignSymbol.Type.IsArrayFunctionOrString()) {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack, assignSymbol.UniqueName);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, resultTrack, Base(assignSymbol), Offset(assignSymbol));
        }
      }
      else if (resultSymbol.IsAutoOrRegister() && (resultSymbol.Offset == 0) &&
         (resultSymbol.AddressSymbol == null)) {
        if (m_trackMap.ContainsKey(resultSymbol)) {
          if (m_trackMap.ContainsKey(assignSymbol)) {
            m_trackMap[resultSymbol] = m_trackMap[assignSymbol];
            m_trackMap.Remove(assignSymbol);
          }
          else {
            Track resultTrack = m_trackMap[resultSymbol];
            LoadValueToTrack(resultTrack, assignSymbol);
          }
        }
        else {
          Track assignTrack = LoadValueToRegister(assignSymbol);
          m_trackMap.Remove(assignSymbol);
          AddToTrackMap(resultSymbol, assignTrack);
        }
      }
      else {
        object resultBase;
        int resultOffset;

        if (resultSymbol.Value is StaticAddress) {
          StaticAddress staticAddress = (StaticAddress) resultSymbol.Value;
          resultBase = staticAddress.Name;
          resultOffset = staticAddress.Offset;
        }
        else if (resultSymbol.AddressSymbol != null) {
          Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
          addressTrack = SetPointer(addressTrack, resultSymbol.AddressSymbol);
          resultBase = addressTrack;
          resultOffset = resultSymbol.AddressOffset;
        }
        else if (resultSymbol.IsAutoOrRegister()) {
          resultBase = BaseRegister(resultSymbol);
          resultOffset = resultSymbol.Offset;
        }
        else {
          resultBase = resultSymbol.UniqueName;
          resultOffset = resultSymbol.Offset;
        }
      
        if (m_trackMap.ContainsKey(assignSymbol)) {
          Track assignTrack = m_trackMap[assignSymbol];
          AddAssemblyCode(AssemblyOperator.mov, resultBase, resultOffset, assignTrack);
          m_trackMap.Remove(assignSymbol);
        }
        else if (assignSymbol.Type.IsIntegralOrPointer() &&
                 assignSymbol.IsValue()) {
          BigInteger bigIntegerX = (BigInteger) assignSymbol.Value;
          BigInteger bigInteger = bigIntegerX;
          bigInteger &= 0xFFFFFFFF;

          int intValue;
          if (bigInteger > (new BigInteger(2147483647))) {
            bigInteger -= 4294967296;
          }
          else if (bigInteger < (new BigInteger(-2147483648))) {
            bigInteger += 4294967296;
          }

          intValue = int.Parse(bigInteger.ToString());


          /*if (longValue >= ) {
            longValue -= 4294967296;
          }*/

          /*int intValue = int.Parse((bigInteger & 0xFFFF).ToString());

          if (intValue < 0) {
            intValue += 65536;
          }*/

          /*List<byte> byteList = new List<byte>(((BigInteger) assignSymbol.Value).ToByteArray());

          while (byteList.Count < 4) {
            byteList.Add((byte) 0);
          }

          List<sbyte> sbyteList = new List<sbyte>();

          foreach (byte b in byteList) {
            sbyteList.Add((sbyte) b);
          }

          int intValue = ((int) sbyteList[0]) +
                         (((int) sbyteList[1]) >> 8) +
                         (((int) sbyteList[1]) >> 16) +
                         (((int) sbyteList[1]) >> 24);*/
                         
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov, assignSymbol.Type.Size());
          AddAssemblyCode(sizeOperator, resultBase, resultOffset, intValue);
        }
        else if (assignSymbol.IsStaticOrExtern() &&
                 assignSymbol.Type.IsArrayFunctionOrString()) {
          AssemblyOperator sizeOperator =
            AssemblyCode.OperatorToSize(AssemblyOperator.mov, Type.PointerSize);
          AddAssemblyCode(sizeOperator, resultBase, resultOffset, assignSymbol.UniqueName);
        }
        /*else if (assignSymbol.Type.IsArray()) {
          AddAssemblyCode(ObjectOperator.mov, resultBase, resultOffset, BaseRegister(assignSymbol));
          ObjectOperator sizeOperator =
            ObjectCode.OperatorToSize(ObjectOperator.add, assignSymbol.Type.Size());
          AddAssemblyCode(sizeOperator, resultBase, resultOffset, assignSymbol.Offset);
          /*Track assignTrack = new Track(assignSymbol);
          m_trackSet.Add(assignTrack);
          AddAssemblyCode(ObjectOperator.mov, assignTack, BaseRegister(assignSymbol));
          AddAssemblyCode(ObjectOperator.add, assignTrack, assignSymbol.Offset);
          AddAssemblyCode(ObjectOperator.mov, resultBase, resultOffset, assignTrack);
        }*/
        else {
          object assignBase;
          int assignOffset;

          if (assignSymbol.Value is StaticValue) {
            StaticValue staticValue = (StaticValue) assignSymbol.Value;
            assignBase = staticValue.Name;
            assignOffset = staticValue.Offset();
          }
          else if (assignSymbol.AddressSymbol != null) {
            Track addressTrack = LoadValueToRegister(assignSymbol.AddressSymbol);
            addressTrack = SetPointer(addressTrack, assignSymbol.AddressSymbol);
            assignBase = addressTrack;
            assignOffset = assignSymbol.AddressOffset;
          }
          else if (assignSymbol.IsAutoOrRegister()) {
            assignBase = BaseRegister(assignSymbol);
            assignOffset = assignSymbol.Offset;
          }
          else {
            assignBase = assignSymbol.UniqueName;
            assignOffset = assignSymbol.Offset;
          }

          if (assignSymbol.IsStaticOrExtern() && assignSymbol.Type.IsArrayStringOrFunction()) {
            AddAssemblyCode(AssemblyOperator.mov, resultBase, resultOffset, assignBase);

            if (assignOffset > 0) {
              AssemblyOperator AddOperator =
                AssemblyCode.OperatorToSize(AssemblyOperator.add, Type.PointerSize);
              AddAssemblyCode(AddOperator, resultBase, resultOffset, assignOffset);
            }
            else if (assignOffset < 0) {
              AssemblyOperator subOperator =
                AssemblyCode.OperatorToSize(AssemblyOperator.sub, Type.PointerSize);
              AddAssemblyCode(subOperator, resultBase, resultOffset, -assignOffset);
            }
          }
          else if (assignSymbol.IsAutoOrRegister() && assignSymbol.Type.IsArray() /*&& (assignSymbol.AddressSymbol == null)*/) {
            AddAssemblyCode(AssemblyOperator.mov, resultBase, resultOffset, assignBase);
            AssemblyOperator sizeOperator =
              AssemblyCode.OperatorToSize(AssemblyOperator.add, Type.PointerSize);
            AddAssemblyCode(sizeOperator, resultBase, resultOffset, assignOffset);
          }
          else {
            Track assignTrack = new Track(assignSymbol);
            m_trackSet.Add(assignTrack);
            AddAssemblyCode(AssemblyOperator.mov, assignTrack, assignBase, assignOffset);
            AddAssemblyCode(AssemblyOperator.mov, resultBase, resultOffset, assignTrack);
          }
        }
      }
    }

    public void GenerateUnary(MiddleOperator middleOperator, Symbol resultSymbol,
                              Symbol unarySymbol) {
      AssemblyOperator objectOperator =
        m_middleToIntegralUnaryTargetMap[middleOperator];

      Track unaryTrack;
      if (m_trackMap.TryGetValue(unarySymbol, out unaryTrack)) {
        AddAssemblyCode(objectOperator, unaryTrack);
      }
      else if ((resultSymbol == null) || resultSymbol.Equals(unarySymbol)) { // ++i; i = -i;
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, unarySymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(unarySymbol), Offset(unarySymbol));
      }
      else {
        unaryTrack = LoadValueToRegister(unarySymbol);
        AddAssemblyCode(objectOperator, unaryTrack);
        m_trackMap[resultSymbol] = unaryTrack;
      }
    }

    /*public void GenerateUnary(MiddleOperator middleOperator, Symbol resultSymbol,
                              Symbol unarySymbol) {
      ObjectOperator objectOperator =
        m_middleToIntegralUnaryTargetMap[middleOperator];

      if (resultSymbol == null) {
        ObjectOperator sizeOperator =
          ObjectCode.OperatorToSize(objectOperator, unarySymbol.Type.Size());

        if (unarySymbol.AddressSymbol != null) {
          Track addressTrack = LoadValueToRegister(unarySymbol.AddressSymbol);
          AddAssemblyCode(sizeOperator, addressTrack, unarySymbol.AddressOffset);
        }
        else if (unarySymbol.IsAutoOrRegister()) {
          AddAssemblyCode(sizeOperator, BaseRegister(unarySymbol), unarySymbol.Offset);
        }
        else {
          AddAssemblyCode(sizeOperator, unarySymbol.UniqueName, 0);
        }
      }
      else {
        Track unaryTrack = LoadValueToRegister(unarySymbol);
        AddAssemblyCode(objectOperator, unaryTrack);
      
        if (resultSymbol.IsTemporary() && (resultSymbol.AddressSymbol == null)) {
          AddToTrackMap(resultSymbol, unaryTrack);
        }
        else {
          SaveFromRegister(unaryTrack, resultSymbol);
        }
      }
    }*/
  
    public void GenerateIntegralMultiplyX(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode.GetOperand(1);
      Register leftRegister = LeftMultiplyMap[leftSymbol.Type.Size()];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);
      AddAssemblyCode(AssemblyOperator.empty, leftTrack);

      Pair<MiddleOperator,int> pair = new Pair<MiddleOperator,int>(middleCode.Operator, leftSymbol.Type.Size());
      Register clearRegister = ExtraMultiplyMap[pair];
      Track clearTrack = new Track(leftSymbol, clearRegister);
      m_trackSet.Add(clearTrack);
      AddAssemblyCode(AssemblyOperator.xor, clearTrack, clearTrack);

      Symbol rightSymbol = (Symbol) middleCode.GetOperand(2);
      AssemblyOperator objectOperator =
        m_middleToIntegralUnaryTargetMap[middleCode.Operator];

      if (rightSymbol.IsTemporary() && (rightSymbol.AddressSymbol == null)) {
        Track rightTrack = LoadValueToRegister(rightSymbol);
        AddAssemblyCode(objectOperator, rightTrack);
      }
      else {
        AssemblyOperator sizeOperator =
          AssemblyCode.OperatorToSize(objectOperator, rightSymbol.Type.Size());
        AddAssemblyCode(sizeOperator, Base(rightSymbol), Offset(rightSymbol));
      }

      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0);
      Register resultRegister = ResultMultiplyMap[pair];
      Track resultTrack = new Track(resultSymbol, resultRegister);
      m_trackSet.Add(resultTrack);

      Assert.Error(resultSymbol.IsTemporary() && (resultSymbol.AddressSymbol == null));
      m_trackMap[resultSymbol] = resultTrack;
      AddAssemblyCode(AssemblyOperator.empty, resultTrack);

      Track nextTrack = new Track(resultSymbol, resultRegister);
      nextTrack.PreviousTrack = resultTrack;
      nextTrack.FirstLine = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.empty, nextTrack);
      AddAssemblyCode(AssemblyOperator.empty);
      m_trackMap[resultSymbol] = nextTrack;
      AddAssemblyCode(AssemblyOperator.empty, resultTrack);
    }

    public void GenerateIntegralMultiply(MiddleCode middleCode) {
      Symbol leftSymbol = (Symbol) middleCode.GetOperand(1);
      Register leftRegister = LeftMultiplyMap[leftSymbol.Type.Size()];
      Track leftTrack = LoadValueToRegister(leftSymbol, leftRegister);
      AddAssemblyCode(AssemblyOperator.empty, leftTrack);
      //LoadValueToRegister(leftSymbol, leftRegister);
      m_trackMap.Remove(leftSymbol);

      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0);          
      Pair<MiddleOperator,int> resultPair =
        new Pair<MiddleOperator,int>(middleCode.Operator, resultSymbol.Type.Size());
      Register resultRegister = ResultMultiplyMap[resultPair];

      Register clearRegister = ExtraMultiplyMap[resultPair];
      AddAssemblyCode(AssemblyOperator.xor, clearRegister, clearRegister, null);

      //Track leftTrack = new Track(leftSymbol, leftRegister);
      //leftTrack.AddCode(Track.Clear, m_objectCodeList.Count);
      //m_trackSet.Add(leftTrack);

      Track resultTrack = new Track(resultSymbol, resultRegister);

      if (resultRegister != leftRegister) {
        resultTrack.AddCode(Track.Clear, m_assemblyCodeList.Count);
        m_trackSet.Add(resultTrack);
      }

      if (resultSymbol.IsTemporary()) {
        AddToTrackMap(resultSymbol, resultTrack);
      }

      if ((clearRegister != leftRegister) &&
          (clearRegister != resultRegister)) {
        Track clearTrack = new Track(resultSymbol, clearRegister);
        clearTrack.AddCode(Track.Clear, m_assemblyCodeList.Count);
        m_trackSet.Add(clearTrack);
      }

  //          Register extraRegister = ExtraMultiplyMap[resultPair);
  //          pair = LookupRegister(extraRegister);
  //          if ((pair != null) && (pair.First != null) &&
  //              (pair.First != leftSymbol)) {
  //            saveFromRegister(pair.Second, pair.First);
  //            m_trackMap.Remove(pair.First);
  //          }

      //ObjectOperator objectOperator =
      //  m_middleToIntegralMultiplyTargetMap[middleCode.Operator);
      Symbol rightSymbol = (Symbol) middleCode.GetOperand(2);
    
      //Track rightTrack = LoadValueToRegister(rightSymbol);
      //AddAssemblyCode(objectOperator, rightTrack);
      //GenerateUnary(objectOperator, rightSymbol);
      GenerateUnary(middleCode.Operator, null, rightSymbol);
      //m_trackMap.Remove(leftSymbol);
      m_trackMap.Remove(rightSymbol);

      if (resultSymbol.AddressSymbol != null) {
        AddAssemblyCode(AssemblyOperator.mov, resultSymbol.AddressSymbol, resultSymbol.AddressOffset, resultRegister);
        m_trackMap.Remove(resultSymbol);
      }
      else if (resultSymbol.IsTemporary()) {
        AddAssemblyCode(AssemblyOperator.empty, resultTrack);
        Track nextTrack = new Track(resultSymbol, resultRegister);
        nextTrack.PreviousTrack = resultTrack;
        nextTrack.FirstLine = m_assemblyCodeList.Count;
        AddAssemblyCode(AssemblyOperator.empty);
        AddAssemblyCode(AssemblyOperator.empty);
        AddAssemblyCode(AssemblyOperator.empty, nextTrack);
        m_trackMap[resultSymbol] = nextTrack;
      }
      else {
        if (resultSymbol.IsStaticOrExtern()) {
          AddAssemblyCode(AssemblyOperator.mov, resultSymbol.UniqueName, 0, resultRegister);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, BaseRegister(resultSymbol), resultSymbol.Offset, resultRegister);
        }
      }
    }
  
    public void GenerateIntegralRelation(MiddleCode middleCode, int index) {
      GenerateIntegralRelationBinary((Symbol) middleCode.GetOperand(1), (Symbol) middleCode.GetOperand(2));
      /*GenerateIntegralBinary(MiddleOperator.Compare, null,
                             (Symbol) middleCode.GetOperand(1), (Symbol) middleCode.GetOperand(2));*/

      AssemblyOperator objectOperator = m_middleToIntegralBinaryTargetMap[middleCode.Operator];
      int target = (int)middleCode.GetOperand(0);
      AddAssemblyCode(objectOperator, null, null, target);
//      AddAssemblyCode(objectOperator, null, null, index + 1);
//      AddAssemblyCode(AssemblyOperator.long_jmp, null, null, target);
    }

    public void GenerateIntegralIncrementDecrement(MiddleCode middleCode) {
      GenerateUnary(middleCode.Operator, (Symbol) middleCode.GetOperand(0),
                    (Symbol) middleCode.GetOperand(1));
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToIntegralUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.not);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.neg);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Increment, ObjectOperator.inc);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.Decrement, ObjectOperator.dec);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedMultiply, ObjectOperator.imul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedDivide, ObjectOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.SignedModulo, ObjectOperator.idiv);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedMultiply, ObjectOperator.mul);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedDivide, ObjectOperator.div);
      m_middleToIntegralUnaryTargetMap.
        Add(MiddleOperator.UnsignedModulo, ObjectOperator.div);
    }*/

    // neg word [bp + 2]
  
    public void GenerateIntegralUnary(MiddleCode middleCode) {
      GenerateUnary(middleCode.Operator, (Symbol) middleCode.GetOperand(0),
                    (Symbol) middleCode.GetOperand(1));
    }

    public void GenerateAddress(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0),
             symbol = (Symbol) middleCode.GetOperand(1);

      Track track = LoadAddressToRegister(symbol);
      AddToTrackMap(resultSymbol, track);
      m_trackMap.Remove(symbol);
    }

    public void GenerateDeref(MiddleCode middleCode) {
      Symbol resultSymbol = (Symbol) middleCode.GetOperand(0);
      //Track addressTrack = LoadAddressToRegister(resultSymbol);
      Track addressTrack = LoadValueToRegister(resultSymbol.AddressSymbol);
      addressTrack.SetPointerx();
      m_trackMap.Add(resultSymbol.AddressSymbol, addressTrack);
      m_trackMap.Remove(resultSymbol);
      //m_trackMap.Remove(resultSymbol.AddressSymbol);

      //AddToTrackMap(resultSymbol.AddressSymbol, addressTrack);

      if (resultSymbol.Switch) {
/*        Track resultTrack = new Track(resultSymbol);
        m_trackSet.Add(resultTrack);
        AddAssemblyCode(AssemblyOperator.mov, resultTrack, addressTrack, resultSymbol.AddressOffset);
        AddAssemblyCode(AssemblyOperator.mov, Base(resultSymbol), Offset(resultSymbol), resultTrack);
        m_trackMap.Remove(resultSymbol.AddressSymbol);
        m_trackMap.Remove(resultSymbol);*/
      }
    }

    public Track LoadAddressToRegister(Symbol symbol) {
      return LoadAddressToRegister(symbol, null);
    }
  
    public Track LoadAddressToRegister(Symbol symbol, Register? register) {
      Symbol addressSymbol = symbol.AddressSymbol;
    
      if (addressSymbol != null) {
        Track addressTrack = LoadValueToRegister(addressSymbol);
        addressTrack = SetPointer(addressTrack, addressSymbol);

        if (register != null) {
          if (addressTrack.Register == null) {
            addressTrack.Register = register.Value;
          }
          else {
            Register fromRegister =
              AssemblyCode.RegisterToSize(addressTrack.Register, symbol.Type.Size());
        
            if (register != fromRegister) {
              addressTrack = new Track(symbol, register.Value);
              addressTrack = SetPointer(addressTrack, symbol);
              AddToTrackMap(symbol, addressTrack);
              AddAssemblyCode(AssemblyOperator.mov, addressTrack, fromRegister);
            }
          }
        }

        //addressTrack = SetPointer(addressTrack);
        return addressTrack;
      }
      else {
        Symbol pointerSymbol = new Symbol(new Type(symbol.Type));
        Track addressTrack = new Track(pointerSymbol, register);
        addressTrack = SetPointer(addressTrack, pointerSymbol);

        if (symbol.IsAutoOrRegister()) {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack, BaseRegister(symbol));
          AddAssemblyCode(AssemblyOperator.add, addressTrack, symbol.Offset);
        }
        else {
          AddAssemblyCode(AssemblyOperator.mov, addressTrack, symbol.UniqueName);
        }

        //addressTrack = SetPointer(addressTrack);
        return addressTrack;
      }
    }

    public void GeneratePushZero(MiddleCode middleCode) {
      Assert.Error((++m_stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
      AddAssemblyCode(AssemblyOperator.fldz);
    }

    public void GeneratePushOne(MiddleCode middleCode) {
      Assert.Error((++m_stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
      AddAssemblyCode(AssemblyOperator.fld1);
    }

    public void GeneratePushFloat(MiddleCode middleCode) {
      Symbol symbol = (Symbol)middleCode.GetOperand(0);
      PushSymbol(symbol);
    }

    /*public static IDictionary<Sort,ObjectOperator> m_floatPushMap = new Dictionary<Sort,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_floatPushMap.Add(Sort.Signed_Int, ObjectOperator.fild_word);
      m_floatPushMap.Add(Sort.Unsigned_Int, ObjectOperator.fild_word);
      m_floatPushMap.Add(Sort.Signed_Long_Int, ObjectOperator.fild_dword);
      m_floatPushMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fild_dword);
      m_floatPushMap.Add(Sort.Float, ObjectOperator.fld_dword);
      m_floatPushMap.Add(Sort.Double, ObjectOperator.fld_qword);
      m_floatPushMap.Add(Sort.Long_Double, ObjectOperator.fld_qword);
    }*/

    private static string AddStaticContainer(Type type) {
      string registerName = "container" + type.Size() + "bytes" + Start.NumberId;
      Symbol symbol = new Symbol(registerName, Storage.Static, type);
      return registerName;
    }

    public void PushSymbol(Symbol symbol) {
      Assert.Error((++m_stackSize) <= Start.FloatingStackMaxSize, "floating stack overflow");
      AssemblyOperator objectOperator = m_floatPushMap[symbol.Type.GetSort()];

      if (symbol.HasValue()) {
        if (symbol.Type.IsIntegralOrPointer()) {
          BigInteger value = (BigInteger) symbol.Value;

          if (value.Equals(BigInteger.Zero)) {
            AddAssemblyCode(AssemblyOperator.fldz);
          }
          if (value.Equals(BigInteger.One)) {
            AddAssemblyCode(AssemblyOperator.fld1);
          }
          else {
            AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
          }
        }
        else {
          decimal value = (decimal) symbol.Value;

          if (value == 0) {
            AddAssemblyCode(AssemblyOperator.fldz);
          }
          else if (value == 1) {
            AddAssemblyCode(AssemblyOperator.fld1);
          }
          else {
            AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
          }
        }
      }
      else if (symbol.Type.IsFunctionArrayStringStructOrUnion()) {
        AssemblyOperator movObjectOp = AssemblyCode.OperatorToSize(AssemblyOperator.mov, Type.PointerSize);
        string containerName = AddStaticContainer(symbol.Type);
        
        if (symbol.IsAutoOrRegister()) {
          AddAssemblyCode(movObjectOp, containerName, 0, BaseRegister(symbol));
          AssemblyOperator addObjectOp = AssemblyCode.OperatorToSize(AssemblyOperator.add, Type.PointerSize);
          AddAssemblyCode(addObjectOp, containerName, 0, symbol.Offset);
        }
        else {
          AddAssemblyCode(movObjectOp, containerName, 0, symbol.UniqueName);
        }

        AddAssemblyCode(objectOperator, containerName, 0);
      }
      else if (m_trackMap.ContainsKey(symbol)) { // XXX
        Track track = m_trackMap[symbol];
        m_trackMap.Remove(symbol);
        string containerName = AddStaticContainer(symbol.Type);
        AddAssemblyCode(AssemblyOperator.mov, containerName, 0, track);
        AddAssemblyCode(objectOperator, containerName, 0);
      }
      else if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        AddAssemblyCode(objectOperator, addressTrack, symbol.AddressOffset);
      }
      else if (symbol.IsAutoOrRegister()) {
        if (symbol.Offset == 0) {
          string containerName = AddStaticContainer(symbol.Type);
          AddAssemblyCode(objectOperator, containerName, 0);
          Track track = new Track(symbol);
          AddAssemblyCode(AssemblyOperator.mov, track, containerName, 0);
        }
        else {
          AddAssemblyCode(objectOperator, BaseRegister(symbol), symbol.Offset);
        }
      }
      else {
        AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
      }
    }

    public enum TopOrPop {Top, Pop};

    public void GenerateTopFloat(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);      
      TopPopSymbol(symbol, TopOrPop.Top);
    }

    public void GeneratePopFloat(MiddleCode middleCode) {
      Symbol symbol = (Symbol) middleCode.GetOperand(0);
      TopPopSymbol(symbol, TopOrPop.Pop);
    }

    public void GeneratePopEmpty(MiddleCode middleCode) {
      string containerName = AddStaticContainer(Type.LongDoubleType);
      AddAssemblyCode(AssemblyOperator.fistp_word, containerName, 0);    
    }

    /*public static IDictionary<Sort,ObjectOperator> m_floatTopMap = new Dictionary<Sort,ObjectOperator>(),
                                                  m_floatPopMap = new Dictionary<Sort,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_floatTopMap.Add(Sort.Signed_Int, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Unsigned_Int, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Pointer, ObjectOperator.fist_word);
      m_floatTopMap.Add(Sort.Signed_Long_Int, ObjectOperator.fist_dword);
      m_floatTopMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fist_dword);
      m_floatTopMap.Add(Sort.Float, ObjectOperator.fst_dword);
      m_floatTopMap.Add(Sort.Double, ObjectOperator.fst_qword);
      m_floatTopMap.Add(Sort.Long_Double, ObjectOperator.fst_qword);
  
      m_floatPopMap.Add(Sort.Signed_Int, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Unsigned_Int, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Pointer, ObjectOperator.fistp_word);
      m_floatPopMap.Add(Sort.Signed_Long_Int, ObjectOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Unsigned_Long_Int, ObjectOperator.fistp_dword);
      m_floatPopMap.Add(Sort.Float, ObjectOperator.fstp_dword);
      m_floatPopMap.Add(Sort.Double, ObjectOperator.fstp_qword);
      m_floatPopMap.Add(Sort.Long_Double, ObjectOperator.fstp_qword);    
    }*/
  
    public void TopPopSymbol(Symbol symbol, TopOrPop TopOrPop) {
      AssemblyOperator objectOperator;

      if (TopOrPop == TopOrPop.Pop) {
        objectOperator = m_floatPopMap[symbol.Type.GetSort()];
        Assert.Error((--m_stackSize) >= 0);
      }
      else {
        objectOperator = m_floatTopMap[symbol.Type.GetSort()];
      }
    
      if (symbol.AddressSymbol != null) {
        Track addressTrack = LoadValueToRegister(symbol.AddressSymbol);
        addressTrack = SetPointer(addressTrack, symbol.AddressSymbol);
        AddAssemblyCode(objectOperator, addressTrack, symbol.AddressOffset);
      }
      else if (symbol.IsAutoOrRegister()) {
        string containerName = AddStaticContainer(symbol.Type);

        if (symbol.Offset == 0) {
          AddAssemblyCode(objectOperator, containerName, 0);
          Track track = new Track(symbol);
          AddToTrackMap(symbol, track);
          AddAssemblyCode(AssemblyOperator.mov, track, containerName, 0);
        }
        else {
          AddAssemblyCode(objectOperator, BaseRegister(symbol), symbol.Offset);
        }
      }
      else {
        AddAssemblyCode(objectOperator, symbol.UniqueName, 0);
      }
    }

    /*public static IDictionary<Pair<int,int>,int>
                    m_maskMap = new Dictionary<Pair<int,int>,int>();

    static ObjectCodeGenerator() {
      m_maskMap.Add(new Pair<int,int>(1,2), 0x00FF);
      m_maskMap.Add(new Pair<int,int>(1,4), 0x000000FF);
      m_maskMap.Add(new Pair<int,int>(2,4), 0x0000FFFF);  
    }*/
  
    public void GenerateIntegralToIntegral(MiddleCode middleCode, int index) {
      Symbol toSymbol = (Symbol) middleCode.GetOperand(0),
             fromSymbol = (Symbol) middleCode.GetOperand(1);

      Type toType = toSymbol.Type, fromType = fromSymbol.Type;
      int toSize = toType.Size(), fromSize = fromType.Size();

      Track track = LoadValueToRegister(fromSymbol);
      AddAssemblyCode(AssemblyOperator.set_track_size, track, toSize);

      if (fromSize != toSize) {
        if (fromSize < toSize) {
          int mask = m_maskMap[new Pair<int,int>(fromSize, toSize)];
          AddAssemblyCode(AssemblyOperator.and, track, mask, null);
        }

        if (fromType.IsSigned() && toType.IsSigned()) {
          AddAssemblyCode(AssemblyOperator.set_track_size, track, fromSize);
          AddAssemblyCode(AssemblyOperator.cmp, track, 0, null);
          AddAssemblyCode(AssemblyOperator.long_jge, null, null, index + 1);
          AddAssemblyCode(AssemblyOperator.neg, track);
          AddAssemblyCode(AssemblyOperator.set_track_size, track, toSize);
          AddAssemblyCode(AssemblyOperator.neg, track);
        }
      }

      AddToTrackMap(toSymbol, track);
      m_trackMap.Remove(fromSymbol);
    }

    public void GenerateIntegralToFloating(MiddleCode middleCode) {
      Symbol fromSymbol = (Symbol) middleCode.GetOperand(1);
      PushSymbol(fromSymbol);
    }
  
    public void GenerateFloatingToIntegral(MiddleCode middleCode) {
      Symbol toSymbol = (Symbol) middleCode.GetOperand(0);
      TopPopSymbol(toSymbol, TopOrPop.Pop);
    }

    public void GenerateIntegralParameter(MiddleCode middleCode) {
      Symbol fromSymbol = (Symbol) middleCode.GetOperand(1);
      int recordSize = (int) middleCode.GetOperand(0),
          paramOffset = (int) middleCode.GetOperand(2);

      foreach (int size in m_recordStack) {
        recordSize += size;
      }

      Symbol toSymbol = new Symbol(fromSymbol.Type);
      toSymbol.Offset = recordSize + paramOffset;
      GenerateAssignParameter(middleCode.Operator, toSymbol, fromSymbol);
      m_trackMap.Remove(fromSymbol);
      m_trackMap.Remove(toSymbol);
    }

    public void GenerateIntegralGetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode.GetOperand(0);
      m_trackMap.Add(returnSymbol, m_returnTrack);

      /*Track nextTrack = new Track(returnSymbol, returnRegister);
      nextTrack.PreviousTrack = returnTrack;
      nextTrack.FirstLine = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.empty);
      AddAssemblyCode(AssemblyOperator.empty);
      m_trackMap[returnSymbol] = nextTrack;*/
    }

    /*public void GenerateIntegralGetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode.GetOperand(0);
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, returnSymbol.Type.Size());
      CheckRegister(returnSymbol, returnRegister);
      Track returnTrack = new Track(returnSymbol, returnRegister);
      m_trackSet.Add(returnTrack);
      AddAssemblyCode(AssemblyOperator.empty, returnTrack);

      Track nextTrack = new Track(returnSymbol, returnRegister);
      nextTrack.PreviousTrack = returnTrack;
      nextTrack.FirstLine = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.empty);
      AddAssemblyCode(AssemblyOperator.empty);
      m_trackMap[returnSymbol] = nextTrack;
    }*/

    public void GenerateIntegralSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode.GetOperand(1);
      Register returnRegister =
        AssemblyCode.RegisterToSize(AssemblyCode.ReturnValueRegister, returnSymbol.Type.Size());
      LoadValueToRegister(returnSymbol, returnRegister);
      m_trackMap.Remove(returnSymbol);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingBinaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinaryAdd, ObjectOperator.fadd);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.BinarySubtract, ObjectOperator.fsub);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedMultiply, ObjectOperator.fmul);
      m_middleToFloatingBinaryTargetMap.
        Add(MiddleOperator.SignedDivide, ObjectOperator.fdiv);
    }*/

    public void GenerateFloatingBinary(MiddleCode middleCode) {
      Assert.Error((--m_stackSize) >= 0);
      AddAssemblyCode(m_middleToFloatingBinaryTargetMap[middleCode.Operator]);
    }

    /*public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingRelationTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.Equal, ObjectOperator.jne);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.NotEqual, ObjectOperator.je);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThan, ObjectOperator.jbe);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedLessThanEqual, ObjectOperator.jb);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThan, ObjectOperator.jae);
      m_middleToFloatingRelationTargetMap.
        Add(MiddleOperator.SignedGreaterThanEqual, ObjectOperator.ja);
    }

    public void GenerateFloatingRelation(MiddleCode middleCode, int index) {
      int target = (int) middleCode.Operand(0);
      AddAssemblyCode(ObjectOperator.fcompp);
      AddAssemblyCode(ObjectOperator.fstsw, Register.ax);
      AddAssemblyCode(ObjectOperator.sahf);
      ObjectOperator objectOperator =
        m_middleToFloatingRelationTargetMap[middleCode.Operator];
      AddAssemblyCode(objectOperator, null, null, index + 1);
      AddAssemblyCode(ObjectOperator.long_jmp, null, null, target);
    }

    public static IDictionary<MiddleOperator,ObjectOperator>
                    m_middleToFloatingUnaryTargetMap = new Dictionary<MiddleOperator,ObjectOperator>();
  
    static ObjectCodeGenerator() {
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnaryAdd,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.BitwiseNot,ObjectOperator.empty);
      m_middleToFloatingUnaryTargetMap.
        Add(MiddleOperator.UnarySubtract, ObjectOperator.fchs);
    }*/

    public void GenerateFloatingUnary(MiddleCode middleCode) {
      AddAssemblyCode(m_middleToFloatingUnaryTargetMap[middleCode.Operator]);
    }

    public void GenerateFloatingParameter(MiddleCode middleCode) {
      Symbol paramSymbol = (Symbol) middleCode.GetOperand(1);
      Symbol saveSymbol = new Symbol(paramSymbol.Type);
      int recordSize = (int)middleCode.GetOperand(0),
          paramOffset = (int) middleCode.GetOperand(2);

      foreach (int size in m_recordStack) {
        recordSize += size;
      }

      saveSymbol.Offset = recordSize + paramOffset;
      TopPopSymbol(saveSymbol, TopOrPop.Pop);
    }

    public void GenerateStructUnionAssign(MiddleCode middleCode, int index) {
      Symbol targetSymbol = (Symbol) middleCode.GetOperand(0),
             sourceSymbol = (Symbol) middleCode.GetOperand(1);

      Track targetaddressTrack = LoadAddressToRegister(targetSymbol),
            sourceaddressTrack = LoadAddressToRegister(sourceSymbol);

      GenerateMemoryCopy(targetaddressTrack, sourceaddressTrack,
                         targetSymbol.Type.Size(), index);
    }

    public void GenerateStructUnionParameter(MiddleCode middleCode, int index) {
      Symbol sourceSymbol = (Symbol) middleCode.GetOperand(1);
      Symbol targetSymbol = new Symbol(Type.PointerTypeX);

      int recordSize = (int) middleCode.GetOperand(0),
          paramOffset = (int) middleCode.GetOperand(2);

      foreach (int size in m_recordStack) {
        recordSize += size;
      }

      targetSymbol.Offset = recordSize + paramOffset;
      Track sourceaddressTrack = LoadAddressToRegister(sourceSymbol);
      Track targetaddressTrack = LoadAddressToRegister(targetSymbol);

      GenerateMemoryCopy(targetaddressTrack, sourceaddressTrack,
                         sourceSymbol.Type.Size(), index);    
    }

    public void GenerateStructUnionGetReturnValue(MiddleCode middleCode) {
      Symbol targetSymbol = (Symbol) middleCode.GetOperand(0);
      CheckRegister(targetSymbol, AssemblyCode.ReturnPointerRegister);
      Track targetaddressTrack =
        new Track(targetSymbol.AddressSymbol, AssemblyCode.ReturnPointerRegister);
      AddToTrackMap(targetSymbol.AddressSymbol, targetaddressTrack); // XXX
    }

    public void GenerateStructUnionSetReturnValue(MiddleCode middleCode) {
      Symbol returnSymbol = (Symbol) middleCode.GetOperand(1);
      LoadAddressToRegister(returnSymbol, AssemblyCode.ReturnPointerRegister);
    }
  
          // x = x + 1
          // x = 1 + x
          // x += 1
          // x++
        
          // $1 = y + z
          // x = $1
          // x = y + z
        
          // $1 = -y
          // x = $1
          // x = -y
        
          // x = -x -> neg [bp + 10]

  
    // ------------------------------------------------------------------------
  
    public void GenerateMemoryCopy(Track targetaddressTrack,
                                    Track sourceaddressTrack, int size, int index) {
      Type countType = (size < 256) ? Type.UnsignedCharType : Type.UnsignedIntegerType;
      Track countTrack = new Track(countType, null),
            valueTrack = new Track(Type.UnsignedCharType, null);

      AddAssemblyCode(AssemblyOperator.mov, countTrack, size);
      string labelText = Start.CurrentFunction.UniqueName + Start.SeparatorId +
                         index +  Start.SeparatorId + "Loop";
      int labelIndex = m_assemblyCodeList.Count;
      AddAssemblyCode(AssemblyOperator.label, labelText);
      AddAssemblyCode(AssemblyOperator.mov, valueTrack, sourceaddressTrack, 0);
      AddAssemblyCode(AssemblyOperator.mov, targetaddressTrack, 0, valueTrack);
      AddAssemblyCode(AssemblyOperator.inc, sourceaddressTrack);
      AddAssemblyCode(AssemblyOperator.inc, targetaddressTrack, null, null);
      AddAssemblyCode(AssemblyOperator.dec, countTrack);
      AddAssemblyCode(AssemblyOperator.cmp, countTrack, 0, null);
      AddAssemblyCode(AssemblyOperator.long_jne, null, labelIndex, labelText);
    }


    // ------------------------------------------------------------------------
  
    public void AddInitializationCode() {
      m_assemblyCodeList = new MyList<AssemblyCode>();
      AddAssemblyCode(AssemblyOperator.comment, "Initialize Stack Pointer", null, null);
      AddAssemblyCode(AssemblyOperator.mov, AssemblyCode.FrameRegister, Start.StackTopName, null);

      AddAssemblyCode(AssemblyOperator.comment, "Initialize Heap Pointer", null, null);
      AddAssemblyCode(AssemblyOperator.mov_word, null, 65534, 65534);

      AddAssemblyCode(AssemblyOperator.comment, "Initialize FPU Control Word, truncate mode => set bit 10 and 11.", null, null);
      AddAssemblyCode(AssemblyOperator.fstcw, AssemblyCode.FrameRegister, 0, null);
      AddAssemblyCode(AssemblyOperator.or_word, AssemblyCode.FrameRegister, 0, 0x0C00);
      AddAssemblyCode(AssemblyOperator.fldcw, AssemblyCode.FrameRegister, 0, null);
      AddAssemblyCode(AssemblyOperator.mov_word, Start.StackTopName, 0, 0);

    }

    public void AddArgumentCode() {
      List<sbyte> byteList = new MyList<sbyte>();
      IDictionary<int,string> accessMap = new Dictionary<int,string>();
      List<string> textList = new MyList<string>();

      Symbol pathSymbol = new Symbol(Symbol.Status.Variable, Start.PathName, Storage.Static, Type.StringType, Start.PathText,
                                 byteList, accessMap, null, null, null, textList);
    
      Start.CurrentTable.AddSymbol(pathSymbol);
      GenerateInitializer.GenerateByteTextList(pathSymbol);

      string SpaceLoop = Start.SeparatorId + "SpaceLoop",
             WordStart = Start.SeparatorId + "WordStart",
             WordLoop = Start.SeparatorId + "WordLoop",
             WordDone = Start.SeparatorId + "WordDone",
             ListDone = Start.SeparatorId + "ListDone";

      /*  mov si, bp
          mov word [bp], $Path
          add bp, 2
          mov ax, 1
          mov bx, 129
          cmp byte [bx], 13
          je ListDone */

      AddAssemblyCode(AssemblyOperator.comment, "Initialize Argument List", null, null);
      AddAssemblyCode(AssemblyOperator.mov,
                      Register.si, Register.bp);
      AddAssemblyCode(AssemblyOperator.mov_word,
                      Register.bp, 0, Start.PathName);
      AddAssemblyCode(AssemblyOperator.add,
                      Register.bp, 2);
      AddAssemblyCode(AssemblyOperator.mov, Register.ax, 1);
      AddAssemblyCode(AssemblyOperator.mov, Register.bx, 129);
      AddAssemblyCode(AssemblyOperator.cmp_byte,
                      Register.bx, 0, 13);
      AddAssemblyCode(AssemblyOperator.long_je,
                      null, m_assemblyCodeList.Count + 21, ListDone);

      /* SpaceLoop:
         cmp byte [bx], 32
         jne WordStart
         inc bx
         jmp SpaceLoop */

      AddAssemblyCode(AssemblyOperator.label, SpaceLoop);
      AddAssemblyCode(AssemblyOperator.cmp_byte,
                      Register.bx, 0, 32);
      AddAssemblyCode(AssemblyOperator.long_jne,
                      null, m_assemblyCodeList.Count + 3, WordStart);
      AddAssemblyCode(AssemblyOperator.inc, Register.bx);
      AddAssemblyCode(AssemblyOperator.long_jmp,
                      null, m_assemblyCodeList.Count - 4, SpaceLoop);

      /* WordStart:
         inc ax
         mov word [bp], bx
         add bp, 2 */

      AddAssemblyCode(AssemblyOperator.label, WordStart);
      AddAssemblyCode(AssemblyOperator.inc, Register.ax);
      AddAssemblyCode(AssemblyOperator.mov,
                      Register.bp, 0, Register.bx);
      AddAssemblyCode(AssemblyOperator.add,
                      Register.bp, 2);

      /* WordLoop:
         cmp byte [bx], 32
         je WordDone
         cmp byte [bx], 13
         je ListDone
         inc bx
         jmp WordLoop */

      AddAssemblyCode(AssemblyOperator.label, WordLoop);
      AddAssemblyCode(AssemblyOperator.cmp_byte,
                      Register.bx, 0, 32);
      AddAssemblyCode(AssemblyOperator.long_je,
                      null, m_assemblyCodeList.Count + 5, WordDone);
      AddAssemblyCode(AssemblyOperator.cmp_byte,
                      Register.bx, 0, 13);
      AddAssemblyCode(AssemblyOperator.long_je,
                      null, m_assemblyCodeList.Count + 7, ListDone);
      AddAssemblyCode(AssemblyOperator.inc, Register.bx);
      AddAssemblyCode(AssemblyOperator.long_jmp,
                      null, m_assemblyCodeList.Count - 6, WordLoop);

      /* WordDone:
         mov byte [bx], 0; Space -> Zero
         inc bx; Zero -> Next
         jmp SpaceLoop */

      AddAssemblyCode(AssemblyOperator.label, WordDone);
      AddAssemblyCode(AssemblyOperator.mov_byte, Register.bx, 0, 0);
      AddAssemblyCode(AssemblyOperator.inc, Register.bx);
      AddAssemblyCode(AssemblyOperator.long_jmp,
                      null, m_assemblyCodeList.Count - 19, SpaceLoop);

      /* ListDone:
         mov byte [bx], 0; Return -> Zero
         mov word [bp], 0
         add bp, 2
         mov word [bp], 0
         mov [bp + 6], ax
         mov [bp + 8], si */

      AddAssemblyCode(AssemblyOperator.label, ListDone);
      AddAssemblyCode(AssemblyOperator.mov_byte, Register.bx, 0, 0);
      AddAssemblyCode(AssemblyOperator.mov_word, Register.bp, 0, 0);
      AddAssemblyCode(AssemblyOperator.add, Register.bp, 2);
      AddAssemblyCode(AssemblyOperator.mov_word, Register.bp, 0, 0);
      AddAssemblyCode(AssemblyOperator.mov, Register.bp, 6, Register.ax);
      AddAssemblyCode(AssemblyOperator.mov, Register.bp, 8, Register.si);

/*
      AddAssemblyCode(AssemblyOperator.comment,"Initialize Argument List",null,null);
      AddAssemblyCode(AssemblyOperator.mov, Register.si, Register.bp, null);
      AddAssemblyCode(AssemblyOperator.mov_word, Register.bp, 0, Start.PathName);
      AddAssemblyCode(AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(AssemblyOperator.mov, Register.ax, 1, null);
      AddAssemblyCode(AssemblyOperator.mov, Register.bx, 129, null);
    
      AddAssemblyCode(AssemblyOperator.label, label1, null, null);
      AddAssemblyCode(AssemblyOperator.cmp_byte, Register.bx, 0, 32);
      AddAssemblyCode(AssemblyOperator.long_je, null, m_assemblyCodeList.Count + 5, label2);
      AddAssemblyCode(AssemblyOperator.cmp_byte, Register.bx, 0, 0);
      AddAssemblyCode(AssemblyOperator.long_je, null, m_assemblyCodeList.Count + 17, label5);
      AddAssemblyCode(AssemblyOperator.inc, Register.bx, null, null);
      AddAssemblyCode(AssemblyOperator.long_jmp, null,
                    m_assemblyCodeList.Count - 6, label1);

      AddAssemblyCode(AssemblyOperator.label, label2, null, null);
      AddAssemblyCode(AssemblyOperator.cmp, Register.ax, 1, null);
      AddAssemblyCode(AssemblyOperator.long_je, null, m_assemblyCodeList.Count + 2, label3);
      AddAssemblyCode(AssemblyOperator.mov_byte, Register.bx, 0, 0);

      AddAssemblyCode(AssemblyOperator.label, label3, null, null);
      AddAssemblyCode(AssemblyOperator.inc, Register.bx, null, null);
      AddAssemblyCode(AssemblyOperator.cmp_byte, Register.bx, 0, 32);
      AddAssemblyCode(AssemblyOperator.long_je, null, m_assemblyCodeList.Count - 3, label3);
    
      AddAssemblyCode(AssemblyOperator.label, label4, null, null);
      AddAssemblyCode(AssemblyOperator.mov, Register.bp, 0, Register.bx);
      AddAssemblyCode(AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(AssemblyOperator.inc, Register.ax, null, null);
      AddAssemblyCode(AssemblyOperator.long_jmp, null, m_assemblyCodeList.Count - 19, label1);
    
      AddAssemblyCode(AssemblyOperator.label, label5, null, null);    
      AddAssemblyCode(AssemblyOperator.mov_byte, Register.bx, 0, 0);
      AddAssemblyCode(AssemblyOperator.mov_word, Register.bp, 0, 0);
      AddAssemblyCode(AssemblyOperator.add, Register.bp, 2, null);
      AddAssemblyCode(AssemblyOperator.mov, Register.bp, 6, Register.ax);
      AddAssemblyCode(AssemblyOperator.mov, Register.bp, 8, Register.si);*/
    }
  
    public void GenerateStackHeapCheckFunction() {    
      { List<sbyte> byteList = new MyList<sbyte>();
        IDictionary<int,string> accessMap = new Dictionary<int,string>();
        List<string> textList = new MyList<string>();

        Symbol symbol = new Symbol(Symbol.Status.Variable, Start.StackOverflowName, Storage.Static, Type.StringType, Start.StackOverflowMessage,
                                   byteList, accessMap, null, null, null, textList);

        //Symbol symbol = Symbol.createStaticSymbol(Start.StackOverflowName, Type.StringType, Start.StackOverflowMessage);
        GenerateInitializer.GenerateByteTextList(symbol);
        //Start.StaticTable.AddSymbol(symbol);
        Start.CurrentTable.AddSymbol(symbol);
        //Start.GlobalSet.Add(symbol);
        //Start.CurrentTable.AddSymbol(symbol);
      }
    
      { m_assemblyCodeList = new MyList<AssemblyCode>();
        AddAssemblyCode(AssemblyOperator.add, Register.ax, AssemblyCode.FrameRegister, null);
        string label = Start.CheckStackHeapFunctionRegular + Start.SeparatorId + "1";
        AddAssemblyCode(AssemblyOperator.jc, null, m_assemblyCodeList.Count + 4, label);
        AddAssemblyCode(AssemblyOperator.cmp, Register.ax, -2, null); // 65534
        AddAssemblyCode(AssemblyOperator.ja, null, m_assemblyCodeList.Count + 2, label);
        AddAssemblyCode(AssemblyOperator.jmp, Register.bx, null, null);

        AddAssemblyCode(AssemblyOperator.label, label, null, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x40, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.bx, 1, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.cx, Start.StackOverflowMessage.Length, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.dx, Start.StackOverflowName, null);
        AddAssemblyCode(AssemblyOperator.interrupt, 0x21, null, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.al, -1, null);
        AddAssemblyCode(AssemblyOperator.interrupt,0x21,null,null);
        GenerateJumpInfo();
      
        Symbol symbol = new Symbol(Start.CheckStackHeapFunctionRegular);
        //Start.StaticTable.AddSymbol(symbol);
        Start.CurrentTable.AddSymbol(symbol);
        //GenerateMiddleToObjectJumpInfo();
        GenerateJumpInfo();
        GenerateTargetByteList(symbol.ByteList, symbol.AccessMap, symbol.CallMap,
                               symbol.ReturnSet, symbol.ByteToTextMap(), symbol.GetTextList());
        //Start.GlobalSet.Add(symbol);
        Start.CurrentTable.AddSymbol(symbol);
      }

      { m_assemblyCodeList = new MyList<AssemblyCode>();
        AddAssemblyCode(AssemblyOperator.add, Register.ax, AssemblyCode.EllipseRegister, null);
        string label = Start.CheckStackHeapFunctionEllipse + Start.SeparatorId + "1";
        AddAssemblyCode(AssemblyOperator.jc, null, m_assemblyCodeList.Count + 4, label);
        AddAssemblyCode(AssemblyOperator.cmp, Register.ax, -2, null); // 65534
        AddAssemblyCode(AssemblyOperator.ja, null, m_assemblyCodeList.Count + 2, label);
        AddAssemblyCode(AssemblyOperator.jmp, Register.bx, null, null);

        AddAssemblyCode(AssemblyOperator.label, label, null, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x40, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.bx, 1, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.cx, Start.StackOverflowMessage.Length,null);
        //Pair<string,int> pair = new Pair<>(Start.StackOverflowName, 0);
        //AddAssemblyCode(ObjectOperator.mov, Register.dx, pair, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.dx, Start.StackOverflowName, null);
        AddAssemblyCode(AssemblyOperator.interrupt, 0x21, null, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C, null);
        AddAssemblyCode(AssemblyOperator.mov, Register.al, -1, null);
        AddAssemblyCode(AssemblyOperator.interrupt, 0x21, null, null);
        GenerateJumpInfo();

        Symbol symbol = new Symbol(Start.CheckStackHeapFunctionEllipse);
        Start.CurrentTable.AddSymbol(symbol);
        GenerateJumpInfo();
        GenerateTargetByteList(symbol.ByteList, symbol.AccessMap, symbol.CallMap,
                               symbol.ReturnSet, symbol.ByteToTextMap(), symbol.GetTextList());
      }
    }

    // ----------------------------------------------------------------------
  
    private static int ByteListSize(AssemblyCode objectCode) {
      return objectCode.ByteList().Count;
    }

    private static int ByteListSizeX(AssemblyCode objectCode) {
      if ((objectCode.Operator == AssemblyOperator.empty) ||
          (objectCode.Operator == AssemblyOperator.label) ||
          (objectCode.Operator == AssemblyOperator.comment)) {
        return 0;
      }
      /*else if (objectCode.IsCallRegular() || (objectCode.Operator == AssemblyOperator.long_jmp)) {
        return ((objectCode.GetOperand(0) is Track) ||
                (objectCode.GetOperand(0) is Register))
                ? ObjectCode.ShortJumpSize : ObjectCode.LongJumpSize;
      }
      else if ((objectCode.IsRelation() || objectCode.IsJump()) &&
               (objectCode.GetOperand(1) is int)) {
      */else if (objectCode.IsRelationRegular() || objectCode.IsJumpRegular()) {
        int address = (int) objectCode.GetOperand(1);
        string name = Enum.GetName(typeof(AssemblyOperator), objectCode.Operator);

        //if ((address == 126) || (address == 127)) {
        /*if (address == 126) {
          AssemblyOperator operatorX = (AssemblyOperator)Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "").Replace("long_", ""));
          List<sbyte> byteList = ObjectCode.LookupByteArray(operatorX, 2, null, null);
          return byteList.Count;
        }
        else*/ {
          int size = name.Contains("long_") ? Type.IntegerSize : Type.CharSize;
          AssemblyOperator operatorX = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "").Replace("long_", ""));
          List<sbyte> byteList = AssemblyCode.LookupByteArray(operatorX, size, null, null);
          return byteList.Count;
        }
      }
      else {
        return objectCode.ByteList().Count;
      }
    }

    /*public static IDictionary<ObjectOperator,ObjectOperator>
                    m_inverseMap = new Dictionary<ObjectOperator,ObjectOperator>();

    static ObjectCodeGenerator() {
      m_inverseMap.Add(ObjectOperator.je, ObjectOperator.jne);
      m_inverseMap.Add(ObjectOperator.jne, ObjectOperator.je);
      m_inverseMap.Add(ObjectOperator.jl, ObjectOperator.jge);
      m_inverseMap.Add(ObjectOperator.jle, ObjectOperator.jg);
      m_inverseMap.Add(ObjectOperator.jg, ObjectOperator.jle);
      m_inverseMap.Add(ObjectOperator.jge, ObjectOperator.jl);
      m_inverseMap.Add(ObjectOperator.ja, ObjectOperator.jbe);
      m_inverseMap.Add(ObjectOperator.jae, ObjectOperator.jb);
      m_inverseMap.Add(ObjectOperator.jb, ObjectOperator.jae);
      m_inverseMap.Add(ObjectOperator.jbe, ObjectOperator.ja);
    }*/
  
    private void GenerateJumpInfo() {
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode objectCode = m_assemblyCodeList[line];

        if (objectCode.IsRelationRegular() || objectCode.IsJumpRegular()) {
          if (!(objectCode.GetOperand(1) is int)) {
            int middleTarget = (int) objectCode.GetOperand(2);
            objectCode.SetOperand(1, m_middleToObjectMap[middleTarget]);
          }
        }
      }

      IDictionary<int,int> objectToByteMap;

      { int byteSize = 0, line = 0;
        objectToByteMap = new Dictionary<int, int>();
        foreach (AssemblyCode objectCode in m_assemblyCodeList) {
          objectToByteMap.Add(line++, byteSize);
  
          if (!(objectCode.IsRelationRegular() || objectCode.IsJumpRegular())) {
            byteSize += ByteListSize(objectCode);
          }
        }
        objectToByteMap.Add(m_assemblyCodeList.Count, byteSize);
      }

      while (true) {
        for (int line = 0; line < (m_assemblyCodeList.Count - 1); ++line) {
          AssemblyCode thisCode = m_assemblyCodeList[line],
                     nextCode = m_assemblyCodeList[line + 1];

          /*if (thisCode.IsRelation() && nextCode.IsJump() &&
              (thisCode.GetOperand(1) is int) &&
              (nextCode.GetOperand(1) is int)) {
            int thisTarget = (int) thisCode.GetOperand(1);
            Assert.Error(thisTarget == (line + 2), "this target");

            int nextTarget = (int) nextCode.GetOperand(1);
            int toByteAddress = m_objectToByteMap[nextTarget];
            int forwardDistance = toByteAddress - m_objectToByteMap[line + 2];
            int backwardDistance = toByteAddress - m_objectToByteMap[line + 1];

            if ((backwardDistance >= -128) && (forwardDistance <= 127)) {
              thisCode.Operator = m_inverseMap[thisCode.Operator];
              thisCode.SetOperand(1, nextTarget);
              thisCode.SetOperand(2, nextCode.GetOperand(2));
              nextCode.Operator = AssemblyOperator.empty;
              update = true;
              break;
            }
          }
          else if ((thisCode.IsRelation() || thisCode.IsJump()) &&
                   (thisCode.GetOperand(1) is int)) {
          else*/ if (thisCode.IsRelationRegular() || thisCode.IsJumpRegular()) {
            int thisTarget = (int) thisCode.GetOperand(1);
          
            int fromByteAddress = objectToByteMap[line + 1],
                toByteAddress = objectToByteMap[thisTarget];
            int byteDistance = toByteAddress - fromByteAddress;
            string name = Enum.GetName(typeof(AssemblyOperator), thisCode.Operator);

            /*if (byteDistance == 0) {
              thisCode.Operator = AssemblyOperator.empty;
            }
            else*/ {
              thisCode.SetOperand(0, byteDistance);
            }

            /*else if (name.Contains("short_") && (byteDistance == 126)) {
              thisCode.Operator = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "long_"));
              update = true;
              break;
            }
            else if (name.Contains("long_") && (byteDistance != 126) && (byteDistance >= -129) && (byteDistance <= 127)) {
              thisCode.Operator = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("long_", "short_"));
              update = true;
              break;
            }
            else {
              thisCode.Operator = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "long_"));
              update = true;
            }

            Console.Out.WriteLine("Test");
            /*else if ((thisCode.Operator == AssemblyOperator.long_jmp) &&
                     (byteDistance >= -129) && (byteDistance <= 127)) {
              thisCode.Operator = AssemblyOperator.short_jmp;
              update = true;
              break;
            }*/
          }
        }
      
        bool update = false;

        { foreach (AssemblyCode objectCode in m_assemblyCodeList) {
            if ((objectCode.IsRelationRegister() || objectCode.IsJumpRegular())) {
              int byteDistance = (int) objectCode.GetOperand(0);
              
              if (byteDistance == 0) {
                objectCode.Operator = AssemblyOperator.empty;
              }
            }
          }
        }

        { int byteSize = 0, line = 0;
          foreach (AssemblyCode objectCode in m_assemblyCodeList) {
            if (objectToByteMap[line] != byteSize) {
              objectToByteMap[line] = byteSize;
              update = true;
            }
            
            byteSize += objectCode.ByteList().Count;
            ++line;
          }

//          objectToByteMap.Add(m_objectCodeList.Count, byteSize);
        }

        if (!update) {
          break;
        }
      }

      /*foreach (ObjectCode objectCode in m_objectCodeList) {
        Assert.Error(objectCode.ByteList().Count == ByteListSize(objectCode));
      }*/

      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        AssemblyCode objectCode = m_assemblyCodeList[line];

        if (objectCode.IsRelationRegular() || objectCode.IsJumpRegular()) {
          int objectTarget = (int) objectCode.GetOperand(1);
          int fromByteAddress = objectToByteMap[line + 1],
              toByteAddress = objectToByteMap[objectTarget];
          objectCode.SetOperand(0, toByteAddress - fromByteAddress);
        }      
        else if (objectCode.Operator == AssemblyOperator.register_return) {
          int middleAddress = (int) objectCode.GetOperand(1);
          int objectAddress = m_middleToObjectMap[middleAddress];
          int byteAddress = objectToByteMap[objectAddress];
          int nextAddress = objectToByteMap[line + 1];
          int returnAddress = byteAddress - nextAddress + Type.PointerSize;
          objectCode.SetOperand(1, returnAddress);
        }
        else if (objectCode.Operator == AssemblyOperator.address_return) {
          int middleAddress = (int) objectCode.GetOperand(2);
          int objectAddress = m_middleToObjectMap[middleAddress];
          int byteAddress = objectToByteMap[objectAddress];
          int nextAddress = objectToByteMap[line + 1];
          int returnAddress = byteAddress - nextAddress + Type.PointerSize;
          objectCode.SetOperand(2, returnAddress);
        }
      }
    }

    // ------------------------------------------------------------------------
  
    private static int OperandSize(AssemblyOperator operatorX, object operand) {
      string name = Enum.GetName(typeof(AssemblyOperator), operatorX);

      if (name.Contains("mov_"))
      {
        return AssemblyCode.OperatorSize(operatorX);
      }
      else if (name.Contains("cmp_")) {
        return Math.Max(1, ValueSize(operand));
      }
      else {
        return ValueSize(operand);
      }
    }
  
    private static int ValueSize(object operand) {
      return ValueSize(operand, false);
    }
  
    private static int ValueSize(object operand, bool isCompare) {
      if (operand is int) {
        if (isCompare && (((int) operand) == 0)) {
          return 1;
        }
      
        return AssemblyCode.ValueSize((int) operand);
      }
    
      return 0;
    }
  
    private void GenerateTargetByteList(List<sbyte> byteList, IDictionary<int,string> accessMap,
                                        IDictionary<int,string> callMap, ISet<int> returnSet,
                                        IDictionary<int,int> byteToTextMap, List<string> textList) {
      int lastSize = 0;
      for (int line = 0; line < m_assemblyCodeList.Count; ++line) {
        byteToTextMap[byteList.Count] = line;
        //byteToTextMap.Add(byteList.Count, line);
        AssemblyCode objectCode = m_assemblyCodeList[line];
      
        byteList.AddRange(objectCode.ByteList());
        string text = objectCode.ToString();//X(line, m_objectToByteMap[line));
        int codeSize = byteList.Count - lastSize;
        lastSize = byteList.Count;
      
        if (codeSize > 0) {
          textList.Add(text + "\t; " + codeSize);
        }
        else {
          textList.Add(text);
        }
    
        //string returnLabel = objectCode.GetReturnLabel();
    
        AssemblyOperator operatorX = objectCode.Operator;
        object operand0 = objectCode.GetOperand(0),
               operand1 = objectCode.GetOperand(1),
               operand2 = objectCode.GetOperand(2);

        if (objectCode.IsRelationRegular() || objectCode.IsJumpRegular()) {
          // Empty.
        }
        else if ((objectCode.Operator == AssemblyOperator.empty) ||
                 (objectCode.Operator == AssemblyOperator.comment) ||
                 (objectCode.Operator == AssemblyOperator.label)) {
          // Empty.
        }
        else {
          if ((objectCode.Operator == AssemblyOperator.call) &&
                 (objectCode.GetOperand(0) is string)) {
            string calleeName = (string) objectCode.GetOperand(0);
            int address = byteList.Count - Type.PointerSize;
            callMap.Add(address, calleeName);
            byteToTextMap.Add(address, line);
          }
          else if ((objectCode.Operator == AssemblyOperator.register_return) ||
                   (objectCode.Operator == AssemblyOperator.address_return)) {
            int address = byteList.Count - Type.PointerSize;
            returnSet.Add(address);
          }
          else if (operand0 is string) { // Add [g + 1], 2
            string name = (string) objectCode.GetOperand(0);
            string nameX = Enum.GetName(typeof(AssemblyOperator), operatorX);
            bool IsCompare = nameX.Contains("cmp");
            int size = (nameX.Contains("mov") && (operand2 is int))? Type.PointerSize : ValueSize(operand2, IsCompare);
            //int size = operatorX.Name.Contains("_") ? Type.PointerSize : ValueSize(operand2);
            int address = byteList.Count - Type.PointerSize - size;
            accessMap.Add(address, name);
            WriteName(name);
          }
          else if (operand1 is string) {
            if (operand2 is int) { // mov ax, [g + 1]
              int size = OperandSize(operatorX, operand2);
              int address = byteList.Count /*- size*/ - Type.PointerSize;
              string name = (string) operand1;
              accessMap.Add(address, name);
              WriteName(name);
            }
            else {
              int address = byteList.Count - Type.PointerSize; // mov ax, g
              string name = (string) operand1;
              accessMap.Add(address, name);
              WriteName(name);
            }
          }
          else if (operand2 is string) { // Add [bp + 2], g
            string name = (string) operand2;
            int address = byteList.Count - Type.PointerSize;
            accessMap.Add(address, name);
            WriteName(name);
          }
        }
      }
    }

    public static void WriteName(string name) {
//      if ((Start.CurrentFunction != null) &&
//          Start.CurrentFunction.UniqueName.Equals("main")) {
        name = name.Replace("\n", "\\n");
        //Console.Out.WriteLine(name);        
//      }
    }
  }
}

    /* e = x;     d => ax
       a = b / c; b => ax  
       d = e / f; d => ax
       f = b;     b => ?

       a -> b: yes
       b -> a: no
  
       a -> e: yes
       e -> a: no
  
       b -> d: no
       d -> b: yes
  
       b1: 3ax
       b2: 10, 11
       d1: 8
       d2: 9ax
  
       a: 6ax
       b: 3ax, 10, 11
       d: 8, 9ax
       e: 1, 2, 7ax

       1. mov ?, [x]
       2. mov [e], ?
  
       3. mov ax, [b]    ; b => ax
       4. mov ?, [c]
       5. div ?
       6. empty          ; a => ax
  
       7. empty          ; e => ax
       8. mov ?, [f]
       9. div ?          ; d => ax
  
       10. mov ?, [b]
       11. mov [f], ?

      // int 21       value
      // int ax       GetRegister
      // int [bx]     address
    
      // liveSet: stack  mov ax, [si + 2]; neg ax; int ax
      //          static mov ax, [20]; neg ax
      //        

      // !liveSet: stack  neg [si + 2]
      //           static neg [20]

     1 sbyte: al * 8 => ah:al
     2 bytes: ax * 16 => dx:ax
     4 bytes: eax * 32 => edx:eax

          // b = x;     b => ?
          // ...
          // d = e / f; e => ax
          // ...
          // a = b / c; b => ax

     1 sbyte: ax / 8, div: al, mod: ah
     2 bytes: dx:ax / 16, div: ax, mod: dx
     4 bytes: edx:eax / 32, div eax, mod edx */