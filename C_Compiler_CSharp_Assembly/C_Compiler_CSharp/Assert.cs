using System;
using System.IO;

namespace CCompiler {
  public class Assert {
    public static void Error(bool test, string message) {
      Error(test, null, message);
    }
  
    public static void Error(bool test) {
      Error(test, null, null);
    }
  
    public static void Error(bool test, object obj, string message) {
      if (!test) {
        Error((obj != null) ? obj.ToString() : null, message);
      }
    }

    public static void Error(string message) {
      Error(null, message);
    }

    public static void Error(object obj, string messageX) {
      Message("Error", (obj != null) ? obj.ToString() : null, messageX);
/*      Start.BeforeCodeStream.Close();
      Start.MiddleCodeStream.Close();
      Start.AfterCodeStream.Close();
      Start.GraphCodeStream.Close();
      Start.ObjectCodeStream.Close();
      Start.MemberStream.Close();*/
      Console.In.ReadLine();
      Environment.Exit(-1);
    }

    public static void Warning(bool test, object obj, string message) {
      if (!test) {
        Warning((obj != null) ? obj.ToString() : null, message);
      }
    }

    public static void Warning(object obj, string messageX) {
      if (Start.SmallWarning) {
        Message("Warning", (obj != null) ? obj.ToString() : null, messageX);
      
        if (Start.LargeWarning) {
          Environment.Exit(-1);
        }
      }
    }

    private static void Message(string type, string name, string message) {
      string funcText = (Start.CurrentFunction != null) ? (" in function \"" +
                        Start.CurrentFunction.Name + "\"") : "";
      TextWriter errorStream = (Start.ErrorStream != null) ?
                               Start.ErrorStream : Console.Error;
    
      if ((name != null) && (Start.Path != null)) {
        errorStream.WriteLine(type + " in file \"" + Start.Path + "\"" + funcText +
             " at line " + Start.Line + ". \"" + name + "\": " + message + ".");
      }
      else if ((name == null) && (Start.Path != null)) {
        errorStream.WriteLine(type + " in file \"" + Start.Path + "\"" + funcText +
                            " at line " + Start.Line + ": " + message + ".");
      }
      else if ((name != null) && (Start.Path == null)) {
        errorStream.WriteLine(type + ": \"" + name + "\", " + message + ".");
      }
      else if ((name == null) && (Start.Path == null)) {
        errorStream.WriteLine(type + ": " + message + ".");
      }
    }
  }
}