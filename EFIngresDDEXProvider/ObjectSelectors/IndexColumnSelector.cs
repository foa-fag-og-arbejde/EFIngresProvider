using System;

namespace EFIngresDDEXProvider.ObjectSelectors
{
    public class IndexColumnSelector : ObjectSelector
    {
        public override string TypeName { get { return EFIngresObjectTypes.IndexColumn; } }

        protected override string GetSql()
        {
            throw new NotImplementedException();
        }
    }
}
