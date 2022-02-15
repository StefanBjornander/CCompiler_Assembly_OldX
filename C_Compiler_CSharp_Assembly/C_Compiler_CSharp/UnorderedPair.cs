namespace CCompiler {
  public class UnorderedPair<FirstType,SecondType> :
               Pair<FirstType,SecondType>{
    public UnorderedPair(FirstType first, SecondType second)
     :base(first, second) {
      // Empty.
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is UnorderedPair<FirstType,SecondType>) {
        UnorderedPair<FirstType, SecondType> pair = (UnorderedPair<FirstType, SecondType>)obj;
        return base.Equals(pair) || (EqualsFirstSecond(pair) && EqualsSecondFirst(pair));
      }
    
      return false;
    }
  
    private bool EqualsFirstSecond(UnorderedPair<FirstType,SecondType> pair) {
      return (((m_first == null) && (pair.m_second == null)) ||
              ((m_first != null) && (pair.m_second != null) &&
              m_first.Equals(pair.m_second)));
    }
  
    private bool EqualsSecondFirst(UnorderedPair<FirstType,SecondType> pair) {
      return (((m_second == null) && (pair.m_first == null)) ||
              ((m_second != null) && (pair.m_first != null) &&
              m_second.Equals(pair.m_first)));
    }
  
    public override string ToString() {
      return "(" + m_first.ToString() + "," + m_second.ToString() + ")";
    }
  }
}