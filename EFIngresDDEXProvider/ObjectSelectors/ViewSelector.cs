namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class ViewSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.View; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')", "t.table_owner", "t.table_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database""    = dbmsinfo('database'),
                       ""Schema""      = trim(t.table_owner),
                       ""Name""        = trim(t.table_name),
                       ""CheckOption"" = v.check_option,
                       ""IsUpdatable"" = 'N'
                  from iitables t
                  join iiviews v on
                       v.table_owner = t.table_owner
                   and v.table_name  = t.table_name
                 where 1 = 0
                   and t.system_use  = 'U'
                   and t.table_type  = 'V'
                   and t.table_owner not in ('ingres', '$ingres')
                   and dbmsinfo('database') = {0}
                   and t.table_owner        = {1}
                   and t.table_name         = {2}
                 order by 1, 2, 3
            ";
        }
    }
}
