using System;
using System.Collections.Generic;
using System.Text;

namespace ShenGu.Script
{
    #region 元素类
    
    internal enum ResultVisitFlag
    {
        None = 0,
        Get = 1,
        Set = 2,
        GetSet = 3,
        ObjectMember = 4,
    }

    internal static class ScriptHelper
    {
        private static OptMethod[] optMethods;
        public const long CustomObjectId = 2;
        public const long SystemObjectId = 1;

        static ScriptHelper()
        {
            OptExecuteCallback equalsCallback = new OptExecuteCallback(OptEqualsValue);
            OptExecuteCallback bitOptCallback = new OptExecuteCallback(OptBitOperate);
            OptExecuteCallback compareCallback = new OptExecuteCallback(OptCompare);
            OptExecuteCallback computeCallback = new OptExecuteCallback(OptCompute);
            OptExecuteCallback unaryCallback = new OptExecuteCallback(OptUnaryCompute);
            OptMethod[] ms = new OptMethod[]
            {
                new OptMethod(OperatorType.BitOr, bitOptCallback),
                new OptMethod(OperatorType.BitXOr, bitOptCallback),
                new OptMethod(OperatorType.BitAnd, bitOptCallback),
                new OptMethod(OperatorType.EqualsValue, equalsCallback),
                new OptMethod(OperatorType.NotEqualsValue, equalsCallback),
                new OptMethod(OperatorType.Equals, equalsCallback),
                new OptMethod(OperatorType.NotEquals, equalsCallback),
                new OptMethod(OperatorType.Less, compareCallback),
                new OptMethod(OperatorType.LessEquals, compareCallback),
                new OptMethod(OperatorType.Greater, compareCallback),
                new OptMethod(OperatorType.GreaterEquals, compareCallback),
                new OptMethod(OperatorType.InstanceOf, new OptExecuteCallback(OptInstanceOf)),
                new OptMethod(OperatorType.ShiftLeft, bitOptCallback),
                new OptMethod(OperatorType.ShiftRight, bitOptCallback),
                new OptMethod(OperatorType.UnsignedShiftRight, bitOptCallback),
                new OptMethod(OperatorType.Add, computeCallback),
                new OptMethod(OperatorType.Substract, computeCallback),
                new OptMethod(OperatorType.Multiply, computeCallback),
                new OptMethod(OperatorType.Divide, computeCallback),
                new OptMethod(OperatorType.Modulus, computeCallback),
                new OptMethod(OperatorType.Negative, unaryCallback),
                new OptMethod(OperatorType.BitNot, unaryCallback),
                new OptMethod(OperatorType.LogicNot, unaryCallback),
                new OptMethod(OperatorType.Typeof, unaryCallback),
            };
            optMethods = new OptMethod[(int)OperatorType.InvokeMethod + 1];
            foreach (OptMethod item in ms)
                optMethods[(int)item.Type] = item;
        }

        #region 开放方法

        internal static bool EqualsValue(IScriptObject arg1, IScriptObject arg2, bool checkType)
        {
            if (arg1 is ScriptInteger)
            {
                long val1 = ((ScriptInteger)arg1).IntegerValue;

                if (arg2 is ScriptInteger) return val1 == ((ScriptInteger)arg2).IntegerValue;
                if (arg2 is ScriptDecimal) return val1 == ((ScriptDecimal)arg2).DecimalValue;
                if (!checkType)
                {
                    decimal val2;
                    if (arg2 is ScriptString && decimal.TryParse(((ScriptString)arg2).Value, out val2))
                        return val1 == val2;
                }
                return false;
            }
            if (arg1 is ScriptDecimal)
            {
                decimal val1 = ((ScriptDecimal)arg1).DecimalValue;
                if (arg2 is ScriptInteger) return val1 == ((ScriptInteger)arg2).IntegerValue;
                if (arg2 is ScriptDecimal) return val1 == ((ScriptDecimal)arg2).DecimalValue;
                if (!checkType)
                {
                    decimal val2;
                    if (arg2 is ScriptString && decimal.TryParse(((ScriptString)arg2).Value, out val2))
                        return val1 == val2;
                }
                return false;
            }
            if (arg1 is ScriptString)
            {
                string val1 = ((ScriptString)arg1).Value;
                if (!checkType)
                {
                    ScriptNumber num2 = arg2 as ScriptNumber;
                    if (num2 != null)
                    {
                        if (num2.Type == NumberType.Integer)
                        {
                            long l1;
                            return long.TryParse(val1, out l1) && l1 == num2.IntegerValue;
                        }
                        if (num2.Type == NumberType.Decimal)
                        {
                            decimal d1;
                            return decimal.TryParse(val1, out d1) && d1 == num2.DecimalValue;
                        }
                        return false;
                    }
                }
                ScriptString str2 = arg2 as ScriptString;
                return str2 != null && val1 == str2.Value;
            }
            return arg1 == arg2;
        }

        internal static bool IsTrue(IScriptObject obj)
        {
            return ScriptGlobal.IsTrue(obj);
        }

        internal static IScriptObject Compute(ScriptContext context, OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            OperatorExecuteHandler handler = context.GetOperatorExecutor(type);
            if (handler != null)
            {
                OperatorExecuteArgs args = new OperatorExecuteArgs(type, arg1, arg2);
                IScriptObject result = handler(context, args);
                if (!args.IsCancelled) return result;
            }
            OptMethod method = optMethods[(int)type];
            return method.Callback(type, arg1, arg2);
        }

