using System.Collections.Generic;
using System.Data.Common.CommandTrees;

namespace EFIngresProvider.Helpers
{
    internal class CommandTreeUtils
    {
        #region Expression Flattening Helpers

        static private readonly HashSet<DbExpressionKind> _associativeExpressionKinds = new HashSet<DbExpressionKind>(new DbExpressionKind[] {  DbExpressionKind.Or,
                                                                                                                                                DbExpressionKind.And,
                                                                                                                                                DbExpressionKind.Plus,
                                                                                                                                                DbExpressionKind.Multiply});

        /// <summary>
        /// Creates a flat list of the associative arguments.
        /// For example, for ((A1 + (A2 - A3)) + A4) it will create A1, (A2 - A3), A4 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        internal static IEnumerable<DbExpression> FlattenAssociativeExpression(DbExpression expression)
        {
            return FlattenAssociativeExpression(expression.ExpressionKind, expression);
        }

        /// <summary>
        /// Creates a flat list of the associative arguments.
        /// For example, for ((A1 + (A2 - A3)) + A4) it will create A1, (A2 - A3), A4
        /// Only 'unfolds' the given arguments that are of the given expression kind.        
        /// </summary>
        /// <param name="expressionKind"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        internal static IEnumerable<DbExpression> FlattenAssociativeExpression(DbExpressionKind expressionKind, params DbExpression[] arguments)
        {
            if (!_associativeExpressionKinds.Contains(expressionKind))
            {
                return arguments;
            }

            List<DbExpression> outputArguments = new List<DbExpression>();
            foreach (DbExpression argument in arguments)
            {
                ExtractAssociativeArguments(expressionKind, outputArguments, argument);
            }
            return outputArguments;
        }

        /// <summary>
        /// Helper method for FlattenAssociativeExpression.
        /// Creates a flat list of the associative arguments and appends to the given argument list.
        /// For example, for ((A1 + (A2 - A3)) + A4) it will add A1, (A2 - A3), A4 to the list.
        /// Only 'unfolds' the given expression if it is of the given expression kind.
        /// </summary>
        /// <param name="expressionKind"></param>
        /// <param name="argumentList"></param>
        /// <param name="expression"></param>
        private static void ExtractAssociativeArguments(DbExpressionKind expressionKind, List<DbExpression> argumentList, DbExpression expression)
        {
            if (expression.ExpressionKind != expressionKind)
            {
                argumentList.Add(expression);
                return;
            }

            //All associative expressions are binary, thus we must be dealing with a DbBinaryExpresson or 
            // a DbNaryExpression with 2 arguments.
            DbBinaryExpression binaryExpression = expression as DbBinaryExpression;
            if (binaryExpression != null)
            {
                ExtractAssociativeArguments(expressionKind, argumentList, binaryExpression.Left);
                ExtractAssociativeArguments(expressionKind, argumentList, binaryExpression.Right);
                return;
            }

            DbArithmeticExpression naryExpression = (DbArithmeticExpression)expression;
            ExtractAssociativeArguments(expressionKind, argumentList, naryExpression.Arguments[0]);
            ExtractAssociativeArguments(expressionKind, argumentList, naryExpression.Arguments[1]);
        }

        #endregion
    }
}
