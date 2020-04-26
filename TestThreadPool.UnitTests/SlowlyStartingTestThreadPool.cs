using System.Threading;

namespace TestThreadPool.UnitTests
{
    public class SlowlyStartingTestThreadPool : TestThreadPool
    {
        public SlowlyStartingTestThreadPool(int maxTasks, int? maxThreads = null)
            : base(maxTasks, maxThreads)
        {
        }

        protected override void Consume()
        {
            // Ждём 1 сек. перед тем, как начать разбирать очередь задач.
            // Это нужно для теста ShouldFailToAddTooManyActions (1 сек. - достаточно большой таймаут для того, чтобы предположить, что задачи
            // не будут разобраны из очереди до того, как тест попытается добавить лишнюю задачу).
            Thread.Sleep(1000);

            base.Consume();
        }
    }
}