        internal static bool IsNumber(object key)
        {
            return key is int || key is long;
        }

        internal static bool TryParseIndex(object key, out string name, out int value)
        {
            if (IsNumber(key))
            {
                value = Convert.ToInt32(key);
                name = null;
                return true;
            }
            if (key != null)
            {
                name = key.ToString();
                if (int.TryParse(name, out value)) return true;
            }
            else
            {
                name = null;
                value = 0;
            }
            return false;
        }

        internal static IScriptObject CheckGetPropValue(ScriptContext context, IScriptObject instance, IScriptObject value)
        {
            if (value is IScriptProperty)
                value = ((IScriptProperty)value).GetPropValue(context, instance);
            return value;
        }

        internal static bool CheckSetPropValue(ScriptContext context, IScriptObject instance, IScriptObject propValue, IScriptObject value)
        {
            IScriptProperty pv = propValue as IScriptProperty;
            if (pv != null)
            {
                pv.SetPropValue(context, instance, value);
                return true;
            }
            return false;
        }

        #endregion

        #region 操作符方法

        private static long GetLongValue(IScriptObject obj)
        {
            long v = 0;
            ScriptNumber num1 = obj as ScriptNumber;
            if (num1 != null) v = num1.IntegerValue;
            else
            {
                ScriptString str = obj as ScriptString;
                if (str != null) long.TryParse(str.Value, out v);
            }
            return v;
        }
        
        private static IScriptObject OptBitOperate(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            long v1 = GetLongValue(arg1);
            long v2 = GetLongValue(arg2);
            long r;
            switch(type)
            {
                case OperatorType.BitOr: r = v1 | v2; break;
                case OperatorType.BitAnd: r = v1 & v2; break;
                case OperatorType.BitXOr: r = v1 ^ v2; break;
                case OperatorType.ShiftLeft: r = v1 << (int)v2; break;
                case OperatorType.ShiftRight: r = v1 >> (int)v2; break;
                case OperatorType.UnsignedShiftRight: r = (long)((ulong)v1 >> (int)v2); break;
                default: r = 0; break;
            }
            return ScriptNumber.Create(r);
        }
        
        private static IScriptObject OptEqualsValue(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            bool result = false;
            switch(type)
            {
                case OperatorType.EqualsValue:
                    result = EqualsValue(arg1, arg2, false);
                    break;
                case OperatorType.NotEqualsValue:
                    result = !EqualsValue(arg1, arg2, false);
                    break;
                case OperatorType.Equals:
                    result = EqualsValue(arg1, arg2, true);
                    break;
                case OperatorType.NotEquals:
                    result = !EqualsValue(arg1, arg2, true);
                    break;
            }
            return ScriptBoolean.Create(result);
        }

        private static IScriptObject OptCompare(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            int flag = 0;
            ScriptNumber num1 = arg1 as ScriptNumber;
            if (num1 != null)
            {
                if (num1.Type > 0)
                {
                    if (arg2 is ScriptNumber)
                    {
                        if (((ScriptNumber)arg2).Type > 0) flag = 1;
                    }
                    else
                    {
                        ScriptString str2 = arg2 as ScriptString;
                        if (str2 != null)
                        {
                            decimal dec2;
                            if (decimal.TryParse(str2.Value, out dec2))
                            {
                                flag = 1;
                                arg2 = ScriptNumber.Create(dec2);
                            }
                            else
                            {
                                flag = 2;
                                arg1 = ScriptString.Create(num1.ToString());
                            }
                        }
                    }
                }
            }
            else
            {
                ScriptString str1 = arg1 as ScriptString;
                if (str1 != null)
                {
                    if (arg2 is ScriptString)
                        flag = 2;
                    else
                    {
                        ScriptNumber num2 = arg2 as ScriptNumber;
                        if (num2 != null && num2.Type > 0)
                        {
                            decimal dec1;
                            if (decimal.TryParse(str1.Value, out dec1))
                            {
                                flag = 1;
                                arg1 = ScriptNumber.Create(dec1);
                            }
                            else
                            {
                                flag = 2;
                                arg2 = ScriptString.Create(num2.ToString());
                            }
                        }
                    }
                }
            }
            bool result = false;
            if (flag == 1)
            {
                decimal dec1 = ((ScriptNumber)arg1).DecimalValue, dec2 = ((ScriptNumber)arg2).DecimalValue;
                switch(type)
                {
                    case OperatorType.Less: result = dec1 < dec2; break;
                    case OperatorType.LessEquals: result = dec1 <= dec2; break;
                    case OperatorType.Greater: result = dec1 > dec2; break;
                    case OperatorType.GreaterEquals: result = dec1 >= dec2; break;
                }
            }
            else if (flag == 2)
            {
                string str1 = ((ScriptString)arg1).Value, str2 = ((ScriptString)arg2).Value;
                int v = StringComparer.Ordinal.Compare(str1, str2);
                switch(type)
                {
                    case OperatorType.Less: result = v < 0; break;
                    case OperatorType.LessEquals: result = v <= 0; break;
                    case OperatorType.Greater: result = v > 0; break;
                    case OperatorType.GreaterEquals: result = v >= 0; break;
                }
            }
            return ScriptBoolean.Create(result);
        }

        private static IScriptObject OptInstanceOf(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            ScriptObjectBase base1 = arg1 as ScriptObjectBase;
            if (base1 != null && base1.Parent == arg2) return ScriptBoolean.True;
            return ScriptBoolean.False;
        }

