using System.Collections.Generic;

namespace CCompiler {
  public class RegisterAllocator {
    public RegisterAllocator(ISet<Track> trackSet, List<AssemblyCode> objectCodeList) {
      Graph<Track> totalTrackGraph = new Graph<Track>(trackSet);
      GenerateEdgeSet(totalTrackGraph);
      ISet<Graph<Track>> trackGraphSet = totalTrackGraph.Partitionate();

      foreach (Graph<Track> trackGraph in trackGraphSet) {
        Start.GraphCodeStream.WriteLine(trackGraph.ToString());
        CheckColorEdge(trackGraph);
        GenerateMatch(trackGraph, objectCodeList);
      }
    }
  
    private void GenerateEdgeSet(Graph<Track> trackGraph) {
      ISet<Track> trackSet = trackGraph.VertexSet;

      foreach (Track track1 in trackSet) {
        foreach (Track track2 in trackSet) {
          if (!track1.Equals(track2) && track1.DoesOverlap(track2)) {
            trackGraph.AddEdge(track1, track2);
          }
        }
      }
    }

    private void CheckColorEdge(Graph<Track> trackGraph) {
      foreach (UnorderedPair<Track,Track> pair in trackGraph.EdgeSet) {
        Track track1 = pair.First, track2 = pair.Second;
        Assert.Error(!AssemblyCode.RegisterOverlap(track1.Register, track2.Register),
                     "register overlap <" + track1 + "," + track1.Register +
                     "> <" + track2 + "," + track2.Register + ">");
      }
    }

    private void GenerateMatch(Graph<Track> trackGraph,
                               List<AssemblyCode> objectCodeList) {
      List<Track> trackList = new MyList<Track>(trackGraph.VertexSet);

      foreach (Pair<Track,Track> edge in trackGraph.EdgeSet) {
        Assert.Error(((edge.First.Register == null) &&
                      (edge.First.Register == null)) ||
                     (edge.First.Register !=
                      edge.Second.Register));
      }

      try {
        ListSearch(trackList, trackGraph);
        Assert.Error("Out of registers");
      }
      catch (FoundColorMatchException /*exception*/) {
        foreach (Track track in trackGraph.VertexSet) {
          Assert.Error(track.Register != null);
        }

        foreach (Pair<Track,Track> edge in trackGraph.EdgeSet) {
          Assert.Error(edge.First.Register != edge.Second.Register);
        }
      }
    
      foreach (Track track in trackGraph.VertexSet) {
        track.Generate(objectCodeList);
      }
    }
    
