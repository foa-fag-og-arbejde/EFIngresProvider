using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common.CommandTrees;
using System.Data.Metadata.Edm;

namespace EFIngresProvider.TestSqlGen
{
    internal class TestSqlGenerator : DbExpressionVisitor
    {
        internal static string GenerateExpressionTree(DbCommandTree tree)
        {
            //Handle Query
            DbQueryCommandTree queryCommandTree = tree as DbQueryCommandTree;
            if (queryCommandTree != null)
            {
                TestSqlGenerator sqlGen = new TestSqlGenerator();
                return sqlGen.GenerateSql((DbQueryCommandTree)tree);
            }

            throw new NotSupportedException("Unrecognized command tree type");
        }

        private string GenerateSql(DbQueryCommandTree dbQueryCommandTree)
        {
            _depth = 0;
            text = new StringBuilder();
            dbQueryCommandTree.Query.Accept(this);
            return text.ToString();
        }

        private int _depth = 0;
        private StringBuilder text = new StringBuilder();
        private string Indent { get { return new string(' ', _depth * 4); } }

        private void WriteLine(string line)
        {
            text.Append(Indent);
            text.AppendLine(line);
        }

        private void Write(DbExpression expression)
        {
            WriteLine(string.Format("{0} ({1}) : {2}", expression.ExpressionKind, expression.GetType().Name, expression.ResultType));
        }

        private void WriteRawExpression(DbExpression expression)
        {
            WriteLine("RAW: " + expression.GetType().Name);
        }

        private void Write(string name, IEnumerable<DbExpressionBinding> bindings)
        {
            WriteLine(string.Format("{0} (IEnumerable<DbExpressionBinding>):", name));
            _depth++;
            foreach (var binding in bindings)
            {
                Write(name, binding);
            }
            _depth--;
        }

        private void Write(string name, string value)
        {
            WriteLine(string.Format("{0}: {1}", name, value));
        }

        private void Write(string name, DbExpressionBinding binding)
        {
            // WriteLine(binding.GetType().Name);
            WriteLine(string.Format("{0} ({1}): {2}", name, binding.GetType().Name, binding.VariableType));
            _depth++;
            Write("VariableName", binding.VariableName);
            Write("Expression", binding.Expression);
            Write("Variable", binding.Variable);
            _depth--;
        }

        private void Write(string name, IEnumerable<DbAggregate> aggregates)
        {
            WriteLine(string.Format("{0} (IEnumerable<DbAggregate>):", name));
            _depth++;
            foreach (var aggregate in aggregates)
            {
                Write(name, aggregate);
            }
            _depth--;
        }

        private void Write(string name, DbAggregate aggregate)
        {
            WriteLine(string.Format("{0} ({1}): {2}", name, aggregate.GetType().Name, aggregate.ResultType));
            _depth++;
            Write("Arguments", aggregate.Arguments);
            _depth--;
        }

        private void Write(string name, DbGroupExpressionBinding binding)
        {
            WriteLine(string.Format("{0} ({1}):", name, binding.GetType().Name));
            _depth++;
            Write("Expression", binding.Expression);
            Write("GroupAggregate", binding.GroupAggregate);
            Write("GroupVariable", binding.GroupVariable);
            Write("GroupVariableName", binding.GroupVariableName);
            Write("GroupVariableType", binding.GroupVariableType);
            Write("Variable", binding.Variable);
            Write("VariableName", binding.VariableName);
            Write("VariableType", binding.VariableType);
            _depth--;
        }

        private void Write(string name, IEnumerable<DbSortClause> sortClauses)
        {
            WriteLine(string.Format("{0} (IEnumerable<DbSortClause>):", name));
            _depth++;
            foreach (var sortClause in sortClauses)
            {
                Write(name, sortClause);
            }
            _depth--;
        }

        private void Write(string name, DbSortClause sortClause)
        {
            Write(name, "");
            _depth++;
            WriteLine(string.Format("Collation: {0}", "", sortClause.Collation));
            WriteLine(string.Format("Ascending: {0}", "", sortClause.Ascending));
            sortClause.Expression.Accept(this);
            _depth--;
        }

        private void Write(string name, TypeUsage typeUsage)
        {
            WriteLine(string.Format("{0}: {1}", name, typeUsage));
        }

        private void Write(string name, bool value)
        {
            WriteLine(string.Format("{0}: {1}", name, value));
        }

        private void Write(string name, EntitySetBase entitySet)
        {
            WriteLine(string.Format("{0}: {1} {2}", name, entitySet.GetType().Name, entitySet.Name));
        }

        private void Write(string name, RelationshipType relationshipType)
        {
            WriteLine(string.Format("{0}: RelationshipType {1}", name, relationshipType.Name));
        }

