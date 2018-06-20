using System.Collections.Generic;

namespace EFIngresProvider.SqlGen.Functions
{
    public abstract class CreateDateTimeBaseHandler : FunctionHandler
    {
        protected IEnumerable<object> Date(ISqlFragment year, ISqlFragment month, ISqlFragment day)
        {
            return new object[] { "right('0000' + varchar(", year, "), 4) + '-' + varchar(", month, ") + '-' + varchar(", day, ")" };
        }

        protected IEnumerable<object> Time(ISqlFragment hour, ISqlFragment minute, ISqlFragment second)
        {
            return new object[] { "varchar(", hour, ") + ':' + varchar(", minute, ") + ':' + varchar(", second, ")" };
        }

        protected IEnumerable<object> TimeOffset(ISqlFragment offset)
        {
            return new object[] { "case when ", offset, " >= 0 then '+' else '-' end + varchar(abs(", offset, ") / 60) + ':' + varchar(mod(abs(", offset, "), 60))" };
        }
    }
}
