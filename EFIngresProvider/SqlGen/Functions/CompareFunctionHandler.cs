using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class CompareFunctionHandler : FunctionHandler
    {
        public CompareFunctionHandler(string op)
        {
            Operator = op;
        }

        public string Operator { get; }

        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            AssertArgumentCount(e, 2);
            var left = e.Arguments[0].Accept(sqlGenerator);
            var right = e.Arguments[1].Accept(sqlGenerator);
            return WrapPredicate(new SqlBuilder(left, " ", Operator, " ", right));
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