        private static ScriptNumber GetScriptNumber(IScriptObject obj)
        {
            ScriptNumber result = obj as ScriptNumber;
            if (result == null)
            {
                ScriptString str = obj as ScriptString;
                if (str != null)
                {
                    long longValue;
                    decimal decValue;
                    if (long.TryParse(str.Value, out longValue)) result = ScriptNumber.Create(longValue);
                    else if (decimal.TryParse(str.Value, out decValue)) result = ScriptNumber.Create(decValue);
                }
            }
            return result;
        }

        private static IScriptObject OptCompute(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            if (type == OperatorType.Add)
            {
                ScriptNumber num1 = arg1 as ScriptNumber;
                if (num1 != null)
                {
                    ScriptNumber num2 = arg2 as ScriptNumber;
                    if (num2 != null)
                    {
                        if (num1.Type > 0 && num2.Type > 0)
                        {
                            if (num1.Type == NumberType.Decimal || num2.Type == NumberType.Decimal)
                                return ScriptNumber.Create(num1.DecimalValue + num2.DecimalValue);
                            else
                                return ScriptNumber.Create(num1.IntegerValue + num2.IntegerValue);
                        }
                        else if (num1.Type == NumberType.NaN || num2.Type == NumberType.NaN)
                            return ScriptNumber.NaN;
                        else
                            return ScriptNumber.Infinity;
                    }
                }
                return ScriptString.Create(arg1.ToString() + arg2.ToString());
            }
            else
            {
                ScriptNumber num1 = GetScriptNumber(arg1);
                if (num1 != null)
                {
                    ScriptNumber num2 = GetScriptNumber(arg2);
                    if (num2 != null)
                    {
                        if (num1.Type > 0 && num2.Type > 0)
                        {
                            if (type == OperatorType.Modulus)
                                return ScriptNumber.Create(num1.IntegerValue % num2.IntegerValue);
                            bool hasDecimal = num1.Type == NumberType.Decimal || num2.Type == NumberType.Decimal;
                            switch (type)
                            {
                                case OperatorType.Substract:
                                    if (hasDecimal) return ScriptNumber.Create(num1.DecimalValue - num2.DecimalValue);
                                    return ScriptNumber.Create(num1.IntegerValue - num2.IntegerValue);
                                case OperatorType.Multiply:
                                    if (hasDecimal) return ScriptNumber.Create(num1.DecimalValue * num2.DecimalValue);
                                    return ScriptNumber.Create(num1.IntegerValue * num2.IntegerValue);
                                case OperatorType.Divide:
                                    if (hasDecimal) return ScriptNumber.Create(num1.DecimalValue / num2.DecimalValue);
                                    return ScriptNumber.Create(num1.IntegerValue / num2.IntegerValue);
                            }
                        }
                        else if (num1.Type == NumberType.NaN || num2.Type == NumberType.NaN)
                            return ScriptNumber.NaN;
                        else
                            return ScriptNumber.Infinity;
                    }
                }
            }
            return ScriptNumber.NaN;
        }

        private static IScriptObject OptUnaryCompute(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            switch(type)
            {
                case OperatorType.Negative:
                    {
                        ScriptNumber num = arg1 as ScriptNumber;
                        if (num != null)
                        {
                            if (num.Type == NumberType.Decimal) return ScriptNumber.Create(-num.DecimalValue);
                            if (num.Type == NumberType.Integer) return ScriptNumber.Create(-num.IntegerValue);
                        }
                        return ScriptNumber.NaN;
                    }
                case OperatorType.BitNot:
                    {
                        ScriptNumber num = arg1 as ScriptNumber;
                        if (num != null && num.Type > 0)
                            return ScriptNumber.Create(~num.IntegerValue);
                        return ScriptNumber.NaN;
                    }
                case OperatorType.LogicNot:
                    {
                        ScriptNumber num = arg1 as ScriptNumber;
                        if (num != null && num.Type == NumberType.NaN) return ScriptBoolean.False;
                        return ScriptBoolean.Create(!IsTrue(arg1));
                    }
                case OperatorType.Typeof:
                    return ScriptString.Create(arg1.TypeName);
            }
            return ScriptUndefined.Instance;
        }

        #endregion

        #region 内部类

        delegate IScriptObject OptExecuteCallback(OperatorType type, IScriptObject arg1, IScriptObject arg2);

        struct OptMethod
        {
            public OperatorType Type;
            public OptExecuteCallback Callback;

            public OptMethod(OperatorType type, OptExecuteCallback callback)
            {
                this.Type = type;
                this.Callback = callback;
            }
        }

        #endregion
    }

    public class OperatorExecuteArgs
    {
        private OperatorType type;
        private IScriptObject arg1;
        private IScriptObject arg2;
        private bool isCancelled;

        internal OperatorExecuteArgs(OperatorType type, IScriptObject arg1, IScriptObject arg2)
        {
            this.type = type;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public OperatorType Type { get { return type; } }

        public IScriptObject ArgValue1 { get { return arg1; } }

        public IScriptObject ArgValue2 { get { return arg2; } }

        public void Cancel() { this.isCancelled = true; }

        public bool IsCancelled { get { return isCancelled; } }
    }

    public delegate IScriptObject OperatorExecuteHandler(ScriptContext context, OperatorExecuteArgs args);

    internal abstract class ElementBase
    {
        private ResultVisitFlag resultVisit;
        private ElementBase next, prev;

