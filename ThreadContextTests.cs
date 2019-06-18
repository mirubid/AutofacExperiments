using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public class ThreadContextTests
    {
        [Test]
        public async Task CallContextShouldFlow()
        {
            var currentcontext = Thread.CurrentContext;

            WriteLine("Thread.CurrentContext");
            WriteLine(currentcontext.GetType().Name);
            WriteLine(String.Join("\r\n\t", currentcontext.ContextProperties.Select(p => p.Name)));


            var thread = Thread.CurrentThread;
            var context = thread.ExecutionContext;
            WriteLine("");
            WriteLine("Thread.CurrentThread");
            WriteLine(context.GetType().Name);
            

            var exContext = ExecutionContext.Capture();

            var synchContext = SynchronizationContext.Current;

            WriteLine("SynchronizationContext.Current");
            WriteLine(synchContext?.ToString());

            CallContext.LogicalSetData("ut-name", "1");
            var gotValue=CallContext.LogicalGetData("ut-name");

            Assert.AreEqual("1", gotValue, @" ""1"" vs gotValue");

            var gotFromTask = await Task.Run(() =>
            {
                return CallContext.LogicalGetData("ut-name") as string;
            });

            Assert.AreEqual("1", gotFromTask, @" ""1"" vs gotFromTask");

            string gotFromThread=null;
            var t = new Thread(new ThreadStart(() =>
            {
                gotFromThread = CallContext.LogicalGetData("ut-name") as string;

            }));
            t.Start();

            t.Join();
            Assert.AreEqual("1", gotFromThread, @" ""1"" vs gotFromThread");

            
        }
        [Test]
        public void SeparateThreads_Should_InitiateSeparateValues()
        {
            string gotFromThread1 = null;
            string gotFromThread2 = null;
            object sycn = new object();

            var t1 = new Thread(new ThreadStart(() =>
            {
                lock (sycn)
                {
                    gotFromThread1 = CallContext.LogicalGetData("ut2-name") as string;
                    if(gotFromThread1==null)
                        CallContext.LogicalSetData("ut2-name","1");

                    gotFromThread1 = CallContext.LogicalGetData("ut2-name") as string;
                }

            }));
            t1.Start();
            var t2 = new Thread(new ThreadStart(() =>
            {
                lock (sycn)
                {
                    gotFromThread2 = CallContext.LogicalGetData("ut2-name") as string;
                    if (gotFromThread2 == null)
                        CallContext.LogicalSetData("ut2-name", "2");

                    gotFromThread2 = CallContext.LogicalGetData("ut2-name") as string;
                }


            }));
            t2.Start();

            t2.Join();
            t1.Join();

            Assert.IsNotNull(gotFromThread1, "gotFromThread1");
            Assert.IsNotNull(gotFromThread2, "gotFromThread2");

            Assert.AreNotEqual(gotFromThread1, gotFromThread2, @" gotFromThread1 vs gotFromThread2");


        }
        [Test]
        public void EachThread_ShouldHaveItsOwnCopy()
        {
            string gotFromThread1 = null;
            object sycn = new object();
            string key = "ut3-name";
            CallContext.LogicalSetData(key, "main");

            var t1 = new Thread(new ThreadStart(() =>
            {
                lock (sycn)
                {
                    gotFromThread1 = CallContext.LogicalGetData(key) as string;
                    if (gotFromThread1 == "main")
                        CallContext.LogicalSetData(key, "1");

                    gotFromThread1 = CallContext.LogicalGetData(key) as string;
                }

            }));
            t1.Start();
            
            t1.Join();

            Assert.IsNotNull(gotFromThread1, "gotFromThread1");

            Assert.AreNotEqual(gotFromThread1, "main", @" gotFromThread1 vs main");

            Assert.AreEqual("main", CallContext.LogicalGetData(key), @" ""main"" vs CallContext.LogicalGetData(key)");


        }
        void WriteLine(string text)
        {
            TestContext.Progress.WriteLine(text);
        }
    }
}
