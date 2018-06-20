namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class TableSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.Table; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')", "t.table_owner", "t.table_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database"" = dbmsinfo('database'),
                       ""Schema""   = trim(t.table_owner),
                       ""Name""     = trim(t.table_name),
                       ""Type""     = 'BASE TABLE'
                  from iitables t
                 where t.system_use  = 'U'
                   and t.table_type in ('T', 'V')
                   and t.table_owner not in ('ingres', '$ingres')
                   and t.table_name not like 'iietab_%'
                   and dbmsinfo('database') = {0}
                   and t.table_owner        = {1}
                   and t.table_name         = {2}
                 order by 1, 2, 3
            ";
        }
    }
}