        public ResultVisitFlag ResultVisit
        {
            get { return resultVisit; }
            set { resultVisit = value; }
        }

        public ElementBase Next
        {
            get { return next; }
            set { next = value; }
        }

        public ElementBase Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        internal virtual ResultVisitFlag ArgusResultVisit
        {
            get { return ResultVisitFlag.None; }
        }

        internal virtual void Execute(ScriptContext context)
        {
            IScriptObject value = InternalGetValue(context);
            CheckPushValue(context, value);
            context.MoveNext();
        }

        internal void CheckPushValue(ScriptContext context, IScriptObject value)
        {
            if (resultVisit != ResultVisitFlag.None) context.CurrentContext.PushVariable(value);
        }

        protected virtual IScriptObject InternalGetValue(ScriptContext context) { return ScriptUndefined.Instance; }

        public override string ToString()
        {
            string desc = GetDescription();
            if (resultVisit != ResultVisitFlag.None)
                return string.Format("{0} ----{1}", desc, Enum.GetName(typeof(ResultVisitFlag), resultVisit));
            return desc;
        }

        internal virtual void AddOtherDescriptions(ElementDescList list) { }

        internal abstract string GetDescription();

        internal abstract bool AllowGetLastResult { get; }
    }

    internal class IgnoreElement : ElementBase
    {
        internal int ArgusCounter;

        internal override bool AllowGetLastResult { get { return ArgusCounter > 0; } }

        internal override void Execute(ScriptContext context)
        {
            IScriptObject last = null;
            if (ArgusCounter > 0)
                for (int i = 0; i < ArgusCounter; i++)
                    last = context.CurrentContext.PopVariable();
            CheckPushValue(context, last);
            context.MoveNext();
        }

        internal override string GetDescription()
        {
            return "[Ignore], ArgusCounter:" + ArgusCounter;
        }
    }

    internal class JumpElement : ElementBase
    {
        internal ElementBase GotoPointer;
        internal JumpNode Path;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            context.Jump(Path, GotoPointer);
        }

        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(GotoPointer, "Goto");
        }

