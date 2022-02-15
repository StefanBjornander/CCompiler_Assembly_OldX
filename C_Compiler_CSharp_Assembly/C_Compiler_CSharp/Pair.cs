namespace CCompiler {
  public class Pair<FirstType,SecondType> {
    protected FirstType m_first;
    protected SecondType m_second;

    public Pair(FirstType first, SecondType second) {
      m_first = first;
      m_second = second;
    }

    public FirstType First {
      get {
        return m_first;
      }
    
      set {
        m_first = value;
      }
    }

    public SecondType Second {
      get {
        return m_second;
      }

      set {
        m_second = value;
      }
    }

    public override bool Equals(object obj) {
      if (obj is Pair<FirstType,SecondType>) {
        Pair<FirstType, SecondType> pair = (Pair<FirstType, SecondType>)obj;
        return ((((m_first == null) && (pair.m_first == null)) ||
                ((m_first != null) && (pair.m_first != null) &&
                 m_first.Equals(pair.m_first))) &&
                (((m_second == null) && (pair.m_second == null)) ||
                ((m_second != null) && (pair.m_second != null) &&
                 m_second.Equals(pair.m_second))));
      }
    
      return false;
    }
  
    public override int GetHashCode() {
      return ((m_first != null) ? m_first.GetHashCode() : 0) +
             ((m_second != null) ? m_second.GetHashCode() : 0);           
    }

    public override string ToString() {
      string firstText = (m_first != null) ? m_first.ToString() : "<null>",
             secondText = (m_second != null) ? m_second.ToString() : "<null>";
      return "(" + firstText + "," + secondText + ")";
    }
  }
}