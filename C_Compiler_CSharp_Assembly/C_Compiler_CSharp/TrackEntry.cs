namespace CCompiler {
  public class TrackEntry {
    private int m_line, m_position, m_size;

    public TrackEntry(int position, int line, int size) {
      m_line = line;
      m_position = position;
      m_size = size;
    }

    public int Line() {
      return m_line;
    }
  
    public int GetPosition() {
      return m_position;
    }
  
    public int Size() {
      return m_size;
    }
  }
}