    private void ListSearch(List<Track> trackList, Graph<Track> trackGraph) {
      if (trackList.Count == 0) {
        throw (new FoundColorMatchException());
      }
      else {
        Track track = trackList[0];
        trackList.RemoveAt(0);

        if (track.Register != null) {
          ListSearch(trackList, trackGraph);
        }
        else {
          ISet<Register> possibleSet = GetPossibleSet(track, track.GetNotCL);
          ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);

          foreach (Register possibleRegister in possibleSet) {
            bool match = true;
          
            if (possibleRegister == track.getForbidRegister()) {
              match = false;
            }
            else {
              foreach (Track neighbourTrack in neighbourSet) {
                Register? neighbourRegister = neighbourTrack.Register;

                if (AssemblyCode.RegisterOverlap(possibleRegister, neighbourRegister)) {
                  match = false;
                  break;
                }
              }
  
              foreach (Track twinTrack in track.TwinTrackSet) {
                ISet<Track> twinNeighbourSet = trackGraph.GetNeighbourSet(twinTrack);                

                foreach (Track neighbourTrack in twinNeighbourSet) {
                  Register? neighbourRegister = neighbourTrack.Register;

                  if (AssemblyCode.RegisterOverlap(possibleRegister, neighbourRegister)) {
                    match = false;
                    break;
                  }
                }
              }
            }

            if (match) {
              foreach (Track twinTrack in track.TwinTrackSet) {
                Assert.Error((twinTrack.Register == null) || twinTrack.Register.Equals(possibleRegister));
                //twinTrack.Register = possibleRegister;
              }

              track.Register = possibleRegister;
              ListSearch(trackList, trackGraph);
              track.Register = null;
              /*foreach (Track twinTrack in track.TwinTrackSet) {
                twinTrack.Register = null;
              }*/
            }
          }    
        }
        /*else if (track.TwinTrack != null) {
          ISet<Register> possibleSet = GetPossibleSet(track, track.GetNotCL);
          ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);
          ISet<Track> twinNeighbourSet = trackGraph.GetNeighbourSet(track.TwinTrack);

          foreach (Register possibleRegister in possibleSet) {
            bool match = true;
          
            if (possibleRegister == track.getForbidRegister()) {
              match = false;
            }
            else {
              foreach (Track neighbourTrack in neighbourSet) {
                Register? neighbourRegister = neighbourTrack.Register;

                if (ObjectCode.RegisterOverlap(possibleRegister, neighbourRegister)) {
                  match = false;
                  break;
                }
              }

              foreach (Track neighbourTrack in twinNeighbourSet) {
                Register? neighbourRegister = neighbourTrack.Register;

                if (ObjectCode.RegisterOverlap(possibleRegister, neighbourRegister)) {
                  match = false;
                  break;
                }
              }
            }

            if (match) {
              track.Register = possibleRegister;
              track.TwinTrack.Register = possibleRegister;
              ListSearch(trackList, trackGraph);
            }
          }
        
          track.Register = null;
        }
        else {
          ISet<Register> possibleSet = GetPossibleSet(track, track.GetNotCL);
          ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);

          foreach (Register possibleRegister in possibleSet) {
            bool match = true;
          
            if (possibleRegister == track.getForbidRegister()) {
              match = false;
            }
            else {
              foreach (Track neighbourTrack in neighbourSet) {
                Register? neighbourRegister = neighbourTrack.Register;

                if (ObjectCode.RegisterOverlap(possibleRegister, neighbourRegister)) {
                  match = false;
                  break;
                }
              }
            }

            if (match) {
              track.Register = possibleRegister;
              ListSearch(trackList, trackGraph);
            }
          }
        
          track.Register = null;
        }*/
      
        trackList.Insert(0, track);
      }
    }

    public static ISet<Register> m_pointerRegisterSet = new HashSet<Register>(), // ListSetXXX
                                 m_shortRegisterSet = new HashSet<Register>(), // ListSetXXX
                                 m_normalRegisterSet = new HashSet<Register>(); // ListSetXXX

    static RegisterAllocator() {
      m_pointerRegisterSet.Add(Register.si);
      m_pointerRegisterSet.Add(Register.di);
      m_pointerRegisterSet.Add(Register.bx);

      m_shortRegisterSet.Add(Register.al);
      m_shortRegisterSet.Add(Register.ah);
      m_shortRegisterSet.Add(Register.bl);
      m_shortRegisterSet.Add(Register.bh);
      m_shortRegisterSet.Add(Register.cl);
      m_shortRegisterSet.Add(Register.ch);
      m_shortRegisterSet.Add(Register.dl);
      m_shortRegisterSet.Add(Register.dh);

      m_normalRegisterSet.Add(Register.ax);
      m_normalRegisterSet.Add(Register.bx);
      m_normalRegisterSet.Add(Register.cx);
      m_normalRegisterSet.Add(Register.dx);
    }

    private static ISet<Register> GetPossibleSet(Track track, bool notDL) {
      ISet<Register> possibleSet = new HashSet<Register>(); // ListSetXXX
    
      if (track.Pointer) {
        possibleSet.UnionWith(m_pointerRegisterSet);
      
        if (Start.CurrentFunction.Type.IsEllipse()) {
          possibleSet.Remove(AssemblyCode.EllipseRegister);
        }
      }
      else if (track.GetMaxSize() == CCompiler.Type.ShortSize) {
        possibleSet.UnionWith(m_shortRegisterSet);
      
        if (notDL) {
          possibleSet.Remove(Register.dl);
        }
      }
      else {
        possibleSet.UnionWith(m_normalRegisterSet);

        if (notDL) {
          possibleSet.Remove(Register.dx);
        }
      }

      return possibleSet;
    }
  }
}