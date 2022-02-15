namespace CCompiler {
  public class Declarator {
    private string m_name;
    private CCompiler.Type m_firstType, m_lastType;

    public Declarator(string name) {
      m_name = name;
    }
  
    public string Name {
      get {
        return m_name;
      }
    }

    public CCompiler.Type Type {
      get {
        return m_firstType;
      }
    }

    public void Add(CCompiler.Type type) {
      if (m_firstType == null) {
        m_firstType = m_lastType = type;
      }
      else {
        switch (m_lastType.GetSort()) {
          case Sort.Pointer:
            m_lastType.PointerType = type;
            m_lastType = type;
            break;

          case Sort.Array:
            Assert.Error(type.IsComplete(), "array of incomplete type not allowed");
            Assert.Error(!type.IsFunction(), "array of function not allowed");
            m_lastType.ArrayType = type;
            m_lastType = type;
            break;

          case Sort.Function:
            Assert.Error(!type.IsArray(), "function cannot return array");
            Assert.Error(!type.IsFunction(),"function cannot return function");
            m_lastType.ReturnType = type;
            m_lastType = type;
            break;
        }
      }
    }
  }
}