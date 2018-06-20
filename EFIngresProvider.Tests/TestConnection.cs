namespace EFIngresProvider.Tests
{
    public static class TestConnection
    {
        public const string ConnectionString = @"";
        public static string TestDbDirect => $"{ConnectionString};TrimChars=True";
        public static string TestEntities => $"metadata=res://*/TestModel.TestModel.csdl|res://*/TestModel.TestModel.ssdl|res://*/TestModel.TestModel.msl;provider=EFIngresProvider;provider connection string=&quot;{TestDbDirect}&quot;";
    }
}
