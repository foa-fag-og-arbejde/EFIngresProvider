using System.Collections.Generic;
using System.Linq;

namespace EFIngresProvider.SqlGen
{
    /// <summary>
    /// A SqlSelectStatement represents a canonical SQL SELECT statement.
    /// It has fields for the 5 main clauses
    /// <list type="number">
    /// <item>SELECT</item>
    /// <item>FROM</item>
    /// <item>WHERE</item>
    /// <item>GROUP BY</item>
    /// <item>ORDER BY</item>
    /// </list>
    /// We do not have HAVING, since it does not correspond to anything in the DbCommandTree.
    /// Each of the fields is a SqlBuilder, so we can keep appending SQL strings
    /// or other fragments to build up the clause.
    ///
    /// We have a IsDistinct property to indicate that we want distict columns.
    /// This is given out of band, since the input expression to the select clause
    /// may already have some columns projected out, and we use append-only SqlBuilders.
    /// The DISTINCT is inserted when we finally write the object into a string.
    /// 
    /// Also, we have a Top property, which is non-null if the number of results should
    /// be limited to certain number. It is given out of band for the same reasons as DISTINCT.
    ///
    /// The FromExtents contains the list of inputs in use for the select statement.
    /// There is usually just one element in this - Select statements for joins may
    /// temporarily have more than one.
    ///
    /// If the select statement is created by a Join node, we maintain a list of
    /// all the extents that have been flattened in the join in AllJoinExtents
    /// <example>
    /// in J(j1= J(a,b), c)
    /// FromExtents has 2 nodes JoinSymbol(name=j1, ...) and Symbol(name=c)
    /// AllJoinExtents has 3 nodes Symbol(name=a), Symbol(name=b), Symbol(name=c)
    /// </example>
    ///
    /// If any expression in the non-FROM clause refers to an extent in a higher scope,
    /// we add that extent to the OuterExtents list.  This list denotes the list
    /// of extent aliases that may collide with the aliases used in this select statement.
    /// It is set by <see cref="SqlGenerator.Visit(DbVariableReferenceExpression)"/>.
    /// An extent is an outer extent if it is not one of the FromExtents.
    ///
    ///
    /// </summary>
    internal sealed class SqlSelectStatement : ISqlFragment
    {
        public SqlSelectStatement()
        {
            IsDistinct = false;
            OutputColumnsRenamed = false;
            OutputColumns = null;
            AllJoinExtents = null;
            FromExtents = new List<Symbol>();
            OuterExtents = new Dictionary<Symbol, bool>();
            Top = new TopClause();
            Select = new SelectColumnList();
            From = new SqlBuilder();
            Where = new SqlBuilder();
            GroupBy = new SqlBuilder();
            OrderBy = new SqlBuilder();
            IsTopMost = false;
        }

        /// <summary>
        /// Do we need to add a DISTINCT at the beginning of the SELECT
        /// </summary>
        internal bool IsDistinct { get; set; }
        internal bool OutputColumnsRenamed { get; set; }
        internal Dictionary<string, Symbol> OutputColumns { get; set; }
        internal List<Symbol> AllJoinExtents { get; set; }
        internal List<Symbol> FromExtents { get; private set; }
        internal Dictionary<Symbol, bool> OuterExtents { get; private set; }
        internal TopClause Top { get; private set; }
        internal SelectColumnList Select { get; set; }
        internal SqlBuilder From { get; private set; }
        internal SqlBuilder Where { get; private set; }
        internal SqlBuilder GroupBy { get; private set; }
        internal SqlBuilder OrderBy { get; private set; }

        //indicates whether it is the top most select statement, 
        // if not Order By should be omitted unless there is a corresponding TOP
        internal bool IsTopMost { get; set; }

        private IEnumerable<string> GetOuterExtentAliases()
        {
            foreach (Symbol outerExtent in OuterExtents.Keys)
            {
                if (outerExtent is JoinSymbol)
                {
                    foreach (Symbol symbol in ((JoinSymbol)outerExtent).FlattenedExtentList)
                    {
                        yield return symbol.NewName;
                    }
                }
                else
                {
                    yield return outerExtent.NewName;
                }
            }
        }

        #region ISqlFragment Members

        /// <summary>
        /// Write out a SQL select statement as a string.
        /// We have to
        /// <list type="number">
        /// <item>Check whether the aliases extents we use in this statement have
        /// to be renamed.
        /// We first create a list of all the aliases used by the outer extents.
        /// For each of the FromExtents( or AllJoinExtents if it is non-null),
        /// rename it if it collides with the previous list.
        /// </item>
        /// <item>Write each of the clauses (if it exists) as a string</item>
        /// </list>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            #region Check if FROM aliases need to be renamed

            // Create a list of the aliases used by the outer extents
            // JoinSymbols have to be treated specially.
            var outerExtentAliases = GetOuterExtentAliases().ToList();

            // An then rename each of the FromExtents we have
            // If AllJoinExtents is non-null - it has precedence.
            // The new name is derived from the old name - we append an increasing int.
            var extentList = AllJoinExtents ?? FromExtents;
            foreach (Symbol extent in extentList)
            {
                if (outerExtentAliases.Contains(extent.Name))
                {
                    int i = sqlGenerator.AllExtentNames[extent.Name];

                    string newName;
                    do
                    {
                        ++i;
                        newName = SqlGenerator.Format("{0}{1}", extent.Name, i);
                    }
                    while (sqlGenerator.AllExtentNames.ContainsKey(newName));

                    sqlGenerator.AllExtentNames[extent.Name] = i;
                    extent.NewName = newName;

                    // Add extent to list of known names (although i is always incrementing, "prefix11" can
                    // eventually collide with "prefix1" when it is extended)
                    sqlGenerator.AllExtentNames[newName] = 0;
                }

                // Add the current alias to the list, so that the extents
                // that follow do not collide with me.
                outerExtentAliases.Add(extent.NewName);
            }
            #endregion

            // Increase the indent
            using (writer.Indent())
            {
                writer.Write("select ");
                if (IsDistinct)
                {
                    writer.Write("distinct ");
                }

                Select.WriteSql(writer, sqlGenerator);

                writer.WriteLine();
                writer.Write("from ");
                From.WriteSql(writer, sqlGenerator);

                if (!this.Where.IsEmpty)
                {
                    writer.WriteLine();
                    writer.Write("where ");
                    Where.WriteSql(writer, sqlGenerator);
                }

                if (!this.GroupBy.IsEmpty)
                {
                    writer.WriteLine();
                    writer.Write("group by ");
                    GroupBy.WriteSql(writer, sqlGenerator);
                }

                if (!this.OrderBy.IsEmpty && (IsTopMost || Top != null))
                {
                    writer.WriteLine();
                    writer.Write("order by ");
                    OrderBy.WriteSql(writer, sqlGenerator);
                }

                if (!Top.IsEmpty)
                {
                    Top.WriteSql(writer, sqlGenerator);
                }
            }
        }

        #endregion
    }
}
