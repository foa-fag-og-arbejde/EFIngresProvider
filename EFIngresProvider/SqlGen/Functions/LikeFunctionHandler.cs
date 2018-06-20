using System.Data.Common.CommandTrees;
using System.Diagnostics;

namespace EFIngresProvider.SqlGen.Functions
{
    public class LikeFunctionHandler : PatternHandlerBase
    {
        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2, 3);
            var expression = e.Arguments[0].Accept(sqlGenerator);
            var pattern = e.Arguments[1].Accept(sqlGenerator);

            if (e.Arguments.Count == 3)
            {
                var ignoreCase = e.Arguments[2] as DbConstantExpression;
                Debug.Assert(ignoreCase != null && ignoreCase.Value is bool, string.Format("{0}: Parameter ignoreCase should be a boolean", e.Function.Name));
                if (ignoreCase != null && ignoreCase.Value is bool)
                {
                    if ((bool)ignoreCase.Value)
                    {
                        return WrapPredicate(new SqlBuilder(
                            "lowercase(", expression, ") like lowercase(", pattern, ") escape '", EFIngresProviderManifest.LikeEscapeCharString, "'"
                        ));
                    }
                }
            }

            return WrapPredicate(new SqlBuilder(
                expression, " like ", pattern, " escape '", EFIngresProviderManifest.LikeEscapeCharString, "'"
            ));
        }
    }
}
