using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace TestThreadPool.UnitTests
{
    [TestClass]
    public class ThreadPoolTests
    {
        private static readonly Action EmptyAction = () => { };

        [TestMethod]
        public void ShouldAddAndExecuteActions() // Добавляем задачи и проверяем, что выполнено ровно столько задач, сколько добавлено
        {
            var testHelperMock = new Mock<TestHelper>();
            var obj = testHelperMock.Object;

            using (var pool = new TestThreadPool(maxTasks: 3))
            {
                pool.Add(obj.DoNothing);
                pool.Add(obj.DoNothing);
                pool.Add(obj.DoNothing);
            }

            VerifyActionCallTimes();

            testHelperMock.Invocations.Clear();

            using (var pool = new TestThreadPool(maxTasks: 3))
            {
                var added = pool.TryAdd(obj.DoNothing);
                Assert.AreEqual(expected: true, actual: added);

                added = pool.TryAdd(obj.DoNothing);
                Assert.AreEqual(expected: true, actual: added);

                added = pool.TryAdd(obj.DoNothing);
                Assert.AreEqual(expected: true, actual: added);
            }

            VerifyActionCallTimes();

            void VerifyActionCallTimes() =>
                testHelperMock.Verify(t => t.DoNothing(), Times.Exactly(3), "The number of the tasks added and executed should match.");
        }

        [TestMethod]
        public void ShouldFailToAddTooManyActions() // Падаем или false при попытке добавить слишком много задач
        {
            using (var pool = new SlowlyStartingTestThreadPool(maxTasks: 2))
            {
                pool.Add(EmptyAction);
                pool.Add(EmptyAction);

                Assert.ThrowsException<InvalidOperationException>(() => {
                    // ReSharper disable once AccessToDisposedClosure
                    pool.Add(EmptyAction);
                });

                var addedExtraAction = pool.TryAdd(EmptyAction);

                Assert.AreEqual(expected: false, actual: addedExtraAction);
            }
        }

        [TestMethod]
        public void ShouldFailToAddActionToDisposedPool() // Падаем или false при попытке добавить задачу после вызова Dispose
        {
            var pool = new TestThreadPool(maxTasks: 2);

            using (pool)
            {
            }

            Assert.ThrowsException<ObjectDisposedException>(() => {
                pool.Add(EmptyAction);
            });

            var addedToDisposedPool = pool.TryAdd(EmptyAction);

            Assert.AreEqual(expected: false, actual: addedToDisposedPool);
        }

        [TestMethod]
        public void ShouldFailToAddNullAction() // Падаем при попытках добавить null
        {
            Assert.ThrowsException<ArgumentNullException>(() => {
                using (var pool = new TestThreadPool(maxTasks: 2))
                    pool.Add(null);
            });

            Assert.ThrowsException<ArgumentNullException>(() => {
                using (var pool = new TestThreadPool(maxTasks: 2))
                    pool.TryAdd(null);
            });
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        public class TestHelper
        {
            public virtual void DoNothing()
            {
                Thread.Sleep(1000);
            }
        }
    }
}