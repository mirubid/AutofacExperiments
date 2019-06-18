using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Autofac;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace AutofacExperiments
{
    [TestFixture]
    public class Modules
    {
        [Test]
        public void RegisterModuleDemo()
        {
            var cb = new ContainerBuilder();

            // register module by type
            cb.RegisterModule<TestModule>();


            // register module by instance (e.g. might want to do this to assign properties etc)
            cb.RegisterModule(new TestModule()
            {
#if DEBUG
                DebugMode = true
#else
                Debug=false
#endif
            });
            cb.RegisterModule<AnotherModule>();
            // of course we can register multiple modules and combine with inline registrations

            //e.g. CoreModule, WebModule, WindowsServiceModule, TenantModules, etc
            
        }
        [Test]
        public void RegisterFromConfigFile()
        {
            var configBuilder = new ConfigurationBuilder();

            configBuilder.AddJsonFile("autofac.demo.json");


            var cb = new ContainerBuilder();

            // register module by type
            cb.RegisterModule(new ConfigurationModule(configBuilder.Build()));

            var container = cb.Build();


            var service = container.Resolve<ISampleService>();


            Assert.IsNotNull(service, "service -- should have been loaded from configuration");

            Assert.IsTrue(service.Do().Contains("debugMode=True"), @" expected debugMode constructor parameter to be true ");

            var allServices=container.Resolve<IList<ISampleService>>();

            Assert.AreEqual(2, allServices.Count(), @" 2 vs allServices.Count() (expected both modules to have been registered)");


        }
    }

    public class TestModule : Autofac.Module
    {
        public TestModule():this(false) { }
        public TestModule(bool debugMode)
        {
            DebugMode = debugMode;
        }
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(c => new ConcreteSampleService($"built from TestModule; debugMode={DebugMode}"))
                .AsImplementedInterfaces();
        }
        public bool DebugMode { get; set; }

        public List<int> ListProp { get; set; }

        public Dictionary<string,string> DictionaryProp { get; set; }
    }

    public class AnotherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new ConcreteSampleService("registered from AnotherModule"))
                .AsImplementedInterfaces()
                .PreserveExistingDefaults();
        }
    }
}
