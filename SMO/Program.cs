using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMO
{
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
    class Server
    {
        int n;
        public int requestCount;
        public int rejectedCount;
        public int processedCount;
        Dictionary<int, int> thradsInUse = new Dictionary<int, int>();
        struct PoolRecord { 
            public Thread thread; // объект потока
            public bool in_use; // флаг занятости
            public int useCount;
            public Stopwatch waitTime;
            public long totalWaitTime;
        }

        PoolRecord[] pool;
        object threadLock = new object();
        public void proc(object sender, procEventArgs e) { 
            lock (threadLock) { 
                Console.WriteLine("Заявка с номером: {0}", e.id);
                requestCount++; 
                for (int i = 0; i < n; i++) { 
                    if (!pool[i].in_use) { 
                        pool[i].in_use = true; 
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        thradsInUse[e.id] = i;
                        pool[i].useCount++;
                        pool[i].waitTime.Stop();
                        pool[i].totalWaitTime = pool[i].waitTime.ElapsedMilliseconds;
                        pool[i].thread.Start(e.id); 
                        processedCount++; 
                        return; 
                    } 
                } 
                rejectedCount++; 
            }
        }

        public void finish()
        {
            for (int i = 0; i < n; i++)
            {
                pool[i].thread?.Join();
            }
        }

        public void Answer(object id)
        {
            Thread.Sleep(100);
            Console.WriteLine("Заявка с номером {0} обсуженна", id);
            int index = thradsInUse[(int)id];
            pool[index].in_use = false;
            pool[index].waitTime.Start();
        }

        public void startTime()
        {
            for (int i = 0; i < n; i++)
            {
                pool[i].waitTime.Start();
            }
        }

        public void printThreadResolts()
        {
            Console.WriteLine("\t\t\tprocessedCount\tTotal Wait Time");
            for (int i = 0; i < n; i++)
            {
                Console.WriteLine("\t\t" + i.ToString() + "\t" + pool[i].useCount.ToString() + "\t\t" + pool[i].totalWaitTime.ToString());
            }
        }

        public Server(int poolSize)
        {
            n = poolSize;
            rejectedCount = 0;
            requestCount = 0;
            processedCount = 0;
            pool = new PoolRecord[poolSize];
            for (int i = 0; i < n; i++)
            {
                pool[i].useCount = 0;
                pool[i].in_use = false;
                pool[i].waitTime = new Stopwatch();
                pool[i].totalWaitTime = 0;
            }

        }
    }
    class Client
    {

        public event EventHandler<procEventArgs> request;
        Server server;
        public Client(Server server) { 
            this.server = server; 
            this.request += server.proc; 
        }

        protected virtual void OnProc(procEventArgs e) { 
            EventHandler<procEventArgs> handler = request; 
            if (handler != null) { 
                handler(this, e); 
            } 
        }

        public void send(int id)
        {
            procEventArgs prArg = new procEventArgs();
            prArg.id = id;
            OnProc(prArg);
        }
    }
    class Program
    {
        static void Main()
        {
            int servanceInt = 3;
            int sendInt = 5;
            Server serv = new Server(servanceInt);
            Client client = new Client(serv);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int id = 0;
            serv.startTime();
            while (sw.ElapsedMilliseconds < 120000)
            {
                client.send(id);
                Thread.Sleep(100 / sendInt);
                id++;
            }
            serv.finish();
            Console.WriteLine("Total results at the end of 2 min:");
            Console.WriteLine("\tTotal request count = " + serv.requestCount.ToString());
            Console.WriteLine("\tTotal processed count = " + serv.processedCount.ToString());
            Console.WriteLine("\tTotal rejected count = " + serv.rejectedCount.ToString());
            Console.WriteLine("\tFor each thrad:");
            serv.printThreadResolts();

        }
    }
}
