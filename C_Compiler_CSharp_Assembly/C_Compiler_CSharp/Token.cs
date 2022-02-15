namespace CCompiler {
  public class Token {
    private CCompiler_Pre.Tokens m_id;
    private object m_value;
    private bool m_whitespace;
    private int m_newlineCount;
  
    public Token(CCompiler_Pre.Tokens id, object value)
    {
      m_id = id;
      m_value = value;
      m_newlineCount = Start.NewlineCount;
      m_whitespace = Start.Whitespace;
      Start.NewlineCount = 0;
      Start.Whitespace = false;
    }

    public Token(CCompiler_Pre.Tokens id, object value, int newlineCount) {
      m_id = id;
      m_value = value;
      m_newlineCount = newlineCount;
      m_whitespace = false;
    }

    public void AddNewlineCount(int newlineCount) {
      m_newlineCount += newlineCount;
    }  

    public object Clone() {
      Token token = new Token(m_id, m_value);
      token.m_newlineCount = 0;
      return token;
    }
  
    public CCompiler_Pre.Tokens GetId() {
      return m_id;
    }
  
    public object Value {
      get {
        return m_value;
      }

      set {
        m_value = value;
      }
    }
  
    public int GetNewlineCount() {
      return m_newlineCount;
    }
  
    public void ClearNewlineCount() {
      m_newlineCount = 0;
    }
  
    public bool HasWhitespace() {
      return m_whitespace;
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }
  
    public override bool Equals(object obj) {
      if (obj is Token) {
        Token token = (Token) obj;
        return (m_id == token.m_id) &&
                m_value.Equals(token.m_value);
      }
    
      return false;
    }
  
    public override string ToString() {
      switch (m_id) {
        case CCompiler_Pre.Tokens.MARK:
          return "<mark " + ((string) m_value) + ">";

        default:
          return m_value.ToString();
      }
    }
  }
}