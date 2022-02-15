using System.Collections.Generic;

namespace CCompiler {
  class MyList<ListType> : List<ListType> {
    public MyList() {
      // Empty.
    }

    public MyList(IEnumerable<ListType> enumerable)
     :base(enumerable) {
        // Empty.
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj is List<ListType>) {
        List<ListType> list = (List<ListType>) obj;

        if (Count != list.Count) {
          return false;
        }

        for (int index = 0; index < Count; ++index) {
          if (!this[index].Equals(list[index])) {
            return false;
          }
        }
        
        return true;
      }
    
      return false;
    }
  }
}
