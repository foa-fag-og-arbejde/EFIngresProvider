using System.Collections.Generic;
using System.Linq;

namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class RootSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.Root; } }

        public override IEnumerable<string> GetRequiredRestrictions(object[] parameters)
        {
            return Enumerable.Empty<string>();
        }

        protected override string[] DefaultRestrictions
        {
            get { return new string[] { "dbmsinfo('database')" }; }
        }

        protected override string GetSql()
        {
            return @"
                select ""Login""    = dbmsinfo('system_user'),
                       ""Database"" = dbmsinfo('database'),
                       ""User""     = dbmsinfo('system_user'),
                       ""Schema""   = dbmsinfo('dba')
            ";
        }
    }
}
