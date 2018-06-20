using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Metadata.Edm;

namespace EFIngresProvider.SqlGen
{
    /// <summary>
    /// A Join symbol is a special kind of Symbol.
    /// It has to carry additional information
    /// <list type="bullet">
    /// <item>ColumnList for the list of columns in the select clause if this
    /// symbol represents a sql select statement.  This is set by <see cref="SqlGenerator.AddDefaultColumns"/>. </item>
    /// <item>ExtentList is the list of extents in the select clause.</item>
    /// <item>FlattenedExtentList - if the Join has multiple extents flattened at the 
    /// top level, we need this information to ensure that extent aliases are renamed
    /// correctly in <see cref="SqlSelectStatement.WriteSql"/></item>
    /// <item>NameToExtent has all the extents in ExtentList as a dictionary.
    /// This is used by <see cref="SqlGenerator.Visit(DbPropertyExpression)"/> to flatten
    /// record accesses.</item>
    /// <item>IsNestedJoin - is used to determine whether a JoinSymbol is an 
    /// ordinary join symbol, or one that has a corresponding SqlSelectStatement.</item>
    /// </list>
    /// 
    /// All the lists are set exactly once, and then used for lookups/enumerated.
    /// </summary>
    internal sealed class JoinSymbol : Symbol
    {
        private List<Symbol> columnList;
        internal List<Symbol> ColumnList
        {
            get
            {
                if (columnList == null)
                {
                    columnList = new List<Symbol>();
                }
                return columnList;
            }
            set { columnList = value; }
        }

        internal List<Symbol> ExtentList { get; private set; }

        private List<Symbol> flattenedExtentList;
        internal List<Symbol> FlattenedExtentList
        {
            get
            {
                if (flattenedExtentList == null)
                {
                    flattenedExtentList = new List<Symbol>();
                }
                return flattenedExtentList;
            }
            set { flattenedExtentList = value; }
        }

        internal Dictionary<string, Symbol> NameToExtent { get; private set; }
        internal bool IsNestedJoin { get; set; }

        public JoinSymbol(string name, TypeUsage type, List<Symbol> extents)
            : base(name, type)
        {
            ExtentList = new List<Symbol>(extents);
            NameToExtent = ExtentList.ToDictionary(symbol => symbol.Name, symbol => symbol, StringComparer.OrdinalIgnoreCase);
        }
    }
}
