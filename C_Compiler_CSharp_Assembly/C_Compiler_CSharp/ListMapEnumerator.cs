using System;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  class ListMapEnumerator<KeyType,ValueType> : IEnumerator<KeyValuePair<KeyType,ValueType>> {
    private int m_index = -1;
    private List<KeyValuePair<KeyType, ValueType>> m_list;

    public ListMapEnumerator(List<KeyValuePair<KeyType,ValueType>> list) {
      m_list = list;
    }

    public bool MoveNext() {
      ++m_index;
      return (m_index < m_list.Count);
    }

    public void Reset() {
      m_index = 0;
    }

    KeyValuePair<KeyType,ValueType> IEnumerator<KeyValuePair<KeyType,ValueType>>.Current {
      get {
        if (m_index < m_list.Count) {
          return m_list[m_index];
        }
        else {
          throw (new InvalidOperationException());
        }
      }
    }

    object IEnumerator.Current {
      get {
        if (m_index < m_list.Count) {
          return m_list[m_index];
        }
        else {
          throw (new InvalidOperationException());
        }
      }
    }

    public void Dispose() {
      // Empty.
    }
  }
}