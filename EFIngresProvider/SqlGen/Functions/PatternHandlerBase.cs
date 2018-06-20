using System.Text;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;

namespace EFIngresProvider.SqlGen.Functions
{
    public abstract class PatternHandlerBase : FunctionHandler
    {
        protected bool TryGetConstantString(DbExpression e, out string value)
        {
            value = null;
            if (e is DbConstantExpression)
            {
                value = ((DbConstantExpression)e).Value as string;
            }
            return value != null;
        }

        protected ISqlFragment GetLikePredicate(SqlGenerator sqlGenerator, DbExpression target, string pattern, bool insertPercentStart, bool insertPercentEnd)
        {
            bool escapingOccurred;
            pattern = GetLikePattern(pattern, insertPercentStart, insertPercentEnd, out escapingOccurred);
            var result = new SqlBuilder(target.Accept(sqlGenerator), " like ", sqlGenerator.VisitConstantExpression(PrimitiveTypeKind.String, pattern));
            // If escaping did occur (special characters were found), then append the escape character used.
            if (escapingOccurred)
            {
                result.Append(" escape '" + EFIngresProviderManifest.LikeEscapeCharString + "'");
            }
            return result;
        }

        protected static string GetLikePattern(string pattern, bool insertPercentStart, bool insertPercentEnd, out bool escapingOccurred)
        {
            var patternBuilder = new StringBuilder();

            if (insertPercentStart)
            {
                patternBuilder.Append("%");
            }

            patternBuilder.Append(EFIngresProviderManifest.EscapeLikeText(pattern, false, out escapingOccurred));

            if (insertPercentEnd)
            {
                patternBuilder.Append("%");
            }

            return patternBuilder.ToString();
        }

        /// <summary>
        /// Turns a predicate into a statement returning a tinyint
        /// PREDICATE => case when (PREDICATE) then tinyint(1) else tinyint(0) end
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        protected ISqlFragment WrapPredicate(ISqlFragment predicate)
        {
            return new SqlBuilder("case when (", predicate, ") then tinyint(1) else tinyint(0) end");
        }
    }
}
