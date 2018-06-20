using System.Globalization;

namespace EFIngresProvider.SqlGen
{
    /// <summary>
    /// TopClause represents the a TOP expression in a SqlSelectStatement. 
    /// It has a count property, which indicates how many TOP rows should be selected and a 
    /// boolen WithTies property.
    /// </summary>
    class TopClause : ISqlFragment
    {
        /// <summary>
        /// How many top rows should be selected.
        /// </summary>
        internal ISqlFragment TopCount { get; set; }

        /// <summary>
        /// How many top rows should be skipped.
        /// </summary>
        internal ISqlFragment SkipCount { get; set; }

        internal bool IsEmpty { get { return (TopCount == null) && (SkipCount == null); } }

        /// <summary>
        /// Creates a TopClause with the given topCount and withTies.
        /// </summary>
        /// <param name="topCount"></param>
        /// <param name="withTies"></param>
        internal TopClause()
        {
        }

        internal void Clear()
        {
            TopCount = null;
            SkipCount = null;
        }

        internal void SetTopClause(TopClause topClause)
        {
            TopCount = topClause.TopCount;
            SkipCount = topClause.SkipCount;
        }

        internal void SetTopCount(int topCount)
        {
            TopCount = new SqlBuilder(topCount.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetSkipCount(int skipCount)
        {
            SkipCount = new SqlBuilder(skipCount.ToString(CultureInfo.InvariantCulture));
        }

        #region ISqlFragment Members

        /// <summary>
        /// Write out the TOP part of sql select statement 
        /// It basically writes TOP (X) [WITH TIES].
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (SkipCount != null)
            {
                writer.WriteLine();
                writer.Write("offset ");
                writer.Write(SkipCount.GetInt(sqlGenerator) + 1);
                writer.Write(" ");
            }

            if (TopCount != null)
            {
                writer.WriteLine();
                writer.Write("fetch ");
                if (SkipCount != null)
                {
                    writer.Write("next ");
                }
                else
                {
                    writer.Write("first ");
                }
                writer.Write(TopCount.GetInt(sqlGenerator));
                writer.Write(" rows only ");
            }
        }

        #endregion
    }
}
