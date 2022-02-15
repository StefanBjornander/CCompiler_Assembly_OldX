using System;
using System.Collections.Generic;

namespace CCompiler {
  public class SymbolTable {
    public const int ReturnAddressOffset = 0;
    public const int RegularFrameOffset = CCompiler.Type.PointerSize;
    public const int EllipseFrameOffset = 2 * CCompiler.Type.PointerSize;
    public const int FunctionRegularInitSize = 3 * CCompiler.Type.PointerSize;
    public static int TableCount = 0;
  
    private Scope m_scope;
    private int m_tableId;
    private SymbolTable m_parentTable;
    private int m_offset; // m_functionSize;
  
    private IDictionary<string,Symbol> m_entryMap =
      new Dictionary<string,Symbol>();
    private List<Symbol> m_entryList = new List<Symbol>();
    //private IDictionary<string,Symbol> m_entryMap = new ListMap<string,Symbol>(); // ListMapXXX
    private IDictionary<string,CCompiler.Type> m_tagMap = new Dictionary<string,CCompiler.Type>();
    public static ISet<Symbol> m_staticSet = new HashSet<Symbol>();

    public SymbolTable(SymbolTable parentTable, Scope scope) {
      m_tableId = TableCount++;
      m_parentTable = parentTable;
    
      switch (m_scope = scope) {
        case Scope.Global:
          m_staticSet.Clear();
          TableCount = 0;
          break;

        case Scope.Struct:
        case Scope.Union:
          m_offset = 0;
          break;

        case Scope.Function:
          m_offset = FunctionRegularInitSize;
          break;

        case Scope.Block:
          m_offset = m_parentTable.m_offset;
          break;
      }
    }

    // ------------------------------------------------------------------------
  
    public int TableId {
      get {
        return m_tableId;
      }
    }
  
    public Scope Scope {
      get {
        return m_scope;
      }
    }

    public SymbolTable ParentTable {
      get {
        return m_parentTable;
      }
    }

    public IDictionary<string,Symbol> EntryMap {
      get {
        return m_entryMap;
      }
    }

    public List<Symbol> EntryList {
      get {
        return m_entryList;
      }
    }

    public ISet<Symbol> StaticSet {
      get {
        return m_staticSet;
      }
    }
  
    public int CurrentOffset {
      get {
        return m_offset;
      }
    }
  
    // ------------------------------------------------------------------------
  
    public void AddSymbol(Symbol newSymbol) {
      string name = newSymbol.Name;

      if (name != null) {
/*        if (Start.Path.Contains("File.h") && name.Equals("WRITE")) {
          int i = 1;
        }*/

        Symbol oldSymbol;

        if (m_entryMap.TryGetValue(name, out oldSymbol) &&
            !name.EndsWith(Start.NumberId) &&
            !name.StartsWith(Start.TemporaryId)) {
          if (newSymbol.IsExtern()) {
            Assert.Error(oldSymbol.Type.Equals(newSymbol.Type),
                          name, "different types in redeclaration");
            return;
          }
          else if (oldSymbol.IsExtern()) {
            Assert.Error(oldSymbol.Type.Equals(newSymbol.Type),
                          name, "different types in redeclaration");
            m_entryMap.Remove(name);
          }
          else {
            Assert.Error(name, "already defined");
          }

          m_entryList.Remove(oldSymbol);
        }

        if (!name.EndsWith(Start.NumberId)) {
          m_entryMap[name] = newSymbol;
          m_entryList.Add(newSymbol);
        }
      }
    
      //if (!newSymbol.Type.IsFunction()) {
        if (newSymbol.IsAutoOrRegister()) {
          if (m_scope == Scope.Union) {
            newSymbol.Offset = 0;
          }
          else {
            newSymbol.Offset = m_offset;
            m_offset += newSymbol.Type.Size();
          }
        }

        if (newSymbol.IsStatic()) {
          m_staticSet.Add(newSymbol);
        }
      //}

      if ((newSymbol.Name != null) && newSymbol.Name.Equals("Array_#")) {
        //int i = 1;
      }
    }

    public Symbol LookupSymbol(string name) {
      Symbol symbol;

      if (m_entryMap.TryGetValue(name, out symbol)) {
        return symbol;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupSymbol(name);
      }

      return null;
    }

    public void AddTag(string name, CCompiler.Type type) {
      m_tagMap.Add(name, type);
    }

    public CCompiler.Type LookupTag(string name) {
      CCompiler.Type type;

      if (m_tagMap.TryGetValue(name, out type)) {
        return type;
      }
      else if (m_parentTable != null) {
        return m_parentTable.LookupTag(name);
      }

      return null;
    }
 
    public void RemoveTemporary(Symbol symbol) {
      if (symbol.IsTemporary() && m_entryMap.ContainsKey(symbol.Name)) {
        m_entryMap.Remove(symbol.UniqueName);
        Assert.Error((symbol.Offset + symbol.Type.Size()) == m_offset);
        m_offset -= symbol.Type.Size();
      }
    }
    
    public override string ToString() {
      return "size 0" + /* m_functionSize + */  ", (0," + CCompiler.Type.PointerSize +
             ",return address), (" + CCompiler.Type.PointerSize + "," + CCompiler.Type.PointerSize +
             ",stack pointer), (" + (2 * CCompiler.Type.PointerSize) + "," +
             CCompiler.Type.PointerSize + "," + (Start.CurrentFunction.Type.IsEllipse() ?
             "ellipse pointer)" : "not used)");
    }
  }
}