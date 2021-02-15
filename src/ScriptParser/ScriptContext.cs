using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ShenGu.Script
{
    public static class ScriptGlobal
    {
        private static Dictionary<Type, ScriptTypeMembers> dicMembers = new Dictionary<Type, ScriptTypeMembers>();
        private static ReaderWriterLock dicLock = new ReaderWriterLock();
        private static readonly Type ObjectType = typeof(object);
        private static ScriptObjectConvertHandler objectConverter;

        static ScriptGlobal()
        {
            RegisterType(typeof(DataTableProxy));
            RegisterType(typeof(DataRowProxy));
        }
        
        public static void RegisterType(Type type)
        {
            ScriptTypeMembers members = ScriptTypeMembers.Load(type);
            dicLock.AcquireWriterLock(-1);
            try
            {
                dicMembers[members.Type] = members;
            }
            finally
            {
                dicLock.ReleaseWriterLock();
            }
        }

        public static void RegisterTypes(params Type[] types)
        {
            int count = types != null ? types.Length : 0;
            if (count > 0)
            {
                ScriptTypeMembers[] typeMembers = new ScriptTypeMembers[count];
                for(int i = 0; i < count; i++)
                {
                    Type t = types[i];
                    if (t != null)
                        typeMembers[i] = ScriptTypeMembers.Load(t);
                }
                dicLock.AcquireWriterLock(-1);
                try
                {
                    for(int i = 0; i < count; i++)
                    {
                        ScriptTypeMembers m = typeMembers[i];
                        if (m != null)
                            dicMembers[m.Type] = m;
                    }
                }
                finally
                {
                    dicLock.ReleaseWriterLock();
                }
            }
        }

        public static void RegisterType(Type type, IScriptMemberList typeMembers, IScriptMemberList instanceMembers)
        {
            ScriptTypeMembers members = new ScriptTypeMembers(type, null, typeMembers, instanceMembers);
            dicLock.AcquireWriterLock(-1);
            try
            {
                dicMembers[type] = members;
            }
            finally
            {
                dicLock.ReleaseWriterLock();
            }
        }

        internal static ScriptTypeMembers InternalGetMembers(Type type)
        {
            ScriptTypeMembers result;
            if (type == null || type == ObjectType) result = null;
            else
            {
                bool exist;
                dicLock.AcquireReaderLock(-1);
                try
                {
                    exist = dicMembers.TryGetValue(type, out result);
                }
                finally
                {
                    dicLock.ReleaseReaderLock();
                }
                if (!exist && ScriptTypeMembers.IsScriptType(type))
                {
                    ScriptTypeMembers mem = ScriptTypeMembers.Load(type);
                    dicLock.AcquireWriterLock(-1);
                    try
                    {
                        if (!dicMembers.TryGetValue(type, out result))
                            dicMembers[type] = result = mem;
                    }
                    finally
                    {
                        dicLock.ReleaseWriterLock();
                    }
                }
            }
            return result;
        }

        public static IScriptMemberList GetInstanceMembers(ScriptContext context, Type type)
        {
            if (context != null) return context.GetInstanceMembers(type);
            ScriptTypeMembers mem = InternalGetMembers(type);
            if (mem == null) throw new ScriptExecuteException(string.Format("找不到类型为“{0}”的实例成员列表。", type));
            return mem.InstanceMembers;
        }

        public static IScriptMemberList GetTypeMembers(ScriptContext context, Type type)
        {
            if (context != null) return context.GetTypeMembers(type);
            ScriptTypeMembers mem = InternalGetMembers(type);
            if (mem == null) throw new ScriptExecuteException(string.Format("找不到类型为“{0}”的实例成员列表。", type));
            return mem.TypeMembers;
        }

        private static void AppendString(StringBuilder sb, IScriptObject value, ScriptContext context)
        {
            if (value is ScriptString) sb.Append('"' + ((ScriptString)value).Value + '"');
            else if (value is ScriptUndefined || value is ScriptNull || value is ScriptBoolean || value is ScriptNumber)
                sb.Append(value.ToValueString(context));
            else
            {
                IScriptArray array = value as IScriptArray;
                if (array != null && array.IsArray)
                {
                    int arrayLength = array.ArrayLength;
                    sb.Append('[');
                    for (int i = 0; i < arrayLength; i++)
                    {
                        if (i > 0) sb.Append(',');
                        AppendString(sb, array.GetElementValue(context, i), context);
                    }
                    sb.Append(']');
                }
                else
                {
                    sb.Append('{');
                    bool isFirst = true;
                    IEnumerator en = value.GetEnumerator(context, true);
                    while (en.MoveNext())
                    {
                        object obj = en.Current;
                        string key;
                        if (obj is IScriptObject)
                            key = ScriptGlobal.ConvertValue(context, (IScriptObject)obj, typeof(string)) as string;
                        else
                            key = obj != null ? obj.ToString() : null;

                        if (key != null)
                        {
                            if (isFirst) isFirst = false;
                            else sb.Append(',');
                            sb.Append(key);
                            sb.Append(':');
                            IScriptObject v = value.GetValue(context, key);
                            AppendString(sb, v, context);
                        }
                    }
                    sb.Append('}');
                }
            }
        }
        
        public static string Stringify(ScriptContext context, IScriptObject value)
        {
            StringBuilder sb = new StringBuilder();
            AppendString(sb, value, context);
            return sb.ToString();
        }

        public static IScriptObject ConvertValue(ScriptContext context, object value)
        {
            if (value == null) return ScriptNull.Instance;
            if (value is IScriptObject) return (IScriptObject)value;
            if (value is IScriptObject[]) return new ScriptArray((IScriptObject[])value);
            if (value is Type) return new ScriptType(context, (Type)value);
            if (value is Delegate) return new ScriptNativeDelegate((Delegate)value);
            Type type = value.GetType();
            if (type.IsArray)
                return new ScriptNativeArray((Array)value);
            TypeCode tcode = Type.GetTypeCode(type);
            switch (tcode)
            {
                case TypeCode.DBNull: return ScriptNull.Instance;
                case TypeCode.Boolean: return ScriptBoolean.Create((bool)value);
                case TypeCode.String: return ScriptString.Create((string)value);
                case TypeCode.Char: return ScriptString.Create(new string((char)value, 1));
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return ScriptNumber.Create(Convert.ToInt64(value));
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return ScriptNumber.Create(Convert.ToDecimal(value));
                case TypeCode.DateTime:
                    return ScriptDate.Create((DateTime)value);
            }
            if (context == null) context = ScriptContext.Current;
            IScriptMemberList members = GetInstanceMembers(context, type);
            IScriptMemberSupportProxy m = members as IScriptMemberSupportProxy;
            if (m != null && m.ProxyType != null)
            {
                if (type == m.ProxyType)
                {
                    IScriptProxy proxy = (IScriptProxy)value;
                    proxy.RealInstance = Activator.CreateInstance(m.RealType);
                }
                else
                {
                    IScriptProxy proxy = (IScriptProxy)Activator.CreateInstance(m.ProxyType);
                    proxy.RealInstance = value;
                    value = proxy;
                }
            }
            if (value is IScriptObject) return (IScriptObject)value;
            return ScriptNativeObject.CreateNativeObject(context, value, members);
        }

        private static object ToDefaultValue(Type toType)
        {
            TypeCode tcode = Type.GetTypeCode(toType);
            switch (tcode)
            {
                case TypeCode.DBNull: return DBNull.Value;
                case TypeCode.Char: return '\0';
                case TypeCode.Byte: return (byte)0;
                case TypeCode.Int16: return (short)0;
                case TypeCode.UInt16: return (ushort)0;
                case TypeCode.Int32: return 0;
                case TypeCode.UInt32: return (uint)0;
                case TypeCode.Int64: return 0L;
                case TypeCode.UInt64: return (ulong)0;
                case TypeCode.Single: return 0f;
                case TypeCode.Double: return 0d;
                case TypeCode.Decimal: return 0m;
                case TypeCode.DateTime: return DateTime.MinValue;
                default: return null;
            }
        }

        public static object ConvertValue(ScriptContext context, IScriptObject obj, Type toType)
        {
            bool isObject = toType == typeof(object);
            if (!isObject && toType.IsInstanceOfType(obj)) return obj;
            if (IsNull(obj)) return ToDefaultValue(toType);
            if (context == null) context = ScriptContext.Current;
            if (isObject) return obj.ToValue(context);
            if (typeof(string).IsAssignableFrom(toType)) return obj.ToValueString(context);
            if (toType.IsArray)
            {
                IScriptArray array = obj as IScriptArray;
                if (array != null && array.IsArray)
                {
                    Type elemType = toType.GetElementType();
                    int length = array.ArrayLength;
                    Array to = Array.CreateInstance(toType.GetElementType(), length);
                    for (int i = 0; i <length; i++)
                    {
                        IScriptObject scriptItem = array.GetElementValue(context, i);
                        object item = ConvertValue(context, scriptItem, elemType);
                        to.SetValue(item, i);
                    }
                    return to;
                }
            }
            object result = obj.ToValue(context);
            if (toType != null)
            {
                if (result == null) result = ToDefaultValue(toType);
                else
                {
                    TypeCode tcode = Type.GetTypeCode(toType);
                    switch (tcode)
                    {
                        case TypeCode.DBNull: return DBNull.Value;
                        case TypeCode.Boolean: return Convert.ToBoolean(result);
                        case TypeCode.Char: return Convert.ToChar(result);
                        case TypeCode.Byte: return Convert.ToByte(result);
                        case TypeCode.Int16: return Convert.ToInt16(result);
                        case TypeCode.UInt16: return Convert.ToUInt16(result);
                        case TypeCode.Int32: return Convert.ToInt32(result);
                        case TypeCode.UInt32: return Convert.ToUInt32(result);
                        case TypeCode.Int64: return Convert.ToInt64(result);
                        case TypeCode.UInt64: return Convert.ToUInt64(result);
                        case TypeCode.Single: return Convert.ToSingle(result);
                        case TypeCode.Double: return Convert.ToDouble(result);
                        case TypeCode.Decimal: return Convert.ToDecimal(result);
                        case TypeCode.DateTime: return Convert.ToDateTime(result);
                    }
                    if (!toType.IsAssignableFrom(result.GetType()))
                        throw new ScriptExecuteException(string.Format("无法将类型“{0}”强制转化成类型“{1}”", result.GetType(), toType));
                }
            }
            return result;
        }

        public static bool IsNull(IScriptObject obj)
        {
            return obj == null || obj is ScriptNull || obj is ScriptUndefined;
        }

        public static bool IsTrue(IScriptObject obj)
        {
            ScriptObjectBase scriptValue = obj as ScriptObjectBase;
            return scriptValue != null && scriptValue.BooleanValue;
        }

        private static int ReadIndex(string template, int index)
        {
            if (index + 2 < template.Length)
            {
                char ch = template[index];
                if (ch >= '0' && ch <= '9')
                {
                    char ch2 = template[index + 1];
                    if (ch > '0' && ch2 >= '0' && ch2 <= '9')
                    {
                        if (index + 3 < template.Length && template[index + 2] == '}')
                            return (ch - '0') * 10 + (ch2 - '0');
                    }
                    else if (ch2 == '}')
                        return ch - '0';
                }
            }
            return -1;
        }

        public static string FormatString(string template, params string[] argus)
        {
            StringBuilder sb = null;
            if (argus != null && argus.Length > 0)
            {
                int index = 0, lastIndex = 0;
                do
                {
                    index = template.IndexOf('{', index);
                    if (index >= 0)
                    {
                        int value = ReadIndex(template, index + 1);
                        if (value >= 0)
                        {
                            if (value >= argus.Length)
                                throw new ArgumentOutOfRangeException("argus", string.Format("格式化错误：索引“{0}”超出了参数数组的长度。", value));
                            if (sb == null)
                            {
                                int length = template.Length;
                                foreach(string str in argus)
                                    if (str != null) length += str.Length;
                                sb = new StringBuilder(length);
                            }
                            if (lastIndex < index)
                                sb.Append(template, lastIndex, index - lastIndex);
                            sb.Append(argus[value]);
                            lastIndex = index + (value < 10 ? 3 : 4);
                            index = lastIndex;
                        }
                        else
                            index++;
                    }
                    else
                    {
                        if (sb != null)
                            sb.Append(template, lastIndex, template.Length - lastIndex);
                        break;
                    }
                } while (true);
            }
            return sb == null ? template : sb.ToString();
        }

        public static ScriptObjectConvertHandler ObjectConverter
        {
            get { return objectConverter; }
            set { objectConverter = value; }
        }
    }

    public delegate object ScriptObjectConvertHandler(ScriptContext context, ScriptObject instance);

    public class ScriptContext
    {
        private static ScriptObjectCreator objectCreator, arrayCreator, stringCreator, numberCreator, dateCreator;
        [ThreadStatic]
        private static ScriptContext current;
        private static ScriptMappingList defaultSystemMappings;
        private ScriptExecuteContext context;
        private Dictionary<string, object> values;
        private long objectId;
        private int contextCount;
        private ScriptFieldInfo[][] fieldInfos;
        private Dictionary<Type, ScriptTypeMembers> dicMembers;
        private ScriptMappingList mappings;
        private ScriptMappingList systemMappings;
        private JumpNode jumpPath;
        private ElementBase jumpGotoPointer;
        private Dictionary<string, object> cacheValues;
        private object thisObject;
        private OperatorExecuteHandler[] optExecutes;
        internal ScriptExecuteContext CurrentContext;
        internal ScriptExecuteException Error;
        internal Thread executingThread;

        #region 构造函数

        static ScriptContext()
        {
            objectCreator = new ScriptObjectCreator(new CreateScriptObjectHandler(CreateObject));
            arrayCreator = new ScriptObjectCreator(new CreateScriptObjectHandler(CreateArray));
            stringCreator = new ScriptObjectCreator(new CreateScriptObjectHandler(CreateString));
            numberCreator = new ScriptObjectCreator(new CreateScriptObjectHandler(CreateNumber));
            dateCreator = new ScriptObjectCreator(new CreateScriptObjectHandler(CreateDate));
        }

        public ScriptContext()
        {
            systemMappings = defaultSystemMappings;
            if (systemMappings == null)
            {
                systemMappings = CreateSystemMappings(true);
                Interlocked.CompareExchange<ScriptMappingList>(ref defaultSystemMappings, systemMappings, null);
                systemMappings = defaultSystemMappings;
            }
        }

        public ScriptContext(ScriptMappingList systemMappings)
        {
            this.systemMappings = systemMappings;
        }

        #endregion

        #region 系统函数

        private static IScriptObject CreateObject(ScriptContext context)
        {
            return new ScriptType(context, typeof(ScriptObject));
        }

        private static IScriptObject CreateArray(ScriptContext context)
        {
            return new ScriptType(context, typeof(ScriptArray));
        }

        private static IScriptObject CreateString(ScriptContext context)
        {
            return new ScriptType(context, typeof(ScriptString));
        }

        private static IScriptObject CreateNumber(ScriptContext context)
        {
            return new ScriptType(context, typeof(ScriptNumber));
        }

        private static IScriptObject CreateDate(ScriptContext context)
        {
            return new ScriptType(context, typeof(ScriptDate));
        }

        [ScriptMapping("parseInt")]
        private static IScriptObject ParseInt(IScriptObject value, ScriptContext context)
        {
            if (ScriptGlobal.IsNull(value)) return ScriptNumber.Create(0);
            if (value is ScriptInteger) return value;
            if (value is ScriptDecimal) return ScriptInteger.Create(((ScriptDecimal)value).IntegerValue);
            string strValue = value == null ? null : value.ToValueString(context);
            if (strValue == null) return ScriptNumber.Create(0);
            long result;
            if (long.TryParse(strValue, out result)) return ScriptNumber.Create(result);
            decimal dec;
            if (decimal.TryParse(strValue, out dec)) return ScriptNumber.Create((long)dec);
            return ScriptNumber.NaN;
        }

        [ScriptMapping("parseFloat")]
        private static IScriptObject ParseFloat(IScriptObject value, ScriptContext context)
        {
            if (value is ScriptDecimal) return value;
            if (value is ScriptInteger) return ScriptDecimal.Create(((ScriptInteger)value).DecimalValue);
            string strValue = value == null ? null : value.ToValueString(context);
            if (strValue == null) return ScriptNumber.Create(0.0M);
            decimal result;
            if (decimal.TryParse(strValue, out result)) return ScriptNumber.Create(result);
            return ScriptNumber.NaN;
        }

        private static void AppendString(StringBuilder sb, IScriptObject value, ScriptContext context)
        {
            if (value is ScriptString) sb.Append('"' + ((ScriptString)value).Value + '"');
            else if (value is ScriptUndefined || value is ScriptNull || value is ScriptBoolean || value is ScriptNumber)
                sb.Append(value.ToValueString(context));
            else
            {
                IScriptArray array = value as IScriptArray;
                if (array != null && array.IsArray)
                {
                    int arrayLength = array.ArrayLength;
                    sb.Append('[');
                    for(int i = 0; i < arrayLength; i++)
                    {
                        if (i > 0) sb.Append(',');
                        AppendString(sb, array.GetElementValue(context, i), context);
                    }
                    sb.Append(']');
                }
                else
                {
                    sb.Append('{');
                    bool isFirst = true;
                    IEnumerator en = value.GetEnumerator(context, true);
                    while(en.MoveNext())
                    {
                        object obj = en.Current;
                        string key;
                        if (obj is IScriptObject)
                            key = ScriptGlobal.ConvertValue(context, (IScriptObject)obj, typeof(string)) as string;
                        else
                            key = obj != null ? obj.ToString() : null;

                        if (key != null)
                        {
                            if (isFirst) isFirst = false;
                            else sb.Append(',');
                            sb.Append(key);
                            sb.Append(':');
                            IScriptObject v = value.GetValue(context, key);
                            AppendString(sb, v, context);
                        }
                    }
                    sb.Append('}');
                }
            }
        }

        [ScriptMapping("stringify")]
        private static IScriptObject Stringify(IScriptObject value, ScriptContext context)
        {
            string str = ScriptGlobal.Stringify(context, value);
            return ScriptGlobal.ConvertValue(context, str);
        }

        [ScriptMapping("eval")]
        private static IScriptObject Eval(IScriptObject value, ScriptContext context)
        {
            string str = value.ToValueString(context);
            if (str != null)
            {
                ElementBase currentElem = context.CurrentContext.Current;
                int flag = 0;
                if (currentElem.ResultVisit == ResultVisitFlag.Get) flag |= 1;
                ScriptParser parser = ScriptParser.ParseForEval(context, str, context.objectId, flag);
                context.objectId = parser.BeginObjectId;
                DefineContext ctx = parser.Context;
                context.SetEvalContext(ctx.First, ctx.Last);
            }
            return null;
        }

        #endregion

        #region 内部控制函数

        private bool InnerMoveCatch()
        {
            bool result = false;
            if (CurrentContext != null)
            {
                do
                {
                    TryCatchBlock block = CurrentContext.PeekTryBlock();
                    if (block == null)
                    {
                        if (CurrentContext == context) break;
                        PopContext();
                    }
                    else
                    {
                        result = block.CheckMoveNext(this, true);
                        if (result) break;
                        CurrentContext.PopTryBlock();
                    }
                } while (true);
            }
            return result;
        }

        internal bool CheckMoveCatch(Exception ex)
        {
            if (jumpPath != null)
            {
                jumpGotoPointer = null;
                jumpPath = null;
            }
            Error = ex as ScriptExecuteException;
            if (Error == null) Error = new ScriptExecuteException(null, ex);
            return InnerMoveCatch();
        }

        private JumpNode ProcessJumpNode(JumpNode node)
        {
            ScriptExecuteContext currentContext = CurrentContext;
            while (node != null)
            {
                if (node.Type == JumpNode.TYPE_TryFinally)
                {
                    TryCatchBlock block = currentContext.PeekTryBlock();
                    if (block.CheckMoveNext(this, false)) break;
                    currentContext.PopTryBlock();
                }
                else if (node.Type == JumpNode.TYPE_Switch)
                    currentContext.PopVariable();
                node = node.Parent;
            }
            return node;
        }

        internal void Jump(JumpNode node, ElementBase gotoPointer)
        {
            if (node != null)
            {
                if (Error != null) Error = null;
                node = ProcessJumpNode(node);
            }
            if (node != null)
            {
                jumpPath = node;
                jumpGotoPointer = gotoPointer;
            }
            else
                MoveTo(gotoPointer);
        }

        internal void DoTryEnd()
        {
            if (jumpPath != null)
            {
                jumpPath = ProcessJumpNode(jumpPath.Parent);
                if (jumpPath == null)
                {
                    MoveTo(jumpGotoPointer);
                    jumpGotoPointer = null;
                }
            }
            else
            {
                if (Error != null && !InnerMoveCatch())
                    throw Error;
                MoveNext();
            }
        }

        internal void MoveNext()
        {
            if (CurrentContext != null)
                CurrentContext.Current = CurrentContext.Current.Next;
        }

        internal void MoveTo(ElementBase elem)
        {
            if (CurrentContext != null)
                CurrentContext.Current = elem;
        }

        internal void Init(int contextCount, long beginObjectId, RootExecuteContext execContext)
        {
            this.contextCount = contextCount;
            this.objectId = beginObjectId;
            this.CurrentContext = this.context = execContext;
            if (this.thisObject != null)
            {
                execContext.ThisObject = ScriptGlobal.ConvertValue(this, this.thisObject);
                this.thisObject = null;
            }
            if (mappings != null) execContext.ResetValueMembers(mappings);
            if (systemMappings != null) execContext.ResetSystemMembers(systemMappings);
            if (values != null)
                foreach (KeyValuePair<string, object> kv in values)
                    this.context.SetValue(null, kv.Key, ScriptGlobal.ConvertValue(this, kv.Value));
            if (this.contextCount > 0)
                this.fieldInfos = new ScriptFieldInfo[this.contextCount][];
        }

        internal void Finish()
        {
            this.Error = null;
            this.jumpPath = null;
            this.jumpGotoPointer = null;
            this.cacheValues = null;
        }

        internal ScriptExecuteContext ResetRootContext(ScriptExecuteContext context)
        {
            ScriptExecuteContext result = this.context;
            this.context = context;
            return result;
        }

        internal void PushContext(ScriptExecuteContext context)
        {
            context.PreviousContext = this.CurrentContext;
            this.CurrentContext = context;
        }

        internal ScriptExecuteContext PopContext()
        {
            ScriptExecuteContext result = this.CurrentContext;
            this.CurrentContext = (ScriptExecuteContext)result.PreviousContext;
            result.PreviousContext = null;
            return result;
        }

        internal long NewObjectId() { return ++objectId; }

        internal ScriptFieldInfo GetFieldInfo(int varIndex, bool isFuncContext, string fieldName)
        {
            int contextIndex = this.CurrentContext.ContextIndex;
            ScriptFieldInfo[] list = this.fieldInfos[contextIndex];
            if (list == null) this.fieldInfos[contextIndex] = list = new ScriptFieldInfo[this.CurrentContext.VariableCount];
            ScriptFieldInfo result = list[varIndex];
            if (result == null) list[varIndex] = result = new ScriptFieldInfo(this, isFuncContext, fieldName);
            return result;
        }

        private int invokeFlag;

        internal void BeginInvokeEnabled()
        {
            invokeFlag = 0;
        }

        internal void SetInvokeContext(ScriptExecuteContext execContext)
        {
            ScriptExecuteContext prevContext = this.CurrentContext;
            execContext.ResultVisit = prevContext.Current.ResultVisit;
            prevContext.Current = prevContext.Current.Next;
            PushContext(execContext);
            this.invokeFlag = 1;
        }

        internal void SetEvalContext(ElementBase first, ElementBase last)
        {
            last.Next = CurrentContext.Current.Next;
            CurrentContext.Current = first;
            this.invokeFlag = 2;
        }

        internal int EndInvokeEnabled()
        {
            int r = invokeFlag;
            invokeFlag = 0;
            return r;
        }

        #endregion

        #region 公共方法/函数

        public IScriptObject Eval(string script)
        {
            if (!string.IsNullOrEmpty(script))
            {
                ScriptParser parser = ScriptParser.ParseForEval(this, script, objectId, 3);
                objectId = parser.BeginObjectId;
                ScriptExecuteContext current = CurrentContext;
                if (current == null)
                {
                    parser.Execute(this);
                    current = CurrentContext;
                }
                else
                {
                    ElementBase currentElem = current.Current;
                    try
                    {
                        current.Current = parser.Context.First;
                        ScriptParser.InnerExecute(this, current);
                    }
                    finally
                    {
                        current.Current = currentElem;
                    }
                }
                return current.Result;
            }
            return null;
        }

        public void RegisterType(Type type)
        {
            ScriptTypeMembers members = ScriptTypeMembers.Load(type);
            if (dicMembers == null) dicMembers = new Dictionary<Type, ScriptTypeMembers>();
            dicMembers[members.Type] = members;
        }

        public void RegisterType(Type type, IScriptMemberList typeMembers, IScriptMemberList instanceMembers)
        {
            ScriptTypeMembers members = new ScriptTypeMembers(type, null, typeMembers, instanceMembers);
            if (dicMembers == null) dicMembers = new Dictionary<Type, ScriptTypeMembers>();
            dicMembers[type] = members;
        }

        public void RegisterTypes(params Type[] types)
        {
            if (types != null && types.Length > 0)
            {
                if (dicMembers == null) dicMembers = new Dictionary<Type, ScriptTypeMembers>();
                foreach(Type t in types)
                {
                    ScriptTypeMembers members = ScriptTypeMembers.Load(t);
                    dicMembers[members.Type] = members;
                }
            }
        }

        public void RegisterOperatorExecutor(OperatorType type, OperatorExecuteHandler handler)
        {
            if (optExecutes == null)
                optExecutes = new OperatorExecuteHandler[(int)OperatorType.InvokeMethod + 1];
            optExecutes[(int)type] = handler;
        }

        public OperatorExecuteHandler GetOperatorExecutor(OperatorType type)
        {
            if (optExecutes != null) return optExecutes[(int)type];
            return null;
        }

        private ScriptTypeMembers InternalGetMembers(Type type)
        {
            ScriptTypeMembers result;
            if (dicMembers == null)
            {
                dicMembers = new Dictionary<Type, ScriptTypeMembers>();
                result = null;
            }
            else dicMembers.TryGetValue(type, out result);
            if(result == null)
            {
                ScriptTypeMembers typeMembers = ScriptGlobal.InternalGetMembers(type);
                if (typeMembers != null)
                    result = new ScriptTypeMembers(typeMembers.Type, typeMembers.ProxyType, new ScriptMemberListProxy(typeMembers.TypeMembers), new ScriptMemberListProxy(typeMembers.InstanceMembers));
                else
                    result = ScriptTypeMembers.Load(type);
                dicMembers[type] = result;
            }
            return result;
        }

        public IScriptMemberList GetTypeMembers(Type type)
        {
            ScriptTypeMembers ms = InternalGetMembers(type);
            return ms.TypeMembers;
        }

        public IScriptMemberList GetInstanceMembers(Type type)
        {
            ScriptTypeMembers ms = InternalGetMembers(type);
            return ms.InstanceMembers;
        }

        public static ScriptContext Current
        {
            get { return current; }
            internal set { current = value; }
        }

        public void AddValue(string name, object value)
        {
            if (values == null) values = new Dictionary<string, object>();
            values.Add(name, value);
        }

        public void RemoveValue(string name)
        {
            if (values != null)
                values.Remove(name);
        }

        public object GetValue(string name)
        {
            object result;
            if (values != null && values.TryGetValue(name, out result)) return result;
            return null;
        }

        public ScriptMappingList Mappings
        {
            get
            {
                if (mappings == null)
                    mappings = new ScriptMappingList();
                return mappings;
            }
        }

        public void AddMappings(Type type)
        {
            Mappings.AddMappings(type);
        }

        public void AddMappings(object instance)
        {
            Mappings.AddMappings(instance);
        }

        public ScriptObjectBase Context
        {
            get { return context; }
        }

        public IScriptObject ScriptResult { get { return context != null ? context.Result : null; } }

        public IScriptObject ScriptThisObject { get { return context != null ? context.ThisObject : null; } }

        public object Result
        {
            get { return context != null && context.Result != null ? context.Result.ToValue(this) : null; }
        }

        public object ThisObject
        {
            get { return context != null && context.ThisObject != null ? context.ThisObject.ToValue(this) : thisObject; }
            set { this.thisObject = value; }
        }

        public object GetCacheValue(string key)
        {
            object result;
            if (cacheValues != null && cacheValues.TryGetValue(key, out result))
                return result;
            return null;
        }

        public void SetCacheValue(string key, object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (cacheValues == null) cacheValues = new Dictionary<string, object>();
            cacheValues[key] = value;
        }

        public static ScriptMappingList CreateSystemMappings(bool useDefaultMappings)
        {
            ScriptMappingList mapping = new ScriptMappingList();
            if (useDefaultMappings)
            {
                mapping.Register("Object", objectCreator);
                mapping.Register("Array", arrayCreator);
                mapping.Register("String", stringCreator);
                mapping.Register("Number", numberCreator);
                mapping.Register("Date", dateCreator);
                mapping.AddMappings(typeof(ScriptContext));
            }
            return mapping;
        }

        #endregion
    }

    class TryCatchBlock
    {
        public TryStartElement TryElement;
        public int VarIndex;
        public int Step;
        public IScriptObject CatchVariable;

        public const int STEP_Try = 0, STEP_Catch = 1, STEP_Finally = 2, STEP_End = 3;

        public bool CheckMoveNext(ScriptContext context, bool checkCatch)
        {
            if (Step != STEP_End) context.CurrentContext.ResetVariable(VarIndex);
            switch (Step)
            {
                case STEP_Try:
                    if (checkCatch && TryElement.CatchPointer != null)
                    {
                        context.MoveTo(TryElement.CatchPointer);
                        return true;
                    }
                    if (TryElement.FinallyPointer != null)
                    {
                        context.MoveTo(TryElement.FinallyPointer);
                        return true;
                    }
                    break;
                case STEP_Catch:
                    if (TryElement.FinallyPointer != null)
                    {
                        context.MoveTo(TryElement.FinallyPointer);
                        return true;
                    }
                    break;
            }
            return false;
        }
    }

    class RootExecuteContext : ScriptExecuteContext
    {
        private ScriptContext context;
        private ScriptMappingList systemMembers;
        private IScriptMemberList valueMembers;

        public RootExecuteContext(ScriptContext context) { this.context = context; }

        internal void ResetSystemMembers(ScriptMappingList systemMembers)
        {
            this.InitSystemMembers(systemMembers);
            this.systemMembers = systemMembers;
        }

        internal void ResetValueMembers(IScriptMemberList members)
        {
            this.InitValueMembers(members);
            this.valueMembers = members;
        }

        protected override int OnFindSystemMember(ScriptContext context, string key)
        {
            int index = base.OnFindSystemMember(context, key);
            if (index >= 0)
            {
                ScriptObjectCreator creator = systemMembers.InternalGetValue(context, index) as ScriptObjectCreator;
                if (creator != null)
                {
                    index = BaseSetValue(context, key, creator.CreateInstance(context));
                    index += systemMembers.Count;
                    if (valueMembers != null) index += valueMembers.Count;
                }
            }
            return index;
        }

        public override IScriptObject ThisObject
        {
            get
            {
                IScriptObject result = base.ThisObject;
                if (result == null) base.ThisObject = result = new ScriptObject();
                return result;
            }
            set
            {
                base.ThisObject = value;
            }
        }
    }

    class ScriptExecuteContext : ScriptObjectBase
    {
        internal ScriptExecuteContext PreviousContext;
        internal int ContextIndex;
        internal int VariableCount;
        internal ElementBase Current;
        internal ResultVisitFlag ResultVisit;
        internal bool IsNewObject;
        private IScriptObject[] varStack;
        private int varStackCount;
        private TryCatchBlock[] tryStack;
        private int tryStackCount;
        private IScriptObject thisObject, result;

        public ScriptExecuteContext() : base(false) { }

        public void PushVariable(IScriptObject obj)
        {
            if (obj == null) obj = ScriptUndefined.Instance;
            if (varStack == null || varStack.Length == varStackCount)
                Array.Resize<IScriptObject>(ref varStack, varStackCount + 16);
            varStack[varStackCount++] = obj;
        }

        public IScriptObject PopVariable()
        {
            if (varStackCount > 0)
            {
                IScriptObject result = varStack[--varStackCount];
                varStack[varStackCount] = null;
                return result;
            }
            return null;
        }

        public IScriptObject PeekVariable()
        {
            if (varStackCount > 0) return varStack[varStackCount - 1];
            return null;
        }

        internal void ResetVariable(int varIndex)
        {
            if (varIndex < varStackCount)
            {
                for (int i = varIndex; i < varStackCount; i++)
                    varStack[i] = null;
                varStackCount = varIndex;
            }
        }

        public void PushTryBlock(TryStartElement elem)
        {
            if (tryStack == null || tryStack.Length == tryStackCount)
                Array.Resize<TryCatchBlock>(ref tryStack, tryStackCount + 4);
            tryStack[tryStackCount++] = new TryCatchBlock() { TryElement = elem, VarIndex = varStackCount };
        }

        public TryCatchBlock PopTryBlock()
        {
            if (tryStackCount > 0)
            {
                TryCatchBlock result = tryStack[--tryStackCount];
                tryStack[tryStackCount] = null;
                return result;
            }
            return null;
        }

        public TryCatchBlock PeekTryBlock()
        {
            return tryStackCount > 0 ? tryStack[tryStackCount - 1] : null;
        }

        public void ResetTryBlock(int step, IScriptObject catchVariable)
        {
            TryCatchBlock block = PeekTryBlock();
            if (block != null)
            {
                block.Step = step;
                if (step == TryCatchBlock.STEP_Catch) block.CatchVariable = catchVariable;
                else if (step == TryCatchBlock.STEP_End) PopTryBlock();
            }
        }

        public TryCatchBlock GetTryBlockByCatchElement(CatchStartElement elem)
        {
            for (int i = tryStackCount - 1; i >= 0; i--)
            {
                TryCatchBlock item = tryStack[i];
                if (item.TryElement.CatchPointer == elem) return item;
            }
            return null;
        }

        public virtual IScriptObject ThisObject
        {
            get { return thisObject; }
            set { this.thisObject = value; }
        }

        public IScriptObject Result
        {
            get { return result == null ? ScriptUndefined.Instance : result; }
            set { result = value; }
        }

        internal override bool IsFuncContext { get { return true; } }

        public override object ToValue(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        public override string ToValueString(ScriptContext context)
        {
            throw new NotImplementedException();
        }
    }

}
