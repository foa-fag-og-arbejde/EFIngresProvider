namespace EFIngresProvider.Tests.TestModel
{
    partial class TestEntities
    {
        public TestEntities(string providerConnectionString)
            : base(GetConnectionString("TestModel.TestModel", providerConnectionString))
        {
        }

        private static string GetConnectionString(string model, string providerConnectionString)
        {
            var Metadata = string.Format(@"res://*/{0}.csdl|res://*/{1}.ssdl|res://*/{2}.msl", model, model, model);
            return string.Format(@"metadata={0};provider={1};provider connection string=""{2}""", Metadata, TestHelper.ProviderName, providerConnectionString);
        }
    }
}
