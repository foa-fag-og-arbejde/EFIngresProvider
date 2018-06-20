namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class IndexSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.Index; } }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "i.base_owner", "i.base_name", "i.index_name" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Database""  = dbmsinfo('database'),
                       ""Schema""    = trim(i.base_owner),
                       ""Table""     = trim(i.base_name),
                       ""Name""      = trim(i.index_name),
                       ""IsUnique""  = case i.unique_rule when 'U' then 1 else 0 end,
                       ""IsPrimary"" = case ifnull(c.constraint_type, '') when 'P' then 1 else 0 end
                  from iiindexes i
                  left join iiconstraint_indexes ci on
                       ci.schema_name   = i.index_owner
                   and ci.index_name    = i.index_name
                  left join iiconstraints c on
                       c.schema_name     = ci.schema_name
                   and c.constraint_name = ci.constraint_name
                   and c.text_sequence   = 1
                 where i.system_use  = 'U'
                   and i.base_owner  = {0}
                   and i.base_name   = {1}
                   and i.index_name  = {2}
                 order by 1, 2, 3, 4
            ";
        }
    }
}
