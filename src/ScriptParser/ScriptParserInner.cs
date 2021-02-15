using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ShenGu.Script
{
    partial class ScriptParser
    {
        #region 内部类

        private class KeywordFlag
        {
            internal string Keyword;
            internal int Flag;

            internal KeywordFlag(string keyword, int flag)
            {
                this.Keyword = keyword;
                this.Flag = flag;
            }
        }

        private enum ParseAbility
        {
            None = 0,
            Expr = 1,   //表达式
            Declare = 2,    //变量声明
            Flow = 4,	//流程控制：if...else，do...while，while，switch，try...catch...finally
            BlockStart = 8,    //块起始：{
            All = Expr + Declare + Flow + BlockStart
        }

        private abstract class ParsingCacheBase
        {
            internal ParsingCacheBase Parent;
            internal bool InStack;

            internal virtual void Reset()
            {
                Parent = null;
            }

            internal abstract int CheckExprEnd(ScriptParser parser, int flag);

        }

        private class ParsingInnerCache : ParsingCacheBase
        {
            internal const int TYPE_Object = 1, TYPE_Array = 2, TYPE_InnerExpr = 3, TYPE_Var = 4
                , TYPE_InvokeMethod = 5, TYPE_GetArrayMember = 6, TYPE_Condition = 7, TYPE_Function = 8, TYPE_Return = 9, TYPE_Throw = 10;
            internal int Type;
            //当类型为TYPE_InvokeMethod时，用于记录方法参数的数量
            //当类型为TYPE_Condition时，用于表示进行到哪一步：0-准备解析第1个值；1-准备解析第2个值；
            internal int ArgCount;
            internal ElementBase Elem, Elem2;
            
            internal override void Reset()
            {
                base.Reset();
                Type = 0;
                ArgCount = 0;
                Elem = Elem2 = null;
            }

            internal override int CheckExprEnd(ScriptParser parser, int flag)
            {
                switch (Type)
                {
                    case TYPE_Object:
                        return parser.CheckParseObjectEnd(this, flag);
                    case TYPE_Array:
                        return parser.CheckParseArrayEnd(this, flag);
                    case TYPE_InnerExpr:
                        return parser.CheckParseInnerExprEnd(this, flag);
                    case TYPE_Var:
                        return parser.CheckParseVarEnd(this, flag);
                    case TYPE_InvokeMethod:
                        return parser.CheckParseInvokeMethodEnd(this, flag);
                    case TYPE_GetArrayMember:
                        return parser.CheckParseGetArrayMemberEnd(this, flag);
                    case TYPE_Condition:
                        return parser.CheckParseConditionEnd(this, flag);
                    case TYPE_Function:
                        return parser.CheckParseFunctionDefinedEnd(this, flag);
                    case TYPE_Return:
                        return parser.CheckParseReturnEnd(this, flag);
                    case TYPE_Throw:
                        return parser.CheckParseThrowEnd(this, flag);
                }
                parser.ThrowParseError();
                return 0;
            }
        }

        private class ParsingExprCache : ParsingCacheBase
        {
            internal ElementBase LastInsert;
            internal ElementBase Insert;
            internal ParseAbility Ability;
            internal int Step;      //0-首次进入，1-解析操作元，2-解析操作符
            internal int Flag;
            
            internal override void Reset()
            {
                base.Reset();
                this.LastInsert = null;
                this.Insert = null;
                this.Ability = ParseAbility.None;
                this.Flag = 0;
            }

            internal override int CheckExprEnd(ScriptParser parser, int flag)
            {
                return parser.CheckExprEnd(this, flag);
            }

            internal void CheckLastOperandVisitState()
            {
                if (Insert != null && Insert != LastInsert)
                {
                    Insert.Prev.ResultVisit = Insert.ArgusResultVisit;
                }
            }
        }

        private class ParsingLogicCache : ParsingCacheBase
        {
            internal const int TYPE_If = 1, TYPE_ForIn = 2, TYPE_For = 3, TYPE_While = 4, TYPE_DoWhile = 5, TYPE_Switch = 6, TYPE_Try = 7, TYPE_BlockStart = 8;
            internal int Type;
            internal int Step;
            internal ElementBase Elem, Elem2, Elem3, LastElem;
            internal List<ElementBase> CaseList;    //当类型为Switch时，用于缓存Case的元素

            internal override int CheckExprEnd(ScriptParser parser, int flag)
            {
                switch(Type)
                {
                    case TYPE_If:
                        return parser.CheckParseIfEnd(this, flag);
                    case TYPE_For:
                        return parser.CheckParseForEnd(this, flag);
                    case TYPE_ForIn:
                        return parser.CheckParseForInEnd(this, flag);
                    case TYPE_While:
                        return parser.CheckParseWhileEnd(this, flag);
                    case TYPE_DoWhile:
                        return parser.CheckParseDoWhileEnd(this, flag);
                    case TYPE_Switch:
                        return parser.CheckParseSwitchEnd(this, flag);
                    case TYPE_Try:
                        return parser.CheckParseTryEnd(this, flag);
                    case TYPE_BlockStart:
                        return parser.CheckParseBlockStartEnd(this, flag);
                }
                parser.ThrowParseError();
                return 0;
            }
        }
        
        #endregion

        #region 内部对象

        private class LoopCheckElement : ElementBase
        {
            private List<JumpElement> continueList;

            internal void AddContinueElem(JumpElement elem)
            {
                if (continueList == null) continueList = new List<JumpElement>();
                continueList.Add(elem);
            }

            internal void Finish(ScriptParser parser, ElementBase continueElem)
            {
                if (continueList != null)
                    foreach (JumpElement elem in continueList)
                        elem.GotoPointer = continueElem == null ? this.Next : continueElem;
                parser.RemoveElement(this);
            }

            internal override bool AllowGetLastResult { get { return false; } }

            internal override string GetDescription()
            {
                return "[LoopCheck]";
            }
        }

        private class CaseBeginElement : ElementBase
        {
            internal CaseElement CaseElem;

            internal override bool AllowGetLastResult { get { return false; } }

            internal override string GetDescription()
            {
                return "[CaseBegin]";
            }
        }

        #endregion

    }

    internal delegate void ContextFinishedCallback(DefineContext context, object[] argus);

    internal class DefineContext : ScriptObjectBase
    {
        internal int ContextIndex;
        internal int ArgusCount;
        internal ElementBase First, Last;
        internal DefineContext ParentContext;
        private int variableCount;
        private CatchStartElement[] catchVariableList;
        private int catchVariableCount;
        private CheckAtLastInfo[] finishList;
        private int finishCount;
        private ElementBase[] lastInserts;
        private int lastInsertCount;
        private readonly static CheckHashValue<IScriptObject> checkFunc = new CheckHashValue<IScriptObject>(CheckFunctionValue);

        internal DefineContext(long objectId, DefineContext parentContext) : base(false)
        {
            this.ObjectId = objectId;
            this.ParentContext = parentContext;
        }

        public IScriptObject this[string name]
        {
            get { return InnerGetValue(null, name); }
            set { InnerSetValue(null, name, value); }
        }

        internal int NewVarIndex()
        {
            return variableCount < 0 ? variableCount : variableCount++;
        }

        internal void UseVariableSaved()
        {
            variableCount = -2;
        }
        
        internal void DisableVariable()
        {
            variableCount = -1;
        }

        public override object ToValue(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        public override string ToValueString(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        internal void FinishParsing()
        {
            catchVariableCount = 0;
            catchVariableList = null;
            if (finishCount > 0)
            {
                for (int i = 0; i < finishCount; i++)
                    finishList[i].Callback(this, finishList[i].Argus);
                finishCount = 0;
                finishList = null;
            }
        }

        internal void PushCatchVariable(CatchStartElement elem)
        {
            if (catchVariableList == null || catchVariableList.Length == catchVariableCount)
                Array.Resize<CatchStartElement>(ref catchVariableList, catchVariableCount + 4);
            catchVariableList[catchVariableCount++] = elem;
        }

        internal CatchStartElement PopCatchVariable()
        {
            if (catchVariableCount > 0)
            {
                CatchStartElement result = catchVariableList[--catchVariableCount];
                catchVariableList[catchVariableCount] = null;
                return result;
            }
            return null;
        }

        internal CatchStartElement FindCatchVariable(string varName)
        {
            if (catchVariableCount > 0)
            {
                for (int i = catchVariableCount - 1; i >= 0; i--)
                {
                    CatchStartElement elem = catchVariableList[i];
                    if (elem.VarName == varName) return elem;
                }
            }
            return null;
        }
        internal void AddFinishCallback(ContextFinishedCallback callback, params object[] argus)
        {
            if (finishList == null || finishCount == finishList.Length)
                Array.Resize<CheckAtLastInfo>(ref finishList, finishCount + 8);
            finishList[finishCount].Argus = argus;
            finishList[finishCount++].Callback = callback;
        }

        internal ElementBase PeekLastInsert()
        {
            if (lastInsertCount > 0) return lastInserts[lastInsertCount - 1];
            return null;
        }

        internal void PushLastInsert(ElementBase elem)
        {
            if (lastInserts == null || lastInsertCount == lastInserts.Length)
                Array.Resize<ElementBase>(ref lastInserts, lastInsertCount + 4);
            lastInserts[lastInsertCount++] = elem;
        }

        internal bool CheckPopLastInsert(ElementBase elem)
        {
            if (lastInsertCount > 0 && lastInserts[lastInsertCount - 1] == elem)
            {
                lastInserts[--lastInsertCount] = null;
                return true;
            }
            return false;
        }

        internal ElementDesc[] Elements
        {
            get
            {
                ElementDescList list = new ElementDescList();
                if (First != null) list.AddList(First, null);
                return list.ToArray();
            }
        }

        private static IScriptObject CheckFunctionValue(IScriptObject obj, object state)
        {
            ScriptFunctionProxy funcProxy = obj as ScriptFunctionProxy;
            if (funcProxy != null)
                return new ScriptFunction(funcProxy.Context, (ScriptExecuteContext)state);
            return obj;
        }

        internal ScriptExecuteContext CreateExecuteContext(ScriptExecuteContext result, IScriptObject[] argus)
        {
            if (result == null)
                result = new ScriptExecuteContext();
            this.CleanAssignTo(result, checkFunc, result, argus, this.ArgusCount);
            result.Current = this.First;
            result.ObjectId = this.ObjectId;
            result.ContextIndex = this.ContextIndex;
            result.VariableCount = this.variableCount;
            return result;
        }

        #region 内部类/结构

        private struct CheckAtLastInfo
        {
            public ContextFinishedCallback Callback;
            public object[] Argus;
        }

        #endregion
    }
    
    internal class ScriptFunctionProxy : IScriptObject
    {
        internal DefineContext Context;

        internal ScriptFunctionProxy(DefineContext context)
        {
            this.Context = context;
        }

        #region IScriptObject

        string IScriptObject.TypeName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        IScriptObject IScriptObject.GetValue(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        bool IScriptObject.Remove(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        void IScriptObject.SetValue(ScriptContext context, string name, IScriptObject value)
        {
            throw new NotImplementedException();
        }

        object IScriptObject.ToValue(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        string IScriptObject.ToValueString(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ElementDescList
    {
        private List<ElementDesc> list = new List<ElementDesc>();
        private Dictionary<ElementBase, ElementDesc> dic = new Dictionary<ElementBase, ElementDesc>();
        private List<string> descStack = new List<string>();

        public void AddList(ElementBase elem, string prefix)
        {
            if (elem != null)
            {
                if (!string.IsNullOrEmpty(prefix)) descStack.Add(prefix);
                string currentDesc;
                if (descStack.Count == 0) currentDesc = null;
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string d in descStack)
                    {
                        sb.Append('@');
                        sb.Append(d);
                        sb.Append(">>");
                    }
                    currentDesc = sb.ToString();
                }
                ElementDesc desc;
                if (!dic.TryGetValue(elem, out desc))
                {
                    ElementDesc d = desc = new ElementDesc(elem);
                    ElementBase e = elem;
                    ElementBase endElem = null;
                    do
                    {
                        dic.Add(e, d);
                        if (e.Next == null || dic.ContainsKey(e.Next))
                        {
                            endElem = e;
                            break;
                        }
                        e = e.Next;
                        d = new ElementDesc(e);
                    } while (true);

                    e = elem;
                    do
                    {
                        desc.Init(currentDesc, list.Count);
                        list.Add(desc);
                        e.AddOtherDescriptions(this);
                        if (e.Next == null) break;
                        if (e == endElem)
                        {
                            ElementDesc d2;
                            if (dic.TryGetValue(e.Next, out d2))
                                d2.AddDelayNextElement(desc);
                            break;
                        }
                        e = e.Next;
                        if (!dic.TryGetValue(e, out desc) || desc.Index >= 0) break;
                    } while (true);
                }
                else
                {
                    ElementDesc d2 = new ElementDesc(null);
                    d2.Init(currentDesc, string.Empty, list.Count);
                    desc.AddDelayNextElement(d2);
                    list.Add(d2);
                }
                if (!string.IsNullOrEmpty(prefix)) descStack.RemoveAt(descStack.Count - 1);
            }
        }

        public ElementDesc[] ToArray() { return list.ToArray(); }
    }

    internal class ElementDesc
    {
        private int index = -1;
        private string text;
        private ElementBase element;
        private List<ElementDesc> delayNextList;

        public ElementDesc(ElementBase elem)
        {
            this.element = elem;
        }

        public string Text { get { return text; } }
        
        public ElementBase Element { get { return element; } }

        public int Index { get { return index; } }

        public void Init(string prefix, int index)
        {
            Init(prefix, element.ToString(), index);
        }

        public void Init(string prefix, string text, int index)
        {
            if (!string.IsNullOrEmpty(prefix)) this.text = prefix + text;
            else this.text = text;
            if (element == null) this.text = "    " + this.text;
            this.index = index;
            if (delayNextList != null)
            {
                foreach (ElementDesc desc in delayNextList)
                    desc.text += string.Format("  =>[{0}]", index);
                delayNextList = null;
            }
        }

        public void AddDelayNextElement(ElementDesc desc)
        {
            if (index >= 0)
            {
                desc.text += string.Format("  =>[{0}]", index);
            }
            else
            {
                if (delayNextList == null) delayNextList = new List<ElementDesc>();
                delayNextList.Add(desc);
            }
        }

        public override string ToString()
        {
            return text;
        }
    }
}
