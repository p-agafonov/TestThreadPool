using System;
using System.Threading;

namespace TestThreadPool
{
    class Program
    {
        static void Main(string[] args)
        {
            var testPool = new TestThreadPool(maxTasks: 30, maxThreads: 6);

            using (testPool)
            {
                Console.WriteLine("Created a pool");

                for (var i = 0; i < 20; i++)
                {
                    var tmp = i;

                    testPool.Add(() => {
                        Thread.Sleep(500);
                        Console.WriteLine("Task nr. {0}, ThreadId: {1}", tmp, Thread.CurrentThread.ManagedThreadId);

                    });
                }

                Console.WriteLine("Sleeping...");
                Thread.Sleep(30000);

                testPool.Add(() => {
                    Console.WriteLine("Added after sleeping. Thread Id: {0}", Thread.CurrentThread.ManagedThreadId);
                });
            }

            try
            {
                testPool.Add(() => {
                    Console.WriteLine("This task will not be queued");
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("All tasks complete");
            Console.ReadLine();
        }
    }
}