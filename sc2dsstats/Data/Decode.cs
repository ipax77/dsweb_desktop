using pax.s2decode.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public static class Decode
    {
        private static pax.s2decode.s2decode s2dec = new pax.s2decode.s2decode();
        private static BlockingCollection<string> _jobs_decode = new BlockingCollection<string>();
        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 4;
        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);
        public static List<string> Failed { get; set; } = new List<string>();


        public static ScanState Scan { get; set; } = new ScanState();

        public static async Task<dsreplay> ScanRep(string file, DSreplays Data, bool GetDetails = false)
        {
            return await Task.Run(() =>
            {
                s2dec.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                return s2dec.DecodePython(file, false, GetDetails);
            });
        }

        public static void Doit(DSreplays Data, ScanStateChange stateChange, int cores = 2)
        {
            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            Scan.Done = 0;
            Failed = new List<string>();
            Console.WriteLine("Engine start.");
            //s2dec.DEBUG = 1;
            s2dec.JsonFile = Program.myJson_file;
            s2dec.REPID = Data.ID;
            s2dec.ReplayFolder = Data.ReplayFolder;
            s2dec.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            //s2dec.LoadEngine();
            s2dec.END = new DateTime();
            s2dec.START = DateTime.UtcNow;
            stateChange.Update = !stateChange.Update;
            int total = 0;
            lock (Data.Todo)
            {
                _jobs_decode = new BlockingCollection<string>();
                foreach (var ent in Data.Todo)
                {
                    try
                    {
                        _jobs_decode.Add(ent);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
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
                int i = 0;
                while (!_empty.WaitOne(1000))
                {
                    double twr = 0;
                    if (s2dec.TOTAL > 0)
                    {
                        twr = (double)s2dec.TOTAL_DONE * 100 / (double)s2dec.TOTAL;
                    }
                    Scan.Done = Math.Round(twr, 2);
                    string bab = s2dec.TOTAL_DONE + "/" + s2dec.TOTAL + " done. (" + Scan.Done.ToString() + "%)";
                    Scan.Info = bab;
                    Console.Write("\r{0}   ", bab);

                    if (_jobs_decode.Count() == 0)
                    {
                        i++;
                        if (!s2dec.END.Equals(DateTime.MinValue) || i > 20)
                        {

                            break;
                        }
                    }
                    stateChange.Update = !stateChange.Update;
                }
                /*
                Console.WriteLine("\r   " + s2dec.TOTAL + "/" + s2dec.TOTAL + " done. (100%)");
                Console.WriteLine("Jobs done.");
                Scan.Info = s2dec.TOTAL + "/" + s2dec.TOTAL + " done. (100%)";
                Scan.Done = 100;
                */
                double wr = 0;
                if (s2dec.TOTAL > 0)
                {
                    wr = (double)s2dec.TOTAL_DONE * 100 / (double)s2dec.TOTAL;
                }
                Scan.Done = Math.Round(wr, 2);
                string info = s2dec.TOTAL_DONE + "/" + s2dec.TOTAL + " done. (" + Scan.Done.ToString() + "%)";
                Scan.Info = info;
                Scan.Running = false;
                Elapsed = s2dec.END - s2dec.START;
                if (s2dec.THREADS > 0)
                {
                    int j = 0;
                    while (s2dec.THREADS > 0 || j > 60)
                    {
                        Thread.Sleep(250);
                        j++;
                    }
                }
                Reload(Data);
                Failed = new List<string>(s2dec.REDO.Keys.ToList());
                stateChange.Update = !stateChange.Update;
            }, TaskCreationOptions.AttachedToParent);
        }

        public static void StopIt()
        {
            try
            {
                source.Cancel();
            }
            catch { }
            finally
            {
                source.Dispose();
            }
        }

        private static void Reload(DSreplays Data)
        {
            Data.LoadData(true);
        }

        private static void OnHandlerStart(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;

            try
            {
                foreach (var job in _jobs_decode.GetConsumingEnumerable(token))
                {
                    s2dec.DecodePython(job);
                }
            }
            catch (OperationCanceledException)
            {
                try
                {
                    s2dec.END = DateTime.UtcNow;
                }
                catch { }
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
