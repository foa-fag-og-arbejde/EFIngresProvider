using EFIngresProvider.Helpers;
using System.Collections.Generic;
using System.Reflection;

namespace EFIngresProvider.Tests.TestModel
{
    public static class TestData
    {
        private static readonly Assembly Assembly = typeof(TestData).Assembly;
        private const string ResourceRoot = "EFIngresProvider.Tests.TestModel.Data";
        private static readonly Dictionary<string, IEnumerable<string>> _scripts = new Dictionary<string, IEnumerable<string>>();

        public static void CreateTables()
        {
            RunScript("CreateTestDB.sql");
        }

        public static void InsertCustomers()
        {
            RunScript("InsertCustomers.sql");
        }

        public static void InsertIngresDates()
        {
            RunScript("InsertIngresDates.sql");
        }

        private static void RunScript(string scriptName)
        {
            var helper = new TestHelper();
            using (var connection = helper.CreateDbConnection())
            {
                connection.Open();
                SqlScript.ExecuteScript(connection, GetScript(scriptName), ignoreErrors: true);
            }
        }

        private static string GetResourceName(string scriptName)
        {
            return string.Format("{0}.{1}", ResourceRoot, scriptName);
        }

        private static IEnumerable<string> GetScript(string scriptName)
        {
            var resourceName = GetResourceName(scriptName);
            IEnumerable<string> script;
            if (!_scripts.TryGetValue(resourceName, out script))
            {
                using (var stream = Assembly.GetManifestResourceStream(resourceName))
                {
                    script = SqlScript.ReadScript(stream);
                    _scripts[scriptName] = script;
                }
            }
            return script;
        }
    }
}
