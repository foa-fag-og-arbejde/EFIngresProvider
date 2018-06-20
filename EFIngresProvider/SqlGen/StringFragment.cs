namespace EFIngresProvider.SqlGen
{
    internal class StringFragment : ISqlFragment
    {
        public StringFragment(params string[] values)
        {
            Value = string.Concat(values);
        }

        public string Value { get; private set; }

        void ISqlFragment.WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            writer.Write(Value);
        }
    }
}
