using Newtonsoft.Json;
using System.IO;

namespace EFIngresProvider.Tests
{
    public class TestConnection
    {
        public static TestConnection Load()
        {
            return JsonConvert.DeserializeObject<TestConnection>(File.ReadAllText(@"TestConnection.json"));
        }

        public string ConnectionString { get; set; }
        public string TestDbDirect => $"{ConnectionString};TrimChars=True";
        public string TestEntities => $"metadata=res://*/TestModel.TestModel.csdl|res://*/TestModel.TestModel.ssdl|res://*/TestModel.TestModel.msl;provider=EFIngresProvider;provider connection string=&quot;{TestDbDirect}&quot;";
    }
}
