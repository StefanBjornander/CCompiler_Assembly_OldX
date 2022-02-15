using System;
using System.Collections;
using System.Collections.Generic;

namespace CCompiler {
  class ListSetEnumerator<SetType> : IEnumerator<SetType> {
    private int m_index = -1;
    private List<SetType> m_list;

    public ListSetEnumerator(List<SetType> list) {
      m_list = list;
    }

    public bool MoveNext() {
      ++m_index;
      return (m_index < m_list.Count);
    }

    public void Reset() {
      m_index = 0;
    }

    SetType IEnumerator<SetType>.Current {
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