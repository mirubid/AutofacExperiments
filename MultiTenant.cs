using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
// include this package for out of the box multi-tenant support
using Autofac.Multitenant;
using Moq;
using NUnit.Framework;

namespace AutofacExperiments
{
    [TestFixture]
    public class MultiTenant
    {
        // for use with Moq .REturns setup
        delegate bool TryIdentifyTenantSetup(out object tenantId);
        [Test]
        public void ShouldBeAbleToCreateMultiTenantContainer()
        {
            string currentTenant = "test";

            var cb = new ContainerBuilder();

            // perform default registrations
            cb.RegisterInstance("none").As<string>();

            var root = cb.Build();
            var strategy = new Mock<ITenantIdentificationStrategy>();

            // for the purpose of this test, select tenant based on the value of a local variable
            // in practice, this normally come from some ambient value (e.g. HttpContext.Current,
            // OperationContext.Current, ExcecutionContext etc)
            strategy.Setup(s => s.TryIdentifyTenant(out It.Ref<object>.IsAny))
                .Returns(new TryIdentifyTenantSetup((out object tenantId) =>
                {
                    if (string.IsNullOrEmpty(currentTenant))
                    {
                        tenantId = "default";
                        return true;
                    }
                    tenantId = currentTenant;
                    return true;
                }
                )
            );
            ITenantIdentificationStrategy tenantIdStrategy=strategy.Object;

            var multi = new MultitenantContainer(
                    tenantIdStrategy,
                    root

                );
            
            // each tenant can supply configuration overrides as necessary
            multi.ConfigureTenant("test1", (tenantCb) =>
            {
                tenantCb.RegisterInstance("tenant is test1").As<string>();
            });
            multi.ConfigureTenant("test2", (tenantCb) =>
            {
                tenantCb.RegisterInstance("tenant is test2").As<string>();
            });
            

            Assert.IsTrue(multi.TenantIsConfigured("test1"), @" multi.TenantIsConfigured(""test1"") ");

            // tenant 1
            currentTenant = "test1";
            using (var scope = multi.BeginLifetimeScope())
            {
                var resolved = scope.Resolve<string>();
                Assert.AreEqual("tenant is test1", resolved, @" ""tenant is test1"" vs resolved");

            }
            Assert.IsTrue(multi.TenantIsConfigured("test2"), @" multi.TenantIsConfigured(""test2"") ");
            // tenant 2
            currentTenant = "test2";
            using (var scope = multi.BeginLifetimeScope())
            {
                var resolved = scope.Resolve<string>();
                Assert.AreEqual("tenant is test2", resolved, @" ""tenant is test2"" vs resolved");

            }

            // no tenant
            currentTenant = null;
            using (var scope = multi.BeginLifetimeScope())
            {
                var resolved = scope.Resolve<string>();
                Assert.AreEqual("none", resolved, @" ""none"" vs resolved");

            }

            //unregistered tenant
            currentTenant = "not registered tenant";
            using (var scope = multi.BeginLifetimeScope())
            {
                var resolved = scope.Resolve<string>();
                Assert.AreEqual("none", resolved, @" ""none"" vs resolved");

            }
        }
    }
}
