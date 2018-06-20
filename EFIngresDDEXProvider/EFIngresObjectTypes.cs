namespace EFIngresDDEXProvider
{
    /// <summary>
    /// Represents constant string values for all the supported data object
    /// types.  This list must be in sync with the data object support XML.
    /// </summary>
    internal static class EFIngresObjectTypes
    {
        public const string Root = "";
        public const string Table = "Table";
        public const string Column = "Column";
        public const string Index = "Index";
        public const string IndexColumn = "IndexColumn";
        public const string ForeignKey = "ForeignKey";
        public const string ForeignKeyColumn = "ForeignKeyColumn";
        public const string View = "View";
        public const string ViewColumn = "ViewColumn";
        public const string DatabaseProcedure = "DatabaseProcedure";
        public const string DatabaseProcedureParameter = "DatabaseProcedureParameter";
    }
}
