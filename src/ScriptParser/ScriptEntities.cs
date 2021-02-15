using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ShenGu.Script
{
    public class HashEntryList<TValue> : IEnumerable<KeyValuePair<string, TValue>>
    {
        #region 内部类

        private struct Entry
        {
            public int hashCode;
            public int next;
            public string key;
            public TValue value;

            public override string ToString()
            {
                return string.Format("[H: {0}, K: {1}, V: {2}]", hashCode, key, value);
            }
        }

        private struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>, IEnumerator
        {
            private HashEntryList<TValue> dictionary;
            private int version;
            private int index;
            private KeyValuePair<string, TValue> current;
            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            public KeyValuePair<string, TValue> Current { get { return current; } }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == dictionary.count + 1)
                        throw new ArgumentOutOfRangeException("value");
                    return new KeyValuePair<string, TValue>(current.Key, current.Value);
                }
            }

            internal Enumerator(HashEntryList<TValue> dictionary)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                current = default(KeyValuePair<string, TValue>);
            }
            
            public bool MoveNext()
            {
                if (version != dictionary.version)
                    throw new ArgumentOutOfRangeException("version");
                while ((uint)index < (uint)dictionary.count)
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<string, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }
                index = dictionary.count + 1;
                current = default(KeyValuePair<string, TValue>);
                return false;
            }
            
            void IEnumerator.Reset()
            {
                if (version != dictionary.version)
                    throw new ArgumentOutOfRangeException("version");
                index = 0;
                current = default(KeyValuePair<string, TValue>);
            }

            void IDisposable.Dispose() { }
        }

        #endregion

        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private static IEqualityComparer<string> comparer = StringComparer.Ordinal;

        #region 私有方法

        #region 质数
        private static readonly int[] primes = new int[72] {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };
        #endregion

        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int num = (int)Math.Sqrt(candidate);
                for (int i = 3; i <= num; i += 2)
                {
                    if (candidate % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            return candidate == 2;
        }

        private static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentOutOfRangeException("min");
            for (int i = 0; i < primes.Length; i++)
            {
                int num = primes[i];
                if (num >= min)
                {
                    return num;
                }
            }
            for (int j = min | 1; j < int.MaxValue; j += 2)
            {
                if (IsPrime(j) && (j - 1) % 101 != 0)
                {
                    return j;
                }
            }
            return min;
        }

        private static int ExpandPrime(int oldSize)
        {
            int num = 2 * oldSize;
            if ((uint)num > 2146435069u && 2146435069 > oldSize)
            {
                return 2146435069;
            }
            return GetPrime(num);
        }

        private int CalcHashCode(string key)
        {
            int hashCode = comparer.GetHashCode(key);
            if (hashCode == 0) return 0x4321;
            return hashCode & int.MaxValue;
        }

        private void Initialize(int capacity)
        {
            int prime = GetPrime(capacity);
            buckets = new int[prime];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = -1;
            }
            entries = new Entry[prime];
            freeList = -1;
        }

        private void Resize()
        {
            Resize(ExpandPrime(count), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            int[] array = new int[newSize];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = -1;
            }
            Entry[] array2 = new Entry[newSize];
            Array.Copy(entries, 0, array2, 0, count);
            if (forceNewHashCodes)
            {
                for (int j = 0; j < count; j++)
                {
                    if (array2[j].hashCode != -1)
                    {
                        array2[j].hashCode = CalcHashCode(array2[j].key);
                    }
                }
            }
            for (int k = 0; k < count; k++)
            {
                if (array2[k].hashCode >= 0)
                {
                    int num = array2[k].hashCode % newSize;
                    array2[k].next = array[num];
                    array[num] = k;
                }
            }
            buckets = array;
            entries = array2;
        }

        private void InternalSetValue(int index, TValue value)
        {
            TValue oldValue = entries[index].value;
            entries[index].value = value;
            version++;
            OnModified(entries[index].key, oldValue, value);
        }

        #endregion

        #region 可重载方法

        protected virtual void OnModified(string key, TValue oldValue, TValue newValue) { }

        protected virtual void OnAdded(string key, TValue newValue) { }

        protected virtual void OnRemoved(string key, TValue oldValue) { }

        #endregion

        #region 内部方法

        /// <summary>查找相应的<paramref name="key"/>对应的位置</summary>
        protected virtual int InnerFind(ScriptContext context, string key)
        {
            if (buckets != null)
            {
                int hashCode = CalcHashCode(key);
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>添加元素，命中返回正数；没命中返回新位置的取反值</summary>
        protected virtual int InnerSetValue(ScriptContext context, string key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            CheckReadOnly();
            if (buckets == null)
                Initialize(0);
            int hashCode = CalcHashCode(key);
            int bucketIndex = hashCode % buckets.Length;
            int counter = 0;
            for (int index = buckets[bucketIndex]; index >= 0; index = entries[index].next)
            {
                if (entries[index].hashCode == hashCode && comparer.Equals(entries[index].key, key))
                    InternalSetValue(index, value);
                counter++;
            }
            int newIndex;
            if (freeCount > 0)
            {
                newIndex = freeList;
                freeList = entries[newIndex].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    bucketIndex = hashCode % buckets.Length;
                }
                newIndex = count;
                count++;
            }
            entries[newIndex].hashCode = hashCode;
            entries[newIndex].next = buckets[bucketIndex];
            entries[newIndex].key = key;
            entries[newIndex].value = value;
            buckets[bucketIndex] = newIndex;
            version++;
            if (counter > 100)
                Resize(entries.Length, true);
            OnAdded(key, value);
            return ~newIndex;
        }

        protected virtual bool InnerSetValue(ScriptContext context, int index, TValue value)
        {
            if (index < 0 || index > count)
                throw new ArgumentNullException("index");
            CheckReadOnly();
            if (entries[index].hashCode > 0)
            {
                InternalSetValue(index, value);
                return true;
            }
            else
                throw new ArgumentOutOfRangeException("index", string.Format("错误的索引位置！"));
        }

        protected virtual TValue InnerGetValue(ScriptContext context, int index)
        {
            if (index < 0 || index > count)
                throw new ArgumentNullException("index");
            if (entries[index].hashCode > 0)
                return entries[index].value;
            else
                throw new ArgumentOutOfRangeException("index", string.Format("错误的索引位置！"));
        }

        protected virtual TValue InnerGetValue(ScriptContext context, string name)
        {
            int index = InnerFind(context, name);
            if (index >= 0) return InnerGetValue(context, index);
            return default(TValue);
        }

        protected virtual bool InnerRemove(ScriptContext context, string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            CheckReadOnly();
            if (buckets != null)
            {
                int num = CalcHashCode(key);
                int num2 = num % buckets.Length;
                int num3 = -1;
                for (int num4 = buckets[num2]; num4 >= 0; num4 = entries[num4].next)
                {
                    if (entries[num4].hashCode == num && comparer.Equals(entries[num4].key, key))
                    {
                        TValue oldValue = entries[num4].value;
                        if (num3 < 0)
                        {
                            buckets[num2] = entries[num4].next;
                        }
                        else
                        {
                            entries[num3].next = entries[num4].next;
                        }
                        entries[num4].hashCode = -1;
                        entries[num4].next = freeList;
                        entries[num4].key = key;
                        entries[num4].value = default(TValue);
                        freeList = num4;
                        freeCount++;
                        version++;
                        OnRemoved(key, oldValue);
                        return true;
                    }
                    num3 = num4;
                }
            }
            return false;
        }

        internal protected virtual bool IsReadOnly { get { return false; } }
        
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("只读列表无法修改元素。");
        }
        
        #endregion

        #region 公共方法

        public int Count { get { return this.count - this.freeCount; } }

        public IEqualityComparer<string> Comparer
        {
            get
            {
                return comparer;
            }
        }

        #region IEnumerable<KeyValuePair<string, TValue>>

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        internal void CleanAssignTo(HashEntryList<TValue> list, CheckHashValue<TValue> checkValue, object state, TValue[] newValues, int newValueCount)
        {
            int length;
            if (buckets != null)
            {
                length = this.buckets.Length;
                list.buckets = new int[length];
                Array.Copy(this.buckets, list.buckets, length);
            }
            if (entries != null)
            {
                length = this.entries.Length;
                list.entries = new Entry[length];
                Array.Copy(this.entries, list.entries, length);
                if (checkValue != null)
                {
                    for (int i = 0; i < length; i++)
                        list.entries[i].value = checkValue(list.entries[i].value, state);
                }
            }
            list.count = this.count;
            list.version = this.version;
            list.freeList = this.freeList;
            list.freeCount = this.freeCount;

            if (newValues == null) newValueCount = 0;
            else if (newValueCount > newValues.Length) newValueCount = newValues.Length;
            if (newValueCount > list.count) newValueCount = list.count;
            if (newValueCount > 0)
            {
                int index = 0, valueIndex = 0;
                while (index < list.count && valueIndex < newValueCount)
                {
                    if (list.entries[index].hashCode >= 0)
                        list.entries[index].value = newValues[valueIndex++];
                    index++;
                }
            }
        }

        #endregion
    }

    internal delegate TValue CheckHashValue<TValue>(TValue value, object state);
}
