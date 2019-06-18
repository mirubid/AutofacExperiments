using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Autofac;


namespace AutofacExperiments
{
    [TestFixture]
    public class Scopes
    {
        [Test]
        public void ResolvingLifetimeScopedServicesInNestedScopes()
        {
            var cb = new ContainerBuilder();

            cb.RegisterType<ConcreteSampleService>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var root = cb.Build();
            ILifetimeScope inner1=null;
            ILifetimeScope inner2=null;
            // start unit of work by beginning a new lifetime scope
            try
            {
                inner1 = root.BeginLifetimeScope();
                inner2 = root.BeginLifetimeScope();

                var s2 = inner2.Resolve<ISampleService>();
                var s1 = inner1.Resolve<ISampleService>();
                var s0 = root.Resolve<ISampleService>();

                #region Lifetime scoped services have separate instances in different lifetime scopes

                Assert.AreNotEqual(s1, s2, @" s1 vs s2");
                Assert.AreNotEqual(s0, s1);
                Assert.AreNotEqual(s0, s2);
                #endregion

                #region But the always resolve to the same instance within a scope (i.e. its a singleton within its scope)

                Assert.AreEqual(s0, root.Resolve<ISampleService>());
                Assert.AreEqual(s1, inner1.Resolve<ISampleService>());
                Assert.AreEqual(s2, inner2.Resolve<ISampleService>());
                #endregion


            }
            finally
            {
                inner1?.Dispose();
                inner2?.Dispose();
            }
            
        }
        [Test]
        public void ResolvingSingletonServicesInNestedScopes()
        {
            var cb = new ContainerBuilder();

            cb.RegisterType<ConcreteSampleService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            var root = cb.Build();
            ILifetimeScope inner1 = null;
            ILifetimeScope inner2 = null;
            // start unit of work by beginning a new lifetime scope
            try
            {
                inner1 = root.BeginLifetimeScope();
                inner2 = root.BeginLifetimeScope();

                var s2 = inner2.Resolve<ISampleService>();
                var s1 = inner1.Resolve<ISampleService>();
                var s0 = root.Resolve<ISampleService>();

                #region Singleton scoped services share instances in different lifetime scopes

                Assert.AreEqual(s1, s2, @" s1 vs s2");
                Assert.AreEqual(s0, s1);
                Assert.AreEqual(s0, s2);
                #endregion

                #region and within a scope (i.e. its a global singleton)

                Assert.AreEqual(s0, root.Resolve<ISampleService>());
                Assert.AreEqual(s1, inner1.Resolve<ISampleService>());
                Assert.AreEqual(s2, inner2.Resolve<ISampleService>());
                #endregion


            }
            finally
            {
                inner1?.Dispose();
                inner2?.Dispose();
            }

        }

        [Test]
        public void ServiceLifetimesAreControllerByLifetimeScopes()
        {
            var cb = new ContainerBuilder();
            int disposeCount = 0;

            cb.Register(c => new MyDisposable(() =>{
                disposeCount++;
                TestContext.Progress.WriteLine("disposing service");
            })
            )
                .InstancePerDependency();

            var root = cb.Build();
            ILifetimeScope inner1 = null;
            
            // start unit of work by beginning a new lifetime scope
            
            inner1 = root.BeginLifetimeScope();
                
            var service = inner1.Resolve<MyDisposable>();

            Assert.IsNotNull(service, "service");

            // at this point the service hasn't been disposed
            Assert.AreEqual(0, disposeCount, @" 0 vs dispseCount");

            // terminate the lifetime scope by disposing it
            TestContext.Progress.WriteLine("disposing scope");
            inner1.Dispose();

            // we expect that autofac has also disposed this dependency

            Assert.AreEqual(1, disposeCount, @" 1 vs disposeCount");
            Assert.IsTrue(service.Disposed, @" service.Disposed ");

        }
        [Test]
        public void CustomizingInnerScopes()
        {
            var cb = new ContainerBuilder();

            cb.Register(c=>new ConcreteSampleService("default implementation"))
                .AsImplementedInterfaces();

            var root = cb.Build();

            // create a child scope
            var customized = root.BeginLifetimeScope(
                   // startup configuration for this scope
                   (builder) =>
                   {
                        // override the default registration in this scope
                        builder.Register(c => new ConcreteSampleService("customized implementation"))
                       .AsImplementedInterfaces();
                   }
                   );


            ILifetimeScope inner1 = null;
            ILifetimeScope inner2 = null;
            // start unit of work by beginning a new lifetime scope
            try
            {
               

                inner1 = customized.BeginLifetimeScope();
                inner2 = root.BeginLifetimeScope();

                var s2 = inner2.Resolve<ISampleService>();
                var s1 = inner1.Resolve<ISampleService>();
                

                #region Singleton scoped services share instances in different lifetime scopes

                Assert.AreNotEqual(s1, s2, @" s1 vs s2");
                Assert.AreNotEqual(s1.Do(), s2.Do(), @" s1.Do() vs s2.Do()");

                Assert.IsTrue(s1.Do().Contains("customized"), @" s1.Do().Contains(""customized"") ");

                #endregion
            }
            finally
            {
                inner1?.Dispose();
                inner2?.Dispose();
            }

        }
        public class MyDisposable : IDisposable
        {
            private Action _disposing;
            private bool _disposed = false;

            public bool Disposed => _disposed;
            public MyDisposable(Action disposing)
            {
                _disposing = disposing;
            }
            public void Dispose()
            {
                _disposing?.Invoke();
                _disposed = true;
            }
        }
    }
}
