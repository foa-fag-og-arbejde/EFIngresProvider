using System.Data.Common.CommandTrees;

namespace EFIngresProvider.SqlGen.Functions
{
    public class NamedFunctionHandler : FunctionHandler
    {
        public NamedFunctionHandler(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public override ISqlFragment HandleFunction(SqlGenerator sqlGenerator, DbFunctionExpression e)
        {
            var result = new SqlBuilder(Name);
            HandleFunctionArgumentsDefault(sqlGenerator, e, result);
            return result;
        }
    }
}
