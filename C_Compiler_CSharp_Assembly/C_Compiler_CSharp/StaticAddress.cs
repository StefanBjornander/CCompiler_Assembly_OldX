namespace CCompiler {
  public class StaticAddress {
    private string m_name;
    private int m_offset;
  
    public StaticAddress(string name, int offset) {
      Assert.Error(offset >= 0);
      m_name = name;
      m_offset = offset;
    }
  
    public string Name {
      get {
        return m_name;
      }
    }
  
    public int Offset {
      get {
        return m_offset;
      }
    }
  
    public override string ToString() {
      return "(" + m_name + "," + m_offset + ")";
    }
  }
}