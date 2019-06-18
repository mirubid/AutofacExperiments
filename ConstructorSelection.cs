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
    public class ConstructorSelection
    {
        [Test]
        public void ShouldUseDefaultConstructorWhenNoTypesRegistered()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<ComplexConstructorService>();

            var service = cb.Build().Resolve<ComplexConstructorService>();

            Assert.IsNotNull(service, "service");

            // the default constructor will be used because none of the other constructors can be used
            Assert.AreEqual("default", service.ConstructorUsed, @" ""default"" vs service.ConstructorUsed");


        }
        [Test]
        public void ShouldUseMostComplexConstructorAvailable()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<ComplexConstructorService>();
            cb.Register(c => 42).As<int>();
            cb.Register(c => "a string").As<string>();
            cb.Register(c => DateTime.Now).As<DateTime>();
            cb.Register(c => long.MaxValue).As<long>();
            
            var service = cb.Build().Resolve<ComplexConstructorService>();

            Assert.IsNotNull(service, "service");

            // the big constructor will be used because all of the required types are registered
            Assert.AreEqual("all", service.ConstructorUsed, @" ""all"" vs service.ConstructorUsed");


        }
        [Test]
        public void OptionalParametersDontHaveToBeRegistered()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<ComplexConstructorService>();
            cb.Register(c => 42).As<int>();
            cb.Register(c => "a string").As<string>();
            cb.Register(c => DateTime.Now).As<DateTime>();


            var service = cb.Build().Resolve<ComplexConstructorService>();

            Assert.IsNotNull(service, "service");

            // the big constructor will be used because none of the other constructors can be used
            Assert.AreEqual("all", service.ConstructorUsed, @" ""all"" vs service.ConstructorUsed");


        }
        [Test]
        public void EquallyComplexConstructorsCauseError()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<ComplexConstructorService>();
            cb.Register(c => 42).As<int>();
            cb.Register(c => "a string").As<string>();


            Assert.Throws<Autofac.Core.DependencyResolutionException>(
                () =>
                {
                    var service = cb.Build().Resolve<ComplexConstructorService>();
                });


        }
    }
    public class ComplexConstructorService
    {
        private readonly string _constructorUsed = null;
        public ComplexConstructorService(int number,string astring, DateTime date, long longNumber=0)
        {
            _constructorUsed = "all";
        }
        public ComplexConstructorService(int number, int number2)
        {
            _constructorUsed = "equally long constructor a";
        }
        public ComplexConstructorService(int number, string astring)
        {
            _constructorUsed = "equally long constructor b";
        }
        public ComplexConstructorService()
        {
            _constructorUsed = "default";
        }
        public ComplexConstructorService(string astring)
        {
            _constructorUsed = "string";
        }
        public string ConstructorUsed => _constructorUsed;

    }
}
