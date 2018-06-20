namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class DatabaseProcedureSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.DatabaseProcedure; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')", "p.procedure_owner", "p.procedure_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database"" = dbmsinfo('database'),
                       ""Schema""   = trim(p.procedure_owner),
                       ""Name""     = trim(p.procedure_name)
                  from iiprocedures p
                 where p.system_use = 'U'
                   and p.procedure_owner not in ('ingres', '$ingres')
                   and dbmsinfo('database') = {0}
                   and p.procedure_owner    = {1}
                   and p.procedure_name     = {2}
                 order by 1, 2, 3
            ";
        }
    }
}
