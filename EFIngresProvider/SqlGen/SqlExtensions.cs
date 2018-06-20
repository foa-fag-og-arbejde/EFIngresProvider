namespace EFIngresProvider.SqlGen
{
    public static class SqlExtensions
    {
        public static string GetString(this ISqlFragment fragment, SqlGenerator sqlGenerator)
        {
            using (var writer = new SqlWriter())
            {
                if (fragment != null)
                {
                    fragment.WriteSql(writer, sqlGenerator);
                }
                return writer.ToString();
            }
        }

        public static int GetInt(this ISqlFragment fragment, SqlGenerator sqlGenerator)
        {
            return int.Parse(fragment.GetString(sqlGenerator).Trim());
        }
    }
}