        private void Write(string name, RelationshipEndMember relationshipEndMember)
        {
            WriteLine(string.Format("{0}: RelationshipEndMember {1}", name, relationshipEndMember.Name));
        }

        private void Write(string name, DbExpression expression)
        {
            WriteLine(name + ":");
            _depth++;
            expression.Accept(this);
            _depth--;
        }

        private void Write(string name, IEnumerable<DbExpression> expressions)
        {
            WriteLine(name + ":");
            _depth++;
            foreach (var expression in expressions)
            {
                expression.Accept(this);
            }
            _depth--;
        }

        public override void Visit(DbAndExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbApplyExpression expression)
        {
            WriteRawExpression(expression);
        }

        public override void Visit(DbArithmeticExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Arguments", expression.Arguments);
            _depth--;
        }

        public override void Visit(DbCaseExpression expression)
        {
            Write(expression);
            _depth++;
            Write("When", expression.When);
            Write("Then", expression.Then);
            Write("Else", expression.Else);
            _depth--;
        }

        public override void Visit(DbCastExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbComparisonExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbConstantExpression expression)
        {
            Write(expression);
            _depth++;
            WriteLine(string.Format("Value: {0}", expression.Value));
            _depth--;
        }

        public override void Visit(DbCrossJoinExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Inputs", expression.Inputs);
            _depth--;
        }

        public override void Visit(DbDerefExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbDistinctExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbElementExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbEntityRefExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbExceptExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbExpression expression)
        {
            WriteRawExpression(expression);
        }

        public override void Visit(DbFilterExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("Predicate", expression.Predicate);
            _depth--;
        }

        public override void Visit(DbFunctionExpression expression)
        {
            Write(expression);
            _depth++;
            WriteLine("Function: " + expression.Function.FullName);
            Write("Arguments", expression.Arguments);
            _depth--;
        }

        public override void Visit(DbGroupByExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("Aggregates", expression.Aggregates);
            Write("Keys", expression.Keys);
            _depth--;
        }

        public override void Visit(DbIntersectExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbIsEmptyExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbIsNullExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbIsOfExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            Write("OfType", expression.OfType);
            _depth--;
        }

        public override void Visit(DbJoinExpression expression)
        {
            Write(expression);
            _depth++;
            Write("JoinCondition", expression.JoinCondition);
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbLikeExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            Write("Pattern", expression.Pattern);
            Write("Escape", expression.Escape);
            _depth--;
        }

        public override void Visit(DbLimitExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            Write("Limit", expression.Limit);
            Write("WithTies", expression.WithTies);
            _depth--;
        }

        public override void Visit(DbNewInstanceExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Arguments", expression.Arguments);
            _depth--;
        }

        public override void Visit(DbNotExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbNullExpression expression)
        {
            Write(expression);
        }

        public override void Visit(DbOfTypeExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            Write("OfType", expression.OfType);
            _depth--;
        }

        public override void Visit(DbOrExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbParameterReferenceExpression expression)
        {
            Write(expression);
            _depth++;
            Write("ParameterName", expression.ParameterName);
            _depth--;
        }

        public override void Visit(DbProjectExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("Projection", expression.Projection);
            _depth--;
        }

        public override void Visit(DbPropertyExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Name", expression.Property.Name);
            Write("Instance", expression.Instance);
            _depth--;
        }

        public override void Visit(DbQuantifierExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("Predicate", expression.Predicate);
            _depth--;
        }

        public override void Visit(DbRefExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            Write("EntitySet", expression.EntitySet);
            _depth--;
        }

        public override void Visit(DbRefKeyExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbRelationshipNavigationExpression expression)
        {
            Write(expression);
            _depth++;
            Write("NavigationSource", expression.NavigationSource);
            Write("NavigateFrom", expression.NavigateFrom);
            Write("NavigateTo", expression.NavigateTo);
            Write("Relationship", expression.Relationship);
            _depth--;
        }

        public override void Visit(DbScanExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Target", expression.Target);
            _depth--;
        }

        public override void Visit(DbSkipExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("Count", expression.Count);
            Write("SortOrder", expression.SortOrder);
            _depth--;
        }

        public override void Visit(DbSortExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Input", expression.Input);
            Write("SortOrder", expression.SortOrder);
            _depth--;
        }

        public override void Visit(DbTreatExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Argument", expression.Argument);
            _depth--;
        }

        public override void Visit(DbUnionAllExpression expression)
        {
            Write(expression);
            _depth++;
            Write("Left", expression.Left);
            Write("Right", expression.Right);
            _depth--;
        }

        public override void Visit(DbVariableReferenceExpression expression)
        {
            Write(expression);
            _depth++;
            WriteLine("VariableName: " + expression.VariableName);
            _depth--;
        }
    }
}
