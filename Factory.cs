using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace AutofacExperiments
{
    [TestFixture]
    public class Features
    {
        [Test]
        public void ShouldBeAbleToResolveAGenericFactory()
        {

            var cb = new ContainerBuilder();

            // register my service
            cb.RegisterType<ConcreteSampleService>()
                .As<ISampleService>();

            var container = cb.Build();


            // I should be able to resole a factory even though I didn't explicitly register it
            var factory = container.Resolve < Func<string, ISampleService>>();


            Assert.IsNotNull(factory, "factory");

            // the factory will match the arguments to constructor arguments by type
            var service = factory("param1");

            Assert.IsNotNull(service, "service");
            Assert.AreEqual("param1", service.Do(), @" ""param1"" vs service.Param");



        }

        [Test]
        public void LaterRegistrationsOverrideEarlierRegistrations()
        {
            var cb = new ContainerBuilder();

            var instance1 = new ConcreteSampleService("one");
            var instance2 = new ConcreteSampleService("two");
            var instance3 = new ConcreteSampleService("three");

            // register my service
            cb.RegisterInstance(instance1)
                .As<ISampleService>();
            // later registration will override previous
            cb.RegisterInstance(instance2)
                .As<ISampleService>();

            cb.RegisterInstance(instance3)
                .As<ISampleService>()
                // unless we use "PreserveExistingDefaults"
                .PreserveExistingDefaults();
            var container = cb.Build();


            // The last registered instance will be resolved here
            var service = container.Resolve<ISampleService>();

            Assert.IsNotNull(service, "service");
            Assert.AreEqual("two", service.Do(), @" ""two"" vs service.Do()");
        }

        [Test]
        public void CanGetAListOfAllRegisteredServices()
        {
            var cb = new ContainerBuilder();

            var instance1 = new ConcreteSampleService("one");
            var instance2 = new ConcreteSampleService("two");
            var instance3 = new ConcreteSampleService("named");
            // register multiple instances

            // named service won't be included unless it is also registered without the name
            cb.RegisterInstance(instance3)
                .As<ISampleService>()
                .Named<ISampleService>("name");

            cb.RegisterInstance(instance1)
                .As<ISampleService>();

            cb.RegisterInstance(instance2)
                .As<ISampleService>();



            var container = cb.Build();


            // The last registered instance will be resolved here
            var service = container.Resolve<ISampleService>();

            Assert.IsNotNull(service, "service");
            Assert.AreEqual("two", service.Do(), @" ""two"" vs service.Do()");

            // I can resolve IEnumerable<Type> even though I didn't explicitly register it
            var services = container.Resolve<IEnumerable<ISampleService>>();
            Assert.IsNotNull(services, "services");
            Assert.AreEqual(services.Count(), 3, @" services.Count() vs 2");

            // IList<Type> also works
            var serviceList = container.Resolve<IList<ISampleService>>();

            Assert.IsNotNull(serviceList, "serviceList");

        }
        [Test]
        public void CanResolveLazy()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<ConcreteSampleService>().As<ISampleService>();
            var container = cb.Build();

            // I can resolve Lazy<ISampleService> even though I didn't explicitly register it
            var lazy=container.Resolve<Lazy<ISampleService>>();

            // service hasn't been initialized
            Assert.IsTrue(!lazy.IsValueCreated, @" !lazy.IsValueCreated ");

            TestContext.Progress.WriteLine("constructor should be called after this, and not before");
            // until I access it
            var service = lazy.Value;
        }

        [Test]
        public void CanRegisterDecorator()
        {
            var cb = new ContainerBuilder();

            var instance1 = new ConcreteSampleService("one");
            
            

            // register my service
            cb.RegisterInstance(instance1)
                .As<ISampleService>();


            // register a decorator for the service
            cb.RegisterDecorator<DecoratorService, ISampleService>();


            var container = cb.Build();

            var service = container.Resolve<ISampleService>() as DecoratorService;

            Assert.IsNotNull(service, "service");

            Assert.IsTrue(service.Do().Contains("<wrapped>"), @" service.Do().Contains(""<wrapped>"") ");
            Assert.IsTrue(service.Do().Contains("one"), @" service.Do().Contains(""one"") ");

            Assert.IsInstanceOf<ConcreteSampleService>(service.Inner);

        }
    }
    public interface ISampleService
    {
        string Do();
    }
    public class ConcreteSampleService:ISampleService
    {
        private string _param1;
        public ConcreteSampleService():this("default")
        {

        }
        public ConcreteSampleService(string param1)
        {
            _param1 = param1;
            TestContext.Progress.WriteLine($"newing ConcreteSampleService({param1})");
        }

        public string Do()
        {
            return _param1;
        }
        public string Param { get { return _param1; } }
    }
    public class DecoratorService : ISampleService
    {
        private ISampleService _inner;
        public ISampleService Inner => _inner;

        public DecoratorService(ISampleService inner)
        {
            _inner = inner;
        }
        public string Do()
        {
            return $"<wrapped>{_inner.Do()}</wrapped>";
        }
    }
}
