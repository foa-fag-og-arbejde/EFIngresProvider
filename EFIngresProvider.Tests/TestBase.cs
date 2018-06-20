using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace EFIngresProvider.Tests
{
    [TestClass]
    public abstract class TestBase
    {
        public TestBase()
        {
            TestHelper = new TestHelper();
        }

        public TestContext TestContext { get; set; }
        public TestHelper TestHelper { get; private set; }

        [TestInitialize]
        public void TestInitialize()
        {
            BeforeTestInitialize();
            TestHelper.IsLogging = true;
            TestHelper.Log(new string('=', 100));
            TestHelper.Log("Starting test {0}.{1}", TestContext.FullyQualifiedTestClassName, TestContext.TestName);
            TestHelper.Log(new string('-', 100));
            AfterTestInitialize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            BeforeTestCleanup();
            TestHelper.Log(new string('-', 100));
            TestHelper.Log("{0}", TestContext.CurrentTestOutcome);
            TestHelper.Log(new string('=', 100));
            TestHelper.Log();
            TestHelper.IsLogging = false;
            AfterTestCleanup();
        }

        protected virtual void BeforeTestInitialize()
        {
        }

        protected virtual void AfterTestInitialize()
        {
        }

        protected virtual void BeforeTestCleanup()
        {
        }

        protected virtual void AfterTestCleanup()
        {
        }

        protected Exception Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}
