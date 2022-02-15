using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  public class ListMapX<KeyType,ValueType> : IDictionary<KeyType,ValueType> {
    private List<KeyValuePair<KeyType,ValueType>> m_list = new MyList<KeyValuePair<KeyType,ValueType>>();

    public int Count {
      get {
        return m_list.Count;
      }
    }

    public void Clear() {
      m_list.Clear();
    }

    public bool IsReadOnly {
      get {
        return true;
      }
    }

    public ValueType this[KeyType key] {
      get {
        ValueType value;

        if (TryGetValue(key, out value)) {
          return value;
        }
        else {
          throw (new InvalidOperationException());
        }
      }

      set {
        if (ContainsKey(key)) {
          Remove(key);
        }

        m_list.Add(new KeyValuePair<KeyType,ValueType>(key, value));
      }
    }

    public ICollection<KeyType> Keys {
      get {
        ICollection<KeyType> set = new MyList<KeyType>();

        foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
          set.Add(pair.Key);
        }

        return set;
      }
    }

    public ICollection<ValueType> Values {
      get {
        ICollection<ValueType> set = new MyList<ValueType>();

        foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
          set.Add(pair.Value);
        }

        return set;
      }
    }

    public void Add(KeyType key, ValueType value) {
      Add(new KeyValuePair<KeyType,ValueType>(key, value));
    }

    public void Add(KeyValuePair<KeyType,ValueType> Addpair) {
      for (int index = 0; index < m_list.Count; ++index) {
        KeyValuePair<KeyType,ValueType> pair = m_list[index];

        if (pair.Key.Equals(Addpair.Key)) {
          throw (new InvalidOperationException());
        }
      }

      m_list.Add(Addpair);
    }

    public bool ContainsKey(KeyType key) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        if (pair.Key.Equals(key)) {
          return true;
        }
      }

      return false;
    }

    public bool Contains(KeyValuePair<KeyType,ValueType> containspair) {
      for (int index = 0; index < m_list.Count; ++index) {
        KeyValuePair<KeyType,ValueType> pair = m_list[index];

        if (pair.Key.Equals(containspair)) {
          return true;
        }
      }

      return false;
    }

    public bool Remove(KeyType key) {
      for (int index = 0; index < m_list.Count; ++index) {
        KeyValuePair<KeyType,ValueType> pair = m_list[index];

        if (pair.Key.Equals(key)) {
          m_list.RemoveAt(index);
          return true;
        }
      }

      return false;
    }

    public bool Remove(KeyValuePair<KeyType,ValueType> Removepair) {
      for (int index = 0; index < m_list.Count; ++index) {
        KeyValuePair<KeyType,ValueType> pair = m_list[index];

        if (pair.Key.Equals(Removepair)) {
          m_list.RemoveAt(index);
          return true;
        }
      }

      return false;
    }

    public void CopyTo(KeyValuePair<KeyType,ValueType>[] array, int index) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        array[index++] = pair;
      }
    }

    public bool TryGetValue(KeyType key, out ValueType value) {
      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        if (pair.Key.Equals(key)) {
          value = pair.Value;
          return true;
        }
      }

      value = default(ValueType);
      return false;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (new ListMapEnumerator<KeyType,ValueType>(m_list));
    }

    IEnumerator<KeyValuePair<KeyType,ValueType>> IEnumerable<KeyValuePair<KeyType,ValueType>>.GetEnumerator() {
      return (new ListMapEnumerator<KeyType,ValueType>(m_list));
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is IEnumerable<KeyValuePair<KeyType,ValueType>>) {
        IEnumerable<KeyValuePair<KeyType,ValueType>> enumerable =
          (IEnumerable<KeyValuePair<KeyType,ValueType>>) obj;
        int count = 0;

        foreach (KeyValuePair<KeyType,ValueType> pair in enumerable) {
          if (!m_list.Contains(pair)) {
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

      foreach (KeyValuePair<KeyType,ValueType> pair in m_list) {
        buffer.Append((first ? "" : ",") + "(" + pair.Key.ToString() + "," + pair.Value.ToString() + ")");
        first = false;
      }

      return "{" + buffer.ToString() + "}";
    }
  }
}