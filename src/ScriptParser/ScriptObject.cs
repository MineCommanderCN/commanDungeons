using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ShenGu.Script
{
    internal class ScriptFieldInfo
    {
        internal readonly ScriptContext Context;
        internal readonly string FieldName;
        internal readonly bool IsFuncContext;
        internal int FieldIndex;
        internal long ObjectId;
        internal int ParentLevel;

        public ScriptFieldInfo(ScriptContext context, bool isFuncContext, string fieldName)
        {
            this.Context = context;
            this.IsFuncContext = isFuncContext;
            this.FieldName = fieldName;
        }

        public long NewObjectId()
        {
            return Context.NewObjectId();
        }
    }

    public interface IScriptEnumerable
    {
        IEnumerator GetEnumerator(ScriptContext context, bool isKey);
    }

    public interface IScriptObject : IScriptEnumerable
    {
        IScriptObject GetValue(ScriptContext context, string name);

        void SetValue(ScriptContext context, string name, IScriptObject value);

        bool Remove(ScriptContext context, string name);

        /// <summary>转成具体的值</summary>
        object ToValue(ScriptContext context);

        /// <summary>获取值的字符串显示</summary>
        string ToValueString(ScriptContext context);

        string TypeName { get; }
    }

    public abstract class ScriptObjectBase : HashEntryList<IScriptObject>, IScriptObject
    {
        private int objectIdFlag;   // 1-新增时重置，2-删除时重置，4-使用内部成员列表的ObjectId
        private bool resetIfDeleted;
        private long objectId;
        private bool reloadSysMembers;
        private IScriptMemberList systemMembers, valueMembers;
        private IScriptObject prototype;
        private bool readOnly;
        internal IScriptObject Parent;

        #region 构造函数

        protected ScriptObjectBase() : this(true, true) { }

        protected ScriptObjectBase(bool useSystemMembers) : this(useSystemMembers, useSystemMembers)
        {
        }

        protected ScriptObjectBase(bool useSystemMembers, bool useMemberObjectId)
        {
            this.reloadSysMembers = useSystemMembers;
            if (useMemberObjectId) this.objectIdFlag = 4;
        }

        #endregion

        #region 内部方法

        private void CheckSystemMembers(ScriptContext context)
        {
            if (reloadSysMembers)
            {
                systemMembers = ScriptGlobal.GetInstanceMembers(context, this.GetType());
                reloadSysMembers = false;
            }
        }

        internal void InitSystemMembers(IScriptMemberList members)
        {
            this.systemMembers = members;
            this.reloadSysMembers = false;
        }

        protected void InitValueMembers(IScriptMemberList members)
        {
            this.valueMembers = members;
        }

        internal virtual bool IsFuncContext { get { return false; } }

        internal long ObjectId { get { return objectId; } set { objectId = value; } }

        private ScriptObjectBase FindFieldInfo(ScriptFieldInfo fieldInfo)
        {
            ScriptObjectBase result = this;
            int parentLevel = 0;
            bool resetObjectId = this.objectId == 0;
            if (fieldInfo.ObjectId > 0 && fieldInfo.ObjectId == this.objectId)
            {
                if (fieldInfo.ParentLevel == 0) return result;
                else
                {
                    int lastParentLevel = fieldInfo.ParentLevel;
                    for (int i = 0; i < lastParentLevel; i++)
                    {
                        result = (ScriptObjectBase)result.Parent;
                        if (result.objectId == 0 || result.objectId > this.objectId)
                        {
                            parentLevel = i + 1;
                            resetObjectId = true;
                            break;
                        }
                    }
                    if (parentLevel == 0)
                        return result;
                }
            }
            int index;
            do
            {
                if (result != this && result.objectId == 0)
                {
                    if (!result.IsReadOnly)
                        result.ResetObjectId(fieldInfo, false);
                    resetObjectId = true;
                }
                else if (!resetObjectId && result.objectId > this.objectId)
                    resetObjectId = true;
                index = result.InnerFind(fieldInfo.Context, fieldInfo.FieldName);
                if (index >= 0) break;
                else
                {
                    IScriptObject p2 = result.Parent;
                    ScriptObjectBase p = p2 as ScriptObjectBase;
                    if (p != null)
                    {
                        if (!result.IsReadOnly) result.objectIdFlag |= 1;
                        parentLevel++;
                        result = p;
                    }
                    else
                    {
                        if (p2 == null) index = -1;
                        else index = -2;
                        break;
                    }
                }
            } while (true);
            if (!result.IsReadOnly)
                result.objectIdFlag |= (index < 0 ? 1 : 2);
            if (resetObjectId && !this.IsReadOnly) this.ResetObjectId(fieldInfo, result != this);
            fieldInfo.FieldIndex = index;
            fieldInfo.ParentLevel = parentLevel;
            fieldInfo.ObjectId = this.objectId;
            return result;
        }

        private void ResetObjectId(ScriptFieldInfo fieldInfo, bool forceRenew)
        {
            if (!forceRenew && (this.objectIdFlag & 4) != 0)
            {
                IScriptMemberList memberList = valueMembers;
                if (memberList == null)
                {
                    CheckSystemMembers(fieldInfo.Context);
                    memberList = systemMembers;
                }
                if (memberList != null)
                {
                    this.objectId = memberList.ObjectId;
                    if (this.objectId == 0)
                        memberList.ObjectId = this.objectId = fieldInfo.NewObjectId();
                    this.objectIdFlag = 1;
                    return;
                }
            }
            this.objectId = fieldInfo.NewObjectId();
            this.objectIdFlag = 0;
        }

        internal IScriptObject GetValue(ScriptFieldInfo fieldInfo)
        {
            if (objectId < 0) return GetValue(fieldInfo.Context, fieldInfo.FieldName);
            else
            {
                ScriptObjectBase obj = FindFieldInfo(fieldInfo);
                if (fieldInfo.FieldIndex == -1)
                {
                    if (fieldInfo.IsFuncContext)
                        throw new ScriptExecuteException(string.Format("全局变量'{0}'未定义。", fieldInfo.FieldName));
                    return ScriptUndefined.Instance;
                }
                if (fieldInfo.FieldIndex == -2)
                    return obj.Parent.GetValue(fieldInfo.Context, fieldInfo.FieldName);
                return obj.InnerGetValue(fieldInfo.Context, fieldInfo.FieldIndex);
            }
        }

        internal void SetValue(ScriptFieldInfo fieldInfo, IScriptObject value)
        {
            if (!IsReadOnly)
            {
                if (objectId < 0) SetValue(fieldInfo.Context, fieldInfo.FieldName, value);
                else if (fieldInfo.IsFuncContext)
                {
                    ScriptObjectBase obj = FindFieldInfo(fieldInfo);
                    switch (fieldInfo.FieldIndex)
                    {
                        case -1: fieldInfo.FieldIndex = obj.InnerSetValue(fieldInfo.Context, fieldInfo.FieldName, value); break;
                        case -2: obj.Parent.SetValue(fieldInfo.Context, fieldInfo.FieldName, value); break;
                        default:
                            {
                                if (!obj.InnerSetValue(fieldInfo.Context, fieldInfo.FieldIndex, value))
                                    fieldInfo.FieldIndex = ~obj.InnerSetValue(fieldInfo.Context, fieldInfo.FieldName, value);
                            }
                            break;
                    }
                }
                else
                {
                    if (fieldInfo.ObjectId > 0 && fieldInfo.ObjectId == this.objectId)
                        this.InnerSetValue(fieldInfo.Context, fieldInfo.FieldIndex, value);
                    else
                    {
                        int index = this.InnerFind(fieldInfo.Context, fieldInfo.FieldName);
                        if (index < 0 || !this.InnerSetValue(fieldInfo.Context, index, value))
                            index = ~this.InnerSetValue(fieldInfo.Context, fieldInfo.FieldName, value);
                        if (this.objectId == 0) this.objectId = fieldInfo.NewObjectId();
                        fieldInfo.ObjectId = this.objectId;
                        fieldInfo.ParentLevel = 0;
                        fieldInfo.FieldIndex = index;
                    }
                }
            }
        }

        internal void SetReadOnly() { this.readOnly = true; }

        #endregion

        #region 重载方法

        protected virtual int OnFindSystemMember(ScriptContext context, string key)
        {
            return systemMembers.Find(context, key);
        }

        protected override int InnerFind(ScriptContext context, string key)
        {
            CheckSystemMembers(context);
            int index = base.InnerFind(context, key);
            if (index >= 0)
            {
                if (systemMembers != null) index += systemMembers.Count;
                if (valueMembers != null) index += valueMembers.Count;
                return index;
            }
            if (valueMembers != null)
            {
                index = valueMembers.Find(context, key);
                if (index >= 0)
                {
                    if (systemMembers != null) index += systemMembers.Count;
                    return index;
                }
            }
            if (systemMembers != null)
            {
                index = OnFindSystemMember(context, key);
                if (index >= 0) return index;
            }
            return index;
        }

        protected override IScriptObject InnerGetValue(ScriptContext context, int index)
        {
            if (index < 0) return ScriptUndefined.Instance;
            CheckSystemMembers(context);
            int count = 0, c2, i2;
            if (systemMembers != null)
            {
                c2 = systemMembers.Count;
                if (index < c2) return systemMembers.GetValue(context, this, index);
                count = c2;
            }
            if (valueMembers != null)
            {
                c2 = valueMembers.Count;
                i2 = index - count;
                if (i2 < c2) return valueMembers.GetValue(context, this, i2);
                count += c2;
            }
            c2 = this.Count;
            i2 = index - count;
            if (i2 < c2) return ScriptHelper.CheckGetPropValue(context, this, base.InnerGetValue(context, i2));
            return ScriptUndefined.Instance;
        }

        /// <summary>根据<paramref name="index"/>来设置值。当<paramref name="index"/>指定的位置是systemMembers或valueMembers时，必须是IScriptProperty的值，才允许设置，否则返回<c>false</c>。</summary>
        protected override bool InnerSetValue(ScriptContext context, int index, IScriptObject value)
        {
            int count = 0, c2, index2;
            CheckSystemMembers(context);
            if (systemMembers != null)
            {
                c2 = systemMembers.Count;
                if (index < c2)
                    return systemMembers.CheckSetValue(context, this, index, value);
                count = c2;
            }
            if (valueMembers != null)
            {
                c2 = valueMembers.Count;
                index2 = index - count;
                if (index2 < c2)
                    return valueMembers.CheckSetValue(context, this, index2, value);
                count += c2;
            }
            c2 = this.Count;
            index2 = index - count;
            IScriptObject propValue = base.InnerGetValue(context, index2);
            if (!ScriptHelper.CheckSetPropValue(context, this, propValue, value))
                base.InnerSetValue(context, index2, value);
            return true;
        }

        /// <summary>直接根据<paramref name="key"/>来设置值。不检查systemMembers和valueMembers。</summary>
        protected override int InnerSetValue(ScriptContext context, string key, IScriptObject value)
        {
            int index = base.InnerFind(context, key);
            if (index >= 0)
            {
                IScriptObject propValue = base.InnerGetValue(context, index);
                if (!ScriptHelper.CheckSetPropValue(context, this, propValue, value))
                    base.InnerSetValue(context, index, value);
                return index;
            }
            index = base.InnerSetValue(context, key, value);
            int count = 0;
            CheckSystemMembers(context);
            if (systemMembers != null) count += systemMembers.Count;
            if (valueMembers != null) count += valueMembers.Count;
            if (count > 0)
            {
                bool navigate = index < 0;
                if (navigate) index = ~index;
                index += count;
                if (navigate) index = ~index;
            }
            return index;
        }

        internal int BaseSetValue(ScriptContext context, string key, IScriptObject value)
        {
            return base.InnerSetValue(context, key, value);
        }

        protected override void OnAdded(string key, IScriptObject newValue)
        {
            base.OnAdded(key, newValue);
            if ((this.objectIdFlag & 1) != 0)
            {
                this.objectId = 0;
                this.objectIdFlag = 0;
            }
        }

        protected override void OnRemoved(string key, IScriptObject oldValue)
        {
            base.OnRemoved(key, oldValue);
            if ((this.objectIdFlag & 2) != 0)
            {
                this.objectId = 0;
                this.objectIdFlag = 0;
            }
        }

        protected internal override bool IsReadOnly { get { return readOnly; } }

        #endregion

        #region 系统方法/属性

        [ObjectMember("prototype", IsEnumerable = false)]
        public IScriptObject ProtoType
        {
            get { return prototype == null ? this : prototype;}
            set { this.prototype = value; }
        }

        [ObjectMember("toString", IsEnumerable = false)]
        public virtual IScriptObject GetString(ScriptContext context)
        {
            string str = ToValueString(context);
            if (str == null) return ScriptNull.Instance;
            return ScriptString.Create(str);
        }

        #endregion

        #region IScriptObject

        public virtual bool Remove(ScriptContext context, string name)
        {
            return !IsReadOnly && InnerRemove(context, name);
        }

        public abstract object ToValue(ScriptContext context);

        public abstract string ToValueString(ScriptContext context);

        public override string ToString()
        {
            string str = ToValueString(null);
            int count = this.Count;
            if (count == 0) return str;
            StringBuilder sb = new StringBuilder();
            sb.Append(str);
            sb.Append(", {");
            int index = 0;
            foreach(KeyValuePair<string, IScriptObject> kv in (IEnumerable<KeyValuePair<string, IScriptObject>>)this)
            {
                sb.Append(kv.Key);
                sb.Append(':');
                sb.Append(kv.Value.ToValueString(null));
                sb.Append(',');
                if (++index > 5) break;
            }
            if (index == count)
                sb.Length--;
            else
                sb.Append("...");
            sb.Append('}');
            return sb.ToString();
        }

        public virtual IScriptObject GetValue(ScriptContext context, string name)
        {
            int index = InnerFind(context, name);
            if (index >= 0) return InnerGetValue(context, index);
            else
            {
                IScriptObject p = this.Parent;
                while(p != null)
                {
                    ScriptObjectBase sp = p as ScriptObjectBase;
                    if (sp != null)
                    {
                        index = sp.InnerFind(context, name);
                        if (index >= 0)
                            return sp.InnerGetValue(context, index);
                        else
                            p = sp.Parent;
                    }
                    else
                        return p.GetValue(context, name);
                }
            }
            return InnerGetValue(context, name);
        }

        public virtual void SetValue(ScriptContext context, string name, IScriptObject value)
        {
            if (!IsReadOnly)
            {
                int index = InnerFind(context, name);
                if (index >= 0)
                {
                    if (!InnerSetValue(context, index, value))
                        InnerSetValue(context, name, value);
                }
                else
                {
                    bool processed = false;
                    IScriptObject p = this.Parent;
                    while (p != null)
                    {
                        ScriptObjectBase sp = p as ScriptObjectBase;
                        if (sp != null)
                        {
                            index = sp.InnerFind(context, name);
                            if (index >= 0)
                            {
                                if (!sp.InnerSetValue(context, index, value))
                                    sp.InnerSetValue(context, name, value);
                                processed = true;
                                break;
                            }
                            else
                            {
                                if (IsFuncContext && sp.Parent == null)
                                {
                                    sp.InnerSetValue(context, name, value);
                                    processed = true;
                                    break;
                                }
                                else
                                    p = sp.Parent;
                            }
                        }
                        else
                            break;
                    }
                    if (!processed)
                        InnerSetValue(context, name, value);
                }
            }
        }

        public virtual IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            return new ScriptEnumerator(context, isKey, this);
        }

        public virtual bool BooleanValue { get { return true; } }

        public virtual string TypeName { get { return "object"; } }

        #endregion

        #region 内部类

        struct ScriptEnumerator : IEnumerator
        {
            private ScriptContext context;
            private ScriptObjectBase instance;
            private IEnumerator<KeyValuePair<string, IScriptObject>> instanceValues;
            private IEnumerator<KeyValuePair<string, IScriptObject>> systemMemberValues, valueMemberValues;
            private int status;
            private bool isKey;
            private object current;

            public ScriptEnumerator(ScriptContext context, bool isKey, ScriptObjectBase instance)
            {
                this.context = context;
                this.isKey = isKey;
                this.instance = instance;
                this.instance.CheckSystemMembers(context);
                this.instanceValues = ((IEnumerable<KeyValuePair<string, IScriptObject>>)this.instance).GetEnumerator();
                this.systemMemberValues = this.instance.systemMembers != null ? this.instance.systemMembers.GetEnumerator() : null;
                this.valueMemberValues = this.instance.valueMembers != null ? this.instance.valueMembers.GetEnumerator() : null;
                this.status = -1;
                this.current = null;
            }

            public object Current
            {
                get
                {
                    if (status < 0)
                        throw new ArgumentOutOfRangeException("Current");
                    if (!isKey)
                    {
                        IScriptProperty propCurrent = current as IScriptProperty;
                        if (propCurrent != null && propCurrent is IScriptObject)
                            return ScriptHelper.CheckGetPropValue(context, instance, (IScriptObject)propCurrent);
                    }
                    return current;
                }
            }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose()
            {
            }

            private bool CheckMoveNext(IEnumerator<KeyValuePair<string, IScriptObject>> list)
            {
                if (list != null)
                {
                    if (list.MoveNext())
                    {
                        KeyValuePair<string, IScriptObject> kv = list.Current;
                        do
                        {
                            IScriptValueEnumerable p = kv.Value as IScriptValueEnumerable;
                            if (p == null || p.IsEnumerable)
                            {
                                if (isKey) current = kv.Key;
                                else current = kv.Value;
                                return true;
                            }
                        } while (list.MoveNext());
                    }
                }
                return false;
            }

            public bool MoveNext()
            {
                if (status >= -1)
                {
                    if (CheckMoveNext(systemMemberValues))
                    {
                        status = 0;
                        return true;
                    }
                    if (CheckMoveNext(valueMemberValues))
                    {
                        status = 1;
                        return true;
                    }
                    if (CheckMoveNext(instanceValues))
                    {
                        status = 2;
                        return true;
                    }
                    status = -2;
                }
                return false;
            }

            public void Reset()
            {
                status = -1;
                if (systemMemberValues != null) systemMemberValues.Reset();
                if (valueMemberValues != null) valueMemberValues.Reset();
                if (instanceValues != null) instanceValues.Reset();
            }
        }

        #endregion
    }

    /// <summary></summary>
    public enum NumberType
    {
        NaN = -2,
        Infinity = -1,
        Integer = 1,
        Decimal = 2
    }

    public abstract class ScriptNumber : ScriptObjectBase
    {
        private const int Flag_Infinity = 1, Flag_NaN = 2;
        public readonly static ScriptNumber Infinity = new ScriptNumberConst(NumberType.Infinity);
        public readonly static ScriptNumber NaN = new ScriptNumberConst(NumberType.NaN);
        private long longValue;
        private decimal decimalValue;
        private readonly static ScriptInteger[] DefineValues = new ScriptInteger[101];

        internal ScriptNumber(bool useSystemMembers) : base(useSystemMembers) { }

        [ObjectConstructor]
        public static ScriptNumber Create(ScriptMethodArgus argus)
        {
            if (argus.Arguments != null && argus.Arguments.Length >0)
            {
                ScriptNumber num = argus.Arguments[0] as ScriptNumber;
                if (num != null) return num;
            }
            return Create(0);
        }

        public static ScriptNumber Create(long value)
        {
            ScriptInteger result;
            if (value >= 0 && value <= 100)
            {
                result = DefineValues[value];
                if (result == null)
                {
                    result = new ScriptInteger(value);
                    Interlocked.CompareExchange<ScriptInteger>(ref DefineValues[value], result, null);
                }
            }
            else
                result = new ScriptInteger(value);
            return result;
        }

        public static ScriptNumber Create(decimal value)
        {
            return new ScriptDecimal(value);
        }

        public abstract NumberType Type { get; }
        
        public abstract long IntegerValue { get; }
        public abstract decimal DecimalValue { get; }

        public override string TypeName { get { return "number"; } }

        private class ScriptNumberConst : ScriptNumber
        {
            private NumberType type;

            public ScriptNumberConst(NumberType type) : base(false) { this.type = type; }

            public override NumberType Type { get { return type; } }

            public override long IntegerValue
            {
                get { return long.MaxValue; }
            }

            public override decimal DecimalValue
            {
                get { return Decimal.MaxValue; }
            }

            public override object ToValue(ScriptContext context)
            {
                throw new ScriptExecuteException("不合法的数值：" + ToString());
            }

            public override bool BooleanValue { get { return type != NumberType.NaN; } }

            public override string ToValueString(ScriptContext context)
            {
                return type == NumberType.Infinity ? "Infinity" : "NaN";
            }
        }
    }

    public sealed class ScriptInteger : ScriptNumber
    {
        private long value;
        internal ScriptInteger(long value) : base(true) { this.value = value; }

        public override NumberType Type { get { return NumberType.Integer; } }

        public override long IntegerValue
        {
            get { return value; }
        }

        public override decimal DecimalValue
        {
            get { return (decimal)value; }
        }
        
        public override object ToValue(ScriptContext context) { return value; }

        public override bool BooleanValue { get { return value != 0; } }

        public override string ToValueString(ScriptContext context)
        {
            return value.ToString();
        }
    }

    public sealed class ScriptDecimal : ScriptNumber
    {
        private decimal value;

        internal ScriptDecimal(decimal value) : base(true) { this.value = value; }

        public override NumberType Type { get { return NumberType.Decimal; } }

        public override long IntegerValue
        {
            get { return (long)value; }
        }

        public override decimal DecimalValue
        {
            get { return value; }
        }

        public override object ToValue(ScriptContext context) { return value; }

        public override bool BooleanValue { get { return value != 0; } }

        public override string ToValueString(ScriptContext context) { return value.ToString(); }
    }

    public sealed class ScriptString : ScriptObjectBase
    {
        private string value;

        private ScriptString() { }

        [ObjectConstructor]
        public static ScriptString Create(string value)
        {
            ScriptString result = new ScriptString();
            result.value = value;
            return result;
        }

        public string Value { get { return this.value; } }

        public override object ToValue(ScriptContext context) { return value; }

        public override string ToValueString(ScriptContext context) { return value; }

        #region 脚本方法

        [ObjectMember("charAt")]
        public string CharAt(int index)
        {
            if (index < 0 || index >= value.Length) return string.Empty;
            return new string(value[index], 1);
        }

        [ObjectMember("charCodeAt")]
        public ScriptNumber CharCodeAt(int index)
        {
            if (index < 0 || index >= value.Length) return ScriptNumber.NaN;
            return ScriptNumber.Create((int)value[index]);
        }

        [ObjectMember("concat")]
        public ScriptString Concat(ScriptMethodArgus argus)
        {
            IScriptObject[] argusValues = argus.Arguments;
            if (argusValues == null || argusValues.Length == 0) return this;
            if (argusValues.Length == 1) return ScriptString.Create(value + argusValues[0].ToValueString(argus.Context));
            StringBuilder sb = new StringBuilder(value);
            foreach(IScriptObject item in argusValues)
                sb.Append(item.ToValueString(argus.Context));
            return ScriptString.Create(sb.ToString());
        }

        [ObjectMember("indexOf")]
        public int IndexOf(string searchValue, int fromIndex)
        {
            if (string.IsNullOrEmpty(searchValue) || fromIndex < 0 || fromIndex > value.Length - searchValue.Length) return -1;
            return value.IndexOf(searchValue, fromIndex);
        }

        [ObjectMember("lastIndexOf")]
        public int LastIndexOf(string searchValue, IScriptObject fromIndex)
        {
            if (fromIndex is ScriptNumber)
            {
                int startIndex = (int)((ScriptNumber)fromIndex).IntegerValue;
                if (string.IsNullOrEmpty(searchValue) || startIndex < 0 || startIndex > value.Length - searchValue.Length) return -1;
                return value.LastIndexOf(searchValue, startIndex);
            }
            return string.IsNullOrEmpty(searchValue) ? -1 : value.LastIndexOf(searchValue);
        }

        [ObjectMember("localeCompare")]
        public int LocaleCompare(string target)
        {
            return StringComparer.CurrentCulture.Compare(value, target);
        }

        [ObjectMember("slice")]
        public string Slice(int start, IScriptObject end)
        {
            int i1 = start;
            if (i1 < 0) i1 = value.Length + i1;
            int i2;
            ScriptNumber numEnd = end as ScriptNumber;
            if (numEnd != null) i2 = (int)numEnd.IntegerValue;
            else i2 = value.Length;
            if (i1 == i2) return string.Empty;
            if (i1 > i2)
            {
                int tmp = i1;
                i1 = i2;
                i2 = tmp;
            }
            if (i1 < value.Length) return value.Substring(i1, i2 - i1);
            return string.Empty;
        }

        [ObjectMember("split")]
        public string[] Split(string separator, IScriptObject howmany)
        {
            ScriptInteger num = howmany as ScriptInteger;
            if (num != null)
                return value.Split(new string[] { separator }, (int)num.IntegerValue, StringSplitOptions.None);
            else
                return value.Split(new string[] { separator }, StringSplitOptions.None);
        }

        [ObjectMember("substring")]
        public string Substring(int start, IScriptObject stop)
        {
            if (start < 0) start = 0;
            ScriptInteger iStop = stop as ScriptInteger;
            if (iStop != null)
            {
                int end = (int)iStop.IntegerValue;
                if (end < 0) end = 0;
                else if (end > value.Length) end = value.Length;
                if (start > end)
                {
                    int tmp = start;
                    start = end;
                    end = tmp;
                }
                return start == end || start >= value.Length ? string.Empty : value.Substring(start, end - start);
            }
            if (start >= value.Length) return string.Empty;
            return value.Substring(start);
        }

        [ObjectMember("toLocaleLowerCase")]
        public string ToLocaleLowerCase()
        {
            return value.ToLower(System.Globalization.CultureInfo.CurrentCulture);
        }

        [ObjectMember("toLocaleUpperCase")]
        public string ToLocaleUpperCase()
        {
            return value.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
        }

        [ObjectMember("toLowerCase")]
        public string ToLowerCase()
        {
            return value.ToLower();
        }

        [ObjectMember("toUpperCase")]
        public string ToUpperCase()
        {
            return ToUpperCase();
        }

        [ObjectMember("toString")]
        public override IScriptObject GetString(ScriptContext context)
        {
            return this;
        }

        [ObjectMember("length")]
        public int Length { get { return value.Length; } }

        #endregion
    }

    public sealed class ScriptBoolean : ScriptObjectBase
    {
        private bool value;
        public readonly static ScriptBoolean True = new ScriptBoolean(true);
        public readonly static ScriptBoolean False = new ScriptBoolean(false);
        
        private ScriptBoolean(bool value) { this.value = value; }

        public bool Value { get { return value; } }

        [ObjectConstructor]
        public static ScriptBoolean Create(bool value)
        {
            return value ? True : False;
        }

        public override object ToValue(ScriptContext context) { return value; }

        public override bool BooleanValue { get { return value; } }

        public override string ToValueString(ScriptContext context) { return value ? "true" : "false"; }

        public override string TypeName { get { return "boolean"; } }
    }

    public sealed class ScriptDate : ScriptObjectBase
    {
        private DateTime date;

        private ScriptDate(DateTime date) { this.date = date; }
        public override bool BooleanValue { get { return true; } }

        public override string ToValueString(ScriptContext context) { return date.ToString("yyyy-MM-dd HH:mm:ss"); }

        public override string TypeName { get { return "object"; } }

        public override object ToValue(ScriptContext context)
        {
            return date;
        }

        #region 脚本方法/属性

        public static ScriptDate Create(DateTime date) { return new ScriptDate(date); }

        [ObjectConstructor]
        public static ScriptDate Create(ScriptMethodArgus info)
        {
            if (info.HasArguments)
            {
                IScriptObject value = info.Arguments[0];
                if (value is ScriptString)
                {
                    string str = ((ScriptString)value).Value;
                    return Create(DateTime.Parse(str));
                }
            }
            return Create(DateTime.Now);
        }

        #endregion
    }

    public sealed class ScriptUndefined : ScriptObjectBase
    {
        public readonly static ScriptUndefined Instance = new ScriptUndefined();

        private ScriptUndefined() : base(false) { }

        public override object ToValue(ScriptContext context) { return null; }

        public override bool BooleanValue { get { return false; } }

        public override string ToValueString(ScriptContext context) { return "undefined"; }

        public override string TypeName { get { return "undefined"; } }
    }

    public sealed class ScriptNull : ScriptObjectBase
    {
        public static ScriptNull Instance = new ScriptNull();

        private ScriptNull() : base(false) { }
        public override object ToValue(ScriptContext context) { return null; }

        public override bool BooleanValue { get { return false; } }

        public override string ToValueString(ScriptContext context) { return "null"; }

        public override string TypeName { get { return "null"; } }
    }

    public sealed class ScriptObject : ScriptObjectBase
    {
        [ObjectConstructor]
        public ScriptObject() { }

        public override object ToValue(ScriptContext context)
        {
            ScriptObjectConvertHandler handler = ScriptGlobal.ObjectConverter;
            if (handler != null) return handler(context, this);
            return this;
        }

        public override string ToValueString(ScriptContext context) { return "[Object]"; }
    }

    public interface IScriptFunction
    {
        IScriptObject Invoke(IScriptObject instance, IScriptObject[] argus);
    }

    public class FunctionInvoker
    {
        private ScriptFunctionBase func;
        private ScriptContext context;

        internal FunctionInvoker(ScriptContext context, ScriptFunctionBase func)
        {
            this.context = context;
            this.func = func;
        }

        public ScriptContext Context { get { return context; } }

        public ScriptFunctionBase Function { get { return func; } }

        public object Invoke(params object[] argus)
        {
            IScriptObject[] scriptArgus = argus as IScriptObject[];
            if (scriptArgus == null && argus != null)
            {
                int length = argus.Length;
                if (length > 0)
                {
                    scriptArgus = new IScriptObject[length];
                    for (int i = 0; i < length; i++)
                        scriptArgus[i] = ScriptGlobal.ConvertValue(context, argus[i]);
                }
            }
            if (scriptArgus == null) scriptArgus = new IScriptObject[0];
            return func.Invoke(context, false, false, null, scriptArgus);
        }
    }

    public abstract class ScriptFunctionBase : ScriptObjectBase
    {
        protected ScriptFunctionBase() { }

        internal protected abstract IScriptObject Invoke(ScriptContext context, bool isScriptEnv, bool isNewObject, IScriptObject instance, IScriptObject[] argus);

        protected abstract ScriptFunctionBase OnBind(ScriptContext context, IScriptObject instance);

        public override object ToValue(ScriptContext context)
        {
            return new FunctionInvoker(context, this);
        }

        public override string ToValueString(ScriptContext context) { return "[Function]"; }

        public override string TypeName { get { return "function"; } }

        [ObjectMember("call")]
        internal IScriptObject Call(ScriptMethodArgus argus)
        {
            if (argus.Arguments.Length > 0)
            {
                IScriptObject instance = argus.Arguments[0];
                IScriptObject[] values;
                if (argus.Arguments.Length > 1)
                {
                    values = new IScriptObject[argus.Arguments.Length - 1];
                    Array.Copy(argus.Arguments, 1, values, 0, values.Length);
                }
                else values = new IScriptObject[0];
                return Invoke(argus.Context, true, false, instance, values);
            }
            throw new ScriptExecuteException("call方法必须传入对象参数。");
        }

        [ObjectMember("apply")]
        internal IScriptObject Apply(IScriptObject instance, IScriptObject argus, ScriptContext context)
        {
            if (instance != null)
            {
                IScriptObject[] values;
                IScriptArray array = argus as IScriptArray;
                if (array != null)
                {
                    int length = array != null ? array.ArrayLength : 0;
                    values = new IScriptObject[length];
                    for (int i = 0; i < length; i++)
                        values[i] = array.GetElementValue(context, i);
                }
                else values = new IScriptObject[0];
                return Invoke(context, true, false, instance, values);
            }
            throw new ScriptExecuteException("apply方法必须传入对象参数。");
        }

        [ObjectMember("bind")]
        internal IScriptObject Bind(IScriptObject instance, ScriptContext context)
        {
            return OnBind(context, instance);
        }
    }

    public sealed class ScriptFunction : ScriptFunctionBase
    {
        private ScriptExecuteContext parentContext;
        private DefineContext context;
        private IScriptObject instance;

        internal ScriptFunction(DefineContext context, ScriptExecuteContext parentContext)
        {
            this.context = context;
            this.parentContext = parentContext;
        }

        internal DefineContext Context { get { return context; } }

        protected internal override IScriptObject Invoke(ScriptContext scriptContext, bool isScriptEnv, bool isNewObject, IScriptObject instance, IScriptObject[] argus)
        {
            ScriptExecuteContext execContext = this.context.CreateExecuteContext(null, argus);
            execContext.Parent = parentContext;
            execContext.IsNewObject = isNewObject;
            if (isNewObject)
            {
                ScriptObject newObj = new ScriptObject();
                newObj.Parent = this.ProtoType;
                execContext.ThisObject = newObj;
            }
            else
            {
                if (this.instance != null) instance = this.instance;
                execContext.ThisObject = instance;
            }
            if (isScriptEnv)
            {
                scriptContext.SetInvokeContext(execContext);
                return null;
            }
            else
            {
                Thread current = Thread.CurrentThread;
                bool exchangeThread = scriptContext.executingThread == null;
                if (!exchangeThread && scriptContext.executingThread != current)
                    throw new ScriptExecuteException("同一个脚本上下文的方法，不允许多线程执行。");
                if (exchangeThread && Interlocked.CompareExchange<Thread>(ref scriptContext.executingThread, current, null) !=null)
                    throw new ScriptExecuteException("同一个脚本上下文的方法，不允许在多处同时执行。");
                int step = 0;
                ScriptExecuteContext oldRoot = null;
                try
                {
                    oldRoot = scriptContext.ResetRootContext(execContext);
                    step = 1;
                    scriptContext.PushContext(execContext);
                    step = 2;
                    ScriptParser.InnerExecute(scriptContext, execContext);
                }
                finally
                {
                    if (step >= 2) scriptContext.PopContext();
                    if (step >= 1) scriptContext.ResetRootContext(oldRoot);
                    if (exchangeThread)
                        Interlocked.CompareExchange<Thread>(ref scriptContext.executingThread, null, current);
                }
                return isNewObject ? execContext.ThisObject : execContext.Result;
            }
        }

        protected override ScriptFunctionBase OnBind(ScriptContext context, IScriptObject instance)
        {
            ScriptFunction result = new ScriptFunction(this.context, this.parentContext);
            result.instance = instance;
            return result;
        }
    }

    public interface IScriptArray
    {
        IScriptObject GetElementValue(ScriptContext context, int index);

        void SetElementValue(ScriptContext context, int index, IScriptObject value);

        int ArrayLength { get; }

        bool IsArray { get; }
    }
    
    public sealed class ScriptArray : ScriptObjectBase, IScriptArray
    {
        private IScriptObject[] list;
        private int listCount;
        private ScriptArrayItem[] otherList;
        private int otherCount;
        private int version;

        public ScriptArray() { }

        public ScriptArray(int length)
        {
            CheckListCapacity(length);
            listCount = length;
        }

        public ScriptArray(IScriptObject[] elements)
        {
            if (elements != null && elements.Length > 0)
            {
                list = elements;
                listCount = elements.Length;
            }
        }

        #region 私有方法

        private int FindOther(int index)
        {
            if (otherCount > 0)
            {
                int l = 0, h = otherCount - 1, m, sub;
                do
                {
                    m = (l + h) >> 1;
                    sub = otherList[m].Index - index;
                    if (sub == 0) return m;
                    if (sub > 0) h = m - 1;
                    else l = m + 1;
                } while (l <= h);
                return ~l;
            }
            else return ~0;
        }

        private void CheckCapacity<T>(ref T[] array, ref int count, int increment)
        {
            if (array == null || count + increment > array.Length)
            {
                int newSize;
                if (count < 128)
                    newSize = ((increment >> 4) + 1) << 4;
                else if (count < 1024)
                    newSize = ((increment >> 8) + 1) << 8;
                else
                    newSize = ((increment >> 10) + 1) << 10;
                Array.Resize<T>(ref array, count + newSize);
            }
        }

        private void CheckListCapacity(int increment)
        {
            CheckCapacity<IScriptObject>(ref list, ref listCount, increment);
        }

        private void CheckOtherCapacity(int increment)
        {
            CheckCapacity<ScriptArrayItem>(ref otherList, ref otherCount, increment);
        }

        private IScriptObject[] ToArray()
        {
            int arrayLen = ArrayLength;
            IScriptObject[] result = new IScriptObject[arrayLen];
            if (arrayLen > 0)
            {
                if (listCount > 0)
                {
                    for (int i = 0; i < listCount; i++)
                        result[i] = list[i];
                }
                if (otherCount > 0)
                {
                    for (int i = 0; i < otherCount; i++)
                        result[otherList[i].Index] = otherList[i].Value;
                    for (int i = listCount; i < arrayLen; i++)
                        if (result[i] == null) result[i] = ScriptUndefined.Instance;
                }
            }
            return result;
        }

        private string InnerJoin(IScriptObject separator, ScriptContext context)
        {
            string strSep;
            if (separator == null || separator is ScriptUndefined || separator is ScriptNull)
                strSep = ",";
            else
                strSep = separator.ToValueString(context);
            StringBuilder sb = new StringBuilder();
            if (this.listCount > 0)
            {
                for (int i = 0; i < listCount; i++)
                {
                    if (sb.Length > 0) sb.Append(strSep);
                    sb.Append(list[i].ToValueString(context));
                }
            }
            if (this.otherCount > 0)
            {
                for (int i = 0; i < otherCount; i++)
                {
                    if (sb.Length > 0) sb.Append(strSep);
                    sb.Append(otherList[i].Value.ToValueString(context));
                }
            }
            return sb.ToString();
        }

        #endregion

        #region 重载方法

        public IScriptObject this[int index]
        {
            get
            {
                IScriptObject result = null;
                if (index >= 0)
                {
                    if (index < listCount) result = list[index];
                    else if (otherCount > 0)
                    {
                        int i = FindOther(index);
                        if (i >= 0) result = otherList[i].Value;
                    }
                }
                if (result == null) result = ScriptUndefined.Instance;
                return result;
            }
            set
            {
                if (index >= 0)
                {
                    if (index < listCount) list[index] = value;
                    else if (index == listCount)
                    {
                        CheckListCapacity(1);
                        list[index] = value;
                        listCount++;
                        if (otherCount > 0 && otherList[0].Index == listCount)
                        {
                            int c = 1;
                            while (c < otherCount)
                            {
                                if (otherList[c].Index != listCount + c) break;
                                c++;
                            }
                            CheckListCapacity(c);
                            for(int i = 0; i < c; i++)
                                list[listCount + i] = otherList[i].Value;
                            listCount += c;
                            if (c < otherCount)
                                Array.Copy(otherList, c, otherList, 0, otherCount - c);
                            otherCount = c;
                        }
                    }
                    else
                    {
                        int i = FindOther(index);
                        if (i < 0)
                        {
                            i = ~i;
                            CheckOtherCapacity(1);
                            if (i < otherCount)
                                Array.Copy(otherList, i, otherList, i + 1, otherCount - i);
                            otherCount++;
                            otherList[i].Index = index;
                        }
                        otherList[i].Value = value;
                    }
                    version++;
                }
            }
        }

        public override IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            int oldVersion = version;
            for (int i = 0; i < listCount; i++)
            {
                if (oldVersion != version)
                    throw new ScriptExecuteException("数组枚举期间，不能添加或删除数组的成员。");
                if (isKey) yield return i;
                else yield return list[i];
            }
            for (int i = 0; i < otherCount; i++)
            {
                if (oldVersion != version)
                    throw new ScriptExecuteException("数组枚举期间，不能添加或删除数组的成员。");
                if (isKey) yield return otherList[i].Index;
                else yield return otherList[i].Value;
            }
            IEnumerator en = base.GetEnumerator(context, isKey);
            while (en.MoveNext())
                yield return en.Current;
        }

        public IScriptObject GetElementValue(ScriptContext context, int index)
        {
            return this[index];
        }

        public void SetElementValue(ScriptContext context, int index, IScriptObject value)
        {
            this[index] = value;
        }

        public int ArrayLength
        {
            get
            {
                if (otherCount > 0) return otherList[otherCount - 1].Index + 1;
                return listCount;
            }
        }

        public bool IsArray { get { return true; } }

        public override string ToValueString(ScriptContext context) { return InnerJoin(null, context); }

        public override object ToValue(ScriptContext context)
        {
            int len = Length;
            object[] result = new object[len];
            if (listCount > 0)
            {
                for (int i = 0; i < listCount; i++)
                    result[i] = list[i].ToValue(context);
            }
            if (otherCount > 0)
            {
                for (int i = 0; i < otherCount; i++)
                    result[otherList[i].Index] = otherList[i].Value.ToValue(context);
            }
            return result;
        }

        #endregion

        #region 脚本方法/属性

        [ObjectConstructor]
        public static ScriptArray CreateInstance(ScriptMethodArgus argus)
        {
            IScriptObject[] objArgus = argus.Arguments;
            if (objArgus != null && objArgus.Length > 0)
            {
                if (objArgus.Length == 1 && objArgus[0] is ScriptInteger)
                {
                    long value = ((ScriptInteger)objArgus[0]).IntegerValue;
                    return new ScriptArray((int)value);
                }
                return new ScriptArray(objArgus);
            }
            return new ScriptArray();
        }

        [ObjectMember("concat")]
        public ScriptArray Concat(ScriptMethodArgus methodArgus)
        {
            ScriptArray result = new ScriptArray();
            if (this.listCount > 0)
            {
                result.list = new IScriptObject[this.list.Length];
                result.listCount = this.listCount;
                Array.Copy(this.list, result.list, this.listCount);
            }
            if (this.otherCount > 0)
            {
                result.otherList = new ScriptArrayItem[this.otherList.Length];
                result.otherCount = this.otherCount;
                Array.Copy(this.otherList, result.otherList, this.otherCount);
            }

            IScriptObject[] argusList = methodArgus.Arguments;
            if (argusList != null && argusList.Length > 0)
            {
                foreach (IScriptObject objArgus in argusList)
                {
                    ScriptArray arr = objArgus as ScriptArray;
                    if (arr != null)
                    {
                        if (result.otherCount > 0)
                        {
                            int resultLength = result.ArrayLength;
                            int length = arr.listCount + arr.otherCount;
                            result.CheckOtherCapacity(length);
                            if (arr.listCount > 0)
                            {
                                for (int i = 0; i < arr.listCount; i++)
                                    result.otherList[result.otherCount++] = new ScriptArrayItem() { Index = resultLength + i, Value = arr.list[i] };
                            }
                            if (arr.otherCount > 0)
                            {
                                for (int i = 0; i < arr.otherCount; i++)
                                    result.otherList[result.otherCount++] = new ScriptArrayItem() { Index = resultLength + arr.otherList[i].Index, Value = arr.otherList[i].Value };
                            }
                        }
                        else
                        {
                            int resultLength = result.listCount;
                            if (arr.listCount > 0)
                            {
                                result.CheckListCapacity(arr.listCount);
                                for (int i = 0; i < arr.listCount; i++)
                                    result.list[result.listCount++] = arr.list[i];
                            }
                            if (arr.otherCount > 0)
                            {
                                result.CheckOtherCapacity(arr.otherCount);
                                for (int i = 0; i < arr.otherCount; i++)
                                    result.otherList[result.otherCount++] = new ScriptArrayItem() { Index = resultLength + arr.otherList[i].Index, Value = arr.otherList[i].Value };
                            }
                        }
                    }
                    else
                    {
                        IScriptArray iArr = objArgus as IScriptArray;
                        if (iArr != null && iArr.IsArray)
                        {
                            int length = iArr.ArrayLength;
                            if (result.otherCount > 0)
                            {
                                int resultLength = result.ArrayLength;
                                result.CheckOtherCapacity(length);
                                for(int i = 0; i <length; i++)
                                    result.otherList[result.otherCount++] = new ScriptArrayItem() { Index = resultLength + i, Value = iArr.GetElementValue(methodArgus.Context, i) };
                            }
                            else
                            {
                                result.CheckListCapacity(length);
                                for (int i = 0; i < length; i++)
                                    result.list[result.listCount++] = iArr.GetElementValue(methodArgus.Context, i);
                            }
                        }
                        else result.Push(objArgus);
                    }
                }
            }

            return result;
        }

        [ObjectMember("join")]
        public ScriptString Join(IScriptObject separator, ScriptContext context)
        {
            return ScriptString.Create(InnerJoin(separator, context));
        }

        [ObjectMember("pop")]
        public IScriptObject Pop()
        {
            if (otherCount > 0)
            {
                IScriptObject result = otherList[--otherCount].Value;
                otherList[otherCount].Value = null;
                version++;
                return result;
            }
            else if (listCount > 0)
            {
                IScriptObject result = list[--listCount];
                list[listCount] = null;
                version++;
                return result;
            }
            return ScriptUndefined.Instance;
        }

        [ObjectMember("push")]
        public void Push(IScriptObject item)
        {
            if (otherCount > 0)
            {
                CheckOtherCapacity(1);
                ScriptArrayItem arrayItem = new ScriptArrayItem();
                arrayItem.Index = otherList[otherCount - 1].Index + 1;
                arrayItem.Value = item;
                otherList[otherCount++] = arrayItem;
            }
            else
            {
                CheckListCapacity(1);
                list[listCount++] = item;
            }
            version++;
        }

        [ObjectMember("reverse")]
        public void Reverse()
        {
            int arrayLength = this.ArrayLength;
            if (arrayLength > 0)
            {
                IScriptObject[] arrayList = new IScriptObject[arrayLength];
                if (listCount > 0)
                {
                    for (int i = 0; i < listCount; i++)
                        arrayList[arrayLength - i - 1] = this.list[i];
                }
                if (otherCount > 0)
                {
                    for (int i = 0; i < otherCount; i++)
                        arrayList[arrayLength - this.otherList[i].Index - 1] = this.otherList[i].Value;
                    for (int i = 0; i < arrayLength; i++)
                        if (arrayList[i] == null) arrayList[i] = ScriptUndefined.Instance;
                }
                this.list = arrayList;
                this.listCount = arrayLength;
                this.otherList = null;
                this.otherCount = 0;
                this.version++;
            }
        }

        [ObjectMember("shift")]
        public IScriptObject Shift()
        {
            IScriptObject result;
            if (listCount > 0)
            {
                result = list[0];
                if (--listCount > 0)
                    Array.Copy(list, 1, list, 0, listCount);
                list[listCount] = null;
                version++;
            }
            else if (otherCount > 0)
            {
                result = otherList[0].Value;
                if (--otherCount > 1)
                    Array.Copy(otherList, 1, otherList, 0, otherCount);
                otherList[otherCount].Value = null;
                version++;
            }
            else
                result = ScriptUndefined.Instance;
            return result;
        }

        [ObjectMember("slice")]
        public ScriptArray Slice(IScriptObject start, IScriptObject end)
        {
            int istart, iend;
            ScriptNumber numStart = start as ScriptNumber;
            if (numStart != null)
            {
                istart = (int)numStart.IntegerValue;
                if (istart < 0) istart = 0;
            }
            else istart = 0;
            ScriptNumber numEnd = end as ScriptNumber;
            int arrayLen = ArrayLength;
            if (numEnd != null)
            {
                iend = (int)numEnd.IntegerValue;
                if (iend >= arrayLen) iend = arrayLen;
            }
            else iend = arrayLen;

            ScriptArray result;
            if (iend > istart)
            {
                int valueLength = iend - istart;
                IScriptObject[] values = new IScriptObject[valueLength];
                for (int i = 0; i < valueLength; i++)
                    values[i] = this[istart + i];
                result = new ScriptArray(values);
            }
            else result = new ScriptArray();
            return result;
        }

        [ObjectMember("sort")]
        public void Sort(IScriptObject orderBy, ScriptContext context)
        {
            if (!(orderBy is ScriptUndefined) && !(orderBy is ScriptFunctionBase))
                throw new ScriptExecuteException("数据排序的比较函数，必须是方法或者undefined");
            IScriptObject[] objList = ToArray();
            ScriptFunctionBase func = orderBy as ScriptFunctionBase;
            FunctionInvoker invoker = func != null ? func.ToValue(context) as FunctionInvoker : null;
            ScriptObjectComparer comparer = new ScriptObjectComparer(context, invoker);
            Array.Sort<IScriptObject>(objList, comparer);
            this.list = objList;
            this.listCount = objList.Length;
            this.otherList = null;
            this.otherCount = 0;
        }

        [ObjectMember("splice")]
        public ScriptArray Splice(ScriptMethodArgus methodArgus)
        {
            IScriptObject[] argus = methodArgus.Arguments;
            if (argus.Length < 2)
                throw new ScriptExecuteException("splice方法的参数个数错误！");
            ScriptNumber numIndex = argus[0] as ScriptNumber;
            ScriptNumber numCount = argus[1] as ScriptNumber;
            if (numIndex == null)
                throw new ScriptExecuteException("splice方法的参数'index'必须为Number对象。");
            if (numCount == null)
                throw new ScriptExecuteException("splice方法的参数'count'必须为Number对象。");
            int index = (int)numIndex.IntegerValue, count = (int)numCount.IntegerValue;
            int arrayLength = ArrayLength;
            if (index < 0)
                index = arrayLength + index;
            IScriptObject[] objList = ToArray();
            int objCount = objList.Length;
            ScriptArray result = new ScriptArray();
            if (index < arrayLength && count > 0)
            {
                if (count > arrayLength - index) count = arrayLength - index;
                IScriptObject[] removed = new IScriptObject[count];
                Array.Copy(objList, index, removed, 0, count);
                if (index + count < arrayLength)
                {
                    Array.Copy(objList, index + count, objList, index, arrayLength - index - count);
                    objCount = arrayLength - count;
                    for (int i = objCount; i < arrayLength; i++)
                        objList[i] = null;
                }
                result.list = removed;
                result.listCount = removed.Length;
            }
            if (argus.Length > 2)
            {
                int argusLen = argus.Length - 2;
                int len = objCount + argusLen;
                if (len > arrayLength)
                    Array.Resize<IScriptObject>(ref objList, len);
                Array.Copy(objList, index, objList, index + argusLen, objCount - index);
                Array.Copy(argus, 2, objList, index, argusLen);
                objCount = len;
            }
            list = objList;
            listCount = objCount;
            otherList = null;
            otherCount = 0;
            return result;
        }

        [ObjectMember("unshift")]
        public int Unshift(ScriptMethodArgus methodArgus)
        {
            IScriptObject[] argus = methodArgus.Arguments;
            if (argus == null || argus.Length == 0)
                throw new ScriptExecuteException("unshift方法的参数不能为空。");
            IScriptObject[] objList;
            if (ArrayLength > 0)
            {
                int argusLen = argus.Length;
                objList = ToArray();
                int objCount = objList.Length;
                Array.Resize<IScriptObject>(ref objList, objCount + argusLen);
                Array.Copy(objList, 0, objList, argusLen, objCount);
                Array.Copy(argus, 0, objList, 0, argusLen);
            }
            else objList = argus;
            this.list = objList;
            this.listCount = objList.Length;
            this.otherList = null;
            this.otherCount = 0;
            return listCount;
        }

        [ObjectMember("length")]
        public int Length { get { return ArrayLength; } }

        #endregion

        #region 内部类

        private struct ScriptArrayItem
        {
            public int Index;
            public IScriptObject Value;
        }
        
        private class ScriptObjectComparer : IComparer<IScriptObject>
        {
            private ScriptContext context;
            private FunctionInvoker invoker;

            public ScriptObjectComparer(ScriptContext context, FunctionInvoker invoker)
            {
                this.context = context;
                this.invoker = invoker;
            }

            int IComparer<IScriptObject>.Compare(IScriptObject x, IScriptObject y)
            {
                if (invoker != null)
                {
                    ScriptNumber r = invoker.Invoke(x, y) as ScriptNumber;
                    if (r != null)
                    {
                        if (r.IntegerValue > 0) return 1;
                        if (r.IntegerValue < 0) return -1;
                    }
                    return 0;
                }
                else
                {
                    string strX = x.ToValueString(context), strY = y.ToValueString(context);
                    return string.Compare(strX, strY);
                }
            }
        }

        #endregion
    }

    internal interface IScriptAssignObject
    {
        IScriptObject GetFieldValue(ScriptContext context);

        void SetFieldValue(ScriptContext context, IScriptObject value);

        IScriptObject GetFieldValue2(ScriptContext context);

        void RemoveField(ScriptContext context);
    }

    internal class ScriptArrayAssignObject : IScriptObject, IScriptAssignObject
    {
        private IScriptArray instance;
        private int index;

        public ScriptArrayAssignObject(IScriptArray instance, int key)
        {
            this.instance = instance;
            this.index = key;
        }

        public IScriptObject GetFieldValue(ScriptContext context)
        {
            return instance.GetElementValue(context, index);
        }

        public void SetFieldValue(ScriptContext context, IScriptObject value)
        {
            instance.SetElementValue(context, index, value);
        }

        public IScriptObject GetFieldValue2(ScriptContext context)
        {
            return instance.GetElementValue(context, index);
        }

        public void RemoveField(ScriptContext context)
        {
            ((IScriptObject)instance).Remove(context, index.ToString());
        }

        #region IScriptObject

        IScriptObject IScriptObject.GetValue(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        void IScriptObject.SetValue(ScriptContext context, string name, IScriptObject value)
        {
            throw new NotImplementedException();
        }

        bool IScriptObject.Remove(ScriptContext context, string name)
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

        string IScriptObject.TypeName { get { throw new NotImplementedException(); } }

        #endregion

        #region IScriptEnumerable

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ScriptAssignObject : IScriptObject, IScriptAssignObject
    {
        private IScriptObject instance;
        private string field;
        private ScriptFieldInfo fieldInfo, fieldInfo2;

        public ScriptAssignObject(IScriptObject instance, string field)
        {
            this.instance = instance;
            this.field = field;
        }

        public ScriptAssignObject(IScriptObject value, ScriptFieldInfo fieldInfo)
        {
            this.instance = value;
            this.fieldInfo = fieldInfo;
        }

        public ScriptAssignObject(IScriptObject value, ScriptFieldInfo fieldInfo, ScriptFieldInfo fieldInfo2) : this(value, fieldInfo)
        {
            this.fieldInfo2 = fieldInfo2;
        }

        public IScriptObject Instance { get { return instance; } }

        public string Field { get { return field; } }

        internal ScriptFieldInfo FieldInfo { get { return fieldInfo; } }

        public IScriptObject GetFieldValue(ScriptContext context)
        {
            if (fieldInfo != null) return ((ScriptObjectBase)instance).GetValue(fieldInfo);
            return instance.GetValue(context, field);
        }

        public void SetFieldValue(ScriptContext context, IScriptObject value)
        {
            if (fieldInfo != null) ((ScriptObjectBase)instance).SetValue(fieldInfo, value);
            else instance.SetValue(context, field, value);
        }

        public IScriptObject GetFieldValue2(ScriptContext context)
        {
            if (fieldInfo2 != null) return ((ScriptObjectBase)instance).GetValue(fieldInfo2);
            return instance.GetValue(context, fieldInfo != null ? fieldInfo.FieldName : field);
        }

        public void RemoveField(ScriptContext context)
        {
            string name = field;
            if (name == null && fieldInfo != null) name = fieldInfo.FieldName;
            instance.Remove(context, name);
        }

        #region IScriptObject

        IScriptObject IScriptObject.GetValue(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        void IScriptObject.SetValue(ScriptContext context, string name, IScriptObject value)
        {
            throw new NotImplementedException();
        }

        bool IScriptObject.Remove(ScriptContext context, string name)
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

        string IScriptObject.TypeName { get { throw new NotImplementedException(); } }

        #endregion

        #region IScriptEnumerable

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ScriptMemberProxy : IScriptObject
    {
        private IScriptObject instance;
        private IScriptObject member;

        public ScriptMemberProxy(IScriptObject instance, IScriptObject member)
        {
            this.instance = instance;
            this.member = member;
        }

        public IScriptObject Instance { get { return instance; } }

        public IScriptObject Member { get { return member; } }

        #region IScriptObject

        string IScriptObject.TypeName
        {
            get
            {
                throw new NotImplementedException();
            }
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

        #region IScriptEnumerable

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ScriptObjectEnumerator : IScriptObject
    {
        private IEnumerator en;

        public ScriptObjectEnumerator(IEnumerator en) { this.en = en; }

        public bool MoveNext() { return en != null && en.MoveNext(); }

        public IScriptObject GetCurrentKey(ScriptContext context) { return ScriptGlobal.ConvertValue(context, en.Current); }

        #region IScriptObject

        IScriptObject IScriptObject.GetValue(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        void IScriptObject.SetValue(ScriptContext context, string name, IScriptObject value)
        {
            throw new NotImplementedException();
        }

        bool IScriptObject.Remove(ScriptContext context, string name)
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

        string IScriptObject.TypeName { get { throw new NotImplementedException(); } }

        #endregion

        #region IScriptEnumerable

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
