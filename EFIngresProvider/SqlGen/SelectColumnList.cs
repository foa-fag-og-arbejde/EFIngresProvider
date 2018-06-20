using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace EFIngresProvider.SqlGen
{
    public class SelectColumnList : ISqlFragment, IEnumerable<SelectColumn>
    {
        public SelectColumnList()
        {
            Columns = new List<SelectColumn>();
        }

        public List<SelectColumn> Columns { get; private set; }
        public int Count { get { return Columns.Count; } }
        public bool IsEmpty { get { return Count == 0; } }

        public void Append(SelectColumn column)
        {
            Columns.Add(column);
        }

        public void Append(IEnumerable<SelectColumn> columns)
        {
            Debug.Assert(columns != null, "columns is null");
            foreach (var column in columns)
            {
                Append(column);
            }
        }

        #region ISqlFragment Members

        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (IsEmpty)
            {
                writer.Write("*");
            }
            else
            {
                var count = Columns.Count;
                var i = 0;
                foreach (var column in Columns)
                {
                    i += 1;
                    column.WriteSql(writer, sqlGenerator);
                    if (i < count)
                    {
                        writer.WriteLine(",");
                    }
                }
            }
        }

        #endregion

        #region IEnumerator<SelectColumn> Members

        public IEnumerator<SelectColumn> GetEnumerator()
        {
            return Columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
