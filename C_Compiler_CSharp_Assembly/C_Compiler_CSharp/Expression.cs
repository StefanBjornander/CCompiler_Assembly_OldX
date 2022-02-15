using System.Collections.Generic;

namespace CCompiler {
  public class Expression {
    private Symbol m_symbol, m_logicalSymbol;
    private List<MiddleCode> m_shortList, m_longList, m_logicalList;
    private Register? m_register = null;
  
    public bool IsLogical() {
      return (m_logicalSymbol != null) || m_symbol.Type.IsLogical();
    }

    public Expression(Symbol symbol, List<MiddleCode> shortList,
                      List<MiddleCode> longList, Register? register) {
      m_symbol = symbol;
      m_shortList = (shortList != null) ? shortList : (new MyList<MiddleCode>());
      m_longList = (longList != null) ? longList : (new MyList<MiddleCode>());
      m_logicalSymbol = null;
      m_logicalList = null;
      m_register = register;
    }
  
    public Expression(Symbol symbol, List<MiddleCode> shortList,
                      List<MiddleCode> longList) {
      m_symbol = symbol;
      m_shortList = (shortList != null) ? shortList : (new MyList<MiddleCode>());
      m_longList = (longList != null) ? longList : (new MyList<MiddleCode>());
      m_logicalSymbol = null;
      m_logicalList = null;
    }
  
    public Expression(Symbol symbol, List<MiddleCode> shortList,
                      List<MiddleCode> longList, Symbol logicalSymbol,
                      List<MiddleCode> logicalList) {
      m_symbol = symbol;
      m_shortList = (shortList != null) ? shortList : (new MyList<MiddleCode>());
      m_longList = (longList != null) ? longList : (new MyList<MiddleCode>());
      m_logicalSymbol = logicalSymbol;
      m_logicalList = logicalList;
    }
  
    public Symbol Symbol() {
      return m_symbol;
    }
  
    public List<MiddleCode> ShortList() {
      return m_shortList;
    }
  
    public List<MiddleCode> LongList() {
      return m_longList;
    }
  
    public Symbol logicalSymbol() {
      return m_logicalSymbol;
    }
  
    public List<MiddleCode> logicalList() {
      return m_logicalList;
    }

    public Register? Register() {
      return m_register;
    }
  
    public override string ToString() {
      return m_symbol.ToString();
    }
  }
}