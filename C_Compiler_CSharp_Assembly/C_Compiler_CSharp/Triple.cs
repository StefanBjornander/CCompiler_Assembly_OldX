namespace CCompiler {
  public class Triple<FirstType,SecondType,ThirdType> :
               Pair<FirstType,SecondType> {
    protected ThirdType m_third;

    public Triple(FirstType first, SecondType second, ThirdType third) 
     :base(first, second) {
      m_third = third;
    }
  
    public ThirdType Third {
      get {
        return m_third;
      }

      set {
        m_third = value;
      }
    }
  
    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is Triple<FirstType,SecondType,ThirdType>) {
        Triple<FirstType, SecondType, ThirdType> triple = (Triple<FirstType, SecondType, ThirdType>) obj;
        return base.Equals((Pair<FirstType,SecondType>)triple) &&
               (((m_third == null) && (triple.m_third == null)) ||
                ((m_third != null) && (triple.m_third != null) &&
                 m_third.Equals(triple.m_third)));
      }
    
      return false;
    }
  }
}