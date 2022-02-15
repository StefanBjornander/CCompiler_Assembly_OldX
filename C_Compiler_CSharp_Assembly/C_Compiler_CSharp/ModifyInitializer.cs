using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace CCompiler {
  class ModifyInitializer {
    private static string SimpleNameX(string name) {
      int index = name.LastIndexOf(Start.SeparatorId);
      return ((index != -1) ? name.Substring(index + 1) : name).Replace(Start.NumberId, "");
    }  

    public static void PrintList(TextWriter textWriter, object init) {
      if (init is Expression) {
        textWriter.Write(SimpleNameX((((Expression) init)).Symbol().Name));
      }
      else {
        textWriter.Write("[");

        List<object> list = (List<object>) init;
        bool first = true;

        foreach (object member in list) {
          textWriter.Write(first ? "" : ",");
          PrintList(textWriter, member);
          first = false;
        }

        textWriter.Write("]");
      }
    }

/*    private static void CheckTopDimension(object init, int topDimension) {
      if (topDimension > 0) {
        if (init is Expression) {
          Symbol symbol = ((Expression) init).Symbol();
          Assert.Error(symbol.Type.Dimension <= topDimension); // int a[] = b;
        }
        else {
          List<object> list = (List<object>) init;
          foreach (object member in list) {
            CheckTopDimension(member, topDimension - 1);
          }
        }
      }
    }*/

    /*private static void GenerateTypeToDimensionMap(Type type, Dictionary<Type,int> typeToDimensionMap, IDictionary<int,Type> dimensionToSizeMap) {
      int dimension = typeToDimensionMap[type];
      dimensionToSizeMap[dimension] = type;

      if (dimension > 0) {
        GenerateTypeToDimensionMap(type.ArrayType, typeToDimensionMap, dimensionToSizeMap);
      }
    }*/

    public static object DoInit(Type type, object init) {
      if (type.IsArray() && (init is List<object>)) {
        List<object> list = (List<object>) init;
        IDictionary<int,int> dimensionToSizeMap = new Dictionary<int,int>();
        GenerateDimensionToSizeMap(type, dimensionToSizeMap);
        IDictionary<object,int> initToDimensionMap = new Dictionary<object,int>();
        GenerateInitToDimensionlMap(list, initToDimensionMap);

        // int a[2][2][2] = {1,2,3,4,5,6,7,8};
        // {{1,2],{3,4},{5,6},{7,8}}
        // {{{1,2],{3,4}},{{5,6},{7,8}}}

        //List<object> list = (List<object>) init;
        for (int dimension = 1; dimension < type.Dimension; ++dimension) {
          List<object> totalList = new List<object>(), currentList = new List<object>();
          int arraySize = dimensionToSizeMap[dimension];
          Assert.Error(arraySize > 0);

          foreach (object member in list) {
            if (initToDimensionMap[member] < dimension) {
              currentList.Add(member);
            }
            else {
              if (currentList.Count > 0) {
                initToDimensionMap[currentList] = dimension;
                totalList.Add(currentList);
              }

              totalList.Add(member);
              currentList = new List<object>();
            }

            if (currentList.Count == arraySize) {
              initToDimensionMap[currentList] = dimension;
              totalList.Add(currentList);
              currentList = new List<object>();
            }
          }

          if (currentList.Count > 0) {
            initToDimensionMap[currentList] = dimension;
            totalList.Add(currentList);
          }

          list = totalList;
        }

        //PrintList(Console.Out, list);
        //Console.Out.WriteLine();
        return list;
      }

      return init;
    }

    private static void GenerateDimensionToSizeMap(Type type, IDictionary<int,int> dimensionToSizeMap) {
      if (type.IsArray()) {
        dimensionToSizeMap[type.Dimension] = type.ArraySize;
        GenerateDimensionToSizeMap(type.ArrayType, dimensionToSizeMap);
      }
    }

    private static int GenerateInitToDimensionlMap(object init, IDictionary<object,int> totalMap) {
      if (init is Expression) {
        Symbol symbol = ((Expression) init).Symbol();
        totalMap[init] = symbol.Type.Dimension;
        return symbol.Type.Dimension;
      }
      else {
        List<object> list = (List<object>) init;
        int maxDimension = 0;

        foreach (object member in list) {
          int dimension = GenerateInitToDimensionlMap(member, totalMap);
          maxDimension = Math.Max(maxDimension, dimension);
        }

        totalMap[list] = maxDimension + 1;
        return maxDimension + 1;
      }
    }
  }
}