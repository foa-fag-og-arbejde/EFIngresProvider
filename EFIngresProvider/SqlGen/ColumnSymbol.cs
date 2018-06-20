namespace EFIngresProvider.SqlGen
{
    public class ColumnSymbol : ISqlFragment
    {
        public Symbol Source { get; set; }
        public Symbol Column { get; set; }

        public ColumnSymbol(Symbol source, Symbol column)
        {
            Source = source;
            Column = column;
        }

        #region ISqlFragment Members

        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            Source.WriteSql(writer, sqlGenerator);
            writer.Write(".");
            Column.WriteSql(writer, sqlGenerator);
        }

        #endregion
    }
}
