using System;
using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Track {
    private Register? m_register;
    private Symbol m_symbol;
    public List<TrackEntry> m_entryList = new MyList<TrackEntry>();
    private bool m_pointer, m_notCL;
    private int m_currSize, m_maxSize;
    private ISet<Track> m_twinTrackSet = new HashSet<Track>();

    private static int m_count = 0;
    private string m_name;

    public const int Left = 0;
    public const int Middle = 1;
    public const int Right = 2;
    public const int Clear = 3;

    Register? m_forbiddenRegister = null;

    public string Name {
      get {
        return m_name;
      }
    }
  
    private int m_firstLine = -1;

    public int FirstLine {
      get {
        return m_firstLine;
      }

      set {
        m_firstLine = value;
      }
    }
    
    private Track m_previousTrack = null;

    public Track PreviousTrack {
      get {
        return m_previousTrack;
      }

      set {
        m_previousTrack = value;
      }
    }

    public Register? getForbidRegister() {
      return m_forbiddenRegister;
    }
  
    public override string ToString() {
      StringBuilder buffer = new StringBuilder(m_name);

      if (m_name != null) {
        buffer.Append(" " + m_name);
      }

      if (m_symbol!= null) {
        buffer.Append(" " + m_symbol.Name);
      }

      if (m_register != null) {
        buffer.Append(" " + Enum.GetName(typeof(Register), m_register));
      }

      foreach (TrackEntry entry in m_entryList) {
        buffer.Append(" " + entry.Line());
      }

      return buffer.ToString();
    }
  
    public Track(Symbol symbol)
     :this(symbol, null) {
        // Empty.
    }

    public Track(Type type) {
      m_name = "type" + (m_count++);
      Assert.Error(type != null);
      //Assert.ErrorXXX(!type.IsStructOrUnion());
      Assert.Error(!type.IsArrayFunctionOrString());
      m_currSize = m_maxSize = type.Size();
    }

    public Track(Symbol symbol, Register? register) {
      m_symbol = symbol;

      if ((symbol != null) && (symbol.IsTemporary())) {
        m_name = "t" + (m_count++);
      }
      else {
        m_name = "a" + (m_count++);
      }

      // t1613 2627 154
      // t1616 2628 149 151 155
    
      Assert.Error((symbol != null) || (register != null));

      if (register != null) {
        Assert.Error(m_register == null);
        m_register = register;
      }
    
      if (symbol != null) {
        CCompiler.Type type = symbol.Type;
    
        if (type.IsFunctionArrayStringStructOrUnion()) {
          m_maxSize = m_currSize = CCompiler.Type.PointerSize;
        }
        else {
          m_maxSize = m_currSize = type.Size();
        }
      }
      else {
        m_maxSize = AssemblyCode.RegisterSize(m_register.Value);
      }

      Assert.Error((m_maxSize == 1) || (m_maxSize == 2) || (m_maxSize == 4));
    }

    public Track(CCompiler.Type type, Register? register) {
      m_name = "t" + (m_count++);
      Assert.Error((type != null) || (register != null));      
      m_register = register;

      if (type != null) {
        if (type.IsFunctionArrayStringStructOrUnion()) {
          m_maxSize = m_currSize = CCompiler.Type.PointerSize;
        }
        else {
          m_maxSize = m_currSize = type.Size();
        }
      }
      else {
        m_maxSize = AssemblyCode.RegisterSize(m_register.Value);
      }
    
      Assert.Error((m_maxSize == 1) || (m_maxSize == 2) || (m_maxSize == 4));
    }

    public int Size {
      get {
        return m_currSize;
      }

      set {
        m_currSize = value;
        m_maxSize = Math.Max(m_maxSize, m_currSize);
        Assert.Error((m_maxSize == 1) || (m_maxSize == 2) || (m_maxSize == 4));
      }
    }
  
    public ISet<Track> TwinTrackSet {
      get {
        return m_twinTrackSet;
      }
    }

    /*public Track TwinTrack {
      get {
        return m_twinTrack;
      }

      set {
        m_twinTrack = value;
      }
    }*/

    public void AddCode(int position, int line) {
      m_entryList.Add(new TrackEntry(position, line, m_currSize));
    }

    public void AddCode(int position, int line, CCompiler.Type type) {
      int size = type.IsArithmeticPointerStructOrUnion() ? CCompiler.Type.PointerSize : type.Size();
      m_entryList.Add(new TrackEntry(position, line, size));
      m_maxSize = Math.Max(m_maxSize, m_currSize = size);
    }

    public void AddCode(int position, int line, Register? register) {
      Assert.Error((m_register == null) || m_register.Equals(register), "entry register");
      
      if (register != null) {
        m_register = register.Value;
        m_currSize = AssemblyCode.RegisterSize(m_register.Value);
        m_maxSize = Math.Max(m_maxSize, m_currSize);
      }

      Assert.Error((position == Left) || (position == Middle) || 
                   (position == Right) || (position == Clear), "entry position");
      m_entryList.Add(new TrackEntry(position, line, m_currSize));
    }

    public bool IsEmpty() {
      return (m_entryList.Count == 0);
    }
  
    public int GetLastLine() {
      Assert.Error(m_entryList.Count > 0);
      return m_entryList[m_entryList.Count - 1].Line();
    }
  
    public int GetMaxSize() {
      return m_maxSize;
    }

    public bool GetNotCL {
      get {
        return m_notCL;
      }

      set {
        m_notCL = value;
      }
    }

    public Register? Register {
      get {
        return m_register;
      }

      set {
        Assert.Error((value == null) || (m_register == null) ||
                      AssemblyCode.RegisterOverlap(value, m_register));// && (value != m_forbiddenRegister)));
        m_register = value;

        foreach (Track twinTrack in m_twinTrackSet) {
          Assert.Error((value == null) || (twinTrack.m_register == null) ||
                       AssemblyCode.RegisterOverlap(value, twinTrack.m_register));
          twinTrack.m_register = value;
        }

        /*if (m_twinTrack != null) {
          Assert.Error((value == null) || (m_twinTrack.m_register == null) ||
                       ObjectCode.RegisterOverlap(value, m_twinTrack.m_register));
          m_twinTrack.m_register = value;
        }*/
      }
    }

    /*public Register? Register {
      get {
        return m_register;
      }

      set {
        if (value != null) {
          Assert.Error((m_register == null) && (value != m_forbiddenRegister));
          m_register = value;
        }
      }
    }*/

    public bool Pointer {
      get {
        return m_pointer;
      }

      set {
        m_pointer = value;
      }
    }
  
    public bool SetPointerx() {
      if (m_pointer) {
        return true;
      }
    
      Register? register = Register;
    
      if ((register != null) &&
          !RegisterAllocator.m_pointerRegisterSet.Contains(register.Value)) {
        return false;
      }

      m_pointer = true;
      return true;
    }
  
    // ------------------------------------------------------------------------
  
    public bool DoesOverlap(Track track) {
      if ((m_entryList.Count == 0) || (track.m_entryList.Count == 0)) {
        return false;
      }
    
      TrackEntry minEntry1 = m_entryList[0],
                 minEntry2 = track.m_entryList[0],
                 maxEntry1 = m_entryList[m_entryList.Count - 1],
                 maxEntry2 = track.m_entryList[track.m_entryList.Count - 1];

      if (maxEntry1.Line() == minEntry2.Line()) {
        return maxEntry1.GetPosition() > minEntry2.GetPosition();
      }    
      else if (maxEntry2.Line() == minEntry1.Line()) {
        return maxEntry2.GetPosition() > minEntry1.GetPosition();
      }
    
      // mov bx, ax
      // mov ax, 123
      // mul bx

      else {    
        return ((minEntry2.Line() <= minEntry1.Line()) && (minEntry1.Line() <= maxEntry2.Line())) ||
               ((minEntry2.Line() <= maxEntry1.Line()) && (maxEntry1.Line() <= maxEntry2.Line())) ||
               ((minEntry1.Line() <= minEntry2.Line()) && (minEntry2.Line() <= maxEntry1.Line())) ||
               ((minEntry1.Line() <= maxEntry2.Line()) && (maxEntry2.Line() <= maxEntry1.Line()));
      }
    }

    public void Generate(List<AssemblyCode> objectCodeList) {
      foreach (TrackEntry entry in m_entryList) {
        Register sizeRegister = AssemblyCode.RegisterToSize(m_register, entry.Size());
        AssemblyCode objectCode = objectCodeList[entry.Line()];
        int position = entry.GetPosition();

        object o = objectCode.GetOperand(1);

        if ((position == Track.Left) || (position == Track.Middle) || 
            (position == Track.Right)) {
          objectCode.SetOperand(position, sizeRegister);
        }
      }
    }
  }
}