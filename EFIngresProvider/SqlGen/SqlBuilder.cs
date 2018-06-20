using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace EFIngresProvider.SqlGen
{
    public class SqlBuilder : ISqlFragment
    {
        private List<ISqlFragment> _sqlFragments = new List<ISqlFragment>();

        public SqlBuilder(params object[] fragments)
        {
            Append(fragments);
        }

        /// <summary>
        /// Add an object to the list - we do not verify that it is a proper sql fragment
        /// since this is an internal method.
        /// </summary>
        /// <param name="sqlFragment"></param>
        public void Append(ISqlFragment sqlFragment)
        {
            Debug.Assert(sqlFragment != null);
            if (sqlFragment == null) throw new ArgumentNullException("sqlFragment");

            _sqlFragments.Add(sqlFragment);
        }

        /// <summary>
        /// Add an object to the list - we do not verify that it is a proper sql fragment
        /// since this is an internal method.
        /// </summary>
        /// <param name="strings"></param>
        public void Append(params string[] strings)
        {
            Debug.Assert(strings.Length > 0);
            if (strings.Length == 0) throw new ArgumentException("strings");

            Append(new StringFragment(strings));
        }

        /// <summary>
        /// Add an object to the list - we do not verify that it is a proper sql fragment
        /// since this is an internal method.
        /// </summary>
        /// <param name="strings"></param>
        public void Append(object obj)
        {
            Debug.Assert(obj != null);
            if (obj == null) throw new ArgumentNullException("obj");

            if (obj is IEnumerable<object>)
            {
                Append((IEnumerable<object>)obj);
            }
            else if (obj is ISqlFragment)
            {
                Append((ISqlFragment)obj);
            }
            else if (obj is string)
            {
                Append((string)obj);
            }
            else
            {
                Append(obj.ToString());
            }
        }

        public void Append(params object[] objects)
        {
            foreach (var obj in objects)
            {
                Append(obj);
            }
        }

        public void Append(IEnumerable<object> objects)
        {
            foreach (var obj in objects)
            {
                Append(obj);
            }
        }

        /// <summary>
        /// This is to pretty print the SQL.  The writer <see cref="SqlWriter.Write"/>
        /// needs to know about new lines so that it can add the right amount of 
        /// indentation at the beginning of lines.
        /// </summary>
        public void AppendLine()
        {
            Append("\r\n");
        }

        /// <summary>
        /// Whether the builder is empty.  This is used by the <see cref="SqlGenerator.Visit(DbProjectExpression)"/>
        /// to determine whether a sql statement can be reused.
        /// </summary>
        public bool IsEmpty
        {
            get { return !_sqlFragments.Any(); }
        }

        public void Clear()
        {
            _sqlFragments.Clear();
        }

        public void Set(SqlBuilder sqlBuilder)
        {
            Clear();
            Append(sqlBuilder._sqlFragments);
        }

        #region ISqlFragment Members

        /// <summary>
        /// We delegate the writing of the fragment to the appropriate type.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            _sqlFragments.ForEach(sqlFragment => sqlFragment.WriteSql(writer, sqlGenerator));
        }

        #endregion
    }
}
