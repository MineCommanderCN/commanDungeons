using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ShenGu.Script
{
    public class ScriptNativeObject : ScriptObjectBase, IScriptArray
    {
        private ScriptMemberList members;
        private object instance;
        private ScriptNativeObject(object instance, IScriptMemberList instanceMembers)
        {
            this.instance = instance;
            this.InitValueMembers(instanceMembers);
            this.members = instanceMembers as ScriptMemberList;
        }

        internal static ScriptNativeObject CreateNativeObject(ScriptContext context, object instance, IScriptMemberList instanceMembers)
        {
            ScriptNativeObject result = new ScriptNativeObject(instance, instanceMembers);
            IScriptNativeProxy proxy = instance as IScriptNativeProxy;
            if (proxy != null)
                proxy.AfterCreated(context, result);
            return result;
        }

        public object Instance { get { return instance; } }

        public override object ToValue(ScriptContext context)
        {
            IScriptProxy proxy = instance as IScriptProxy;
            return proxy == null ? instance : proxy.RealInstance;
        }

        public override string ToValueString(ScriptContext context)
        {
            return "[Native Object]";
        }

        public override IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            IEnumerator result = base.GetEnumerator(context, isKey);
            IScriptEnumerable en = instance as IScriptEnumerable;
            if (en != null)
                result = new ListEnumerator(en.GetEnumerator(context, isKey), result);
            return result;
        }

        public void AddScriptMember(ScriptContext context, string name, string script)
        {
            IScriptObject value = context.Eval(script);
            SetValue(context, name, value);
        }

        #region IScriptArray

        public bool IsArray { get { return members != null && members.IndexInfo != null; } }

        int IScriptArray.ArrayLength
        {
            get
            {
                if (members != null && members.ArrayLengthInfo != null)
                {
                    object result = members.ArrayLengthInfo.GetValue(instance, null);
                    if (result is int) return (int)result;
                    return Convert.ToInt32(result);
                }
                return -1;
            }
        }

        public IScriptObject GetElementValue(ScriptContext context, int index)
        {
            object value = members.IndexInfo.GetValue(instance, new object[] { index });
            return ScriptGlobal.ConvertValue(context, value);
        }

        public void SetElementValue(ScriptContext context, int index, IScriptObject value)
        {
            object v = ScriptGlobal.ConvertValue(context, value, members.IndexInfo.PropertyType);
            members.IndexInfo.SetValue(instance, v, new object[] { index });
        }

        #endregion

        #region 内部类

        private struct ListEnumerator : IEnumerator
        {
            private IEnumerator list, list2;
            private object current;

            public ListEnumerator(IEnumerator list, IEnumerator list2)
            {
                this.list = list;
                this.list2 = list2;
                this.current = null;
            }

            object IEnumerator.Current { get { return current; } }

            public bool MoveNext()
            {
                if (list.MoveNext())
                {
                    current = list.Current;
                    return true;
                }
                if (list2.MoveNext())
                {
                    current = list2.Current;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                list.Reset();
                list2.Reset();
            }
        }

        #endregion
    }

    public class ScriptNativeArray : ScriptObjectBase, IScriptArray
    {
        private Array instance;

        internal ScriptNativeArray(Array instance) { this.instance = instance; }

        public bool IsArray { get { return true; } }

        public IScriptObject GetElementValue(ScriptContext context, int index)
        {
            object value = instance.GetValue(index);
            return ScriptGlobal.ConvertValue(context, value);
        }

        public void SetElementValue(ScriptContext context, int index, IScriptObject value)
        {
            Type elemType = instance.GetType().GetElementType();
            object v = ScriptGlobal.ConvertValue(context, value, elemType);
            instance.SetValue(v, index);
        }

        public override object ToValue(ScriptContext context)
        {
            return instance;
        }

        public override string ToValueString(ScriptContext context)
        {
            return "[Native Array]";
        }

        public override IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            if (isKey)
                for (int i = 0; i < instance.Length; i++)
                    yield return i;
            else
                foreach (object r in instance)
                    yield return r;
        }

        [ObjectMember("length")]
        public int Length
        {
            get { return ArrayLength; }
        }

        public int ArrayLength
        {
            get { return instance.Length; }
        }
    }

    public class ScriptNativeDelegate : ScriptFunctionBase
    {
        private ScriptMethodInfo info;

        private ScriptNativeDelegate() { }

        internal ScriptNativeDelegate(Delegate instance)
        {
            this.info = new ScriptMethodInfo(instance);
        }

        protected override ScriptFunctionBase OnBind(ScriptContext context, IScriptObject instance)
        {
            ScriptNativeDelegate result = new ScriptNativeDelegate();
            result.info = info;
            return result;
        }
        
        protected internal override IScriptObject Invoke(ScriptContext context, bool isScriptEnv, bool isNewObject, IScriptObject instance, IScriptObject[] argus)
        {
            return info.Invoke(context, isNewObject, null, argus);
        }
    }

    public class ScriptNativeFunction : ScriptFunctionBase, IScriptValueEnumerable
    {
        private object instance;
        private ScriptMemberSource source;
        private ScriptMethodInfo info;
        private bool isEnumerable;

        internal ScriptNativeFunction(ScriptMemberSource source, MethodInfo info, bool isEnumerable)
        {
            this.source = source;
            this.info = new ScriptMethodInfo(info);
            this.isEnumerable = isEnumerable;
        }
        internal ScriptNativeFunction(object instance, MethodInfo info, bool isEnumerable)
        {
            this.instance = instance;
            this.source = this.instance is IScriptObject ? ScriptMemberSource.ScriptObject : ScriptMemberSource.RealValue;
            this.info = new ScriptMethodInfo(info);
            this.isEnumerable = isEnumerable;
        }

        private ScriptNativeFunction(ScriptMemberSource source, ScriptMethodInfo info, bool isEnumerable)
        {
            this.source = source;
            this.info = info;
            this.isEnumerable = isEnumerable;
        }
        
        public ScriptMethodInfo Info { get { return info; } }

        public ScriptMemberSource Source { get { return source; } }

        public object Instance { get { return instance; } }

        public override string ToValueString(ScriptContext context)
        {
            return "[Native Function]";
        }

        private object GetRealInstance(ScriptContext context, IScriptObject instance)
        {
            object result = instance;
            if (result == null)
                throw new ScriptExecuteException("无法对null或undefined对象调用方法。");
            if (source == ScriptMemberSource.ScriptObject)
                result = instance;
            else if (instance is ScriptNativeObject)
                result = ((ScriptNativeObject)instance).Instance;
            else
                result = instance.ToValue(context);
            return result;
        }

        protected internal override IScriptObject Invoke(ScriptContext context, bool isScriptEnv, bool isNewObject, IScriptObject instance, IScriptObject[] argus)
        {
            object obj;
            if (info.IsStatic) obj = null;
            else
            {
                obj = this.instance;
                if (obj == null)
                    obj = GetRealInstance(context, instance);
            }
            return info.Invoke(context, isNewObject, obj, argus);
        }

        protected override ScriptFunctionBase OnBind(ScriptContext context, IScriptObject instance)
        {
            ScriptNativeFunction result = new ScriptNativeFunction(source, info, isEnumerable);
            if (!info.IsStatic)
            {
                object obj = GetRealInstance(context, instance);
                if (obj != null)
                {
                    Type objType = obj.GetType();
                    if (!info.Method.DeclaringType.IsAssignableFrom(objType))
                        throw new ScriptExecuteException(string.Format("bind失败：对象“{0}”无法绑定到方法“{1}.{2}”。", objType.Name, info.Method.DeclaringType.Name, info.Method.Name));
                    result.instance = obj;
                }
            }
            return result;
        }

        protected internal override bool IsReadOnly { get { return true; } }

        public bool IsEnumerable { get { return isEnumerable; } }
    }

    public interface IScriptValueEnumerable
    {
        bool IsEnumerable { get; }
    }

    public interface IScriptProperty : IScriptValueEnumerable
    {
        IScriptObject GetPropValue(ScriptContext context, IScriptObject instance);

        void SetPropValue(ScriptContext context, IScriptObject instance, IScriptObject value);
    }

    public sealed class ScriptNativeProperty : ScriptObjectBase, IScriptProperty
    {
        private object instance;
        private ScriptMemberSource source;
        private PropertyInfo info;
        private bool isEnumerable;
        
        internal ScriptNativeProperty(ScriptMemberSource source, PropertyInfo info, bool isEnumerable) : base(false)
        {
            this.source = source;
            this.info = info;
            this.isEnumerable = isEnumerable;
        }

        internal ScriptNativeProperty(object instance, PropertyInfo info, bool isEnumerable) :base(false)
        {
            this.instance = instance;
            this.info = info;
            this.isEnumerable = isEnumerable;
        }

        public object Instance { get { return instance; } }

        public ScriptMemberSource Source { get { return source; } }

        public PropertyInfo Info { get { return info; } }

        public IScriptObject GetPropValue(ScriptContext context, IScriptObject instance)
        {
            if (!info.CanRead)
                throw new ScriptExecuteException(string.Format("对象“{0}”的属性“{1}”不支持读操作！", info.DeclaringType, info.Name));
            object obj = this.instance;
            if (obj == null)
            {
                if (source == ScriptMemberSource.ScriptObject)
                    obj = instance;
                else if (instance is ScriptNativeObject)
                    obj = ((ScriptNativeObject)instance).Instance;
                else
                    obj = instance.ToValue(context);
            }
            object result = info.GetValue(obj, null);
            return ScriptGlobal.ConvertValue(context, result);
        }

        public void SetPropValue(ScriptContext context, IScriptObject instance, IScriptObject value)
        {
            if (!info.CanWrite)
                throw new ScriptExecuteException(string.Format("对象“{0}”的属性“{1}”不支持写操作！", info.DeclaringType, info.Name));
            object obj = this.instance;
            if (obj == null)
            {
                if (source == ScriptMemberSource.ScriptObject)
                    obj = instance;
                else if (instance is ScriptNativeObject)
                    obj = ((ScriptNativeObject)instance).Instance;
                else
                    obj = instance.ToValue(context);
            }
            object propValue = ScriptGlobal.ConvertValue(context, value, info.PropertyType);
            info.SetValue(obj, propValue, null);
        }

        public bool IsEnumerable { get { return isEnumerable; } }

        public override object ToValue(ScriptContext context)
        {
            return info;
        }

        public override string ToValueString(ScriptContext context)
        {
            return "[Native Property]";
        }
        protected internal override bool IsReadOnly { get { return true; } }
    }

    public class ScriptType : ScriptFunctionBase
    {
        private Type type;
        private ScriptMethodInfo info;

        public ScriptType(Type type)
        {
            this.type = type;
            this.Init(null);
        }

        public ScriptType(ScriptContext context, Type type)
        {
            this.type = type;
            this.Init(context);
        }

        private ScriptType(Type type, ScriptMethodInfo info)
        {
            this.type = type;
            this.info = info;
        }

        private void Init(ScriptContext context)
        {
            IScriptMemberList valueList = ScriptGlobal.GetTypeMembers(context, type);
            if (valueList != null)
            {
                info = valueList.Constructor;
                InitValueMembers(valueList);
            }
            else
            {
                ConstructorInfo conInfo = type.GetConstructor(Type.EmptyTypes);
                if (conInfo != null)
                    info = new ScriptMethodInfo(conInfo);
            }
        }

        public override object ToValue(ScriptContext context)
        {
            return type;
        }

        protected internal override IScriptObject Invoke(ScriptContext context, bool isScriptEnv, bool isNewObject, IScriptObject instance, IScriptObject[] argus)
        {
            if (info == null)
                throw new ScriptExecuteException(string.Format("方法调用失败：类型“{0}”无法解析到构造函数。", type));
            IScriptObject result = info.Invoke(context, isNewObject, null, argus);
            if (isNewObject)
            {
                ScriptObjectBase obj = result as ScriptObjectBase;
                if (obj != null)
                    obj.Parent = this.ProtoType;
            }
            return result;
        }

        protected override ScriptFunctionBase OnBind(ScriptContext context, IScriptObject instance)
        {
            return new ScriptType(type, info);
        }
    }

    public sealed class ScriptMethodArgus
    {
        private ScriptContext context;
        private IScriptObject[] argus;
        private object result;
        private object instance;
        private bool isNewObject;
        private bool isResultSaved, isInstanceSaved;

        public ScriptMethodArgus(ScriptContext context, bool isNewObject, IScriptObject[] argus)
        {
            this.context = context;
            this.isNewObject = isNewObject;
            this.argus = argus;
        }

        public ScriptContext Context { get { return context; } }

        public bool IsNewObject { get { return isNewObject; } }

        public IScriptObject[] Arguments { get { return argus; } }

        public bool HasArguments { get { return argus != null && argus.Length > 0; } }
        
        public object Result { get { return result; } }

        public void SaveResult(object value)
        {
            this.result = value;
            this.isResultSaved = true;
        }

        public bool IsResultSaved { get { return isResultSaved; } }

        public object Instance { get { return instance; } set { instance = value; } }

        public void SaveInstance(object value)
        {
            this.instance = value;
            this.isInstanceSaved = true;
        }

        public bool IsInstanceSaved { get { return isInstanceSaved; } }
    }

    public class ScriptMethodInfo
    {
        private readonly static Type TYPE_Context = typeof(ScriptContext), TYPE_MethodArgus = typeof(ScriptMethodArgus);
        private Delegate delegateMethod;
        private MethodBase method;
        private ConstructorInfo constructor;

        internal ScriptMethodInfo(MethodBase method)
        {
            this.method = method;
            this.constructor = method as ConstructorInfo;
        }

        internal ScriptMethodInfo(Delegate delegateMethod)
        {
            this.delegateMethod = delegateMethod;
        }

        public Delegate DelegateMethod { get { return delegateMethod; } }

        public MethodBase Method { get { return method; } }

        public bool IsStatic { get { return constructor == null && method.IsStatic; } }

        internal IScriptObject Invoke(ScriptContext context, bool isNewObject, object instance, IScriptObject[] argus)
        {
            MethodBase m = delegateMethod != null ? delegateMethod.Method : method;
            ParameterInfo[] ptypes = m.GetParameters();
            ScriptMethodArgus conArgus = null;
            int length = ptypes.Length;
            object[] argValues = new object[length];
            if (length > 0)
            {
                int argusLen = argus != null ? argus.Length : 0;
                int i2 = 0;
                for (int i = 0; i < length; i++)
                {
                    Type ptype = ptypes[i].ParameterType;
                    if (TYPE_Context.IsAssignableFrom(ptype))
                        argValues[i] = context;
                    else if (TYPE_MethodArgus.IsAssignableFrom(ptype))
                    {
                        if (conArgus == null) conArgus = new ScriptMethodArgus(context, isNewObject, argus);
                        argValues[i] = conArgus;
                    }
                    else
                    {
                        IScriptObject value = i2 < argusLen ? argus[i2++] : ScriptUndefined.Instance;
                        argValues[i] = ScriptGlobal.ConvertValue(context, value, ptypes[i].ParameterType);
                    }
                }
            }
            object r;
            if (delegateMethod != null) r = delegateMethod.DynamicInvoke(argValues);
            else if (constructor != null) r = constructor.Invoke(argValues);
            else r = method.Invoke(instance, argValues);
            if (conArgus != null)
            {
                if (isNewObject)
                {
                    if (conArgus.IsInstanceSaved) r = conArgus.Instance;
                }
                else if (conArgus.IsResultSaved) r = conArgus.Result;
            }
            return ScriptGlobal.ConvertValue(context, r);
        }
    }

    public class ScriptTypeMembers
    {
        private static readonly Type ScriptType = typeof(IScriptObject);
        private Type type, proxyType;
        private IScriptMemberList instanceMembers, typeMembers;

        internal ScriptTypeMembers(Type type, Type proxyType, IScriptMemberList typeMembers, IScriptMemberList instanceMembers)
        {
            this.type = type;
            this.proxyType = proxyType;
            this.typeMembers = typeMembers;
            this.instanceMembers = instanceMembers;
        }

        public Type Type { get { return type; } }

        public Type ProxyType { get { return proxyType; } }

        public IScriptMemberList TypeMembers { get { return typeMembers; } }

        public IScriptMemberList InstanceMembers { get { return instanceMembers; } }

        public static ScriptTypeMembers Load(Type type)
        {
            object[] proxyAttrs = type.GetCustomAttributes(typeof(ScriptProxyAttribute), false);
            Type t = type;
            Type proxyType = null;
            if (proxyAttrs != null && proxyAttrs.Length > 0)
            {
                ScriptProxyAttribute attr = ((ScriptProxyAttribute)proxyAttrs[0]);
                if (attr.RealType != null)
                {
                    if (!typeof(IScriptProxy).IsAssignableFrom(type))
                        throw new ArgumentOutOfRangeException("type", string.Format("代理类型“{0}”必须实现接口：IScriptProxy。", type));
                    proxyType = type;
                    type = attr.RealType;
                }
            }
            ScriptMemberList typeMembers = ScriptMemberList.LoadTypeMembers(type, proxyType);
            ScriptMemberList instanceMembers = ScriptMemberList.LoadInstanceMembers(type, proxyType);
            return new ScriptTypeMembers(type, proxyType, typeMembers, instanceMembers);
        }

        public static bool IsScriptType(Type type)
        {
            return ScriptType.IsAssignableFrom(type);
        }
    }

    public interface IScriptMemberList : IEnumerable<KeyValuePair<string, IScriptObject>>
    {
        long ObjectId { get; set; }
        int Find(ScriptContext context, string key);
        IScriptObject GetValue(ScriptContext context, IScriptObject instance, int index);
        /// <summary>修改<paramref name="index"/>位置的值。只有值为<c>IScriptProperty</c>时，才能被修改，并返回<c>true</c>，否则返回<c>false</c></summary>
        bool CheckSetValue(ScriptContext context, IScriptObject instance, int index, IScriptObject value);
        int Count { get; }
        ScriptMethodInfo Constructor { get; }
    }

    public interface IScriptMemberSupportProxy
    {
        Type RealType { get; }
        Type ProxyType { get; }
    }

    public abstract class ScriptMemberListBase : HashEntryList<IScriptObject>, IScriptMemberList
    {
        public virtual ScriptMethodInfo Constructor { get { throw new NotImplementedException(); } }

        public long ObjectId { get; set; }
        public int Find(ScriptContext context, string key) { return InnerFind(context, key); }

        public IScriptObject GetValue(ScriptContext context, IScriptObject instance, int index)
        {
            IScriptObject result = InnerGetValue(context, index);
            return ScriptHelper.CheckGetPropValue(context, instance, result);
        }

        public bool CheckSetValue(ScriptContext context, IScriptObject instance, int index, IScriptObject value)
        {
            IScriptObject propValue = InnerGetValue(context, index);
            return ScriptHelper.CheckSetPropValue(context, instance, propValue, value);
        }

    }

    public class ScriptMemberList : ScriptMemberListBase, IScriptMemberSupportProxy
    {
        private ScriptMethodInfo contructor;
        private PropertyInfo indexInfo, arrayLengthInfo;
        private Type type, proxyType;

        private ScriptMemberList(Type type, Type proxyType)
        {
            this.type = type;
            this.proxyType = proxyType;
        }
        
        public Type RealType { get { return type; } }

        public Type ProxyType { get { return proxyType; } }

        public PropertyInfo IndexInfo { get { return indexInfo; } }

        public PropertyInfo ArrayLengthInfo { get { return arrayLengthInfo; } }

        private static string CheckFirstLowerLetter(string name)
        {
            char ch = name[0];
            if (ch >= 'A' && ch <= 'Z')
            {
                ch = (char)(ch - ('A' - 'a'));
                name = ch + name.Substring(1);
            }
            return name;
        }

        private static bool IsStaticProperty(PropertyInfo pinfo)
        {
            MethodInfo m = pinfo.GetGetMethod();
            if (m == null) m = pinfo.GetSetMethod();
            return m.IsStatic;
        }

        private static void AddToList(ScriptContext context, bool firstLowerLetter, ObjectMemberFlags memberFlags, MemberInfo[] members, ScriptMemberSource source, ScriptMemberList list)
        {
            foreach (MemberInfo info in members)
            {
                object[] objAttrs = info.GetCustomAttributes(typeof(ObjectMemberAttribute), true);
                string name = null;
                bool isEnumerable = true;
                if (objAttrs != null && objAttrs.Length > 0)
                {
                    ObjectMemberAttribute attr = (ObjectMemberAttribute)objAttrs[0];
                    name = attr.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = info.Name;
                        if (firstLowerLetter)
                            name = CheckFirstLowerLetter(name);
                    }
                    isEnumerable = attr.IsEnumerable;
                }
                else if (memberFlags > ObjectMemberFlags.None)
                {
                    objAttrs = info.GetCustomAttributes(typeof(IgnoreMemberAttribute), true);
                    if (objAttrs == null || objAttrs.Length == 0)
                    {
                        MethodInfo minfo = info as MethodInfo;
                        if (minfo != null)
                        {
                            if (minfo.IsStatic)
                            {
                                if ((memberFlags & ObjectMemberFlags.StaticMethods) != ObjectMemberFlags.None)
                                    name = info.Name;
                            }
                            else if ((memberFlags & ObjectMemberFlags.Methods) != ObjectMemberFlags.None)
                                name = info.Name;
                        }
                        else
                        {
                            PropertyInfo pinfo = info as PropertyInfo;
                            if (pinfo != null)
                            {
                                if (IsStaticProperty(pinfo))
                                {
                                    if ((memberFlags & ObjectMemberFlags.StaticProperties) != ObjectMemberFlags.None)
                                        name = info.Name;
                                }
                                else if ((memberFlags & ObjectMemberFlags.Properties) != ObjectMemberFlags.None)
                                    name = info.Name;
                            }
                        }
                        if (name != null && firstLowerLetter)
                            name = CheckFirstLowerLetter(name);
                    }
                }
                if (name != null)
                {
                    MethodInfo minfo = info as MethodInfo;
                    if (minfo != null)
                        list.InnerSetValue(context, name, new ScriptNativeFunction(source, minfo, isEnumerable));
                    else
                    {
                        PropertyInfo pinfo = info as PropertyInfo;
                        if (pinfo != null)
                        {
                            ParameterInfo[] argInfos = pinfo.GetIndexParameters();
                            if (argInfos != null && argInfos.Length > 0)
                            {
                                if (argInfos.Length == 1 && argInfos[0].ParameterType == typeof(int) && !IsStaticProperty(pinfo))
                                {
                                    list.indexInfo = pinfo;
                                }
                            }
                            else
                                list.InnerSetValue(context, name, new ScriptNativeProperty(source, pinfo, isEnumerable));
                        }
                    }
                }
            }
        }

        private static ScriptMethodInfo GetConstructor(MethodBase[] members)
        {
            foreach (MethodBase info in members)
            {
                object[] objAttrs = info.GetCustomAttributes(typeof(ObjectConstructorAttribute), true);
                if (objAttrs != null && objAttrs.Length > 0)
                    return new ScriptMethodInfo(info);
            }
            return null;
        }

        private static void AddMembers(ScriptContext context, Type type, bool isStatic, ScriptMemberList toList)
        {
            object[] objAttrs = type.GetCustomAttributes(typeof(AddMemberAttribute), true);
            if (objAttrs != null && objAttrs.Length > 0)
            {
                foreach (object objAttr in objAttrs)
                {
                    AddMemberAttribute attr = (AddMemberAttribute)objAttr;
                    if (isStatic == attr.IsStatic)
                    {
                        IScriptObject value = new ScriptContext().Eval(attr.Script);
                        if (value != null)
                        {
                            ScriptObjectBase scriptValue = value as ScriptObjectBase;
                            if (scriptValue != null) scriptValue.SetReadOnly();
                            toList.InnerSetValue(context, attr.Name, value);
                        }
                    }
                }
            }
        }

        private static ScriptMemberList InternalLoadInstance(ScriptContext context, Type type, Type proxyType, ScriptMemberSource source)
        {
            ScriptMemberList result = new ScriptMemberList(type, proxyType);
            Type t = proxyType == null ? type : proxyType;
            bool firstLowerLetter;
            ObjectMemberFlags flags;
            object[] objAttrs = t.GetCustomAttributes(typeof(ScriptObjectAttribute), true);
            if (objAttrs != null && objAttrs.Length > 0)
            {
                ScriptObjectAttribute attr = (ScriptObjectAttribute)objAttrs[0];
                firstLowerLetter = attr.FirstLowerLetter;
                flags = attr.MemberFlags;
            }
            else
            {
                firstLowerLetter = false;
                flags = ObjectMemberFlags.None;
            }
            MemberInfo[] members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty);
            AddToList(context, firstLowerLetter, flags, members, source, result);
            //AddMembers(context, t, false, result);
            if (result.indexInfo != null)
            {
                PropertyInfo pinfo;
                if (typeof(ICollection).IsAssignableFrom(t))
                    pinfo = typeof(ICollection).GetProperty("Count");
                else
                {
                    pinfo = t.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (pinfo == null)
                        pinfo = t.GetProperty("Length", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (pinfo != null)
                    {
                        TypeCode tcode = Type.GetTypeCode(pinfo.PropertyType);
                        if (tcode != TypeCode.Int32 && tcode != TypeCode.Int64) pinfo = null;
                        else
                        {
                            ParameterInfo[] argInfos = pinfo.GetIndexParameters();
                            if (argInfos != null && argInfos.Length > 0) pinfo = null;
                        }
                    }
                }
                result.arrayLengthInfo = pinfo;
            }
            return result;
        }

        private static ScriptMemberList InternalLoadType(ScriptContext context, Type type, Type proxyType, ScriptMemberSource source)
        {
            ScriptMemberList result = new ScriptMemberList(type, proxyType);
            Type t = proxyType == null ? type : proxyType;
            bool firstLowerLetter;
            ObjectMemberFlags flags;
            object[] objAttrs = t.GetCustomAttributes(typeof(ScriptObjectAttribute), true);
            if (objAttrs != null && objAttrs.Length > 0)
            {
                ScriptObjectAttribute attr = (ScriptObjectAttribute)objAttrs[0];
                firstLowerLetter = attr.FirstLowerLetter;
                flags = attr.MemberFlags;
            }
            else
            {
                firstLowerLetter = false;
                flags = ObjectMemberFlags.None;
            }
            MemberInfo[] members = t.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty);
            AddToList(context, firstLowerLetter, flags, members, source, result);
            //AddMembers(context, t, true, result);

            MethodInfo[] staticMethods = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            result.contructor = GetConstructor(staticMethods);
            if (result.contructor == null)
            {
                result.contructor = GetConstructor(t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                if (result.contructor == null)
                {
                    ConstructorInfo conInfo = t.GetConstructor(Type.EmptyTypes);
                    if (conInfo != null)
                        result.contructor = new ScriptMethodInfo(conInfo);
                }
            }
            return result;
        }
        
        public static ScriptMemberList LoadInstanceMembers(Type type, Type proxyType)
        {
            Type t = proxyType == null ? type : proxyType;
            ScriptMemberSource source = ScriptTypeMembers.IsScriptType(t) ? ScriptMemberSource.ScriptObject : ScriptMemberSource.RealValue;
            return InternalLoadInstance(null, type, proxyType, source);
        }

        public static ScriptMemberList LoadTypeMembers(Type type, Type proxyType)
        {
            Type t = proxyType == null ? type : proxyType;
            ScriptMemberSource source = ScriptTypeMembers.IsScriptType(t) ? ScriptMemberSource.ScriptObject : ScriptMemberSource.RealValue;
            return InternalLoadType(null, type, proxyType, source);
        }

        public override ScriptMethodInfo Constructor { get { return contructor; } }
    }

    public enum ScriptMemberSource
    {
        ScriptObject, RealValue
    }

    public class ScriptMappingList : ScriptMemberListBase
    {
        public void Register(string key, IScriptObject value)
        {
            InnerSetValue(null, key, value);
        }

        public void Register(string key, CreateScriptObjectHandler objectCreator)
        {
            InnerSetValue(null, key, new ScriptObjectCreator(objectCreator));
        }

        private void InternalAddMappings(Type type, object instance)
        {
            object[] typeAttrs = type.GetCustomAttributes(typeof(ScriptMappingAttribute), false);
            if (typeAttrs != null && typeAttrs.Length > 0)
            {
                ScriptMappingAttribute attr = (ScriptMappingAttribute)typeAttrs[0];
                string name = attr.Name;
                if (string.IsNullOrEmpty(name)) name = type.Name;
                Register(name, new ScriptType(null, type));
            }
            BindingFlags bflags = instance == null ? BindingFlags.Static : BindingFlags.Instance;
            bflags |= BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty;
            MemberInfo[] infos = type.GetMembers(bflags);
            ScriptMemberSource source = ScriptMemberSource.RealValue;
            if (instance == null && ScriptTypeMembers.IsScriptType(type))
                source = ScriptMemberSource.ScriptObject;
            foreach(MemberInfo info in infos)
            {
                object[] attrs = info.GetCustomAttributes(typeof(ScriptMappingAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    ScriptMappingAttribute attr = (ScriptMappingAttribute)attrs[0];
                    string name = attr.Name;
                    if (string.IsNullOrEmpty(name)) name = info.Name;
                    MethodInfo minfo = info as MethodInfo;
                    if (minfo != null)
                    {
                        ScriptNativeFunction func = instance != null ? new ScriptNativeFunction(instance, minfo, false) : new ScriptNativeFunction(source, minfo, false);
                        Register(name, func);
                    }
                    else
                    {
                        PropertyInfo pinfo = (PropertyInfo)info;
                        ScriptNativeProperty prop = instance != null ? new ScriptNativeProperty(instance, pinfo, false) : new ScriptNativeProperty(source, pinfo, false);
                        Register(name, prop);
                    }
                }
            }
        }
        
        public void AddMappings(Type type)
        {
            InternalAddMappings(type, null);
        }

        public void AddMappings(object instance)
        {
            if (instance != null)
            {
                if (instance is Type) AddMappings((Type)instance);
                else InternalAddMappings(instance.GetType(), instance);
            }
        }

        internal IScriptObject InternalGetValue(ScriptContext context, int index)
        {
            return InnerGetValue(context, index);
        }
    }

    internal class ScriptMemberListProxy : IScriptMemberList, IScriptMemberSupportProxy
    {
        private IScriptMemberList realInstance;

        public ScriptMemberListProxy(IScriptMemberList instance)
        {
            this.realInstance = instance;
        }

        public ScriptMethodInfo Constructor { get { return realInstance.Constructor; } }

        public int Count { get { return realInstance.Count; } }

        public long ObjectId { get; set; }

        public Type RealType
        {
            get
            {
                IScriptMemberSupportProxy proxy = this.realInstance as IScriptMemberSupportProxy;
                return proxy != null ? proxy.RealType : null;
            }
        }

        public Type ProxyType
        {
            get
            {
                IScriptMemberSupportProxy proxy = this.realInstance as IScriptMemberSupportProxy;
                return proxy != null ? proxy.ProxyType : null;
            }
        }

        public int Find(ScriptContext context, string key)
        {
            return realInstance.Find(context, key);
        }

        public IEnumerator<KeyValuePair<string, IScriptObject>> GetEnumerator()
        {
            return realInstance.GetEnumerator();
        }

        public IScriptObject GetValue(ScriptContext context, IScriptObject instance, int index)
        {
            return realInstance.GetValue(context, instance, index);
        }

        public bool CheckSetValue(ScriptContext context, IScriptObject instance, int index, IScriptObject value)
        {
            return realInstance.CheckSetValue(context, instance, index, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)realInstance).GetEnumerator();
        }
    }

    public interface IScriptProxy
    {
        object RealInstance { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ScriptProxyAttribute : Attribute
    {
        private Type realType;

        public ScriptProxyAttribute(Type realType)
        {
            this.realType = realType;
        }

        public Type RealType
        {
            get { return realType; }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ScriptMappingAttribute : Attribute
    {
        private string name;

        public ScriptMappingAttribute() { }

        public ScriptMappingAttribute(string name) { this.name = name; }

        public string Name { get { return name; } }
    }

    [Flags]
    public enum ObjectMemberFlags
    {
        None = 0,
        Properties = 1,
        Methods = 2,
        StaticProperties = 4,
        StaticMethods = 8,

        Default = Properties,
        AllMembers = Properties | Methods,
        AllStaticMembers = StaticProperties | StaticMethods,
        All = AllMembers | AllStaticMembers
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ScriptObjectAttribute : Attribute
    {
        private ObjectMemberFlags memberFlags;
        private bool firstLowerLetter;

        public ScriptObjectAttribute(ObjectMemberFlags memberFlags)
        {
            this.memberFlags = memberFlags;
        }

        public ScriptObjectAttribute()
        {
            this.memberFlags = ObjectMemberFlags.Default;
        }

        public ObjectMemberFlags MemberFlags
        {
            get { return memberFlags; }
        }

        public bool FirstLowerLetter
        {
            get { return firstLowerLetter; }
            set { firstLowerLetter = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ObjectMemberAttribute : Attribute
    {
        private string name;
        private bool isEnumerable;

        public ObjectMemberAttribute() { }

        public ObjectMemberAttribute(string name) { this.name = name; }

        public string Name
        {
            get { return name; }
        }

        public bool IsEnumerable
        {
            get { return isEnumerable; }
            set { isEnumerable = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class ObjectConstructorAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AddMemberAttribute : Attribute
    {
        private string name, script;
        private bool isStatic;

        public AddMemberAttribute(string name, string script)
        {
            this.name = name;
            this.script = script;
        }

        public bool IsStatic { get { return isStatic; } set { isStatic = value; } }

        public string Name { get { return name; } }

        public string Script { get { return script; } }
    }

    public delegate IScriptObject CreateScriptObjectHandler(ScriptContext context);

    public class ScriptObjectCreator : IScriptObject
    {
        private CreateScriptObjectHandler creator;

        public ScriptObjectCreator(CreateScriptObjectHandler creator)
        {
            if (creator == null) throw new ArgumentNullException("creator");
            this.creator = creator;
        }

        public CreateScriptObjectHandler Creator { get { return creator; } }

        public IScriptObject CreateInstance(ScriptContext context)
        {
            return creator(context);
        }

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

        IEnumerator IScriptEnumerable.GetEnumerator(ScriptContext context, bool isKey)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public interface IScriptNativeProxy
    {
        void AfterCreated(ScriptContext context, ScriptNativeObject obj);
    }
}
