using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace sc2dsstats.Models
{
    public static class Decode
    {
        private static s2decode.s2decode s2dec = new s2decode.s2decode();
        private static BlockingCollection<string> _jobs_decode = new BlockingCollection<string>();
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 4;
        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);
        public static List<string> Failed { get; set; } = new List<string>();

        public static ScanState Scan { get; set; } = new ScanState();

        public static void Doit(DSdataModel Data, ScanStateChange stateChange, int cores = 2)
        {
            CORES = cores;
            Scan.Done = 0;
            Failed = new List<string>();
            Console.WriteLine("Engine start.");
            s2dec.LoadEngine(Data.ID);
            //s2dec.LoadEngine();
            s2dec.END = new DateTime();
            s2dec.START = DateTime.UtcNow;
            stateChange.Update = !stateChange.Update;
            int total = 0;
            lock (Data.Todo)
            {
                foreach (var ent in Data.Todo)
                {
                    _jobs_decode.Add(ent);
                    total++;
                }
            }
            s2dec.TOTAL = total;
            Scan.Total = total;
            s2dec.TOTAL_DONE = 0;
            Scan.Info = s2dec.TOTAL_DONE + "/" + s2dec.TOTAL + " done. (0%)";

            for (int i = 0; i < CORES; i++)
            {
                Thread thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            Task tsscan = Task.Factory.StartNew(() =>
            {
                while (!_empty.WaitOne(1000))
                {
                    double wr = 0;
                    if (s2dec.TOTAL > 0)
                    {
                        wr = (double)s2dec.TOTAL_DONE * 100 / (double)s2dec.TOTAL;
                    }
                    Scan.Done = Math.Round(wr, 2);
                    string bab = s2dec.TOTAL_DONE + "/" + s2dec.TOTAL + " done. (" + Scan.Done.ToString() + "%)";
                    Scan.Info = bab;
                    Console.Write("\r{0}   ", bab);
                    
                    if (_jobs_decode.Count == 0)
                    {
                        //Thread.Sleep(3000);
                        int i = 0;
                        while (s2dec.END.Equals(DateTime.MinValue))
                        {
                            Thread.Sleep(250);
                            i++;
                            if (i > 20)
                                break;
                        }
                        Console.WriteLine("\r   " + s2dec.TOTAL + "/" + s2dec.TOTAL + " done. (100%)");
                        Console.WriteLine("Jobs done.");
                        Scan.Info = s2dec.TOTAL + "/" + s2dec.TOTAL + " done. (100%)";
                        Scan.Done = 100;
                        Scan.Running = false;
                        Elapsed = s2dec.END - s2dec.START;
                        Reload(Data);
                        Failed = new List<string>(s2dec.REDO.Keys.ToList());
                        stateChange.Update = !stateChange.Update;
                        break;
                    }
                    stateChange.Update = !stateChange.Update;
                }
            }, TaskCreationOptions.AttachedToParent);

        }

        private static void Reload(DSdataModel Data)
        {
            Data.LoadData();
        }

        private static void OnHandlerStart(object obj)
        {
            foreach (var job in _jobs_decode.GetConsumingEnumerable(CancellationToken.None))
            {
                s2dec.DecodePython(job);
            }
            _empty.Set();
        }
    }

    public class ScanState
    {
        public int Total { get; set; } = 0;
        public double Done { get; set; } = 0;
        public string Info { get; set; } = "";
        public bool Running { get; set; } = false;
    }

    public class ScanStateChange : INotifyPropertyChanged
    {
        private bool Update_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ScanState Scan { get; set; } = Decode.Scan;

        public bool Update
        {
            get { return this.Update_value; }
            set
            {
                if (value != this.Update_value)
                {
                    this.Update_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
