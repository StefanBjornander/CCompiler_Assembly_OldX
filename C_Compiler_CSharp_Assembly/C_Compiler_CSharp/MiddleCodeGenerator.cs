using System;
using System.Numerics;
using System.Collections.Generic;

namespace CCompiler {
  public class MiddleCodeGenerator {
    private static Symbol ToLogical(List<MiddleCode> codeList, Expression expr, bool Sets) {
      if (Start.TemporaryCount == 1771) {
        //int i = 1;
      }
      
      /*Console.WriteLine(Start.TemporaryCount);

      if (expr.logicalList() != null) {
        codeList.AddRange(expr.logicalList());
        return expr.logicalSymbol();
      }
      else*/ {
        codeList.AddRange(expr.LongList());
        Symbol symbol = TypeCast.implicitCast(codeList, expr.Symbol(), CCompiler.Type.LogicalType);
      
        if (symbol.IsValue() && Sets) {
          MiddleCode gotoCode = Start.AddMiddleCode(codeList, MiddleOperator.Goto);
          ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();

          if (symbol.Value.Equals(true)) {
            trueSet.Add(gotoCode);
          }                        
          else {
            falseSet.Add(gotoCode);
          }
        
          return (new Symbol(trueSet, falseSet));
        }
        else {
          return symbol;
        }
      }
    }
  
    public static Statement GenerateIfStatement(Expression expr, Statement statement) {
      List<MiddleCode> resultList = new MyList<MiddleCode>();
      Symbol symbol = ToLogical(resultList, expr, true);
      resultList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));

      Start.Backpatch(symbol.trueSet(), statement.List());    
      resultList.AddRange(statement.List());
      MiddleCode nextCode = new MiddleCode(MiddleOperator.Goto);
      resultList.Add(nextCode);
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(statement.NextSet());
      nextSet.UnionWith(symbol.falseSet());
      nextSet.Add(nextCode);
    
