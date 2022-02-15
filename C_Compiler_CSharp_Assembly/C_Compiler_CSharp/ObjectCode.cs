// 6946:
using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class AssemblyCode {
    public const int CallSize = 3;
    public const int RelationSize = 2;
    public const int LongJumpSize = 3;
    public const int ShortJumpSize = 2;
    public const int JumpDifference = LongJumpSize - ShortJumpSize;
  
    public const sbyte NopOperator = -112;
    public const sbyte ShortJumpOperator = -21;
    public const sbyte LongJumpOperator = -23;

    public const int MainInitializationSize = 31;
    public const int MainArgumentSize = 74;

    public const Register CallRegister = Register.ax;
    public const Register FrameRegister = Register.bp;
    public const Register EllipseRegister = Register.di;
    public const Register ReturnValueRegister = Register.bx;
    public const Register ReturnPointerRegister = Register.bx;
    public const Register ExitValueRegister = Register.al;

    public const Register ShiftRegister = Register.cl;
    public const Register MultiplyRegister = Register.ax;
    public const Register MultiplyResult = Register.ax;
    public const Register DivideResult = Register.ah;
    public const Register ModuloResult = Register.al;
  
    public const Register SourceRegister = Register.dx;
    public const Register TargetRegister = Register.dx;
    public const Register MemoryCount = Register.ax;
    public const Register MemoryValue = Register.bl;
    
    private AssemblyOperator m_operator;
    private object[] m_operandArray = new object[3];
    private int m_returnIndex;
    private bool m_longToShort = false;
  
    public bool IsLongToShort {
      get {
        return m_longToShort;
      }
    }
  
    public AssemblyCode(AssemblyOperator operatorX, object operand1,
                      object operand2) {
      m_operator = operatorX;

      m_operandArray[0] = operand1;
      m_operandArray[1] = operand2;
      m_operandArray[2] = null;

      if (operatorX == AssemblyOperator.register_return) {
        m_returnIndex = (int) operand2;
      }
    }

    public AssemblyCode(AssemblyOperator operatorX, object operand1,
                      object operand2, object operand3) {
      m_operator = operatorX;

      m_operandArray[0] = operand1;
      m_operandArray[1] = operand2;
      m_operandArray[2] = operand3;

      if (operatorX == AssemblyOperator.register_return) {
        m_returnIndex = (int) operand2;
      }

      if (operatorX == AssemblyOperator.address_return) {
        m_returnIndex = (int) operand3;
      }
    }

    public AssemblyOperator Operator {
      get {
        return m_operator;
      }

      set {
        m_operator = value;
      }
    }

    public object GetOperand(int index) {
      return m_operandArray[index];
    }
  
    public void SetOperand(int index, object operand) {
      Assert.Error((index >= 0) && (index < 3));
      m_operandArray[index] = operand;
    }

    // ------------------------------------------------------------------------
  
    public bool IsRelationRegular() {
      return IsRelation() && !(m_operandArray[0] is Register);
    }
  
    public bool IsRelationRegister() {
      return IsRelation() && (m_operandArray[0] is Register);
    }
  
    private bool IsRelation() {
      switch (m_operator) {
        case AssemblyOperator.je:
        case AssemblyOperator.jne:
        case AssemblyOperator.jl:
        case AssemblyOperator.jle:
        case AssemblyOperator.jg:
        case AssemblyOperator.jge:
        case AssemblyOperator.jb:
        case AssemblyOperator.jbe:
        case AssemblyOperator.ja:
        case AssemblyOperator.jae:
        case AssemblyOperator.jc:
        case AssemblyOperator.jnc:
        //case AssemblyOperator.jz:
        //case AssemblyOperator.jnz:

        case AssemblyOperator.short_je:
        case AssemblyOperator.short_jne:
        case AssemblyOperator.short_jl:
        case AssemblyOperator.short_jle:
        case AssemblyOperator.short_jg:
        case AssemblyOperator.short_jge:
        case AssemblyOperator.short_jb:
        case AssemblyOperator.short_jbe:
        case AssemblyOperator.short_ja:
        case AssemblyOperator.short_jae:
        case AssemblyOperator.short_jc:
        case AssemblyOperator.short_jnc:
        case AssemblyOperator.short_jz:
        case AssemblyOperator.short_jnz:

        case AssemblyOperator.long_je:
        case AssemblyOperator.long_jne:
        case AssemblyOperator.long_jl:
        case AssemblyOperator.long_jle:
        case AssemblyOperator.long_jg:
        case AssemblyOperator.long_jge:
        case AssemblyOperator.long_jb:
        case AssemblyOperator.long_jbe:
        case AssemblyOperator.long_ja:
        case AssemblyOperator.long_jae:
        case AssemblyOperator.long_jc:
        case AssemblyOperator.long_jnc:
        case AssemblyOperator.long_jz:
        case AssemblyOperator.long_jnz:
          return true;
        
        default:
          return false;        
      }
    }
  
    public bool IsJumpRegular() {
      return IsJumpRegular(m_operator);
    }

    private bool IsJumpRegister() {
      return IsJumpRegister(m_operator);
    }

    public bool IsCallRegular() {
      return IsCallRegular(m_operator);
    }

    public bool IsCallRegister() {
      return IsCallRegister(m_operator);
    }

    public bool IsCallMemory() {
      return IsCallMemory(m_operator);
    }

    public bool IsCallAuto() {
      return IsCallAuto(m_operator);
    }

    public bool IsCallStatic() {
      return IsCallStatic(m_operator);
    }

    public bool IsJumpRegular(AssemblyOperator operatorX) {
      return IsJump() && !(m_operandArray[0] is Register);
    }

    public bool IsJumpRegister(AssemblyOperator operatorX) {
      return IsJump() && (m_operandArray[0] is Register);
    }

    public bool IsJump() {
      return (m_operator == AssemblyOperator.short_jmp) ||
             (m_operator == AssemblyOperator.long_jmp);    
    }
  
    // call test int null null
    public bool IsCallRegular(AssemblyOperator operatorX) {
      return (m_operator == AssemblyOperator.call) &&
             (m_operandArray[0] is string) && 
             (m_operandArray[1] == null) &&
             (m_operandArray[2] == null);
    }

    // call ax Register null null
    public bool IsCallRegister(AssemblyOperator operatorX) {
      return (m_operator == AssemblyOperator.call) &&
             (m_operandArray[0] is Register) && 
             (m_operandArray[1] == null) &&
             (m_operandArray[2] == null);
    }

    public bool IsCallMemory(AssemblyOperator operatorX) {
      return IsCallAuto(operatorX) || IsCallStatic(operatorX);
    }
  
    // call [bp + 10] Register int null
    public bool IsCallAuto(AssemblyOperator operatorX) {
      return (m_operator == AssemblyOperator.call) &&
             (m_operandArray[0] is Register) &&
             (m_operandArray[1] is int) &&
             (m_operandArray[2] == null);
    }

    // call [1000] null int null
    public bool IsCallStatic(AssemblyOperator operatorX) {
      return (m_operator == AssemblyOperator.call) &&
             (m_operandArray[0] == null) &&
             (m_operandArray[1] is Pair<string,int>) &&
             (m_operandArray[2] == null);
    }

    public bool IsRegisterLoad(int position) {
      return ((position == 1) && (m_operandArray[0] is Register) &&
              ((m_operandArray[1] is Register) ||
               (m_operandArray[1] is int)));
    }
  
    public bool IsRegisterSave(int position) {
      return ((position == 3) && (m_operandArray[2] is Register));
    }
  
    // ------------------------------------------------------------------------
  
    public static bool RegisterOverlap(Register? register1,
                                       Register? register2) {
      if ((register1 == null) || (register2 == null)) {
        return false;
      }
    
      if (register1.Equals(register2)) {
        return true;
      }

      string name1 = Enum.GetName(typeof(Register), register1),
             name2 = Enum.GetName(typeof(Register), register2);

      name1 = (name1.Length == 3) ? name1.Substring(1) : name1;
      name2 = (name2.Length == 3) ? name2.Substring(1) : name2;

      if ((name1.Contains("h") && name2.Contains("l")) ||
          (name1.Contains("l") && name2.Contains("h"))) {
        return false;
      }
    
      name1 = (name1.Contains("h") || name1.Contains("l") ||
               name1.Contains("x")) ? name1.Substring(0, 1) : name1;
      name2 = (name2.Contains("h") || name2.Contains("l") ||
               name2.Contains("x")) ? name2.Substring(0, 1) : name2;

      return name1.Equals(name2);
    }
  
    public static Register RegisterToSize(Register? register, int size) {
      string name = Enum.GetName(typeof(Register), register.Value);

      switch (size) {
        case CCompiler.Type.ShortSize:
          name = name.Replace("x", "l").Replace("e", "");
          break;

        case CCompiler.Type.IntegerSize:
          name = name.Replace("h", "x").Replace("l", "x").Replace("e", "");
          break;

        case CCompiler.Type.LongSize:
          name = "e" + name.Replace("l", "x").Replace("e", "");
          break;
      }

      return (Register) Enum.Parse(typeof(Register), name);
    }
  
    public static int RegisterSize(Register register) {
      string name = Enum.GetName(typeof(Register), register);
    
      if (name.Contains("e")) {
        return CCompiler.Type.LongSize;
      }
      else if (name.Contains("l") || name.Contains("h")) {
        return CCompiler.Type.ShortSize;
      }
      else {
        return CCompiler.Type.IntegerSize;
      }
    }

    public static int OperatorSize(AssemblyOperator operatorX) {
      string name = Enum.GetName(typeof(AssemblyOperator), operatorX);

      if (name.Contains("_byte")) {
        return CCompiler.Type.CharSize;
      }
      else if (name.Contains("_word")) {
        return CCompiler.Type.IntegerSize;
      }
      else if (name.Contains("_dword")) {
        return CCompiler.Type.LongSize;
      }

      Assert.Error("operatorX size");
      return 0;
    }
  
    public static AssemblyOperator OperatorToSize(AssemblyOperator operatorX, int size) {
      string name = Enum.GetName(typeof(AssemblyOperator), operatorX);
    
      if (operatorX == AssemblyOperator.interrupt) {
        return AssemblyOperator.interrupt;
      }
    
      switch (size) {
        case CCompiler.Type.CharSize:
          name = name + "_byte";
          break;
        
        case CCompiler.Type.IntegerSize:
          name = name + "_word";
          break;
        
        case CCompiler.Type.LongSize:
          name = name + "_dword";
          break;
      }

      return (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name);
    }

    public static int OperandNoZeroSize(object operand) {
      long value = (int) operand;
      return (value == 0) ? CCompiler.Type.CharSize : OperandSize(operand);    
    }
  
    public static int OperandSize(object operand) {
      long value = (int) operand;

      if (value == 0) {
        return 0;
      }
      else if (/*(value != 126) &&*/ (-128 <= value) && (value <= 127)) {
        return CCompiler.Type.CharSize;
      }
      else if ((-32768 <= value) && (value <= 32767)) {
        return CCompiler.Type.IntegerSize;
      }
      else {
        return CCompiler.Type.LongSize;
      }
    }

    public static int ValueSize(int value) {
      if (value == 0) {
        return 0;
      }
      else if (value < 256) {
        return 1;
      }
      else if (value < 65536) {
        return 2;
      }
      else {
        return 4;
      }
    }
  
    public int ByteSizeCompare(int RegisterSize, int value, bool signed) {
      switch (RegisterSize) {
        case 1:
          if ((value >= 128) && (value <= 255)) {
            value -= 256;
          }
          break;

        case 2:
          if ((value >= 32768) && (value <= 65535)) {
            value -= 65536;
          }
          break;
      }      

      int size;

      if (value == 0) {
        size = 0;
      }
      else if ((value >= -128) && (value <= 127)) {
        size = 1;
      }
      else if ((value >= -32768) && (value <= 32767)) {
        size = 2;
      }
      else {
        size = 4;
      }
    
      return Math.Min(size, ValueSize());
    }
    
    public int ValueSize() {
      string name = Enum.GetName(typeof(AssemblyOperator), m_operator);

      if (m_operator == AssemblyOperator.long_jmp) {
        return CCompiler.Type.PointerSize;
      }
      else if (IsRelationRegular()) {
        int address = (int) m_operandArray[0];
        return (/*(address != 126) &&*/ (address >= -128) && (address <= 127)) ? 1 : 2;
      }
      else if (m_operator == AssemblyOperator.interrupt) {
        return CCompiler.Type.ShortSize;
      }    
      else if (name.Contains("sbyte")) {
        return CCompiler.Type.ShortSize;
      }
      else if (name.Contains("dword")) {
        return CCompiler.Type.LongSize;
      }
      else if (name.Contains("word")) {
        return CCompiler.Type.IntegerSize;
      }
      else {
        return RegisterSize((Register) m_operandArray[0]);
      }
    }

    // ------------------------------------------------------------------------
  
    // mov [global1}, global2
  
    public List<sbyte> ByteList() {
      AssemblyOperator operatorX = m_operator;

      if (IsCallMemory()) {
        operatorX = AssemblyOperator.jmp;
      }

      if (operatorX == AssemblyOperator.register_return) {
        operatorX = OperatorToSize(AssemblyOperator.mov, CCompiler.Type.PointerSize);
      }

      if (operatorX == AssemblyOperator.address_return) {
        operatorX = OperatorToSize(AssemblyOperator.mov, CCompiler.Type.PointerSize);
      }

      if ((operatorX == AssemblyOperator.empty) ||
          (operatorX == AssemblyOperator.label) ||
          (operatorX == AssemblyOperator.comment)) {
        return (new MyList<sbyte>());
      }
      else if (IsCallRegular()) {
        List<sbyte> byteList = LookupByteArray(AssemblyOperator.jmp, CCompiler.Type.PointerSize, null, null);
        LoadByteList(byteList, byteList.Count - CCompiler.Type.PointerSize, CCompiler.Type.PointerSize, 0);
        return byteList;
      }
      /*else if (IsJumpRegular()) {
        int address = (int)m_operandArray[0];
        int size = ((address != 126) && (-128 <= address) && (address <= 127)) ? CCompiler.Type.CharSize : CCompiler.Type.PointerSize;
        List<sbyte> byteList = LookupByteArray(AssemblyOperator.jmp, size, null, null);
        LoadByteList(byteList, byteList.Count - size, size, address);
        return byteList;
      }*/
      else if (IsCallRegister() || IsJumpRegister()) {
        Register register = (Register) m_operandArray[0];
        return LookupByteArray(AssemblyOperator.jmp, register, null, null);
      }
      /*else if (IsRelationRegular() || IsJumpRegular()) {
        /*if (!(m_operandArray[0] is int)) {
          int i = 1;
        }
        
        int address = (int) m_operandArray[0];
        Assert.Error(name.Contains("short_") || name.Contains("long_"));

        if (address == 126) {
          operatorX = (AssemblyOperator)Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "").Replace("long_", ""));
          List<sbyte> byteList = LookupByteArray(operatorX, Type.IntegerSize, null, null);
          LoadByteList(byteList, byteList.Count - Type.IntegerSize, Type.IntegerSize, address);
          return byteList;
        }
        else {
          string name = Enum.GetName(typeof(AssemblyOperator), m_operator);
          Assert.Error(name.Contains("short_") || name.Contains("long_"));
          int size = name.Contains("short_") ? CCompiler.Type.CharSize : CCompiler.Type.IntegerSize;
          operatorX = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "").Replace("long_", ""));
          List<sbyte> byteList = LookupByteArray(operatorX, size, null, null);

          if (m_operandArray[0] is int) {
            int address = (int) m_operandArray[0];
            LoadByteList(byteList, byteList.Count - size, size, address);
          }

          return byteList;
        }
      }*/
      else if (IsRelationRegular() || IsJumpRegular()) {
        int address = (int) m_operandArray[0];
        int size = (/*(address != 126) &&*/ (address >= -129) && (address <= 127)) ? Type.CharSize : Type.IntegerSize;

        if (address == 127) {
          size = 2;
        }

        string name = Enum.GetName(typeof(AssemblyOperator), m_operator);
        Assert.Error(name.Contains("short_") || name.Contains("long_"));
        operatorX = (AssemblyOperator) Enum.Parse(typeof(AssemblyOperator), name.Replace("short_", "").Replace("long_", ""));
        List<sbyte> byteList = LookupByteArray(operatorX, size, null, null);
        LoadByteList(byteList, byteList.Count - size, size, address);
        return byteList;
      }
      else if (IsRelationRegister()) { 
        Register register = (Register) m_operandArray[0];
        return LookupByteArray(operatorX, register, null, null);
      }
      else {
        object operand1 = null, operand2 = null, operand3 = null;
        int size1 = 0, size2 = 0, size3 = 0;
        int value1 = 0, value2 = 0, value3 = 0;
        string name = Enum.GetName(typeof(AssemblyOperator), operatorX);
      
        if (m_operandArray[2] != null) {
          if (m_operandArray[1] is int) {
            /*if ((operatorX == AssemblyOperator.add_dword) &&
                (m_operandArray[0] is Register) &&
                (m_operandArray[1] is int) &&
                (m_operandArray[2] is int) &&
                (OperandSize((int) m_operandArray[2]) == 2)) {
              operand1 = m_operandArray[0];
              size2 = CCompiler.Type.LongSize;
              operand2 = size2;
              value2 = (int) m_operandArray[1];
            }
            // mov [null + integer}, register
            // mov [string + integer}, register
            else*/ if ((m_operandArray[0] == null) ||
                     (m_operandArray[0] is string)) {
              operand1 = null;
              operand2 = CCompiler.Type.PointerSize;
              size2 = CCompiler.Type.PointerSize;
              value2 = (int) m_operandArray[1];
            }
            // mov [register + integer}, not-null
            else if (m_operandArray[0] is Register) {
              operand1 = m_operandArray[0];
              size2 = OperandSize(m_operandArray[1]);
              operand2 = size2;
              value2 = (int) m_operandArray[1];
            }
            else {
              Assert.Error(false);
            }

            if (m_operandArray[2] is Register) {
              operand3 = m_operandArray[2];
            }
            else if (m_operandArray[2] is int) {
              operand3 = name.StartsWith("mov_") ? OperatorSize(operatorX)
                                                 : OperandNoZeroSize(m_operandArray[2]);

              if ((operatorX == AssemblyOperator.add_dword) &&
                  (m_operandArray[0] is Register) &&
                  (m_operandArray[1] is int) &&
                  (m_operandArray[2] is int) &&
                  (OperandSize((int) m_operandArray[2]) == 2)) {
                size3 = CCompiler.Type.LongSize;
              }
              else {
                size3 = (int) operand3;
              }

              value3 = (int) m_operandArray[2];
            }
            else if (m_operandArray[2] is string) {
              Assert.Error(name.StartsWith("mov_"));
              operand3 = OperatorSize(operatorX);
              size3 = OperatorSize(operatorX);
              value3 = 0;
            }
            else {
              Assert.Error(false);
            }
          }
          else {
            Assert.Error(m_operandArray[0] is Register);          
            operand1 = m_operandArray[0];
          
            // mov register, [null + integer}
            // mov register, [string + integer}
            if ((m_operandArray[1] == null) ||
                (m_operandArray[1] is string)) {
              operand3 = OperandSize(m_operandArray[2]);
              size3 = CCompiler.Type.PointerSize;
              value3 = (int) m_operandArray[2];
            }
            // mov register, [register + integer]
            else if (m_operandArray[1] is Register) {
              operand2 = m_operandArray[1];
              size3 = OperandSize(m_operandArray[2]);
              operand3 = size3;
              value3 = (int) m_operandArray[2];
            }
            else {
              Assert.Error(false);
            }
          }
        }
        else if (m_operandArray[1] != null) {
          if (name.Contains("_") ||
             (m_operator == AssemblyOperator.fstcw) ||
             (m_operator == AssemblyOperator.fldcw)) {
            // inc_word [null + integer}
            // inc_word [string + integer}    
            if ((m_operandArray[0] == null) ||
                (m_operandArray[0] is string)) {
              operand1 = null;
              operand2 = CCompiler.Type.PointerSize;
              size2 = CCompiler.Type.PointerSize;
              value2 = (int) m_operandArray[1];
            }
            // inc_word [register + integer}
            else if (m_operandArray[0] is Register) {
              operand1 = m_operandArray[0];
              operand2 = OperandSize(m_operandArray[1]);
              size2 = OperandSize(m_operandArray[1]);
              value2 = (int) m_operandArray[1];
            }
            else {
              Assert.Error(false);
            }
          }
          else {
            Assert.Error(m_operandArray[0] is Register);
            Register register = (Register) m_operandArray[0];
            operand1 = register;

            // mov register, integer
            if (m_operandArray[1] is int) {
              if ((operatorX == AssemblyOperator.add) &&
                  (m_operandArray[0] is Register) &&
                   (((Register) m_operandArray[0]) ==  Register.eax) &&
                   (m_operandArray[1] is int) &&
                   (OperandSize((int) m_operandArray[1]) == 2) &&
                   (m_operandArray[2] == null)) {
                size2 = CCompiler.Type.LongSize;
              }
              else if (operatorX == AssemblyOperator.mov) {
                size2 = RegisterSize(register);
              }
              else if ((operatorX == AssemblyOperator.and) &&
                       (RegisterSize(register) == CCompiler.Type.LongSize)) {
                size2 = CCompiler.Type.LongSize;
              }
              /*else if (((operatorX == AssemblyOperator.and) ||
                        (operatorX == AssemblyOperator.add)) &&
                       (RegisterSize(register) == CCompiler.Type.LongSize)) {
                size2 = CCompiler.Type.LongSize;
              }*/
              else {
                size2 = OperandSize(m_operandArray[1]);
              }
            
              operand2 = size2;
              value2 = (int) m_operandArray[1];
            }
            // mov register, string
            else if (m_operandArray[1] is string) {
              Assert.Error(operatorX == AssemblyOperator.mov);
              operand2 = CCompiler.Type.PointerSize;
              size2 = CCompiler.Type.PointerSize;
            }
            // mov register, register
            else if (m_operandArray[1] is Register) {
              operand2 = m_operandArray[1];
            }
            else {
              Assert.Error(false);
            }
          }
        }
        else if (m_operandArray[0] is Register) {
          operand1 = m_operandArray[0];
        }
        else if (m_operandArray[0] is int) {
          operand1 = OperandSize(m_operandArray[0]);
          size1 = OperandSize(m_operandArray[0]);
          value1 = (int) m_operandArray[0];
        }
        else {
          Assert.Error((m_operandArray[0] == null) &&
                       (m_operandArray[1] == null) &&
                       (m_operandArray[2] == null));
        }

        /*if ((operatorX == ObjectOperator.mov_word) &&
            (operand1 != null) && operand1.Equals(Register.di) &&
            (size2 == 0) && (size3 == 2)) {
          //size2 = 1;
        }*/

        List<sbyte> byteList = LookupByteArray(operatorX, operand1, operand2, operand3);
        LoadByteList(byteList, byteList.Count - (size1 + size2 + size3), size1, value1);
        LoadByteList(byteList, byteList.Count - (size2 + size3), size2, value2);
        LoadByteList(byteList, byteList.Count - size3, size3, value3);
        return byteList;
      }
    }
  
    public static void LoadByteList(List<sbyte> byteList, int index,
                                    int size, int value) {
      switch (size) {
        case 0:
          break;
        
        case CCompiler.Type.CharSize:
          byteList[index] = (sbyte) value;
          break;

        case CCompiler.Type.IntegerSize:
          byteList[index] = (sbyte) value;
          byteList[index + 1] = (sbyte) ((int) (value >> 8));
          break;

        case CCompiler.Type.LongSize:
          byteList[index] = (sbyte) value;
          byteList[index + 1] = (sbyte) ((int) (value >> 8));
          byteList[index + 2] = (sbyte) ((int) (value >> 16));
          byteList[index + 3] = (sbyte) ((int) (value >> 24));
          break;
      }
    }

    public override string ToString() {
      object operand0 = m_operandArray[0],
             operand1 = m_operandArray[1],
             operand2 = m_operandArray[2];

      if (m_operator == AssemblyOperator.empty) {
        return "";
      }
      else if (m_operator == AssemblyOperator.label) {
        return ("\n\n" + operand0 + ":" +
              ((operand1 != null) ? ("\t; " + operand1) : ""));
      }
      else if (m_operator == AssemblyOperator.comment) {
        return ("\t; " + operand0);
      }

      string name = Enum.GetName(typeof(AssemblyOperator), m_operator);

      string operatorName = (m_operator == AssemblyOperator.call) ? "jmp" :
                             (name.EndsWith("jmp") ? "jmp" :
                              (m_operator == AssemblyOperator.interrupt) ? "int" :
                               (m_operator == AssemblyOperator.register_return) ? "mov" :
                                (m_operator == AssemblyOperator.address_return) ? "mov word" :
                                 name.Replace("long_", "").Replace("short_", "").Replace("_", " "));
      StringBuilder buffer = new StringBuilder("\t" + operatorName);

      if (IsCallRegular()) {
        buffer.Append(" " + ToString(operand0));
      }
      else if (IsCallRegister()) {
        buffer.Append(" " + operand0);
      }
      else if (IsCallAuto()) {
        buffer.Append(" [" + ToString(operand0) +
                      withSign(operand1) + "]");
      }
      else if (IsCallStatic()) {
        buffer.Append(" [" + ToString(operand1) + "]");
      }
      else if (IsRelationRegular() || IsJumpRegular()) {
        if (operand2 is int) {
          string label = Start.CurrentFunction.UniqueName + Start.SeparatorId +
                         ((int) operand2);
          buffer.Append(" " + label);
        }
        else if (operand2 is string) {
          buffer.Append(" " + (string) operand2);
        }
        /*else if (operand1 is int) {
          string label = Start.CurrentFunction.UniqueName + Start.SeparatorId +
                         ((int) operand1);
          buffer.Append(" " + label);
        }*/
        else {
          buffer.Append(" " + operand0);
        }
      }
      else if (m_operator == AssemblyOperator.register_return) {
        string label = Start.CurrentFunction.UniqueName + Start.SeparatorId + m_returnIndex;
        buffer.Append(" " + ToString(operand0) + ", " + label);
      }
      else if (m_operator == AssemblyOperator.address_return) {
        string label = Start.CurrentFunction.UniqueName + Start.SeparatorId + m_returnIndex;
        buffer.Append(" [" + ToString(operand0) +
                      withSign(operand1) + "], " + label);
      }
      else if ((operand0 != null) && (operand1 != null) &&
               (operand2 != null)) {
        if (operand1 is int) {
          buffer.Append(" [" + ToString(operand0) +
                        withSign(operand1) + "], " +
                        ToString(operand2));
        }
        else {
          buffer.Append(" " + ToString(operand0) +
                        ", [" + ToString(operand1) +
                        withSign(operand2) + "]");
        }
      }
      else if ((operand0 == null) && (operand1 != null) &&
               (operand2 != null)) {
        buffer.Append(" [" + ToString(operand1) + "], " +
                      ToString(operand2));
      }
      else if ((operand0 != null) && (operand1 == null) &&
               (operand2 != null)) {
        buffer.Append(" " + ToString(operand0) + ", [" +
                      ToString(operand2) + "]");
      }
      else if ((operand0 == null) && (operand1 != null) &&
               (operand2 == null)) {
        buffer.Append(" [" + ToString(operand1) + "]");
      }
      else if ((name.Equals("fstcw") ||
                name.Equals("fldcw")) &&
               (operand0 != null) && (operand1 != null) &&
               (operand2 == null)) {
        buffer.Append(" [" + ToString(operand0) +
                      withSign(operand1) + "]");
      }    
      else if (name.Contains("_") && (operand0 != null) &&
               (operand1 != null) && (operand2 == null)) {
        buffer.Append(" [" + ToString(operand0) +
                      withSign(operand1) + "]");
      }    
      else if (!name.Contains("_") && (operand0 != null) &&
               (operand1 != null) && (operand2 == null)) {
        buffer.Append(" " + ToString(operand0) + ", " +
                      ToString(operand1));
      }    
      else if ((operand0 != null) && (operand1 == null) &&
               (operand2 == null)) {
        buffer.Append(" " + ToString(operand0));
      }    
      else if ((operand0 == null) && (operand1 == null) &&
               (operand2 == null)) {
        // Empty.
      }
      else {
        Assert.Error("obj code ToString");
      }

      return buffer.ToString();
    }
  
    private string withSign(object obj) {
      int value = (int) obj;
    
      if (value > 0) {
        return " + " + value;
      }
      else if (value < 0) {
        return " - " + (-value);
      }
      else {
        return "";
      }
    }

    private string ToString(object obj) {
      if (obj is Pair<string,int>) {
        Pair<string,int> pair = (Pair<string,int>) obj;
        string name = (string) pair.First;
        int offset = (int) pair.Second;
        return name + withSign(offset);
      }
      else if (obj is Track) {
        return ((Track) obj).Name;
      }
      else if (obj is Register) {
        return Enum.GetName(typeof(Register), obj);
      }
      else {
        return obj.ToString();
      }
    }

    public static List<sbyte> LookupByteArray(AssemblyOperator operatorX,
                             object operand1, object operand2, object operand3) {
      if ((operatorX == AssemblyOperator.shl) ||
          (operatorX == AssemblyOperator.shr)) {
        operand1 = (operand1 is int) ? 0 : operand1;
        operand2 = (operand2 is int) ? 0 : operand2;
        operand3 = (operand3 is int) ? 0 : operand3;
      }

      ObjectCodeInfo info = new ObjectCodeInfo(operatorX, operand1, operand2, operand3);
      sbyte[] byteArray = Start.MainArrayMap[info];
      Assert.Error(byteArray != null, "bytearray");
      List<sbyte> byteList = new MyList<sbyte>();

      foreach (sbyte b in byteArray) {
        byteList.Add(b);
      }

      return byteList;
    }
  }
}