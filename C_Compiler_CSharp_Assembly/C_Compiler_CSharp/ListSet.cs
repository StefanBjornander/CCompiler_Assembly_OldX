using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  public class ListSetX<SetType> : ISet<SetType> {
    private List<SetType> m_list;

    public ListSetX() {
      m_list = new MyList<SetType>();
    }

    public int Count {
      get {
        return m_list.Count;
      }
    }

    public bool IsReadOnly {
      get {
        return true;
      }
    }

    public ListSetX(SetType value) {
      m_list = new MyList<SetType>();
      m_list.Add(value);
    }
  
    public ListSetX(SetType[] array) {
      m_list = new MyList<SetType>();
      foreach (SetType value in array) {
        m_list.Add(value);
      }
    }

    public ListSetX(IEnumerable<SetType> enumerable) {
      m_list = new MyList<SetType>();
      foreach (SetType value in enumerable) {
        m_list.Add(value);
      }
    }

    public void CopyTo(SetType[] array, int index) {
      foreach (SetType value in m_list) {
        array[index++] = value;
      }
    }

    public void Clear() {
      m_list.Clear();
    }
        
    public bool Contains(SetType value) {
      return m_list.Contains(value);
    }

    void ICollection<SetType>.Add(SetType value) {
      if (!m_list.Contains(value)) {
        m_list.Add(value);
      }
    }

    bool ISet<SetType>.Add(SetType value) {
      if (!m_list.Contains(value)) {
        m_list.Add(value);
        return true;
      }

      return false;
    }

    public bool Remove(SetType value) {
      if (m_list.Contains(value)) {
        m_list.Remove(value);
        return true;
      }

      return false;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (new ListSetEnumerator<SetType>(m_list));
    }

    IEnumerator<SetType> IEnumerable<SetType>.GetEnumerator() {
      return (new ListSetEnumerator<SetType>(m_list));
    }

    public void IntersectWith(IEnumerable<SetType> enumerable) {
      List<SetType> result = new MyList<SetType>();

      foreach (SetType value in enumerable) {
        if (m_list.Contains(value)) {
          result.Add(value);
        }
      }

      m_list = result;
    }

    public void UnionWith(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          m_list.Add(value);
        }
      }
    }

    public void ExceptWith(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (m_list.Contains(value)) {
          m_list.Remove(value);
        }
      }
    }

    public void SymmetricExceptWith(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          m_list.Add(value);
        }
      }
    }

    public bool IsSubsetOf(IEnumerable<SetType> enumerable) {
      List<SetType> enumerableList = new MyList<SetType>(enumerable);

      foreach (SetType value in m_list) {
        if (!enumerableList.Contains(value)) {
          return false;
        }
      }

      return true;
    }

    public bool IsSupersetOf(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          return false;
        }
      }

      return true;
    }

    public bool IsProperSubsetOf(IEnumerable<SetType> enumerable) {
      return IsSubsetOf(enumerable) && !Equals(enumerable);
    }

    public bool IsProperSupersetOf(IEnumerable<SetType> enumerable) {
      return IsSupersetOf(enumerable) && !Equals(enumerable);
    }

    public bool Overlaps(IEnumerable<SetType> enumerable) {
      foreach (SetType value in enumerable) {
        if (!m_list.Contains(value)) {
          return true;
        }
      }

      return false;
    }

    public bool SetEquals(IEnumerable<SetType> enumerable) {
      return Equals(enumerable);
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is IEnumerable<SetType>) {
        IEnumerable<SetType> enumerable = (IEnumerable<SetType>) obj;
        int count = 0;

        foreach (SetType value in enumerable) {
          if (!m_list.Contains(value)) {
            return false;
          }

          ++count;
        }

        return (count == m_list.Count);
      }

      return false;
    }
    
    public override string ToString() {
      bool first = true;
      StringBuilder buffer = new StringBuilder();

      foreach (SetType value in m_list) {
        buffer.Append((first ? "" : ",") + value.ToString());
        first = false;
      }

      return "{" + buffer.ToString() + "}";
    }
  }
}