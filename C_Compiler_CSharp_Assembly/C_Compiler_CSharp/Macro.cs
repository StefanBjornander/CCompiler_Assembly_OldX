using System.Collections.Generic;

namespace CCompiler {
  public class Macro {
    private int m_parameters;
    private List<Token> m_tokenList;
  
    public Macro(int parameters, List<Token> tokenList) {
      m_parameters = parameters;
      m_tokenList = new MyList<Token>(tokenList);
    
      for (int index = (m_tokenList.Count - 1); index >= 0; --index) {
        if (m_tokenList[index].GetId() == CCompiler_Pre.Tokens.EOL) {
          m_tokenList.RemoveAt(index);
        }
      }
    }

    public int Parameters() {
      return m_parameters;
    }
  
    public List<Token> TokenList() {
      return m_tokenList;
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }
  
    public override bool Equals(object obj) {
      if (obj is Macro) {
        Macro macro = (Macro) obj;
        bool b1 = (m_parameters == macro.m_parameters);
        bool b2 = (m_tokenList.Equals(macro.TokenList()));
        return (m_parameters == macro.m_parameters) &&
               (m_tokenList.Equals(macro.TokenList()));
      }

      return false;
    }
  }
}