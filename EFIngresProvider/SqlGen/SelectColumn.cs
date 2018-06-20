namespace EFIngresProvider.SqlGen
{
    public class SelectColumn : ISqlFragment
    {
        public SelectColumn(Symbol column, ISqlFragment expression)
        {
            Column = column;
            Expression = expression;
        }

        public SelectColumn(Symbol column, Symbol source, Symbol innerName)
            : this(column, new ColumnSymbol(source, innerName))
        {
        }

        public Symbol Column { get; private set; }
        public ISqlFragment Expression { get; protected set; }
        public bool IsSimple { get { return Expression is ColumnSymbol; } }
        public Symbol Source { get { return IsSimple ? ((ColumnSymbol)Expression).Source : null; } }
        public Symbol InnerName { get { return IsSimple ? ((ColumnSymbol)Expression).Column : null; } }
        
        #region ISqlFragment Members

        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            Column.WriteSql(writer, sqlGenerator);
            writer.Write(" = ");
            Expression.WriteSql(writer, sqlGenerator);
        }

        #endregion
    }
}
