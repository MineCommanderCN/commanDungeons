using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ShenGu.Script
{
    [ScriptProxy(typeof(DataTable))]
    public class DataTableProxy : IScriptProxy
    {
        private DataTable table;
        private IScriptObject rows;

        public object RealInstance
        {
            get { return table; }
            set
            {
                table = (DataTable)value;
                rows = null;
            }
        }

        [ObjectMember("name")]
        public string Name { get { return table.TableName; } }

        [ObjectMember("rows")]
        public IScriptObject Rows
        {
            get
            {
                if (rows == null)
                    rows = ScriptGlobal.ConvertValue(ScriptContext.Current, new DataRowCollectionProxy(table));
                return rows;
            }
        }
    }

    public class DataRowCollectionProxy : IScriptEnumerable
    {
        private DataRowMembers rowMembers;
        private DataTable table;
        private DataRowProxy[] rows;
        private int rowCount;

        public DataRowCollectionProxy(DataTable table)
        {
            this.table = table;
            this.rowCount = table.Rows.Count;
            this.rows = new DataRowProxy[rowCount];
            this.rowMembers = new DataRowMembers(table);
        }

        [ObjectMember]
        public DataRowProxy this[int index]
        {
            get
            {
                if (index < 0 || index >= table.Rows.Count)
                    throw new ArgumentOutOfRangeException("index");
                if (index >= rowCount)
                {
                    this.rowCount = table.Rows.Count;
                    Array.Resize<DataRowProxy>(ref this.rows, rowCount);
                }
                DataRowProxy result = rows[index];
                if (result == null || result.Row != table.Rows[index])
                    rows[index] = result = CreateRowProxy(rowMembers, table.Rows[index]);
                return result;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        protected virtual DataRowProxy CreateRowProxy(DataRowMembers rowMembers, DataRow row)
        {
            return new DataRowProxy(rowMembers, row);
        }

        [ObjectMember("count")]
        public int Count { get { return table.Rows.Count; } }

        public IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            int count = table.Rows.Count;
            if (isKey)
            {
                for (int i = 0; i < count; i++)
                    yield return i;
            }
            else
            {
                if (rowCount != count)
                {
                    if (rowCount < count)
                        Array.Resize<DataRowProxy>(ref rows, count);
                    rowCount = count;
                }
                for (int i = 0; i < rowCount; i++)
                {
                    DataRow r = table.Rows[i];
                    DataRowProxy p = rows[i];
                    if (p != null && p.Row == r) yield return p;
                    else
                    {
                        rows[i] = p = CreateRowProxy(rowMembers, r);
                        yield return p;
                    }
                }
            }
        }
    }

    public class DataRowMembers : IScriptMemberList
    {
        private DataTable table;

        public DataRowMembers(DataTable table)
        {
            this.table = table;
        }

        public ScriptMethodInfo Constructor { get { throw new NotImplementedException(); } }

        public int Count { get { return table.Columns.Count; } }

        public long ObjectId { get; set; }

        public int Find(ScriptContext context, string key)
        {
            return table.Columns.IndexOf(key);
        }

        public IScriptObject GetValue(ScriptContext context, IScriptObject instance, int index)
        {
            DataRowProxy proxy = (DataRowProxy)instance;
            return ScriptGlobal.ConvertValue(context, proxy.Row[index]);
        }

        public bool CheckSetValue(ScriptContext context, IScriptObject instance, int index, IScriptObject value)
        {
            DataRowProxy proxy = (DataRowProxy)instance;
            DataColumn col = table.Columns[index];
            proxy.Row[col] = ScriptGlobal.ConvertValue(context, value, col.DataType);
            return true;
        }

        public IEnumerator<KeyValuePair<string, IScriptObject>> GetEnumerator()
        {
            foreach (DataColumn col in table.Columns)
                yield return new KeyValuePair<string, IScriptObject>(col.ColumnName, new RowCellProperty(col));

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private class RowCellProperty : IScriptObject, IScriptProperty
        {
            private DataColumn column;

            public RowCellProperty(DataColumn column) { this.column = column; }

            #region IScriptProperty

            public bool IsEnumerable { get { return true; } }

            public IScriptObject GetPropValue(ScriptContext context, IScriptObject instance)
            {
                DataRowProxy proxy = (DataRowProxy)instance;
                object value = proxy.Row[column];
                return ScriptGlobal.ConvertValue(context, value);
            }

            public void SetPropValue(ScriptContext context, IScriptObject instance, IScriptObject value)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IScriptProperty

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
    }

    [ScriptProxy(typeof(DataRow))]
    public class DataRowProxy : ScriptObjectBase, IScriptObject, IScriptProxy
    {
        private DataRow row;

        public DataRowProxy() : base(true, false) { }

        public DataRowProxy(DataRowMembers rowMembers, DataRow row) : base(true, true)
        {
            this.row = row;
            InitValueMembers(rowMembers);
        }

        #region IScriptProxy

        public object RealInstance
        {
            get { return row; }
            set
            {
                row = (DataRow)value;
                if (row != null)
                {
                    DataRowMembers rowMembers = new DataRowMembers(row.Table);
                    InitValueMembers(rowMembers);
                }
            }
        }

        public DataRow Row { get { return row; } }

        public override string TypeName { get { return "[DataRow]"; } }

        public override IEnumerator GetEnumerator(ScriptContext context, bool isKey)
        {
            foreach (DataColumn col in row.Table.Columns)
            {
                if (isKey) yield return col.ColumnName;
                else yield return row[col];
            }
        }

        public override IScriptObject GetValue(ScriptContext context, string name)
        {
            DataColumn col = row.Table.Columns[name];
            if (col != null)
                return ScriptGlobal.ConvertValue(context, row[col]);
            return ScriptUndefined.Instance;
        }

        public override bool Remove(ScriptContext context, string name)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(ScriptContext context, string name, IScriptObject value)
        {
            DataColumn col = row.Table.Columns[name];
            if (col != null)
                row[col] = ScriptGlobal.ConvertValue(context, value, col.DataType);
        }

        public override object ToValue(ScriptContext context)
        {
            return row;
        }

        public override string ToValueString(ScriptContext context)
        {
            return "[DataRow]";
        }

        #endregion

    }
}
