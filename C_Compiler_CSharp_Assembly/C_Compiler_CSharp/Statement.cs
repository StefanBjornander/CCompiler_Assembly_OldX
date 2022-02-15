using System.Collections.Generic;

namespace CCompiler {
  public class Statement {
    private List<MiddleCode> m_list;
    private ISet<MiddleCode> m_nextSet;
  
    public Statement(List<MiddleCode> list, ISet<MiddleCode> nextSet) {
      m_list = list;
      m_nextSet = nextSet;
    }
  
    public List<MiddleCode> List() {
      return m_list;
    }
  
    public ISet<MiddleCode> NextSet() {
      return m_nextSet;
    }
  }
}