        internal override string GetDescription()
        {
            return "[Jump]";
        }
    }

    internal class JumpNode
    {
        internal const int TYPE_TryFinally = 1, TYPE_Switch = 2;
        internal JumpNode Parent;
        internal int Type;  //1-try...finally，2-switch

        internal JumpNode(int type)
        {
            this.Type = type;
        }
    }

    internal abstract class OperandElement : ElementBase
    {

    }

    internal class ConstElement : OperandElement
    {
        private ScriptObjectBase value;

        public ConstElement(ScriptObjectBase value) { this.value = value; }

        public ScriptObjectBase Value { get { return value; } }

        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return value;
        }

        internal override string GetDescription()
        {
            return "[Const]: " + value;
        }
    }

    internal class VariableElement : OperandElement
    {
        private ScriptContext evalContext;
        private int varIndex = -1, varIndex2 = -1;
        private ScriptFieldInfo fieldInfo, fieldInfo2;
        private string name;

        public VariableElement(ScriptContext evalContext, string name, int varIndex)
        {
            this.name = name;
            this.varIndex = varIndex;
            if (this.varIndex == -2) this.evalContext = evalContext;
        }

        private ScriptFieldInfo GetFieldInfo(ScriptContext context, bool isVariable2)
        {
            if (isVariable2)
            {
                if (varIndex2 >= 0)
                    return context.GetFieldInfo(varIndex2, true, name);
                else if (varIndex2 == -2 && evalContext == context)
                {
                    if (fieldInfo2 == null) fieldInfo2 = new ScriptFieldInfo(context, true, name);
                    return fieldInfo2;
                }
            }
            else
            {
                if (varIndex >= 0)
                    return context.GetFieldInfo(varIndex, true, name);
                else if (varIndex == -2 && evalContext == context)
                {
                    if (fieldInfo == null) fieldInfo = new ScriptFieldInfo(context, true, name);
                    return fieldInfo;
                }
            }
            return null;
        }

        internal void SetVarIndex2(int index) { this.varIndex2 = index; }

        public string Name { get { return name; } }

        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            switch (ResultVisit)
            {
                case ResultVisitFlag.Get:
                    {
                        ScriptFieldInfo info = GetFieldInfo(context, false);
                        if (info != null)
                            return context.CurrentContext.GetValue(info);
                        return context.CurrentContext.GetValue(context, name);
                    }
                case ResultVisitFlag.Set:
                    {
                        ScriptFieldInfo info = GetFieldInfo(context, false);
                        if (info != null)
                            return new ScriptAssignObject(context.CurrentContext, info);
                        return new ScriptAssignObject(context.CurrentContext, name);
                    }
                case ResultVisitFlag.GetSet:
                    {
                        ScriptFieldInfo info = GetFieldInfo(context, false);
                        ScriptFieldInfo info2 = GetFieldInfo(context, true);
                        if (info != null && info2 != null)
                            return new ScriptAssignObject(context.CurrentContext, info, info2);
                        return new ScriptAssignObject(context.CurrentContext, name);
                    }
            }
            return base.InternalGetValue(context);
        }

        internal override string GetDescription()
        {
            return "[Variable]: " + name;
        }
    }

    internal class ThisElement : OperandElement
    {
        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            ScriptExecuteContext p = context.CurrentContext;
            IScriptObject result = null;
            do
            {
                result = p.ThisObject;
                if (result != null) break;
                p = p.Parent as ScriptExecuteContext;
            } while (p != null);
            return result;
        }

        internal override string GetDescription()
        {
            return "[This]";
        }
    }

    internal abstract class OperatorElementBase : ElementBase
    {
        private bool isOtherArgus;
        public abstract OperatorType Type { get; }

        internal override bool AllowGetLastResult { get { return true; } }

        internal static OperatorElementBase Create(OperatorType optType, bool isAfter)
        {
            if (optType < OperatorType.LogicOr) return new AssignElement(optType);
            switch (optType)
            {
                case OperatorType.LogicOr:
                case OperatorType.LogicAnd: return new LogicFirstElement(optType == OperatorType.LogicOr);
                case OperatorType.New: return new InvokeMethodElement(true);
                case OperatorType.InvokeMethod: return new InvokeMethodElement(false);
                case OperatorType.GetArrayMember: return new GetArrayMemberElement();
                case OperatorType.GetObjectMember: return new GetObjectMemberElement();
                case OperatorType.Increment: return new IncrementElement(isAfter, true);
                case OperatorType.Decrement: return new IncrementElement(isAfter, false);
                case OperatorType.Delete: return new DeleteElement();
                default:
                    return new OperatorElement(optType);
            }
        }

        internal ResultVisitFlag GetArgusResultVisit(bool setFlag)
        {
            OperatorType type = Type;
            if (type <= OperatorType.UnsignedShiftRightAssign)
            {
                if (!isOtherArgus)
                {
                    if (setFlag) isOtherArgus = true;
                    return type == OperatorType.Assign ? ResultVisitFlag.Set : ResultVisitFlag.GetSet;
                }
            }
            else
            {
                switch (type)
                {
                    case OperatorType.Increment:
                    case OperatorType.Decrement:
                        return ResultVisitFlag.GetSet;
                    case OperatorType.Delete:
                        return ResultVisitFlag.Set;
                }
            }
            return ResultVisitFlag.Get;
        }

        internal override ResultVisitFlag ArgusResultVisit
        {
            get
            {
                return GetArgusResultVisit(true);
            }
        }

        internal override string GetDescription()
        {
            return "[Operator]: " + Enum.GetName(typeof(OperatorType), Type);
        }
    }

    internal class OperatorElement : OperatorElementBase
    {
        private OperatorType type;

        public OperatorElement(OperatorType type)
        {
            this.type = type;
        }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            OperatorType t = type;
            IScriptObject arg1 = context.CurrentContext.PopVariable();
            IScriptObject arg2;
            if (type < OperatorType.Increment)
            {
                arg2 = arg1;
                arg1 = context.CurrentContext.PopVariable();
            }
            else
                arg2 = null;
            return ScriptHelper.Compute(context, type, arg1, arg2);
        }

        public override OperatorType Type { get { return type; } }
    }

    internal class AssignElement : OperatorElementBase
    {
        private readonly static OperatorType[] OptTypes = new OperatorType[]
        {
            OperatorType.None, OperatorType.None
            , OperatorType.BitOr, OperatorType.BitXOr, OperatorType.BitAnd
            , OperatorType.Add, OperatorType.Substract, OperatorType.Multiply, OperatorType.Divide, OperatorType.Modulus
            , OperatorType.ShiftLeft, OperatorType.ShiftRight, OperatorType.UnsignedShiftRight
        };
        private OperatorType type;

        public AssignElement(OperatorType type)
        {
            this.type = type;
        }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject arg2 = context.CurrentContext.PopVariable();
            IScriptAssignObject obj = context.CurrentContext.PopVariable() as IScriptAssignObject;
            if (type > OperatorType.Assign)
            {
                IScriptObject arg1 = obj.GetFieldValue2(context);
                OperatorType t = OptTypes[(int)type];
                arg2 = ScriptHelper.Compute(context, t, arg1, arg2);
            }
            obj.SetFieldValue(context, arg2);
            return arg2;
        }

        public override OperatorType Type { get { return type; } }
    }

    internal class InvokeMethodElement : OperatorElementBase
    {
        private bool isNewObject;
        private int argCount;

        public InvokeMethodElement(bool isNewObject) { this.isNewObject = isNewObject; }

        public override OperatorType Type { get { return isNewObject ? OperatorType.New : OperatorType.InvokeMethod; } }

        public int ArgCount
        {
            get { return argCount; }
            set { argCount = value; }
        }

        internal override void Execute(ScriptContext context)
        {
            IScriptObject[] argus = new IScriptObject[argCount];
            for (int i = argCount - 1; i >= 0; i--)
                argus[i] = context.CurrentContext.PopVariable();
            IScriptObject obj = context.CurrentContext.PopVariable();
            IScriptObject instance;
            ScriptMemberProxy memberObj = obj as ScriptMemberProxy;
            if (memberObj != null)
            {
                instance = memberObj.Instance;
                obj = memberObj.Member;
            }
            else
                instance = null;
            ScriptFunctionBase func = obj as ScriptFunctionBase;
            if (func != null)
            {
                ScriptExecuteContext curContext = context.CurrentContext;
                IScriptObject result;
                int invokeFlag;
                context.BeginInvokeEnabled();
                try
                {
                    result = func.Invoke(context, true, isNewObject, instance, argus);
                }
                finally
                {
                    invokeFlag = context.EndInvokeEnabled();
                }
                if (invokeFlag == 0)
                {
                    CheckPushValue(context, result);
                    context.MoveNext();
                }
            }
            else
                throw new ScriptExecuteException(string.Format("对象“{0}”无法做为方法执行。", obj.ToValueString(context)));
        }

        internal override string GetDescription()
        {
            return base.GetDescription() + ", ArgCount:" + argCount;
        }
    }

    internal class GetArrayMemberElement : OperatorElementBase
    {
        public override OperatorType Type { get { return OperatorType.GetArrayMember; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject indexObj = context.CurrentContext.PopVariable();
            IScriptObject obj = context.CurrentContext.PopVariable();
            IScriptArray array = obj as IScriptArray;
            object indexValue = indexObj.ToValue(context);
            string name = null;
            int index;
            if (array != null && array.IsArray && ScriptHelper.TryParseIndex(indexValue, out name, out index))
            {
                switch(ResultVisit)
                {
                    case ResultVisitFlag.Get:
                        return array.GetElementValue(context, index);
                    case ResultVisitFlag.Set:
                    case ResultVisitFlag.GetSet:
                        return new ScriptArrayAssignObject(array, index);
                    case ResultVisitFlag.ObjectMember:
                        return new ScriptMemberProxy(obj, array.GetElementValue(context, index));
                }
            }
            else
            {
                if (name == null && indexValue != null) name = indexValue.ToString();
                switch (ResultVisit)
                {
                    case ResultVisitFlag.Get:
                        return obj.GetValue(context, name);
                    case ResultVisitFlag.Set:
                    case ResultVisitFlag.GetSet:
                        return new ScriptAssignObject(obj, name);
                    case ResultVisitFlag.ObjectMember:
                        return new ScriptMemberProxy(obj, obj.GetValue(context, name));
                }
            }
            return base.InternalGetValue(context);
        }
    }

    internal class GetObjectMemberElement : OperatorElementBase
    {
        private ScriptContext evalContext;
        private int varIndex = -1, varIndex2 = -1;
        private ScriptFieldInfo fieldInfo, fieldInfo2;
        private string name;
        
        internal void SetVarIndex(ScriptContext evalContext, int varIndex)
        {
            this.varIndex = varIndex;
            if (this.varIndex == -2) this.evalContext = evalContext;
        }

        internal void SetVarIndex2(int index) { this.varIndex2 = index; }

        public override OperatorType Type { get { return OperatorType.GetObjectMember; } }

        private ScriptFieldInfo GetFieldInfo(ScriptContext context, bool isVariable2)
        {
            if (isVariable2)
            {
                if (varIndex2 >= 0)
                    return context.GetFieldInfo(varIndex2, false, name);
                else if (varIndex2 == -2 && evalContext == context)
                {
                    if (fieldInfo2 == null) fieldInfo2 = new ScriptFieldInfo(context, false, name);
                    return fieldInfo2;
                }
            }
            else
            {
                if (varIndex >= 0)
                    return context.GetFieldInfo(varIndex, false, name);
                else if (varIndex == -2 && evalContext == context)
                {
                    if (fieldInfo == null) fieldInfo = new ScriptFieldInfo(context, false, name);
                    return fieldInfo;
                }
            }
            return null;
        }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject obj = context.CurrentContext.PopVariable();
            switch (ResultVisit)
            {
                case ResultVisitFlag.Get:
                    {
                        ScriptObjectBase scriptObj = obj as ScriptObjectBase;
                        if (scriptObj != null)
                        {
                            ScriptFieldInfo info = GetFieldInfo(context, false);
                            if (info != null)
                                return scriptObj.GetValue(info);
                        }
                        return obj.GetValue(context, name);
                    }
                case ResultVisitFlag.Set:
                    {
                        ScriptObjectBase scriptObj = obj as ScriptObjectBase;
                        if (scriptObj != null)
                        {
                            ScriptFieldInfo info = GetFieldInfo(context, false);
                            if (info != null)
                                return new ScriptAssignObject(obj, info);
                        }
                        return new ScriptAssignObject(context.CurrentContext, name);
                    }
                case ResultVisitFlag.GetSet:
                    {
                        ScriptObjectBase scriptObj = obj as ScriptObjectBase;
                        if (scriptObj != null)
                        {
                            ScriptFieldInfo info = GetFieldInfo(context, false);
                            ScriptFieldInfo info2 = GetFieldInfo(context, true);
                            if (info != null && info2 != null)
                                return new ScriptAssignObject(obj, info, info2);
                        }
                        return new ScriptAssignObject(context.CurrentContext, name);
                    }
                case ResultVisitFlag.ObjectMember:
                    {
                        ScriptObjectBase scriptObj = obj as ScriptObjectBase;
                        IScriptObject member;
                        ScriptFieldInfo info = scriptObj != null ? GetFieldInfo(context, false) : null;
                        if (info != null)
                            member = scriptObj.GetValue(info);
                        else
                            member = obj.GetValue(context, name);
                        return new ScriptMemberProxy(obj, member);
                    }
            }
            return base.InternalGetValue(context);
        }

        public string Name
        {
            get { return name; }
            internal set { name = value; }
        }

        internal override string GetDescription()
        {
            return string.Format("[Operator]: GetObjectMember({0})", name);
        }
    }

    internal class IncrementElement : OperatorElementBase
    {
        private bool isAfter;
        private bool isIncrement;

        public IncrementElement(bool isAfter, bool isIncrement)
        {
            this.isAfter = isAfter;
            this.isIncrement = isIncrement;
        }

        public override OperatorType Type { get { return isIncrement ? OperatorType.Increment : OperatorType.Decrement; } }

        public bool IsAfter { get { return isAfter; } set { isAfter = value; } }
        public bool IsIncrement { get { return isIncrement; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            IScriptAssignObject assignObj = (IScriptAssignObject)value;
            value = assignObj.GetFieldValue2(context);
            ScriptInteger i = value as ScriptInteger;
            if (i != null)
            {
                long iv;
                if (isIncrement)
                    iv = i.IntegerValue + 1;
                else
                    iv = i.IntegerValue - 1;
                ScriptNumber i2 = ScriptNumber.Create(iv);
                assignObj.SetFieldValue(context, i2);
                return isAfter ? i : i2;
            }
            ScriptDecimal d = value as ScriptDecimal;
            if (d != null)
            {
                decimal dv;
                if (isIncrement)
                    dv = d.DecimalValue + 1;
                else
                    dv = d.DecimalValue - 1;
                ScriptNumber d2 = ScriptNumber.Create(dv);
                assignObj.SetFieldValue(context, d2);
                return isAfter ? d : d2;
            }
            assignObj.SetFieldValue(context, ScriptNumber.NaN);
            return ScriptNumber.NaN;
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.GetSet; } }

        internal override string GetDescription()
        {
            return base.GetDescription() + ", IsAfter: " + isAfter + ", IsIncrement: " + isIncrement;
        }
    }

    internal class LogicFirstElement : OperatorElementBase
    {
        private bool isOr;
        internal LogicSecondElement SecondPointer;

        public LogicFirstElement(bool isOr) { this.isOr = isOr; }

        public bool IsOr { get { return isOr; } }

        public override OperatorType Type { get { return isOr ? OperatorType.LogicOr : OperatorType.LogicAnd; } }

        internal override void Execute(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            bool b = ScriptHelper.IsTrue(value);
            if (isOr == b)
            {
                context.CurrentContext.PushVariable(value);
                context.MoveTo(SecondPointer);
            }
            else
                context.MoveNext();
        }

        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(SecondPointer, "LogicSecond");
        }

        internal override string GetDescription()
        {
            return string.Format("[Logic {0} First]", isOr ? "Or" : "And");
        }
    }

    internal class LogicSecondElement : OperatorElementBase
    {
        private bool isOr;

        public LogicSecondElement(bool isOr) { this.isOr = isOr; }

        public override OperatorType Type { get { return isOr ? OperatorType.LogicOr : OperatorType.LogicAnd; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return context.CurrentContext.PopVariable();
        }

        internal override string GetDescription()
        {
            return string.Format("[Logic {0} Second]", isOr ? "Or" : "And");
        }
    }

    internal class DeleteElement : OperatorElementBase
    {
        public override OperatorType Type { get { return OperatorType.Delete; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            IScriptAssignObject assignObj = (IScriptAssignObject)value;
            assignObj.RemoveField(context);
            return ScriptBoolean.True;
        }

        internal override string GetDescription()
        {
            return "[Delete]";
        }
    }

    internal class FunctionElement : ElementBase
    {
        internal DefineContext Context;

        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return new ScriptFunction(this.Context, context.CurrentContext);
        }

        internal override string GetDescription()
        {
            return "[Function]";
        }
    }

    internal class ObjectStartElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return false; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return new ScriptObject();
        }

        internal override string GetDescription()
        {
            return "[Object Start]";
        }
    }

    internal class ObjectFieldElement : ElementBase
    {
        private string field;

        internal override bool AllowGetLastResult { get { return false; } }

        public ObjectFieldElement(string field) { this.field = field; }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            ScriptObject obj = (ScriptObject)context.CurrentContext.PeekVariable();
            obj.SetValue(context, field, value);
            return null;
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Object Field]: " + field;
        }
    }

    internal class ObjectEndElement : ElementBase
    {
        internal long ObjectId;

        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            ScriptObject result = (ScriptObject)context.CurrentContext.PopVariable();
            result.ObjectId = ObjectId;
            return result;
        }

        internal override string GetDescription()
        {
            return "[Object End]";
        }
    }

    internal class ArrayStartElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return false; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return new ScriptArray();
        }

        internal override string GetDescription()
        {
            return "[Array Start]";
        }
    }

    internal class ArrayItemElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return false; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            ScriptArray array = (ScriptArray)context.CurrentContext.PeekVariable();
            array.Push(value);
            return null;
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Array Item]";
        }
    }

    internal class ArrayEndElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return true; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            return context.CurrentContext.PopVariable();
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Array End]";
        }
    }

    internal class RegExprElement : ElementBase
    {
        public const int TYPE_Global = 1, TYPE_IgnoreCase = 2, TYPE_MultiLine = 4;
        private string text;
        private int type;

        internal override bool AllowGetLastResult { get { return true; } }

        public RegExprElement(int type, string text)
        {
            this.type = type;
            this.text = text;
        }

        public int Type { get { return type; } }
        public string Text { get { return text; } }

        internal override string GetDescription()
        {
            return "[RegExpr]";
        }
    }

    internal class CheckElement : ElementBase
    {
        internal ElementBase TruePointer;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            if (ScriptHelper.IsTrue(value))
                context.MoveTo(TruePointer);
            else
                context.MoveNext();
        }
        
        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Check]";
        }

        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(TruePointer, "True");
        }
    }

    internal class CaseElement : ElementBase
    {
        internal ElementBase EqualPointer;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            IScriptObject value2 = context.CurrentContext.PopVariable();
            IScriptObject value = context.CurrentContext.PeekVariable();
            if (ScriptHelper.EqualsValue(value, value2, true))
                context.MoveTo(EqualPointer);
            else
                context.MoveNext();
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Case]";
        }

        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(EqualPointer, "Equal");
        }
    }

    internal class EnumInitElement : ElementBase
    {
        private bool isKey;

        public EnumInitElement(bool isKey) { this.isKey = isKey; }

        internal override bool AllowGetLastResult { get { return false; } }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            IScriptObject value = context.CurrentContext.PopVariable();
            return new ScriptObjectEnumerator(value.GetEnumerator(context, isKey));
        }

        internal override string GetDescription()
        {
            return "[Enum Init]";
        }
    }

    internal class EnumEachElement : ElementBase
    {
        private ScriptContext evalContext;
        private int varIndex;
        private ScriptFieldInfo fieldInfo;
        private string varName;
        internal ElementBase AvailablePointer;

        public EnumEachElement(ScriptContext evalContext, string varName, int varIndex)
        {
            this.varName = varName;
            this.varIndex = varIndex;
            if (this.varIndex == -2) this.evalContext = evalContext;
        }

        internal override bool AllowGetLastResult { get { return false; } }

        private ScriptFieldInfo GetFieldInfo(ScriptContext context)
        {
            if (varIndex >= 0)
                return context.GetFieldInfo(varIndex, true, varName);
            else if (varIndex == -2 && evalContext == context)
            {
                if (fieldInfo == null) fieldInfo = new ScriptFieldInfo(context, true, varName);
                return fieldInfo;
            }
            return null;
        }

        internal override void Execute(ScriptContext context)
        {
            ScriptObjectEnumerator en = (ScriptObjectEnumerator)context.CurrentContext.PeekVariable();
            if (en.MoveNext())
            {
                IScriptObject value = en.GetCurrentKey(context);
                ScriptFieldInfo info = GetFieldInfo(context);
                if (info != null)
                    context.CurrentContext.SetValue(info, value);
                else
                    context.CurrentContext.SetValue(context, varName, value);
                context.MoveTo(AvailablePointer);
            }
            else
                context.MoveNext();
        }

        internal override string GetDescription()
        {
            return "[Enum Each]";
        }

        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(AvailablePointer, "Available");
        }
    }

    internal class TryStartElement : ElementBase
    {
        internal ElementBase CatchPointer;
        internal ElementBase FinallyPointer;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            context.CurrentContext.PushTryBlock(this);
            context.MoveNext();
        }

        internal override string GetDescription()
        {
            return "[Try Start]";
        }
        internal override void AddOtherDescriptions(ElementDescList list)
        {
            base.AddOtherDescriptions(list);
            list.AddList(CatchPointer, "Catch");
            list.AddList(FinallyPointer, "Finally");
        }
    }

    internal class CatchStartElement : ElementBase
    {
        private string varName;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            context.CurrentContext.ResetTryBlock(TryCatchBlock.STEP_Catch, context.Error.Value);
            context.Error = null;
            context.MoveNext();
        }

        public string VarName
        {
            get { return varName; }
            set { varName = value; }
        }

        internal override string GetDescription()
        {
            return "[Catch Start], VarName:" + varName;
        }
    }

    internal class CatchVariableElement : ElementBase
    {
        private CatchStartElement catchElement;

        internal override bool AllowGetLastResult { get { return false; } }

        public CatchVariableElement(CatchStartElement catchElem) { this.catchElement = catchElem; }

        protected override IScriptObject InternalGetValue(ScriptContext context)
        {
            TryCatchBlock block = context.CurrentContext.GetTryBlockByCatchElement(catchElement);
            return block.CatchVariable;
        }

        internal override string GetDescription()
        {
            return "[Catch Variable]";
        }
    }

    internal class FinallyStartElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            context.CurrentContext.ResetTryBlock(TryCatchBlock.STEP_Finally, null);
            context.MoveNext();
        }

        internal override string GetDescription()
        {
            return "[Finally Start]";
        }
    }

    internal class TryEndElement : ElementBase
    {
        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            context.CurrentContext.ResetTryBlock(TryCatchBlock.STEP_End, null);
            context.DoTryEnd();
        }

        internal override string GetDescription()
        {
            return "[Try End]";
        }
    }

    internal class ThrowElement : ElementBase
    {
        internal bool HasError;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override void Execute(ScriptContext context)
        {
            if (HasError)
            {
                IScriptObject error = context.CurrentContext.PopVariable();
                throw new ScriptExecuteException(error);
            }
            else
                throw new ScriptExecuteException(ScriptNull.Instance);
        }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override string GetDescription()
        {
            return "[Throw]";
        }
    }

    internal class ReturnElement : ElementBase
    {
        internal bool HasResult;

        internal override bool AllowGetLastResult { get { return false; } }

        internal override ResultVisitFlag ArgusResultVisit { get { return ResultVisitFlag.Get; } }

        internal override void Execute(ScriptContext context)
        {
            context.CurrentContext.Result = HasResult ? context.CurrentContext.PopVariable() : ScriptUndefined.Instance;
            context.MoveTo(null);
        }

        internal override string GetDescription()
        {
            return "[Return]";
        }
    }

    internal class DebuggerElement : ElementBase
    {
        internal override void Execute(ScriptContext context)
        {
            context.MoveNext();
        }

        internal override bool AllowGetLastResult { get { return false; } }

        internal override string GetDescription()
        {
            return "[Debugger]";
        }
    }

    #endregion

}
