namespace CCompiler {
  public class StaticValue {
    private string m_name;
    private int m_offset;
  
    public StaticValue(string name, int offset) {
      m_name = name;
      m_offset = offset;
    }
  
    public string Name {
      get {
        return m_name;
      }
    }
  
    public int Offset() {
      return m_offset;
    }
  
    public override string ToString() {
      return "("+ m_name + "," + m_offset + ")";
    }
  }
}