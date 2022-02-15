using System;
using System.Linq;
using System.Collections.Generic;

namespace CCompiler {
  class SystemCode {
    private enum InOut {In, Out};
    private static List<Pair<Register, Symbol>> m_outParameterList = new List<Pair<Register, Symbol>>();
    private static IDictionary<String, List<AssemblyCode>> m_initMap = new Dictionary<String, List<AssemblyCode>>();
    private static IDictionary<String, List<Pair<Register,InOut>>> m_parameterMap = new Dictionary<String, List<Pair<Register, InOut>>>();
    private static IDictionary<String, Type> m_returnTypeMap = new Dictionary<String, Type>();
    private static IDictionary<String, Object> m_returnMap = new Dictionary<String, Object>();
    private static IDictionary<String, int> m_valueMap = new Dictionary<String, int>();
    private static IDictionary<String, int> m_carryMap = new Dictionary<String, int>();

    static SystemCode() {
      { { List<AssemblyCode> readCharInitList = new List<AssemblyCode>();
          readCharInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3F));
          readCharInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 1));
          m_initMap.Add("read_char", readCharInitList);
        }

        { List<Pair<Register,InOut>> readCharParameterList = new List<Pair<Register,InOut>>();
          readCharParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          readCharParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("read_char", readCharParameterList);
        }

        m_returnTypeMap.Add("read_char", Type.SignedIntegerType);
        m_returnMap.Add("read_char", Register.ax);
      }

      { { List<AssemblyCode> writeCharInitList = new List<AssemblyCode>();
          writeCharInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x40));
          writeCharInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 1));
          m_initMap.Add("write_char", writeCharInitList);
        }
        
        { List<Pair<Register,InOut>> writeCharParameterList = new List<Pair<Register,InOut>>();
          writeCharParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          writeCharParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("write_char", writeCharParameterList);
        }

        m_returnTypeMap.Add("write_char", Type.SignedIntegerType);
        m_returnMap.Add("write_char", Register.ax);
      }

      { { List<AssemblyCode> fileExistsInitList = new List<AssemblyCode>();
          fileExistsInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x43));
          fileExistsInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.al, 0));
          m_initMap.Add("file_exists", fileExistsInitList);
        }

        { List<Pair<Register,InOut>> fileExistsParameterList = new List<Pair<Register,InOut>>();
          fileExistsParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_exists", fileExistsParameterList);
        }

        m_returnTypeMap.Add("file_exists", Type.SignedIntegerType);
        m_returnMap.Add("file_exists", 1);
        m_carryMap.Add("file_exists", 0);
      }

      /*{ { List<ObjectCode> fileSizeInitList = new List<ObjectCode>();
          fileSizeInitList.Add(new ObjectCode(ObjectOperator.mov, Register.ah, 0x42));
          fileSizeInitList.Add(new ObjectCode(ObjectOperator.mov, Register.cx, 0));
          fileSizeInitList.Add(new ObjectCode(ObjectOperator.mov, Register.dx, 0));
          m_initMap.Add("file_size", fileSizeInitList);
        }

        { List<Pair<Register,InOut>> fileSizeParameterList = new List<Pair<Register,InOut>>();
          fileSizeParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileSizeParameterList.Add(new Pair<Register, InOut>(Register.bx, InOut.Out));
          fileSizeParameterList.Add(new Pair<Register, InOut>(Register.bx, InOut.Out));
          m_parameterMap.Add("file_size", fileSizeParameterList);
        }

        m_returnTypeMap.Add("file_size", Type.UnsignedLongIntegerType);
        m_returnMap.Add("file_size", Register.ax);
        m_carryMap.Add("file_size", -1);
      }*/

      { { List<AssemblyCode> fileCreateInitList = new List<AssemblyCode>();
          fileCreateInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3C));
          fileCreateInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 0));
          m_initMap.Add("file_create", fileCreateInitList);
        }

        { List<Pair<Register,InOut>> fileCreateParameterList = new List<Pair<Register,InOut>>();
          fileCreateParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_create", fileCreateParameterList);
        }

        m_returnTypeMap.Add("file_create", Type.SignedIntegerType);
        m_returnMap.Add("file_create", Register.ax);
        m_carryMap.Add("file_create", -1);
      }

      { { List<AssemblyCode> fileOpenInitList = new List<AssemblyCode>();
          fileOpenInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3D));
          m_initMap.Add("file_open", fileOpenInitList);
        }

        { List<Pair<Register,InOut>> fileOpenParameterList = new List<Pair<Register,InOut>>();
          fileOpenParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          fileOpenParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("file_open", fileOpenParameterList);
        }

        m_returnTypeMap.Add("file_open", Type.SignedIntegerType);
        m_returnMap.Add("file_open", Register.ax);
        m_carryMap.Add("file_open", -1);
      }

      { { List<AssemblyCode> fileCloseInitList = new List<AssemblyCode>();
          fileCloseInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3E));
          m_initMap.Add("file_close", fileCloseInitList);
        }

        { List<Pair<Register,InOut>> fileCloseParameterList = new List<Pair<Register,InOut>>();
          fileCloseParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          m_parameterMap.Add("file_close", fileCloseParameterList);
        }

        m_returnTypeMap.Add("file_close", Type.SignedIntegerType);
        m_returnMap.Add("file_close", 0);
        m_carryMap.Add("file_close", -1);
      }

      { { List<AssemblyCode> fileRemoveInitList = new List<AssemblyCode>();
          fileRemoveInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x41));
          fileRemoveInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cl, 0));
          m_initMap.Add("file_remove", fileRemoveInitList);
        }

        { List<Pair<Register,InOut>> fileRemoveParameterList = new List<Pair<Register,InOut>>();
          fileRemoveParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_remove", fileRemoveParameterList);
        }

        m_returnTypeMap.Add("file_remove", Type.SignedIntegerType);
        m_returnMap.Add("file_remove", 0);
        m_carryMap.Add("file_remove", -1);
      }

      { { List<AssemblyCode> fileRenameInitList = new List<AssemblyCode>();
          fileRenameInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x56));
          fileRenameInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cl, 0));
          m_initMap.Add("file_rename", fileRenameInitList);
        }

        { List<Pair<Register,InOut>> fileRenameParameterList = new List<Pair<Register,InOut>>();
          fileRenameParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          fileRenameParameterList.Add(new Pair<Register,InOut>(Register.di, InOut.In));
          m_parameterMap.Add("file_rename", fileRenameParameterList);
        }

        m_returnTypeMap.Add("file_rename", Type.SignedIntegerType);
        m_returnMap.Add("file_rename", 0);
        m_carryMap.Add("file_rename", -1);
      }

      { { List<AssemblyCode> fileReadInitList = new List<AssemblyCode>();
          fileReadInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x3F));
          m_initMap.Add("file_read", fileReadInitList);
        }

        { List<Pair<Register,InOut>> fileReadParameterList = new List<Pair<Register,InOut>>();
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.In));
          fileReadParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_read", fileReadParameterList);
        }

        m_returnTypeMap.Add("file_read", Type.SignedIntegerType);
        m_returnMap.Add("file_read", Register.ax);
        m_carryMap.Add("file_read", -1);
      }

      { { List<AssemblyCode> fileWriteInitList = new List<AssemblyCode>();
          fileWriteInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x40));
          m_initMap.Add("file_write", fileWriteInitList);
        }

        { List<Pair<Register,InOut>> fileWriteParameterList = new List<Pair<Register,InOut>>();
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.In));
          fileWriteParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_write", fileWriteParameterList);
        }

        m_returnTypeMap.Add("file_write", Type.SignedIntegerType);
        m_returnMap.Add("file_write", Register.ax);
        m_carryMap.Add("file_write", -1);
      }

      { { List<AssemblyCode> fileFSeekInitList = new List<AssemblyCode>();
          fileFSeekInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x42));
          fileFSeekInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.cx, 0));
          m_initMap.Add("file_seek", fileFSeekInitList);
        }

        { List<Pair<Register,InOut>> fileFSeekParameterList = new List<Pair<Register,InOut>>();
          fileFSeekParameterList.Add(new Pair<Register,InOut>(Register.bx, InOut.In));
          fileFSeekParameterList.Add(new Pair<Register, InOut>(Register.al, InOut.In));
          fileFSeekParameterList.Add(new Pair<Register, InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("file_seek", fileFSeekParameterList);
        }

        m_returnTypeMap.Add("file_seek", Type.UnsignedIntegerType);
        m_returnMap.Add("file_seek", Register.ax);
        m_carryMap.Add("file_seek", -1);
      }

      { { List<AssemblyCode> signalInitList = new List<AssemblyCode>();
          signalInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x25));
          m_initMap.Add("signal", signalInitList);
        }

        { List<Pair<Register,InOut>> signalParameterList = new List<Pair<Register,InOut>>();
          signalParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          signalParameterList.Add(new Pair<Register,InOut>(Register.dx, InOut.In));
          m_parameterMap.Add("signal", signalParameterList);
        }

        m_returnTypeMap.Add("signal", Type.VoidType);
      }

      { { List<AssemblyCode> raiseInitList = new List<AssemblyCode>();
          raiseInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x35));
          m_initMap.Add("raise", raiseInitList);
        }

        { List<Pair<Register,InOut>> raiseParameterList = new List<Pair<Register,InOut>>();
          raiseParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("raise", raiseParameterList);
        }

        m_returnTypeMap.Add("raise", Type.UnsignedIntegerType);
        m_returnMap.Add("raise", Register.bx);
      }

      { { List<AssemblyCode> abortInitList = new List<AssemblyCode>();
          abortInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C));
          abortInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.al, -1));
          m_initMap.Add("abort", abortInitList);
        }

        m_returnTypeMap.Add("abort", Type.VoidType);
      }

      { { List<AssemblyCode> exitInitList = new List<AssemblyCode>();
          exitInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x4C));
          m_initMap.Add("exit", exitInitList);
        }

        { List<Pair<Register,InOut>> exitParameterList = new List<Pair<Register,InOut>>();
          exitParameterList.Add(new Pair<Register,InOut>(Register.al, InOut.In));
          m_parameterMap.Add("exit", exitParameterList);
        }

        m_returnTypeMap.Add("exit", Type.VoidType);
      }

      { { List<AssemblyCode> dateInitList = new List<AssemblyCode>();
          dateInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x2A));
          m_initMap.Add("date", dateInitList);
        }

        { List<Pair<Register,InOut>> dateParameterList = new List<Pair<Register,InOut>>();
          dateParameterList.Add(new Pair<Register,InOut>(Register.cx, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.dh, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.dl, InOut.Out));
          dateParameterList.Add(new Pair<Register, InOut>(Register.al, InOut.Out));
          m_parameterMap.Add("date", dateParameterList);
        }

        m_returnTypeMap.Add("date", Type.VoidType);
      }

      { { List<AssemblyCode> timeInitList = new List<AssemblyCode>();
          timeInitList.Add(new AssemblyCode(AssemblyOperator.mov, Register.ah, 0x2C));
          m_initMap.Add("time", timeInitList);
        }

        { List<Pair<Register,InOut>> timeParameterList = new List<Pair<Register,InOut>>();
          timeParameterList.Add(new Pair<Register,InOut>(Register.ch, InOut.Out));
          timeParameterList.Add(new Pair<Register, InOut>(Register.cl, InOut.Out));
          timeParameterList.Add(new Pair<Register, InOut>(Register.dh, InOut.Out));
          //timeParameterList.Add(new Pair<Register, InOut>(Register.dl, InOut.Out));
          m_parameterMap.Add("time", timeParameterList);
        }

        m_returnTypeMap.Add("time", Type.VoidType);
      }
    }

    public static Type ReturnType(String name) {
      Assert.Error(m_returnTypeMap.ContainsKey(name));
      return m_returnTypeMap[name];
    }

    public static void GenerateInit(String name, ObjectCodeGenerator objectCodeGenerator) {
      // Empty.
    }

    public static void GenerateParameter(String name, int index, Symbol argSymbol, ObjectCodeGenerator objectCodeGenerator) {
      Assert.Error(m_parameterMap.ContainsKey(name));
      List<Pair<Register,InOut>> parameterList = m_parameterMap[name];
      Assert.Error(index < parameterList.Count);
      Pair<Register,InOut> pair = parameterList[index];
      Register register = pair.First;
      InOut inOut = pair.Second;

      if (inOut == InOut.In) {
        objectCodeGenerator.LoadValueToRegister(argSymbol, register);
      }
      else {
        m_outParameterList.Add(new Pair<Register,Symbol>(register, argSymbol));
      }
    }

    public static void GenerateCall(String name, Symbol returnSymbol, ObjectCodeGenerator objectCodeGenerator, int middleIndex) {
      List<AssemblyCode> initList = m_initMap[name];

      foreach (AssemblyCode objectCode in initList) {
        objectCodeGenerator.AddAssemblyCode(objectCode);
      }
      
      objectCodeGenerator.AddAssemblyCode(AssemblyOperator.interrupt, 0x21);
      //ObjectCode jumpCode = null;

      foreach (Pair<Register,Symbol> pair in m_outParameterList) {
        Register outRegister = pair.First;
        Symbol outSymbol = pair.Second;
        Track outTrack = new Track(outSymbol, outRegister);
        objectCodeGenerator.SaveFromRegister(outTrack, outSymbol);
//        objectCodeGenerator.AddToTrackMap(outSymbol, outTrack);
//        objectCodeGenerator.m_trackMap.Add(outSymbol, outTrack);
        objectCodeGenerator.m_trackSet.Add(outTrack);
      }

      m_outParameterList.Clear();

      if ((returnSymbol != null) && !returnSymbol.Type.IsVoid()) {
        Object returnObject = m_returnMap[name];
        Track returnTrack;

        if (returnObject is Register) {
          returnTrack = new Track(returnSymbol, (Register) returnObject);
        }
        else {
          returnTrack = new Track(returnSymbol);
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.mov, returnTrack, (int) returnObject);
        }

        if (m_carryMap.ContainsKey(name)) {
/*          jumpCode = new ObjectCode(ObjectOperator.jnc, null, null);
          objectCodeGenerator.AddAssemblyCode(jumpCode);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, returnTrack, m_carryMap[name]);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.long_jmp, null,  null, middleIndex + 1);*/
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.jnc, null, null, middleIndex + 1);
          objectCodeGenerator.AddAssemblyCode(AssemblyOperator.mov, returnTrack, m_carryMap[name]);
        }

        objectCodeGenerator.AddToTrackMap(returnSymbol, returnTrack);
      }
    }

    /*
    public static void GenerateCall(String name, List<Expression> argExprList, Symbol returnSymbol, ObjectCodeGenerator objectCodeGenerator) {
      switch (name) {
        case "read_char":
          Assert.Error(argExprList.Count == 2);
          Assert.Error(argExprList[0].LongList().Count == 0);
          //objectCodeGenerator.GenerateObjectCodeList(argExprList[0].LongList());
          objectCodeGenerator.LoadValueToRegister(argExprList[0].Symbol(), Register.bx);
          Assert.Error(argExprList[1].LongList().Count == 0);
          //objectCodeGenerator.GenerateObjectCodeList(argExprList[1].LongList());
          objectCodeGenerator.LoadValueToRegister(argExprList[1].Symbol(), Register.dx);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.ah, 0x3F);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.cx, 1);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.interrupt, 0x21);
          Assert.Error((returnSymbol == null) || returnSymbol.Type.IsVoid());
          break;

        case "write_char":
          Assert.Error(argExprList.Count == 2);
          Assert.Error(argExprList[0].LongList().Count == 0);
          //objectCodeGenerator.GenerateObjectCodeList(argExprList[0].LongList());
          objectCodeGenerator.LoadValueToRegister(argExprList[0].Symbol(), Register.bx);
          Assert.Error(argExprList[1].LongList().Count == 0);
          //objectCodeGenerator.GenerateObjectCodeList(argExprList[1].LongList());
          objectCodeGenerator.LoadValueToRegister(argExprList[1].Symbol(), Register.dx);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.ah, 0x40);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.mov, Register.cx, 1);
          objectCodeGenerator.AddAssemblyCode(ObjectOperator.interrupt, 0x21);
          Assert.Error((returnSymbol == null) || returnSymbol.Type.IsVoid());
          break;
      }
    }*/
  }
}
