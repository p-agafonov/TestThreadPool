using System;
using System.Collections.Generic;
using System.Threading;

namespace TestThreadPool
{
    public class TestThreadPool : IDisposable
    {
        private static readonly TimeSpan ThreadIdleTimeout = TimeSpan.FromSeconds(10);

        private readonly int _maxTasks;
        private readonly int _maxThreads;

        private readonly object _locker = new object();


        private readonly List<Thread> _workers;
        private readonly Queue<Action> _actionQueue = new Queue<Action>();

        private bool _isDisposed;

        public TestThreadPool(int maxTasks, int? maxThreads = null)
        {
            _maxTasks = maxTasks;
            _maxThreads = maxThreads ?? Environment.ProcessorCount;

            _workers = new List<Thread>(_maxThreads);

            for (var i = 0; i < _maxThreads; i++)
            {
                var worker = new Thread(Consume);

                _workers.Add(worker);

                worker.Start();
            }
        }

        public void Add(Action action)
        {
            EnsureNotNull(action);

            if (_isDisposed)
                throw new ObjectDisposedException(objectName: null, $"The {nameof(TestThreadPool)} instance has already been disposed and cannot be used anymore.");

            lock (_locker)
            {
                if (_actionQueue.Count == _maxTasks)
                    throw new InvalidOperationException("Unable to add an action because max pool capacity has been reached.");

                _actionQueue.Enqueue(action);
                Monitor.Pulse(_locker);
            }
        }

        public bool TryAdd(Action action)
        {
            EnsureNotNull(action);

            if (_isDisposed)
                return false;

            lock (_locker)
            {
                if (_actionQueue.Count == _maxTasks)
                    return false;

                _actionQueue.Enqueue(action);
                Monitor.Pulse(_locker);

                return true;
            }
        }

        public void Dispose()
        {
            EnqueueStopTasksAndWait();
            GC.SuppressFinalize(this);
        }

        ~TestThreadPool()
        {
            EnqueueStopTasksAndWait();
        }

        protected virtual void Consume() // protected virtual для тестируемости (см. SlowlyStartingTestThreadPool в проекте с тестами)
        {
            while (true)
            {
                Action action;

                lock (_locker)
                {
                    while (_actionQueue.Count == 0)
                    {
                        var timedOut = !Monitor.Wait(_locker, ThreadIdleTimeout);

                        if (timedOut && _workers.Count > _maxThreads / 2)
                        {
                            _workers.Remove(Thread.CurrentThread);
                            return;
                        }
                    }

                    action = _actionQueue.Dequeue();
                }

                if (action == null) // Null считаем сигналом к остановке и выходим из цикла
                    return;

                action.Invoke();
            }
        }

        private void EnqueueStopTasksAndWait()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                lock (_locker)
                {
                    foreach (var _ in _workers)
                        _actionQueue.Enqueue(null); // Отправляем null, чтобы завершить поток Consume

                    Monitor.PulseAll(_locker);
                }

                foreach (var worker in _workers)
                    worker.Join();
            }
        }

        private static void EnsureNotNull(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
        }
    }
}