      return (new Statement(resultList, nextSet));
    }
  
    public static Statement GenerateIfElseStatement(Expression expr, Statement trueStatement, Statement falseStatement) {
      List<MiddleCode> resultList = new MyList<MiddleCode>();
      Symbol symbol = ToLogical(resultList, expr, true);
      resultList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));

      Start.Backpatch(symbol.trueSet(), trueStatement.List());
      Start.Backpatch(symbol.falseSet(), falseStatement.List());

      resultList.AddRange(trueStatement.List());
      MiddleCode trueNextCode = new MiddleCode(MiddleOperator.Goto);
      resultList.Add(trueNextCode);
    
      resultList.AddRange(falseStatement.List());
      MiddleCode falseNextCode = new MiddleCode(MiddleOperator.Goto);
      resultList.Add(falseNextCode);

      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(trueStatement.NextSet());
      nextSet.UnionWith(falseStatement.NextSet());
      nextSet.Add(trueNextCode);
      nextSet.Add(falseNextCode);
    
      return (new Statement(resultList, nextSet));
    }
  
    public static void GenerateSwitchHeader() {
      Start.CaseMapStack.Push(new Dictionary<BigInteger,MiddleCode>()); // ListMapXXX
      Start.DefaultStack.Push(null);
      Start.BreakSetStack.Push(new HashSet<MiddleCode>());
    }

    public static Statement GenerateSwitchStatement(Expression expr,
                                                    Statement statement) {
      List<MiddleCode> codeList = new MyList<MiddleCode>();
      codeList.AddRange(expr.LongList());
      Symbol symbol = logicalToInteger(codeList, expr.Symbol());

      if (symbol.IsTemporary()) {
        //symbol.Switch = true;
        Start.CurrentTable.AddSymbol(symbol);
      }

      //codeList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));
    
      foreach (KeyValuePair<BigInteger,MiddleCode> entry in Start.CaseMapStack.Pop()) {
        BigInteger caseValue = entry.Key;
        MiddleCode caseTarget = entry.Value;
        Symbol caseSymbol = new Symbol(symbol.Type, caseValue);
        Start.AddMiddleCode(codeList, MiddleOperator.Case, caseTarget, symbol, caseSymbol);
      }
      Start.AddMiddleCode(codeList, MiddleOperator.CaseEnd, symbol);

/*      foreach (KeyValuePair<BigInteger,MiddleCode> entry in Start.CaseMapStack.Pop()) {
        BigInteger caseValue = entry.Key;
        MiddleCode caseCode = entry.Value;      
        Symbol caseSymbol = new Symbol(symbol.Type, caseValue);
        Start.AddMiddleCode(codeList, MiddleOperator.Equal, caseCode, symbol, caseSymbol);
      }*/
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      MiddleCode defaultCode = Start.DefaultStack.Pop();

      if (defaultCode != null) {
        Start.AddMiddleCode(codeList, MiddleOperator.Goto, defaultCode);
      }
      else {
        MiddleCode gotoCode = Start.AddMiddleCode(codeList, MiddleOperator.Goto);
        nextSet.Add(gotoCode);      
      }

      codeList.AddRange(statement.List());
      nextSet.UnionWith(statement.NextSet());
      nextSet.UnionWith(Start.BreakSetStack.Pop());
      return (new Statement(codeList, nextSet));
    }

    public static Statement GenerateCaseStatement(Expression expr, Statement statement) {
      Symbol symbol = logicalToInteger(expr.LongList(), expr.Symbol());
      //statement.List().Insert(0, new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));
      Assert.Error(Start.CaseMapStack.Count > 0, "case without switch");
    
      BigInteger caseValue = (BigInteger) symbol.Value;
      Assert.Error(caseValue != null, caseValue, "non-constant case value");
      IDictionary<BigInteger,MiddleCode> caseMap = Start.CaseMapStack.Peek();

      Assert.Error(!caseMap.ContainsKey(caseValue), caseValue, "repeated case");
      caseMap.Add(caseValue, GetFirst(statement));
      return statement;
    }
  
    private static MiddleCode GetFirst(Statement statement) {
      if (statement.List().Count == 0) {
        statement.List().Add(new MiddleCode(MiddleOperator.Empty));
      }
    
      return statement.List()[0];
    }
  
    public static Statement GenerateDefaultStatement(Statement statement) {
      Assert.Error(Start.DefaultStack.Count > 0, "default without switch");
      Assert.Error(Start.DefaultStack.Pop() == null, "repeted default");    
      Start.DefaultStack.Push(GetFirst(statement));
      return statement;
    }

    public static Statement GenerateBreakStatement() {
      Assert.Error(Start.BreakSetStack.Count > 0,
                   "break without switch, while, do, or for");
      List<MiddleCode> breakList = new MyList<MiddleCode>();
      MiddleCode breakCode = new MiddleCode(MiddleOperator.Goto);
      breakList.Add(breakCode);
      Start.BreakSetStack.Peek().Add(breakCode);
      return (new Statement(breakList, new HashSet<MiddleCode>()));
    }
  
    public static Statement GenerateContinueStatement() {
      Assert.Error(Start.ContinueSetStack.Count > 0,
                   "continue without while, do, or for");
      List<MiddleCode> continueList = new MyList<MiddleCode>();
      MiddleCode continueCode = new MiddleCode(MiddleOperator.Goto);
      continueList.Add(continueCode);
      Start.ContinueSetStack.Peek().Add(continueCode);
      return (new Statement(continueList, new HashSet<MiddleCode>()));
    }
  
    public static void GenerateLoopHeader() {
      Start.BreakSetStack.Push(new HashSet<MiddleCode>());
      Start.ContinueSetStack.Push(new HashSet<MiddleCode>());    
    }
  
    public static Statement GenerateWhileStatement(Expression expr, Statement statement) {
      List<MiddleCode> resultList = new MyList<MiddleCode>();
      Symbol symbol = ToLogical(resultList, expr, true);
      resultList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));
    
      Start.Backpatch(symbol.trueSet(), statement.List());
      resultList.AddRange(statement.List());

      MiddleCode nextCode = new MiddleCode(MiddleOperator.Goto, resultList[0]);
      resultList.Add(nextCode);
    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(symbol.falseSet());
      nextSet.UnionWith(Start.BreakSetStack.Pop());
      Start.Backpatch(statement.NextSet(), resultList);
      Start.Backpatch(Start.ContinueSetStack.Pop(), resultList);

      return (new Statement(resultList, nextSet));
    }
  
    public static Statement GenerateDoStatement(Statement statement, Expression expr) {
      List<MiddleCode> resultList = new MyList<MiddleCode>();
      resultList.AddRange(statement.List());
    
      List<MiddleCode> exprList = new MyList<MiddleCode>();
      Symbol symbol = ToLogical(exprList, expr, true);
      exprList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));
      Start.Backpatch(statement.NextSet(), exprList);    
      resultList.AddRange(exprList);
    
      Start.Backpatch(symbol.trueSet(), resultList);
      Start.Backpatch(Start.ContinueSetStack.Pop(), resultList);    
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
      nextSet.UnionWith(symbol.falseSet());
      nextSet.UnionWith(Start.BreakSetStack.Pop());

      Start.AddMiddleCode(resultList, MiddleOperator.Goto, resultList[0]);
      return (new Statement(resultList, nextSet));
    }
  
    public static Statement GenerateForStatement(Expression initExpr, Expression testExpr,
                                                 Expression nextExpr, Statement statement) {
      List<MiddleCode> codeList = new MyList<MiddleCode>();
      ISet<MiddleCode> nextSet = new HashSet<MiddleCode>();
    
      if (initExpr != null) {
        codeList.AddRange(initExpr.ShortList());
      }

      MiddleCode testTarget = Start.AddMiddleCode(codeList, MiddleOperator.Empty);
    
      if (testExpr != null) {
        Symbol testSymbol = ToLogical(codeList, testExpr, true);
        codeList.Add(new MiddleCode(MiddleOperator.CheckTrackMapFloatStack));
        Start.Backpatch(testSymbol.trueSet(), Start.AddMiddleCode(codeList, MiddleOperator.Empty));
        nextSet.UnionWith(testSymbol.falseSet());
      }

      codeList.AddRange(statement.List());
      MiddleCode nextTarget = Start.AddMiddleCode(codeList, MiddleOperator.Empty);
      Start.Backpatch(statement.NextSet(), nextTarget);
    
      if (nextExpr != null) {
        codeList.AddRange(nextExpr.ShortList());
      }

      Start.AddMiddleCode(codeList, MiddleOperator.Goto, testTarget);
      Start.Backpatch(Start.ContinueSetStack.Pop(), nextTarget);
      nextSet.UnionWith(Start.BreakSetStack.Pop());
    
      return (new Statement(codeList, nextSet));
    }
  
    public static Statement GenerateLabelStatement(string labelName,
                                                   Statement statement) {
      Assert.Error(!Start.LabelMap.ContainsKey(labelName), labelName, "defined twice");
      Start.LabelMap.Add(labelName, GetFirst(statement));
      return statement;
    }

    public static Statement GenerateGotoStatement(string labelName) {
      if (!Start.GotoSetMap.ContainsKey(labelName)) {
        Start.GotoSetMap.Add(labelName, new HashSet<MiddleCode>());
      }
    
      List<MiddleCode> gotoList = new MyList<MiddleCode>();
      MiddleCode gotoCode = new MiddleCode(MiddleOperator.Goto);
      gotoList.Add(gotoCode);
      Start.GotoSetMap[labelName].Add(gotoCode);
      return (new Statement(gotoList, new HashSet<MiddleCode>()));
    }

    public static Statement GenerateReturnStatement(Expression expr) {
      List<MiddleCode> exprList = new MyList<MiddleCode>();
    
      if (expr != null) {
        Assert.Error(!Start.CurrentFunction.Type.ReturnType.IsVoid(),
                     "non-void return from void function");

        exprList.AddRange(expr.LongList());
        Symbol symbol = TypeCast.implicitCast(exprList, expr.Symbol(), Start.CurrentFunction.Type.ReturnType);

        /*if (Start.CurrentFunction.UniqueName.Equals("main")) {
          Start.AddMiddleCode(exprList, MiddleOperator.Exit, null, symbol);
        }
        else*/ {
          Start.AddMiddleCode(exprList, MiddleOperator.SetReturnValue, null, symbol);
          Start.AddMiddleCode(exprList, MiddleOperator.Return, null, symbol);
        }
      }
      else {
        Assert.Error(Start.CurrentFunction.Type.ReturnType.IsVoid(),
                     "void return from non-void function");

        /*if (Start.CurrentFunction.UniqueName.Equals("main")) {
          Start.AddMiddleCode(exprList, MiddleOperator.Exit);
        }
        else*/ {
          Start.AddMiddleCode(exprList, MiddleOperator.Return);
        }
      }
    
      return (new Statement(exprList, new HashSet<MiddleCode>()));
    }

    public static Statement GenerateExpressionStatement(Expression expr) {
      List<MiddleCode> exprList = new MyList<MiddleCode>();

      if (expr != null) {
        exprList.AddRange(expr.ShortList());
      }
    
      return (new Statement(exprList, new HashSet<MiddleCode>()));
    }

    /*public static Statement GenerateLoadToRegisterStatement(Register register, Expression expr) {
      List<MiddleCode> loadList = new MyList<MiddleCode>();
      loadList.AddRange(expr.LongList());
      //Register register = (Register) Enum.Parse(typeof(Register), name);
      Start.AddMiddleCode(loadList, MiddleOperator.AssignRegister, register, expr.Symbol());
      return (new Statement(loadList, new HashSet<MiddleCode>()));
    }

    public static Statement GenerateSaveFromRegisterStatement(string name,
                                                              Expression expr) {
      List<MiddleCode> saveList = new MyList<MiddleCode>();
      Register register = (Register)Enum.Parse(typeof(Register), name);
      Start.AddMiddleCode(saveList, MiddleOperator.SaveFromRegister, expr.Symbol(), register);
      return (new Statement(saveList, new HashSet<MiddleCode>()));
    }
  
    public static Statement GenerateClearRegistersStatement() {
      List<MiddleCode> codeList = new MyList<MiddleCode>();
      Start.AddMiddleCode(codeList, MiddleOperator.ClearRegisters);
      return (new Statement(codeList, new HashSet<MiddleCode>()));
    }
  
    public static Statement GenerateLoadFlagByteStatement(Expression expr) {
      List<MiddleCode> flagList = new MyList<MiddleCode>();
      Start.AddMiddleCode(flagList, MiddleOperator.LoadFromFlagByte, expr.Symbol());
      return (new Statement(flagList, new HashSet<MiddleCode>()));
    }*/
  
    public static Statement GenerateJumpRegisterStatement(Register register) {
      List<MiddleCode> jumpList = new MyList<MiddleCode>();
      //Register register = (Register) Enum.Parse(typeof(Register), registerName);
      Start.AddMiddleCode(jumpList, MiddleOperator.JumpRegister, register);
      return (new Statement(jumpList, new HashSet<MiddleCode>()));
    }

    public static Statement GenerateInterruptStatement(Expression expr) {
      List<MiddleCode> jumpList = new MyList<MiddleCode>();
      Start.AddMiddleCode(jumpList, MiddleOperator.Interrupt, expr.Symbol().Value);
      return (new Statement(jumpList, new HashSet<MiddleCode>()));
    }

    public static Expression GenerateConstantIntegralExpression(Expression expr) {
      Symbol symbol = ConstantExpression.cast(expr.LongList(), expr.Symbol(), CCompiler.Type.SignedLongIntegerType);
      Assert.Error((symbol != null) && symbol.HasValue(), symbol, "non-constant expression");
      Assert.Error(symbol.Type.IsIntegralOrPointer(),
                   symbol, "non-integral expression");
      return expr;
    }

    public static Expression GenerateCommaExpression(Expression leftExpr,
                                                     Expression rightExpr) {
      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      longList.AddRange(leftExpr.ShortList());
      longList.AddRange(rightExpr.LongList());
        
      return (new Expression(rightExpr.Symbol(), shortList, longList));
    }
  
    public static Expression GenerateSimpleAssignmentExpression(Expression leftExpr,
                                                                Expression rightExpr) {
      Register? register = leftExpr.Register();

      if (register != null) {
        Symbol rightSymbol = rightExpr.Symbol();
        Assert.Error(AssemblyCode.RegisterSize(register.Value) == rightSymbol.Type.Size());
        List<MiddleCode> codeList = new List<MiddleCode>();
        codeList.AddRange(rightExpr.LongList());
        Start.AddMiddleCode(codeList, MiddleOperator.AssignRegister, register, rightSymbol);
        return (new Expression(rightSymbol, codeList, codeList));
      }
      else {
        Assert.Error(leftExpr.Symbol().IsAssignable(), leftExpr, "not assignable");
        Assert.Error(!leftExpr.Symbol().Type.IsArrayOrFunction(), leftExpr,
                     "invalid type in assignment expression");

        List<MiddleCode> longList = new MyList<MiddleCode>();
        longList.AddRange(leftExpr.LongList());    
        Symbol leftSymbol = leftExpr.Symbol();
    
        if (leftSymbol.Type.IsFloating()) {
          Start.AddMiddleCode(longList, MiddleOperator.PopFloat);
          longList.AddRange(rightExpr.LongList());
          TypeCast.implicitCast(longList, rightExpr.Symbol(), leftExpr.Symbol().Type);
          Start.AddMiddleCode(longList, MiddleOperator.TopFloat, leftSymbol);
      
          List<MiddleCode> shortList = new MyList<MiddleCode>();
          shortList.AddRange(longList);
          Start.AddMiddleCode(shortList, MiddleOperator.PopFloat);
          return (new Expression(leftExpr.Symbol(), shortList, longList));
        }
        else {
          longList.AddRange(rightExpr.LongList());
          Symbol rightSymbol = TypeCast.implicitCast(longList, rightExpr.Symbol(), leftExpr.Symbol().Type);

          if (rightSymbol.Type.IsStructOrUnion()) {
            Start.AddMiddleCode(longList, MiddleOperator.AssignInitSize);
          }

          Start.AddMiddleCode(longList, MiddleOperator.Assign, leftSymbol, rightSymbol);

          if (leftSymbol.Type.IsIntegralOrPointer()) {
            BigInteger? bitFieldMask = leftSymbol.Type.GetBitfieldMask();
      
            if (bitFieldMask != null) {
              Symbol maskSymbol = new Symbol(leftSymbol.Type, bitFieldMask);
              Start.AddMiddleCode(longList, MiddleOperator.BitwiseAnd, leftSymbol, leftSymbol, maskSymbol);
            }
          }

          return (new Expression(leftExpr.Symbol(), longList, longList));
        }
      }
    }
  
    public static Expression GenerateCompoundAssignmentExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      Assert.Error(leftExpr.Symbol().IsAssignable(), leftExpr, "not assignable");
      Assert.Error(!leftExpr.Symbol().Type.IsArrayOrFunction(), leftExpr,
                   "invalid type in assignment expression");

      List<MiddleCode> longList = new MyList<MiddleCode>();

      Symbol leftSymbol = leftExpr.Symbol(), rightSymbol = rightExpr.Symbol();
             //rightSymbol = TypeCast.implicitCast(longList, rightExpr.Symbol(), leftExpr.Symbol().Type);
    
      if (leftSymbol.Type.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), operatorX);
        operatorX = (MiddleOperator) Enum.Parse(typeof(MiddleOperator), name.Replace("Signed", "Unsigned"));
      }
    
      if (leftSymbol.Type.IsIntegralOrPointer() &&
          rightSymbol.Type.IsIntegralOrPointer()) {
        Symbol oldLeftSymbol = leftSymbol;
        Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
        longList.AddRange(leftExpr.LongList());
        leftSymbol = TypeCast.implicitCast(longList, leftSymbol, maxType);
        longList.AddRange(rightExpr.LongList());
        rightSymbol = TypeCast.implicitCast(longList, rightSymbol, maxType);

        Symbol tempSymbol = new Symbol(leftSymbol.Type);
        Start.AddMiddleCode(longList, operatorX, tempSymbol, leftSymbol, rightSymbol);

        BigInteger? bitFieldMask = leftSymbol.Type.GetBitfieldMask();
      
        if (bitFieldMask != null) {
          Symbol maskSymbol = new Symbol(leftSymbol.Type, bitFieldMask);
          Start.AddMiddleCode(longList, MiddleOperator.BitwiseAnd, tempSymbol, tempSymbol, maskSymbol);
        }

        tempSymbol = TypeCast.implicitCast(longList, tempSymbol, oldLeftSymbol.Type);
        Start.AddMiddleCode(longList, MiddleOperator.Assign, oldLeftSymbol, tempSymbol);
        return (new Expression(oldLeftSymbol, longList, longList));
      }
      else if (leftSymbol.Type.IsFloating()) {
        longList.AddRange(leftExpr.LongList());
        longList.AddRange(rightExpr.LongList());
        rightSymbol = TypeCast.implicitCast(longList, rightExpr.Symbol(), leftExpr.Symbol().Type);
        Symbol tempSymbol = new Symbol(leftSymbol.Type);
        Start.AddMiddleCode(longList, operatorX, tempSymbol, leftSymbol, rightSymbol);
        Start.AddMiddleCode(longList, MiddleOperator.TopFloat, leftSymbol);
        List<MiddleCode> shortList = new MyList<MiddleCode>();
        shortList.AddRange(longList);
        Start.AddMiddleCode(shortList, MiddleOperator.PopFloat);
        return (new Expression(leftSymbol, shortList, longList));
      }
      else {
        return null;
      }
    }
  
    private static bool IsEmpty(List<MiddleCode> codeList) {
      foreach (MiddleCode middleCode in codeList) {
        if (middleCode.Operator != MiddleOperator.Empty) {
          return false;
        }
      }
    
      return true;
    }
  
    public static Expression GenerateConditionalExpression(Expression testExpr,
                                     Expression trueExpr, Expression falseExpr) {
      List<MiddleCode> longList = new List<MiddleCode>();
      Symbol testSymbol = ToLogical(longList, testExpr, true);
      List<MiddleCode> shortList = new List<MiddleCode>();

      MiddleCode trueTarget = new MiddleCode(MiddleOperator.Empty);
      MiddleCode falseTarget = new MiddleCode(MiddleOperator.Empty);
    
      if (IsEmpty(trueExpr.ShortList()) && IsEmpty(falseExpr.ShortList())) {
        shortList.AddRange(testExpr.ShortList());
      }
      else {
        shortList.AddRange(testExpr.LongList());  // Obs: longList

        trueExpr.ShortList().Insert(0, trueTarget);
        Start.Backpatch(testSymbol.trueSet(), trueExpr.ShortList());
        shortList.AddRange(trueExpr.ShortList());

        MiddleCode nextCode = new MiddleCode(MiddleOperator.Empty, null, null, null);
        Start.AddMiddleCode(shortList, MiddleOperator.Goto, nextCode, null, null);

        falseExpr.ShortList().Insert(0, falseTarget);
        Start.Backpatch(testSymbol.falseSet(), falseExpr.ShortList());
        shortList.AddRange(falseExpr.ShortList());

        shortList.Add(nextCode);
      }

      List<MiddleCode> trueLogicalList = new List<MiddleCode>();
      trueLogicalList.Insert(0, trueTarget);
      Symbol trueLogicalSymbol = ToLogical(trueLogicalList, trueExpr, true);
      Start.Backpatch(testSymbol.trueSet(), trueLogicalList);
    
      List<MiddleCode> falseLogicalList = new List<MiddleCode>();    
      falseLogicalList.Insert(0, falseTarget);

      Symbol falseLogicalSymbol = ToLogical(falseLogicalList, falseExpr, true);
      Start.Backpatch(testSymbol.falseSet(), falseLogicalList);

      List<MiddleCode> logicalList = new List<MiddleCode>();
      logicalList.AddRange(trueLogicalList);
      logicalList.AddRange(falseLogicalList);

      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();
      trueSet.UnionWith(trueLogicalSymbol.trueSet());
      trueSet.UnionWith(falseLogicalSymbol.trueSet());
      falseSet.UnionWith(trueLogicalSymbol.falseSet());
      falseSet.UnionWith(falseLogicalSymbol.falseSet());
      Symbol logicalSymbol = new Symbol(trueSet, falseSet);

      Symbol longSymbol;
      if (trueExpr.IsLogical() && falseExpr.IsLogical()) {
        longSymbol = new Symbol(Type.SignedIntegerType);

        List<MiddleCode> trueLongList = new List<MiddleCode>();
        trueLongList.Insert(0, trueTarget);
        Start.Backpatch(testSymbol.trueSet(), trueLongList);
        longList.AddRange(trueLongList);
    
        List<MiddleCode> falseLongList = new List<MiddleCode>();    
        falseLongList.Insert(0, falseTarget);
        Start.Backpatch(testSymbol.falseSet(), falseLongList);
        longList.AddRange(falseLongList);
      
        Symbol trueSymbol = ToLogical(null, trueExpr, false);
        MiddleCode assignTrue = Start.AddMiddleCode(longList, MiddleOperator.Assign, longSymbol, ((long) 1));
        Start.Backpatch(trueSymbol.trueSet(), assignTrue);

        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(longList, MiddleOperator.Goto, targetCode);

        Symbol falseSymbol = ToLogical(null, falseExpr, false);
        MiddleCode assignFalse = Start.AddMiddleCode(longList, MiddleOperator.Assign, longSymbol, ((long) 0));
        Start.Backpatch(falseSymbol.falseSet(), assignFalse);
      
        longList.Add(targetCode);
      }
      else {
        Type maxType = TypeCast.MaxType(trueExpr.Symbol().Type, falseExpr.Symbol().Type);
        longSymbol = new Symbol(maxType);

        List<MiddleCode> trueLongList = new List<MiddleCode>();
        trueLongList.Insert(0, trueTarget);
        trueLongList.AddRange(trueExpr.LongList());
        Symbol trueSymbol = TypeCast.implicitCast(trueLongList, trueExpr.Symbol(), maxType);
        Start.Backpatch(testSymbol.trueSet(), trueLongList);
      
        if (maxType.IsFloating()) {
          Start.AddMiddleCode(trueLongList, MiddleOperator.DecreaseStack);
        }
        else {
          Start.AddMiddleCode(trueLongList, MiddleOperator.Assign, longSymbol, trueSymbol);
        }

        List<MiddleCode> falseLongList = new List<MiddleCode>();
        falseLongList.Insert(0, falseTarget);
        falseLongList.AddRange(falseExpr.LongList());
        Symbol falseSymbol = TypeCast.implicitCast(falseLongList, falseExpr.Symbol(), maxType);
        Start.Backpatch(testSymbol.falseSet(), falseLongList);

        if (!maxType.IsFloating()) {
          Start.AddMiddleCode(falseLongList, MiddleOperator.Assign, longSymbol, falseSymbol);
        }

        longList.AddRange(trueLongList);
        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(longList, MiddleOperator.Goto, targetCode);
        longList.AddRange(falseLongList);
        longList.Add(targetCode);
      }

      return (new Expression(longSymbol, shortList, longList, logicalSymbol, logicalList));
    }

    /*public static Expression GenerateConditionalExpression(Expression testExpr,
                                     Expression trueExpr, Expression falseExpr) {
      List<MiddleCode> longList = new MyList<MiddleCode>();
      Symbol testSymbol = ToLogical(longList, testExpr, true);
      List<MiddleCode> shortList = new MyList<MiddleCode>();

      MiddleCode trueTarget = new MiddleCode(MiddleOperator.Empty);
      MiddleCode falseTarget = new MiddleCode(MiddleOperator.Empty);
    
      if (IsEmpty(trueExpr.ShortList()) && IsEmpty(falseExpr.ShortList())) {
        shortList.AddRange(testExpr.ShortList());
      }
      else {
        shortList.AddRange(testExpr.LongList());  // Obs: longList

        trueExpr.ShortList().Insert(0, trueTarget);
        Start.Backpatch(testSymbol.trueSet(), trueExpr.ShortList());
        shortList.AddRange(trueExpr.ShortList());

        MiddleCode nextCode = new MiddleCode(MiddleOperator.Empty, null, null, null);
        Start.AddMiddleCode(shortList, MiddleOperator.Goto, nextCode, null, null);

        falseExpr.ShortList().Insert(0, falseTarget);
        Start.Backpatch(testSymbol.falseSet(), falseExpr.ShortList());
        shortList.AddRange(falseExpr.ShortList());

        shortList.Add(nextCode);
      }

      List<MiddleCode> trueLogicalList = new MyList<MiddleCode>();
      trueLogicalList.Insert(0, trueTarget);
      Symbol trueLogicalSymbol = ToLogical(trueLogicalList, trueExpr, true);
      Start.Backpatch(testSymbol.trueSet(), trueLogicalList);
    
      List<MiddleCode> falseLogicalList = new MyList<MiddleCode>();    
      falseLogicalList.Insert(0, falseTarget);

      Symbol falseLogicalSymbol = ToLogical(falseLogicalList, falseExpr, true);
      Start.Backpatch(testSymbol.falseSet(), falseLogicalList);

      List<MiddleCode> logicalList = new MyList<MiddleCode>();
      logicalList.AddRange(trueLogicalList);
      logicalList.AddRange(falseLogicalList);

      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();
      trueSet.UnionWith(trueLogicalSymbol.trueSet());
      trueSet.UnionWith(falseLogicalSymbol.trueSet());
      falseSet.UnionWith(trueLogicalSymbol.falseSet());
      falseSet.UnionWith(falseLogicalSymbol.falseSet());
      Symbol logicalSymbol = new Symbol(trueSet, falseSet);

      Symbol longSymbol;
      if (trueExpr.IsLogical() && falseExpr.IsLogical()) {
        longSymbol = new Symbol(CCompiler.Type.SignedIntegerType);

        List<MiddleCode> trueLongList = new MyList<MiddleCode>();
        trueLongList.Insert(0, trueTarget);
        Start.Backpatch(testSymbol.trueSet(), trueLongList);
        longList.AddRange(trueLongList);
    
        List<MiddleCode> falseLongList = new MyList<MiddleCode>();    
        falseLongList.Insert(0, falseTarget);
        Start.Backpatch(testSymbol.falseSet(), falseLongList);
        longList.AddRange(falseLongList);
      
        Symbol trueSymbol = ToLogical(null, trueExpr, false);
        MiddleCode assignTrue = Start.AddMiddleCode(longList, MiddleOperator.Assign, longSymbol, BigInteger.One);
        Start.Backpatch(trueSymbol.trueSet(), assignTrue);

        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(longList, MiddleOperator.Goto, targetCode);

        Symbol falseSymbol = ToLogical(null, falseExpr, false);
        MiddleCode assignFalse = Start.AddMiddleCode(longList, MiddleOperator.Assign, longSymbol, BigInteger.Zero);
        Start.Backpatch(falseSymbol.falseSet(), assignFalse);
      
        longList.Add(targetCode);
      }
      else {
        CCompiler.Type maxType = TypeCast.maxType(trueExpr.symbol().Type, falseExpr.symbol().Type);
        longSymbol = new Symbol(maxType);

        List<MiddleCode> trueLongList = new MyList<MiddleCode>();
        trueLongList.Insert(0, trueTarget);
        trueLongList.AddRange(trueExpr.LongList());
        Symbol trueSymbol = TypeCast.implicitCast(trueLongList, trueExpr.symbol(), maxType);
        Start.Backpatch(testSymbol.trueSet(), trueLongList);
      
        if (maxType.IsFloating()) {
          Start.AddMiddleCode(trueLongList, MiddleOperator.DecreaseStack);
        }
        else {
          Start.AddMiddleCode(trueLongList, MiddleOperator.Assign, longSymbol, trueSymbol);
        }

        List<MiddleCode> falseLongList = new MyList<MiddleCode>();
        falseLongList.Insert(0, falseTarget);
        falseLongList.AddRange(falseExpr.LongList());
        Symbol falseSymbol = TypeCast.implicitCast(falseLongList, falseExpr.symbol(), maxType);
        Start.Backpatch(testSymbol.falseSet(), falseLongList);

        if (!maxType.IsFloating()) {
          Start.AddMiddleCode(falseLongList, MiddleOperator.Assign, longSymbol, falseSymbol);
        }

        longList.AddRange(trueLongList);
        MiddleCode targetCode = new MiddleCode(MiddleOperator.Empty);
        Start.AddMiddleCode(longList, MiddleOperator.Goto, targetCode);
        longList.AddRange(falseLongList);
        longList.Add(targetCode);
      }

      return (new Expression(longSymbol, shortList, longList, logicalSymbol, logicalList));
    }*/
  
    public static Expression GenerateLogicalOrExpression(Expression leftExpr,
                                                         Expression rightExpr) {
      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      Symbol leftSymbol = ToLogical(longList, leftExpr, false);

      List<MiddleCode> rightList = new MyList<MiddleCode>();
      Symbol rightSymbol = ToLogical(rightList, rightExpr, false);

      Symbol constantSymbol =
        ConstantExpression.Generate(null, MiddleOperator.LogicalOr, 
                                    leftExpr.Symbol(), rightExpr.Symbol());

      if (constantSymbol != null) {
        return (new Expression(constantSymbol, new MyList<MiddleCode>(),
                               new MyList<MiddleCode>()));
      }
    
      Start.Backpatch(leftSymbol.falseSet(), rightList);
      longList.AddRange(rightList);

      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>();
      trueSet.UnionWith(leftSymbol.trueSet());
      trueSet.UnionWith(rightSymbol.trueSet());
    
      Symbol symbol = new Symbol(trueSet, rightSymbol.falseSet());
      return (new Expression(symbol, shortList, longList));
    }
  
    public static Expression GenerateLogicalAndExpression(Expression leftExpr,
                                                          Expression rightExpr) {
      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      Symbol leftSymbol = ToLogical(longList, leftExpr, false);
    
      List<MiddleCode> rightList = new MyList<MiddleCode>();
      Symbol rightSymbol = ToLogical(rightList, rightExpr, false);

      Symbol constantSymbol =
        ConstantExpression.Generate(null, MiddleOperator.LogicalAnd,
                                    leftExpr.Symbol(), rightExpr.Symbol());

      if (constantSymbol != null) {
        return (new Expression(constantSymbol, new MyList<MiddleCode>(),
                               new MyList<MiddleCode>()));
      }
    
      Start.Backpatch(leftSymbol.trueSet(), rightList);
      longList.AddRange(rightList);

      ISet<MiddleCode> falseSet = new HashSet<MiddleCode>();
      falseSet.UnionWith(leftSymbol.falseSet());
      falseSet.UnionWith(rightSymbol.falseSet());
    
      Symbol symbol = new Symbol(rightSymbol.trueSet(), falseSet);
      return (new Expression(symbol, shortList, longList));
    }

    public static Expression GenerateBitwiseExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      List<MiddleCode> constLongList = new MyList<MiddleCode>();
      constLongList.AddRange(leftExpr.LongList());
      constLongList.AddRange(rightExpr.LongList());
    
      Symbol leftSymbol = leftExpr.Symbol(),
             rightSymbol = rightExpr.Symbol();
           
      Symbol constSymbol =
        ConstantExpression.Generate(constLongList, operatorX, leftSymbol, rightSymbol);
      if (constSymbol != null) {
        return (new Expression(constSymbol, new MyList<MiddleCode>(), constLongList));
      }
    
      CCompiler.Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
      Symbol symbol = new Symbol(maxType);
      Assert.Error(maxType.IsIntegralPointerArrayStringOrFunction(),
                   maxType, "invalid type in bitwise expression");
    
      leftSymbol = TypeCast.implicitCast(leftExpr.LongList(), leftSymbol, maxType);
      rightSymbol = TypeCast.implicitCast(rightExpr.LongList(), rightSymbol, maxType);
    
      List<MiddleCode> shortList = new MyList<MiddleCode>(),
                       longList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());    
      longList.AddRange(leftExpr.LongList());
      longList.AddRange(rightExpr.LongList());
    
      MiddleCode opCode = new MiddleCode(operatorX, symbol, leftSymbol, rightSymbol);
      longList.Add(opCode);
      return (new Expression(symbol, shortList, longList));
    }

    public static Expression GenerateShiftExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      Assert.Error(leftExpr.Symbol().Type.IsIntegralPointerArrayStringOrFunction(),
                   leftExpr, "invalid type in shift expression");

      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());
    
      Symbol leftSymbol = leftExpr.Symbol();           
      Symbol symbol = new Symbol(leftSymbol.Type);
      Symbol rightSymbol = TypeCast.implicitCast(rightExpr.LongList(), rightExpr.Symbol(), CCompiler.Type.UnsignedCharType);
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      longList.AddRange(leftExpr.LongList());
      longList.AddRange(rightExpr.LongList());
    
      MiddleCode opCode = new MiddleCode(operatorX, symbol, leftSymbol, rightSymbol);
      longList.Add(opCode);
      return (new Expression(symbol, shortList, longList));
    }

    private static Symbol logicalToInteger(List<MiddleCode> list, Symbol symbol) {
      if (symbol.Type.IsLogical()) {
        return TypeCast.implicitCast(list, symbol, CCompiler.Type.SignedIntegerType);
      }
    
      return symbol;
    }
  
    public static Expression GenerateRelationalExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      switch (operatorX) {
        case MiddleOperator.Equal:
        case MiddleOperator.NotEqual:
          CCompiler.Type leftType = leftExpr.Symbol().Type,
                         rightType = rightExpr.Symbol().Type;
          TypeCast.MaxType(leftType, rightType);
          break;

        default:
          Assert.Error(leftExpr.IsLogical() || leftExpr.Symbol().Type.IsArithmeticOrPointer(),
                       leftExpr, "invalid type in relational expression");
          Assert.Error(leftExpr.IsLogical() || rightExpr.Symbol().Type.IsArithmeticOrPointer(),
                       rightExpr, "invalid type in relational expression");
          break;
      }

      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());
    
      Symbol leftSymbol = leftExpr.Symbol(),
             rightSymbol = rightExpr.Symbol();
           
      CCompiler.Type maxTypeX = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
      Assert.Error(maxTypeX.IsArithmeticPointerArrayStringOrFunction(),
                   maxTypeX, "invalid type in expression");
    
      leftSymbol = TypeCast.implicitCast(leftExpr.LongList(), leftSymbol, maxTypeX);
      rightSymbol = TypeCast.implicitCast(rightExpr.LongList(), rightSymbol, maxTypeX);
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      longList.AddRange(leftExpr.LongList());
      longList.AddRange(rightExpr.LongList());
    
      if (maxTypeX.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), operatorX);
        operatorX = (MiddleOperator) Enum.Parse(typeof(MiddleOperator), name.Replace("Signed", "Unsigned"));
      }
    
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(), falseSet = new HashSet<MiddleCode>();
      trueSet.Add(Start.AddMiddleCode(longList, operatorX, null, leftSymbol, rightSymbol));
      falseSet.Add(Start.AddMiddleCode(longList, MiddleOperator.Goto));

      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, shortList, longList));
    }

    public static Expression GenerateAdditionExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      List<MiddleCode> constLongList = new MyList<MiddleCode>();
      constLongList.AddRange(leftExpr.ShortList());
      constLongList.AddRange(rightExpr.ShortList());
    
      Symbol leftSymbol = leftExpr.Symbol(),
             rightSymbol = rightExpr.Symbol();
    
      Symbol constSymbol =
        ConstantExpression.Generate(constLongList, operatorX, leftSymbol, rightSymbol);
      if (constSymbol != null) {
        return (new Expression(constSymbol, new MyList<MiddleCode>(), constLongList));
      }
    
      Symbol staticSymbol =
        StaticExpression.Generate(operatorX, leftSymbol, rightSymbol);
      if (staticSymbol != null) {
        return (new Expression(staticSymbol, null, null));
      }

      List<MiddleCode> shortList = new MyList<MiddleCode>(),
                       longList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());    
      longList.AddRange(leftExpr.LongList());
      longList.AddRange(rightExpr.LongList());

      CCompiler.Type leftType = leftSymbol.Type,
           rightType = rightSymbol.Type;

      if ((operatorX == MiddleOperator.BinarySubtract) &&
          leftType.IsPointerOrArray() && rightType.IsPointerOrArray() &&
          (leftType.GetPointerOrArrayType().Size() > 1)) {
        if (rightSymbol.IsValue()) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          int sizeValue = rightValue * leftType.GetPointerOrArrayType().Size();

          if (rightSymbol.Value is StaticAddress) {
            StaticAddress oldStaticAddress = (StaticAddress) rightSymbol.Value;
            StaticAddress newStaticAddress =
              new StaticAddress(oldStaticAddress.Name, oldStaticAddress.Offset - sizeValue);
            Symbol multSymbol = new Symbol(CCompiler.Type.SignedIntegerType, newStaticAddress);
            return (new Expression(multSymbol, shortList, longList));
          }
          else if (sizeValue > 1) {
            Symbol multSymbol = new Symbol(CCompiler.Type.SignedIntegerType);
            Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, new BigInteger(sizeValue));
            Start.CurrentTable.AddSymbol(sizeSymbol);
            Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract, multSymbol, leftSymbol, sizeSymbol);
            return (new Expression(multSymbol, shortList, longList));
          }
          else {
            Symbol resultSymbol = new Symbol(CCompiler.Type.SignedIntegerType);
            Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, rightSymbol);
            return (new Expression(resultSymbol, shortList, longList));
          }
        }
        else {
          Symbol diffSymbol = new Symbol(CCompiler.Type.SignedIntegerType);
          Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract,
                             diffSymbol, leftSymbol, rightSymbol);

          BigInteger typeSize = new BigInteger(leftType.GetPointerOrArrayType().Size());
          Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, typeSize);
          Start.CurrentTable.AddSymbol(sizeSymbol);
          Symbol resultSymbol = new Symbol(CCompiler.Type.SignedIntegerType);

          if (diffSymbol.Type.IsSigned()) {
            Start.AddMiddleCode(longList, MiddleOperator.SignedDivide, resultSymbol, diffSymbol, sizeSymbol);
          }
          else {
            Start.AddMiddleCode(longList, MiddleOperator.UnsignedDivide, resultSymbol, diffSymbol, sizeSymbol);
          }

          return (new Expression(resultSymbol, shortList, longList));
          /*Symbol diffSymbol = new Symbol(CCompiler.Type.SignedIntegerType);
          Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract,
                             diffSymbol, leftSymbol, rightSymbol);

          BigInteger typeSize = new BigInteger(leftType.GetPointerOrArrayType().Size());
          Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, typeSize);
          Symbol resultSymbol = new Symbol(CCompiler.Type.SignedIntegerType);
          Start.AddMiddleCode(longList, MiddleOperator.UnsignedDivide, resultSymbol, diffSymbol, sizeSymbol);

          return (new Expression(resultSymbol, shortList, longList));*/
        }
      }
      else if ((operatorX == MiddleOperator.BinaryAdd) &&
               leftType.IsPointerOrArray() && rightType.IsIntegral() &&
               (leftType.GetPointerOrArrayType().Size() > 1)) {
        if (rightSymbol.IsValue()) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          int sizeValue = rightValue * leftType.GetPointerOrArrayType().Size();

          if (rightSymbol.Value is StaticAddress) {
            StaticAddress oldStaticAddress = (StaticAddress) rightSymbol.Value;
            StaticAddress newStaticAddress =
              new StaticAddress(oldStaticAddress.Name, oldStaticAddress.Offset + sizeValue);
            Type pointerType = new Type(leftType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType, newStaticAddress);
            return (new Expression(multSymbol, shortList, longList));
          }
          else {
            Type pointerType = new Type(leftType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType);
            Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, new BigInteger(sizeValue));
            Start.CurrentTable.AddSymbol(sizeSymbol);
            Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, multSymbol, leftSymbol, sizeSymbol);
            return (new Expression(multSymbol, shortList, longList));
          }
        }
        else {
          Type pointerType = new Type(leftType.GetPointerOrArrayType());
          Symbol multSymbol = new Symbol(pointerType);
          BigInteger typeSize = new BigInteger(leftType.GetPointerOrArrayType().Size());
          Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, typeSize);
          Start.CurrentTable.AddSymbol(sizeSymbol);

          if (rightSymbol.Type.IsSigned()) {
            Start.AddMiddleCode(longList, MiddleOperator.SignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          }
          else {
            Start.AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          }

          Symbol resultSymbol = new Symbol(pointerType);
          Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, leftSymbol, multSymbol);
          return (new Expression(resultSymbol, shortList, longList));
        }
      }
      else if ((operatorX == MiddleOperator.BinarySubtract) &&
               leftType.IsPointerOrArray() && rightType.IsIntegral() &&
               (leftType.GetPointerOrArrayType().Size() > 1)) {
        if (rightSymbol.IsValue()) {
          int rightValue = int.Parse(rightSymbol.Value.ToString());
          int sizeValue = rightValue * leftType.GetPointerOrArrayType().Size();

          if (rightSymbol.Value is StaticAddress) {
            StaticAddress oldStaticAddress = (StaticAddress) rightSymbol.Value;
            StaticAddress newStaticAddress =
              new StaticAddress(oldStaticAddress.Name, oldStaticAddress.Offset - sizeValue);
            Type pointerType = new Type(leftType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType, newStaticAddress);
            return (new Expression(multSymbol, shortList, longList));
          }
          else {
            Type pointerType = new Type(leftType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType);
            Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, new BigInteger(sizeValue));
            Start.CurrentTable.AddSymbol(sizeSymbol);
            Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract, multSymbol, leftSymbol, sizeSymbol);
            return (new Expression(multSymbol, shortList, longList));
          }
        }
        else {
          Type pointerType = new Type(leftType.GetPointerOrArrayType());
          Symbol multSymbol = new Symbol(pointerType);
          BigInteger typeSize = new BigInteger(leftType.GetPointerOrArrayType().Size());
          Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, typeSize);
          Start.CurrentTable.AddSymbol(sizeSymbol);

          if (rightSymbol.Type.IsSigned()) {
            Start.AddMiddleCode(longList, MiddleOperator.SignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          }
          else {
            Start.AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, rightSymbol, sizeSymbol);
          }

          Symbol resultSymbol = new Symbol(pointerType);
          Start.AddMiddleCode(longList, MiddleOperator.BinarySubtract, resultSymbol, leftSymbol, multSymbol);
          return (new Expression(resultSymbol, shortList, longList));
        }
      }
      else if ((operatorX == MiddleOperator.BinaryAdd) &&
               leftType.IsIntegral() && rightType.IsPointerOrArray() &&
               (rightType.GetPointerOrArrayType().Size() > 1)) {
        if (leftSymbol.IsValue()) {
          int leftValue = int.Parse(leftSymbol.Value.ToString());
          int sizeValue = leftValue * rightType.GetPointerOrArrayType().Size();
        
          if (rightSymbol.Value is StaticAddress) {
            StaticAddress oldStaticAddress = (StaticAddress) rightSymbol.Value;
            StaticAddress newStaticAddress =
              new StaticAddress(oldStaticAddress.Name, oldStaticAddress.Offset + sizeValue);
            Type pointerType = new Type(rightType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType, newStaticAddress);
            return (new Expression(multSymbol, shortList, longList));
          }
          else {
            Type pointerType = new Type(rightType.GetPointerOrArrayType());
            Symbol multSymbol = new Symbol(pointerType);
            Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, new BigInteger(sizeValue));
            Start.CurrentTable.AddSymbol(sizeSymbol);
            Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, multSymbol, multSymbol, sizeSymbol);
            return (new Expression(multSymbol, shortList, longList));
          }
        }
        else {
          Type pointerType = new Type(rightType.GetPointerOrArrayType());
          Symbol multSymbol = new Symbol(pointerType);
          BigInteger typeSize = new BigInteger(rightType.GetPointerOrArrayType().Size());
          Symbol sizeSymbol = new Symbol(CCompiler.Type.SignedIntegerType, typeSize);
          Start.CurrentTable.AddSymbol(sizeSymbol);

          if (leftSymbol.Type.IsSigned()) {
            Start.AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, leftSymbol, sizeSymbol);
          }
          else {
            Start.AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, multSymbol, leftSymbol, sizeSymbol);
          }


          Symbol resultSymbol = new Symbol(pointerType);
          Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, resultSymbol, multSymbol, rightSymbol);
          return (new Expression(resultSymbol, shortList, longList)); // XXX mult   
        }
      }
      else {
        CCompiler.Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);
        leftSymbol = TypeCast.implicitCast(leftExpr.LongList(), leftSymbol, maxType);
        rightSymbol = TypeCast.implicitCast(rightExpr.LongList(), rightSymbol, maxType);
        Symbol resultSymbol = new Symbol(maxType);
      
        shortList = new MyList<MiddleCode>();
        longList = new MyList<MiddleCode>();
        shortList.AddRange(leftExpr.ShortList());
        shortList.AddRange(rightExpr.ShortList());    
        longList.AddRange(leftExpr.LongList());
        longList.AddRange(rightExpr.LongList());

        Start.AddMiddleCode(longList, operatorX, resultSymbol, leftSymbol, rightSymbol);
        return (new Expression(resultSymbol, shortList, longList));
      }
    }

    public static Expression GenerateMultiplyExpression(Expression leftExpr,
                                     MiddleOperator operatorX, Expression rightExpr) {
      List<MiddleCode> constLongList = new MyList<MiddleCode>();
      constLongList.AddRange(leftExpr.LongList());
      constLongList.AddRange(rightExpr.LongList());

      Symbol leftSymbol = leftExpr.Symbol(),
             rightSymbol = rightExpr.Symbol();
           
      Symbol constSymbol =
        ConstantExpression.Generate(constLongList, operatorX, leftSymbol, rightSymbol);
      if (constSymbol != null) {
        return (new Expression(constSymbol, new MyList<MiddleCode>(), constLongList));
      }

      CCompiler.Type maxType = TypeCast.MaxType(leftSymbol.Type, rightSymbol.Type);

      if (MiddleCode.IsModulo(operatorX)) {
        Assert.Error(maxType.IsIntegralPointerArrayStringOrFunction(),
                     maxType, "invalid type in expression");
      }
      else {
        Assert.Error(maxType.IsArithmeticPointerArrayStringOrFunction(),
                     maxType, "invalid type in expression");
      }

      leftSymbol = TypeCast.implicitCast(leftExpr.LongList(), leftSymbol, maxType);
      rightSymbol = TypeCast.implicitCast(rightExpr.LongList(), rightSymbol, maxType);
    
      List<MiddleCode> shortList = new MyList<MiddleCode>(),
                       longList = new MyList<MiddleCode>();
      shortList.AddRange(leftExpr.ShortList());
      shortList.AddRange(rightExpr.ShortList());    
      longList.AddRange(leftExpr.LongList());
      longList.AddRange(rightExpr.LongList());

      if (maxType.IsUnsigned()) {
        string name = Enum.GetName(typeof(MiddleOperator), operatorX);
        operatorX = (MiddleOperator) Enum.Parse(typeof(MiddleOperator), name.Replace("Signed", "Unsigned"));
      }

      Symbol symbol = new Symbol(maxType);
      Start.AddMiddleCode(longList, operatorX, symbol, leftSymbol, rightSymbol);
      return (new Expression(symbol, shortList, longList));
    }
  
    // ------------------------------------------------------------------------
  
    public static Expression GenerateUnaryExpression(MiddleOperator operatorX,
                                                     Expression expr) {
      CCompiler.Type type = expr.Symbol().Type;
      Assert.Error(expr.IsLogical() || type.IsArithmeticPointerArrayStringOrFunction(),
                   expr, "non-arithmetic symbol in unary expression");

      Symbol constSymbol =
        ConstantExpression.Generate(expr.LongList(), operatorX, expr.Symbol());
    
      if (constSymbol != null) {
        return (new Expression(constSymbol, new MyList<MiddleCode>(), expr.LongList()));
      }
    
      Symbol resultSymbol = new Symbol(expr.Symbol().Type);
      Start.AddMiddleCode(expr.LongList(), operatorX, resultSymbol, expr.Symbol());
      return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
    }

    public static Expression GenerateLogicalNotExpression(Expression expr) {
      List<MiddleCode> longList = new MyList<MiddleCode>();
      Symbol symbol = ToLogical(longList, expr, false);
    
      Symbol constantSymbol =
        ConstantExpression.Generate(null, MiddleOperator.LogicalNot, expr.Symbol());

      if (constantSymbol != null) {
        return (new Expression(constantSymbol, new MyList<MiddleCode>(),
                               new MyList<MiddleCode>()));
      }
        
      Symbol notSymbol = new Symbol(symbol.falseSet(), symbol.trueSet());
      return (new Expression(notSymbol, expr.ShortList(), longList));
    }

    public static Expression GenerateBitwiseNotExpression(Expression expr) {
      Symbol symbol = logicalToInteger(expr.LongList(), expr.Symbol());
      Symbol resultSymbol = new Symbol(symbol.Type);
      Start.AddMiddleCode(expr.LongList(), MiddleOperator.BitwiseNot, resultSymbol, symbol);
      return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
    }

    public static Expression GenerateSizeOfExpression(Expression expr) {
      Assert.Error(expr.Symbol().Storage != Storage.Register,
                   expr, "register storage not allowed in sizof expression");

      CCompiler.Type type = expr.Symbol().Type;
      Assert.Error(!type.IsFunction(), "sizeof applied to function not allowed");
      Assert.Error(!type.IsBitfield(), "sizeof applied to bitfield not allowed");

      Symbol symbol = new Symbol(CCompiler.Type.SignedIntegerType,
                                 new BigInteger(expr.Symbol().Type.Size()));
      return (new Expression(symbol, null, null));
    }

    public static Expression GenerateSizeOfType(CCompiler.Type type) {
      Assert.Error(!type.IsFunction(), "sizeof applied to function not allowed");
      Assert.Error(!type.IsBitfield(), "sizeof applied to bitfield not allowed");

      type = type.IsLogical() ? CCompiler.Type.SignedIntegerType : type;
      Symbol symbol = new Symbol(CCompiler.Type.SignedIntegerType, new BigInteger(type.Size()));
      return (new Expression(symbol, null, null));
    }
 
    // x->y
    // (*x).y
    //&p[i]
  
    public static Expression GenerateAddressExpression(Expression expr) {
      if (!expr.Symbol().IsAddressable()) {
        expr.Symbol().IsAddressable();
      }
    
      Assert.Error(expr.Symbol().IsAddressable(), expr, "not Addressable");
      Assert.Error(expr.Symbol().Storage != Storage.Register,
                   expr, "invalid address of register storage");

      Symbol staticSymbol = StaticExpression.Generate(MiddleOperator.address, expr.Symbol());
      if (staticSymbol != null) {
        return (new Expression(staticSymbol, null, null));
      }
    
      if (expr.Symbol().Type.IsFloating()) {
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PopFloat);
      }

      CCompiler.Type pointerType = new CCompiler.Type(expr.Symbol().Type);
      Symbol resultSymbol = new Symbol(pointerType);
      Start.AddMiddleCode(expr.LongList(), MiddleOperator.address, resultSymbol, expr.Symbol());
      return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
    }

    public static Expression GenerateDerefExpression(Expression expr) {
      Assert.Error(expr.Symbol().Type.IsArithmeticPointerArrayStringOrFunction(),
                   "invalid dereference of non-pointer");

      Symbol staticSymbol = StaticExpression.Generate(MiddleOperator.Deref, expr.Symbol());
      if (staticSymbol != null) {
        return (new Expression(staticSymbol, null, null));
      }

      Symbol symbol = expr.Symbol();
      CCompiler.Type baseType = symbol.Type.GetPointerOrArrayType();
      Symbol resultSymbol = new Symbol(baseType);
      resultSymbol.AddressSymbol = symbol;
      resultSymbol.AddressOffset = 0;
      resultSymbol.SetAssignable(!baseType.Constant);
      Start.AddMiddleCode(expr.LongList(), MiddleOperator.Deref, resultSymbol, symbol, 0);

      if (resultSymbol.Type.IsFloating()) {
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushFloat, resultSymbol);
      }
    
      return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
    }
  
    public static Expression GenerateArrowExpression(Expression expr, string memberName) {
      Symbol symbol = expr.Symbol();
      Assert.Error(symbol.Type.IsPointer(), expr,
                   "not a pointer in arrow expression");
      Assert.Error(symbol.Type.PointerType.IsStructOrUnion(), expr,
                   "not a pointer to a struct or union in arrow expression");
      Symbol memberSymbol = symbol.Type.PointerType.LookupMember(memberName);
      Assert.Error(memberSymbol != null, memberName,
                   "unknown member in arrow expression");

      Symbol resultSymbol = new Symbol(memberSymbol.Type);
      resultSymbol.AddressSymbol = expr.Symbol();
      int memberOffset = memberSymbol.Offset;
      resultSymbol.AddressOffset = memberOffset;
      resultSymbol.SetAddressable(true);
      resultSymbol.SetAssignable(!expr.Symbol().Type.PointerType.Constant &&
                                 !memberSymbol.Type.Constant);
      Start.AddMiddleCode(expr.LongList(), MiddleOperator.Deref, resultSymbol, symbol, memberOffset);

      if (resultSymbol.Type.IsFloating()) {
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushFloat, resultSymbol);
      }

      return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
    }
  
    public static Expression GenerateCastExpression(CCompiler.Type type,
                                                    Expression expr) {
      Symbol constantSymbol = ConstantExpression.cast(expr.LongList(), expr.Symbol(), type);
      if (constantSymbol != null) {
        return (new Expression(constantSymbol, expr.ShortList(), expr.LongList()));
      }
    
      Symbol symbol = TypeCast.explicitCast(expr.LongList(), expr.Symbol(), type);
      return (new Expression(symbol, expr.ShortList(), expr.LongList()));
    }
  
    private static IDictionary<MiddleOperator,MiddleOperator> m_incrementMap =
      new Dictionary<MiddleOperator, MiddleOperator>();
    private static IDictionary<MiddleOperator, MiddleOperator> m_incrementInverseMap =
      new Dictionary<MiddleOperator, MiddleOperator>();
          
    static MiddleCodeGenerator() {
      m_incrementMap.Add(MiddleOperator.Increment, MiddleOperator.BinaryAdd);
      m_incrementMap.Add(MiddleOperator.Decrement, MiddleOperator.BinarySubtract);
      m_incrementInverseMap.Add(MiddleOperator.Increment, MiddleOperator.BinarySubtract);
      m_incrementInverseMap.Add(MiddleOperator.Decrement, MiddleOperator.BinaryAdd);
    }

    public static Expression GeneratePrefixIncrementExpression
                             (MiddleOperator middleOp, Expression expression){
      List<MiddleCode> longList = new List<MiddleCode>();

      Symbol oneSymbol;
      if (expression.Symbol().Type.IsFloating()) {
        oneSymbol = new Symbol(expression.Symbol().Type);
        Start.AddMiddleCode(longList, MiddleOperator.PushOne);
      }
      else {
        oneSymbol = new Symbol(expression.Symbol().Type, BigInteger.One);
      }

      MiddleOperator o = (middleOp == MiddleOperator.Increment) ? MiddleOperator.BinaryAdd : MiddleOperator.BinarySubtract;
      Expression oneExpression = new Expression(oneSymbol, null, longList);
      return GenerateCompoundAssignmentExpression(expression, o, oneExpression);
    }

    public static Expression GeneratePrefixIncrementExpressionX(MiddleOperator operatorX,
                                                               Expression expr) {
      Symbol symbol = expr.Symbol();
      Assert.Error(symbol.IsAssignable(), "not assignable");
      Assert.Error(symbol.Type.IsArithmeticOrPointer(),
                   expr, "invalid type in increment expression");

      if (symbol.Type.IsIntegralOrPointer()) {
        Symbol oneSymbol = new Symbol(symbol.Type, BigInteger.One);
        Start.AddMiddleCode(expr.ShortList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.LongList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
    
        BigInteger? bitFieldMask = symbol.Type.GetBitfieldMask();
        if (bitFieldMask != null) {
          Symbol maskSymbol = new Symbol(symbol.Type, bitFieldMask.Value);
          MiddleCode maskCode = new MiddleCode(MiddleOperator.BitwiseAnd, symbol, symbol, maskSymbol);
          expr.ShortList().Add(maskCode);
          expr.LongList().Add(maskCode);
        }
    
        Symbol resultSymbol = new Symbol(symbol.Type);
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.Assign, resultSymbol, symbol);
      
        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
      else {
        Start.AddMiddleCode(expr.ShortList(), MiddleOperator.PushOne);        
        Symbol oneSymbol = new Symbol(symbol.Type, (decimal) 1.0);
        Start.AddMiddleCode(expr.ShortList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.ShortList(), MiddleOperator.PopFloat, symbol);
        
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushOne);
        Start.AddMiddleCode(expr.LongList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.TopFloat, symbol);

        Symbol resultSymbol = new Symbol(symbol.Type);
        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
    }

    public static Expression GeneratePostfixIncrementExpression
                             (Expression expression, MiddleOperator middleOp) {
      Symbol resultSymbol = new Symbol(expression.Symbol().Type);

      if (expression.Symbol().Type.IsFloating()) {
        List<MiddleCode> longList = new List<MiddleCode>();
        longList.AddRange(expression.LongList());

        Start.AddMiddleCode(longList, MiddleOperator.PushFloat, expression.Symbol());
        Start.AddMiddleCode(longList, MiddleOperator.PushOne);
        Symbol oneSymbol = new Symbol(expression.Symbol().Type, (decimal)1);
        Start.AddMiddleCode(longList, m_incrementMap[middleOp], expression.Symbol(), expression.Symbol(), oneSymbol);
        Start.AddMiddleCode(longList, MiddleOperator.PopFloat, expression.Symbol());

        List<MiddleCode> shortList = new List<MiddleCode>();
        shortList.AddRange(longList);
        Start.AddMiddleCode(shortList, MiddleOperator.PopEmpty);
        return (new Expression(resultSymbol, shortList, longList));
      }
      else {
        List<MiddleCode> codeList = new List<MiddleCode>();
        Expression resultExpression = new Expression(resultSymbol, codeList, codeList);
        codeList.AddRange(GenerateSimpleAssignmentExpression(resultExpression, expression).ShortList());
        codeList.AddRange(GeneratePrefixIncrementExpression(middleOp, expression).ShortList());
        return resultExpression;
      }
    }

    public static Expression GeneratePostfixIncrementExpressionX
                                     (Expression expr, MiddleOperator operatorX) {
      Symbol symbol = expr.Symbol();
      Assert.Error(symbol.IsAssignable(), "not assignable");
      Assert.Error(symbol.Type.IsArithmeticOrPointer(),
                   expr, "invalid type in increment expression");
    
      if (symbol.Type.IsIntegralOrPointer()) {
        Symbol resultSymbol = new Symbol(symbol.Type);
        Symbol oneSymbol = new Symbol(symbol.Type, BigInteger.One);
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.Assign, resultSymbol, symbol);
        Start.AddMiddleCode(expr.ShortList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.LongList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);

        BigInteger? bitFieldMask = symbol.Type.GetBitfieldMask();
        if (bitFieldMask != null) {
          Symbol maskSymbol = new Symbol(symbol.Type, bitFieldMask.Value);
          Start.AddMiddleCode(expr.ShortList(), MiddleOperator.BitwiseAnd, symbol, symbol, maskSymbol);
          Start.AddMiddleCode(expr.LongList(), MiddleOperator.BitwiseAnd, symbol, symbol, maskSymbol);
        }

        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
      else {
        Symbol oneSymbol = new Symbol(symbol.Type, (decimal)1.0);

        Start.AddMiddleCode(expr.ShortList(), MiddleOperator.PushOne);
        Start.AddMiddleCode(expr.ShortList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.ShortList(), MiddleOperator.PopFloat, symbol);

        //Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushFloat, symbol);
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushOne);
        Start.AddMiddleCode(expr.LongList(), m_incrementMap[operatorX], symbol, symbol, oneSymbol);
        Start.AddMiddleCode(expr.LongList(), MiddleOperator.TopFloat, symbol);

        Start.AddMiddleCode(expr.LongList(), MiddleOperator.PushOne);
        Start.AddMiddleCode(expr.LongList(), m_incrementInverseMap[operatorX],
                            symbol, symbol, oneSymbol);

        Symbol resultSymbol = new Symbol(symbol.Type);
        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
    }

    public static Expression GenerateDotExpression(Expression expr, string memberName) {
      Symbol parentSymbol = expr.Symbol();
      Assert.Error(expr.Symbol().Type.IsStructOrUnion(),
                   expr, "not a struct or union in dot expression");
      Symbol memberSymbol = parentSymbol.Type.LookupMember(memberName);
      Assert.Error(memberSymbol != null, memberName,
                   "unknown member in dot expression");

      if (parentSymbol.HasAddressSymbol()) {
        string name = parentSymbol.Name + "." + memberSymbol.Name +
                      Start.SeparatorId + memberSymbol.Offset;
        Symbol resultSymbol =
          new Symbol(parentSymbol.GetStatus(), name, parentSymbol.Storage,
                     memberSymbol.Type, null, null, null, null, null, null, null);
        resultSymbol.UniqueName = parentSymbol.UniqueName;
        resultSymbol.AddressSymbol = parentSymbol.AddressSymbol; // XXX
        resultSymbol.AddressOffset = parentSymbol.AddressOffset;
        resultSymbol.ParentSymbol = parentSymbol;
        resultSymbol.Offset = parentSymbol.Offset + memberSymbol.Offset;
        resultSymbol.SetAddressable(parentSymbol.IsAddressable());
        resultSymbol.SetAssignable(parentSymbol.IsAssignable() &&
                                   !memberSymbol.Type.Constant);
        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
      else {
        Symbol resultSymbol = new Symbol(memberSymbol.Type);
        resultSymbol.Name = parentSymbol.Name + Start.SeparatorId + memberName;
        resultSymbol.UniqueName = parentSymbol.UniqueName;
        resultSymbol.Storage = parentSymbol.Storage;
        resultSymbol.SetAddressable(true);
        resultSymbol.SetAssignable(!expr.Symbol().Type.Constant &&
                                   !memberSymbol.Type.Constant);
        resultSymbol.Offset = parentSymbol.Offset + memberSymbol.Offset;
        return (new Expression(resultSymbol, expr.ShortList(), expr.LongList()));
      }
    }
    
    public static Expression GenerateIndexExpression(Expression pointerExpr, Expression indexExpr) {
      Assert.Error((pointerExpr.Symbol().Type.IsPointerOrArray() && 
                    indexExpr.Symbol().Type.IsIntegral()) ||
                   (pointerExpr.Symbol().Type.IsIntegral()) &&
                    indexExpr.Symbol().Type.IsPointerOrArray(),
                   pointerExpr.ToString() + "," + indexExpr.ToString(),
                   "invalid types in index expression");

      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(pointerExpr.ShortList());
      shortList.AddRange(indexExpr.ShortList());

      List<MiddleCode> longList = new MyList<MiddleCode>();
      longList.AddRange(pointerExpr.LongList());
      longList.AddRange(indexExpr.LongList());

      Symbol pointerSymbol = pointerExpr.Symbol(),
             indexSymbol = indexExpr.Symbol();
            
      /*Symbol staticSymbol = StaticExpression.Generate(MiddleOperator.Index, pointerSymbol, indexSymbol);
      if (staticSymbol != null) {
        return (new Expression(staticSymbol, new List<MiddleCode>(), new List<MiddleCode>()));
      }*/

      if (pointerSymbol.Type.IsIntegral() &&
          indexSymbol.Type.IsPointerOrArray()) {
        Symbol tempSymbol = pointerSymbol;
        pointerSymbol = indexSymbol;
        indexSymbol = tempSymbol;
      }
    
      CCompiler.Type pointerType = pointerSymbol.Type.GetPointerOrArrayType();
    
      if (pointerType.IsArray()) {
        pointerType = new CCompiler.Type(pointerType.ArrayType);
      }
    
      int pointerSize = pointerType.Size();
      Symbol resultSymbol = new Symbol(pointerType);
      resultSymbol.SetAddressable(true);
      resultSymbol.SetAssignable(!pointerType.Constant);
    
      if (indexSymbol.HasValue()) {
        int indexValue = int.Parse(indexSymbol.Value.ToString());
        int memberOffset = indexValue * pointerSize;
      
        if (pointerSymbol.Value is StaticAddress) {
          StaticAddress staticAddress = (StaticAddress) indexSymbol.Value;
          StaticValue staticValue = new StaticValue(staticAddress.Name, staticAddress.Offset + memberOffset);
          resultSymbol.Value = staticValue;
        }
        else if (pointerSymbol.IsStatic()) {
          StaticValue staticValue = new StaticValue(pointerSymbol.UniqueName, memberOffset);
          resultSymbol.Value = staticValue;
          resultSymbol.AddressSymbol = pointerSymbol;
          resultSymbol.AddressOffset = memberOffset;
          //Start.AddMiddleCode(longList, MiddleOperator.Deref, resultSymbol, pointerSymbol, memberOffset);
        }
        else {
          resultSymbol.AddressSymbol = pointerSymbol;
          resultSymbol.ParentSymbol = pointerSymbol;
          //pointerSymbol.AddChildSymbol(resultSymbol);
          resultSymbol.AddressOffset = memberOffset;
          Start.AddMiddleCode(longList, MiddleOperator.Deref, resultSymbol, pointerSymbol, memberOffset);

          if (resultSymbol.Type.IsFloating()) {
            Start.AddMiddleCode(longList, MiddleOperator.PushFloat, resultSymbol);
          }
        }
      }
      else if (pointerSize == 1) {
        Symbol AddSymbol = new Symbol(indexSymbol.Type);
        Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, AddSymbol,
                           pointerSymbol, indexSymbol);
        resultSymbol.ParentSymbol = pointerSymbol;
        //pointerSymbol.AddChildSymbol(resultSymbol);
        resultSymbol.AddressSymbol = AddSymbol;
        resultSymbol.AddressOffset = 0;
        Start.AddMiddleCode(longList, MiddleOperator.Deref, resultSymbol, AddSymbol, 0);

        if (resultSymbol.Type.IsFloating()) {
          Start.AddMiddleCode(longList, MiddleOperator.PushFloat, resultSymbol);
        }
      }
      else {
        Symbol sizeSymbol = new Symbol(indexSymbol.Type, new BigInteger(pointerSize));
        Start.CurrentTable.AddSymbol(sizeSymbol);
        Symbol mulSymbol = new Symbol(indexSymbol.Type);

        if (indexSymbol.Type.IsSigned()) {
          Start.AddMiddleCode(longList, MiddleOperator.SignedMultiply, mulSymbol, indexSymbol, sizeSymbol);
        }
        else {
          Start.AddMiddleCode(longList, MiddleOperator.UnsignedMultiply, mulSymbol, indexSymbol, sizeSymbol);
        }

        Symbol AddSymbol = new Symbol(indexSymbol.Type);
        Start.AddMiddleCode(longList, MiddleOperator.BinaryAdd, AddSymbol, pointerSymbol, mulSymbol);
        resultSymbol.ParentSymbol = pointerSymbol;
        //pointerSymbol.AddChildSymbol(resultSymbol);
        resultSymbol.AddressSymbol = AddSymbol;
        resultSymbol.AddressOffset = 0;
        Start.AddMiddleCode(longList, MiddleOperator.Deref, resultSymbol, AddSymbol, 0);

        if (resultSymbol.Type.IsFloating()) {
          Start.AddMiddleCode(longList, MiddleOperator.PushFloat, resultSymbol);
        }
      }

      return (new Expression(resultSymbol, shortList, longList));
    }

    public static void GenerateCallHeader(Expression expr) {
      Symbol symbol = expr.Symbol();

      if (symbol.IsTemporary()) {
        //symbol.Switch = true;
        Start.CurrentTable.AddSymbol(symbol);
      }

      CCompiler.Type type = symbol.Type;
      CCompiler.Type funcType = type.IsFunction() ? type : type.PointerType;
      Start.TypeListStack.Push(funcType.GetTypeList());
      Start.ParameterIndexStack.Push(0);
      Start.ParameterOffsetStack.Push(0);
      Start.AddMiddleCode(expr.ShortList(), MiddleOperator.CallHeader, Start.CurrentTable.CurrentOffset, symbol);
      Start.AddMiddleCode(expr.LongList(), MiddleOperator.CallHeader, Start.CurrentTable.CurrentOffset, symbol);
    }
  
    public static Expression GenerateParameterExpression(Expression expr) {
      List<CCompiler.Type> typeList = Start.TypeListStack.Peek();
      int index = Start.ParameterIndexStack.Pop(),
          offset = Start.ParameterOffsetStack.Pop();
      Symbol symbol;
      if ((typeList != null) && (index < typeList.Count)) {
        symbol = TypeCast.implicitCast(expr.LongList(), expr.Symbol(), typeList[index]);
      }
      else {
        symbol = TypeCast.parameterCast(expr.LongList(), expr.Symbol());
      }

      Start.ParameterIndexStack.Push(index + 1);
      Start.ParameterOffsetStack.Push(offset + symbol.Type.ArraySizeX());
      return (new Expression(symbol, expr.LongList(), expr.LongList()));
    }
 
    private static Type parameterType(Symbol symbol) {
      switch (symbol.Type.GetSort()) {
        case Sort.Array:
          return (new Type(symbol.Type.ArrayType));

        case Sort.Function:
          return (new Type(symbol.Type));

        case Sort.StringX:
          return (new Type(new Type(Sort.Signed_Char)));

        case Sort.Logical:
          return Type.SignedIntegerType;

        default:
          return symbol.Type;
      }
    }
   
    private static int parameterSize(Symbol symbol) {
      switch (symbol.Type.GetSort()) {
        case Sort.Array:
        case Sort.StringX:
        case Sort.Function:
          return CCompiler.Type.PointerSize;

        default:
          return symbol.Type.Size();
      }
    }
   
    public static Expression GenerateCallExpression(Expression funcExpr,
                                                    List<Expression> argList) {
      Start.TypeListStack.Pop();
      Start.ParameterIndexStack.Pop();
      Start.ParameterOffsetStack.Pop();

      int totalOffset = 0;
      foreach (int currentOffset in Start.ParameterOffsetStack) {
        if (currentOffset > 0) {
          totalOffset += (SymbolTable.FunctionRegularInitSize + currentOffset);
        }
      }
      
      CCompiler.Type funcType = funcExpr.Symbol().Type.IsPointer() ?
                      funcExpr.Symbol().Type.PointerType :
                      funcExpr.Symbol().Type;
    
      List<CCompiler.Type> typeList = funcType.GetTypeList();
      Assert.Error((typeList == null) || (argList.Count >= typeList.Count),
                   funcExpr, "too few parameters in function call");
      Assert.Error(funcType.IsEllipse() || (typeList == null) ||
                   (argList.Count == typeList.Count),
                   funcExpr, "too many parameters in function call");
    
      List<MiddleCode> longList = new MyList<MiddleCode>();
      longList.AddRange(funcExpr.LongList());
      //Start.AddMiddleCode(longList, MiddleOperator.CallHeader);
    
      int index = 0, offset = Start.FunctionHeaderSize, extra = 0;
      foreach (Expression argExpr in argList) {
        longList.AddRange(argExpr.LongList());
        Symbol argSymbol = argExpr.Symbol();
        int argSize;
      
        Type type;
        if ((typeList != null) && (index < typeList.Count)) {
          type = typeList[index];
          argSize = typeList[index].Size();
        }
        else {
          type = parameterType(argSymbol);
          argSize = parameterSize(argSymbol);
          extra += argSize;
        }

        if (Start.CurrentFunction.Name.Equals("atan") &&
            (argSymbol.Name.Equals("£temporary528") ||
             //argSymbol.Name.Equals("£temporary529") ||
             argSymbol.Name.Equals("£temporary534") ||
             argSymbol.Name.Equals("£temporary535") ||
             argSymbol.Name.Equals("£temporary717") ||
             argSymbol.Name.Equals("£temporary718") ||
             argSymbol.Name.Equals("£temporary746"))) {
          offset += 8;
        }

        argSymbol = TypeCast.implicitCast(longList, argSymbol, type);

        if (argSymbol.Type.IsStructOrUnion()) {
          Start.AddMiddleCode(longList, MiddleOperator.ParameterInitSize);
        }

        Start.AddMiddleCode(longList, MiddleOperator.Parameter, Start.CurrentTable.CurrentOffset, argSymbol, totalOffset + offset);
        offset += parameterSize(argSymbol);
        ++index;
      }

      //Start.AddMiddleCode(longList, MiddleOperator.PreCall);
      Start.AddMiddleCode(longList, MiddleOperator.Call, Start.CurrentTable.CurrentOffset, funcExpr.Symbol(), totalOffset, extra);
      //Start.AddMiddleCode(longList, MiddleOperator.PostCall, 0 /*record*/, funcExpr.symbol(), 0, 0 /*stack*/);
      Start.AddMiddleCode(longList, MiddleOperator.PostCall, null, null, Start.CurrentTable.CurrentOffset);
      //Start.AddMiddleCode(longList, MiddleOperator.PostCallIntegral);
      //Start.AddMiddleCode(longList, MiddleOperator.PostCallFloating);
    
      CCompiler.Type returnType = funcType.ReturnType;
      Symbol returnSymbol = new Symbol(returnType);
    
      List<MiddleCode> shortList = new MyList<MiddleCode>();
      shortList.AddRange(longList);
    
      if (!returnType.IsVoid()) {
        if (returnType.IsStructOrUnion()) {
          CCompiler.Type pointerType = new CCompiler.Type(returnType);
          Symbol AddressSymbol = new Symbol(pointerType);
          returnSymbol.AddressSymbol = AddressSymbol;
        }
      
        Start.AddMiddleCode(longList, MiddleOperator.GetReturnValue, returnSymbol);
        //Start.AddMiddleCode(shortList, MiddleOperator.GetReturnValue, null, returnSymbol);
      
        if (returnType.IsFloating()) {
          Start.AddMiddleCode(shortList, MiddleOperator.PopEmpty);
        }
      }

      return (new Expression(returnSymbol, shortList, longList));
    }
  
    public static Expression GenerateSymbolExpression(string name) {
      Symbol symbol = Start.CurrentTable.LookupSymbol(name);
      Assert.Error(symbol != null, "unknown name: " + name);
    
      if (symbol == null) {
        Assert.Warning(name, "unknown name, assuming extern int " + name +"()");
        CCompiler.Type type = new CCompiler.Type(CCompiler.Type.SignedIntegerType, null, false);
        symbol = new Symbol(name, Storage.Extern, type);
      }

      List<MiddleCode> shortList = new MyList<MiddleCode>();
      List<MiddleCode> longList = new MyList<MiddleCode>();
    
      if (symbol.Type.IsFloating()) {
        /*if (symbol.IsTemporary() && (symbol.Offset == 0)) {
          Start.CurrentTable.AddSymbol(symbol); // XXX
        }*/

        Start.AddMiddleCode(shortList, MiddleOperator.PushFloat, symbol);
        Start.AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }

      return (new Expression(symbol, shortList, longList));
    }

    public static Expression GenerateValueExpression(Symbol symbol) {
      List<MiddleCode> longList = new MyList<MiddleCode>();

      if (symbol.Type.IsFloating()) {
        Start.AddMiddleCode(longList, MiddleOperator.PushFloat, symbol);
      }

      symbol.Value = ConstantExpression.CheckUnsigned(symbol.Value, symbol.Type);
      Start.CurrentTable.AddSymbol(symbol);
      return (new Expression(symbol, new MyList<MiddleCode>(), longList));
    }

    public static Expression GenerateSystemCall(String name, List<Expression> argList) {
      List<MiddleCode> shortList = new List<MiddleCode>();
      Start.AddMiddleCode(shortList, MiddleOperator.SystemInit, name);

      int index = 0;
      foreach (Expression arg in argList) {
        shortList.AddRange(arg.LongList());
        Start.AddMiddleCode(shortList, MiddleOperator.SystemParameter, name, index++, arg.Symbol());
      }

      List<MiddleCode> longList = new List<MiddleCode>();
      longList.AddRange(shortList);
      Symbol returnSymbol = new Symbol(SystemCode.ReturnType(name));

      Start.AddMiddleCode(shortList, MiddleOperator.SystemCall, name, null);
      Start.AddMiddleCode(longList, MiddleOperator.SystemCall, name, returnSymbol);
      return (new Expression(returnSymbol, shortList, longList));
    }

    public static Expression GenerateInspectRegisterExpression(Register register) {
      List<MiddleCode> longList = new List<MiddleCode>();

      Type type = null;
      switch (AssemblyCode.RegisterSize(register)) {
        case Type.CharSize:
          type = Type.UnsignedCharType;
          break;

        case Type.IntegerSize:
          type = Type.UnsignedIntegerType;
          break;

        case Type.LongSize:
          type = Type.UnsignedLongIntegerType;
          break;
      }

      Symbol symbol = new Symbol(type);
      Start.AddMiddleCode(longList, MiddleOperator.InspectRegister, symbol, register);
      return (new Expression(symbol, new List<MiddleCode>(), longList, register));
    }

    public static Expression GenerateCarryFlagExpression() {
      ISet<MiddleCode> trueSet = new HashSet<MiddleCode>(),
                       falseSet = new HashSet<MiddleCode>();
      List<MiddleCode> longList = new List<MiddleCode>();
      trueSet.Add(Start.AddMiddleCode(longList, MiddleOperator.Carry));
      falseSet.Add(Start.AddMiddleCode(longList, MiddleOperator.Goto));
      Symbol symbol = new Symbol(trueSet, falseSet);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }

    public static Expression GenerateFlagbyteExpression() {
      List<MiddleCode> longList = new List<MiddleCode>();
      Symbol symbol = new Symbol(Type.UnsignedCharType);
      Start.AddMiddleCode(longList, MiddleOperator.InspectFlagByte, symbol);
      return (new Expression(symbol, new List<MiddleCode>(), longList));
    }
  }
}
 
   /* 1. if a = b goto 2
      2. goto 3
      3. ...  
  
      a += b; => t = a + b; a = t;
 
    x += y;
  
    1. x += y
    
    1. t1 = x + y
    2. x = t1

    (i + 1) ? (s + 1) : (t + 1)
    
    1.  t1 = i + 1
    2.  if t1 != 0 goto 4
    3.  goto 6
    4.  t2 = s + 1
    5.  goto 10
    6.  t3 = t + 1
    7.  t4 = short_to_int t3
    8.  t5 = t4
    9.  goto 12
    10. t6 = short_to_int t2
    11. t5 = t6
    12. ...

    1.  t1 = i + 1
    2.  if t1 == 0 goto 7
    3.  t2 = s + 1
    4.  t3 = short_to_int t2
    5.  t4 = t3
    6.  goto 10
    7.  t5 = t + 1
    8.  t6 = short_to_int t5
    9.  t4 = t6
    10. ...
  
    1.  t1 = i + 1
    2.  if t1 == 0 goto 5
    3.  t2 = s + 1
    4.  goto 9
    5.  t3 = t + 1  
    6.  t4 = short_to_int t3
    7.  t5 = t4
    8.  goto 11
    9.  t6 = short_to_int t2
    10. t5 = t6
    11. ...

    1.  t1 = i + 1
    2.  if t1 != 0 goto 4
    3.  goto 8
    4.  t2 = s + 1
    5.  t3 = short_to_int t2
    6.  t4 = t3
    7.  goto 11
    8.  t5 = t + 1
    9.  t6 = short_to_int t5
    10. t4 = t6
    11. ...

    1.  t1 = i + 1
    2.  if t1 == 0 goto 8
    3.  nop
    4.  t2 = s + 1
    5.  t3 = short_to_int t2
    6.  t4 = t3
    7.  goto 11
    8.  t5 = t + 1
    9.  t6 = short_to_int t5
    10. t4 = t6
    11. ...

    1.  t1 = i + 1
    2.  if t1 == 0 goto 7, soft
    3.  t2 = s + 1
    4.  t3 = short_to_int t2
    5.  t4 = t3
    6.  goto 10, soft
    7.  t5 = t + 1
    8.  t6 = short_to_int t5
    9.  t4 = t6
    10. ...
  */