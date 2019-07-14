using dsweb_electron6.Models;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using s2decode.Models;
using dsweb_electron6;

namespace s2decode
{
    class s2decode
    {
        private ScriptScope SCOPE { get; set; }
        private ScriptEngine ENGINE { get; set; }
        public DateTime START { get; set; }
        public DateTime END { get; set; }
        private static string EXEDIR;

        static int THREADS = 0;
        public int TOTAL { get; set; } = 0;
        public int TOTAL_DONE = 0;
        static int REPID = 0;
        static readonly object _locker = new object();

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        ConcurrentDictionary<string, int> SKIP { get; set; } = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, int> REDO { get; set; } = new ConcurrentDictionary<string, int>();

        public ScriptEngine LoadEngine(int ID)
        {
            Program.Log("Loading Engine ..", 3);
            REPID = ID + 1;
            string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            EXEDIR = exedir;
            string pylib1 = exedir + "/s2decode/pylib";
            string pylib2 = exedir + "/s2decode/pylib/site-packages";

            Dictionary<string, object> options = new Dictionary<string, object>();
            if (Program.DEBUG > 1)
            {
                options["Debug"] = ScriptingRuntimeHelpers.True;
                options["ExceptionDetail"] = ScriptingRuntimeHelpers.True;
                options["ShowClrExceptions"] = ScriptingRuntimeHelpers.True;
            }
            //options["MTA"] = ScriptingRuntimeHelpers.True;
            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine(options);

            var paths = engine.GetSearchPaths();
            paths.Add(pylib1);
            paths.Add(pylib2);
            engine.SetSearchPaths(paths);

            ScriptScope scope = engine.CreateScope();

            dynamic result = null;
            result = engine.ExecuteFile(exedir + "/s2decode/pylib/site-packages/mpyq.py", scope);
            if (result != null) Program.Log(result.ToString());
            result = engine.Execute("import s2protocol", scope);
            if (result != null) Program.Log(result);
            result = engine.Execute("from s2protocol import versions", scope);
            if (result != null) Program.Log(result);
            //Thread.Sleep(1000);
            SCOPE = scope;
            ENGINE = engine;
            Program.Log("Loading Engine comlete.", 3);
            return engine;
        }


        public void DecodePython(Object stateInfo)
        {
            Interlocked.Increment(ref THREADS);
            //Console.WriteLine("Threads running: " + THREADS);

            string rep = (string)stateInfo;
            string id = Path.GetFileNameWithoutExtension(rep);
            Program.Log("Working on rep ..", 3);
            Program.Log("Loading s2protocol ..", 3);
            dynamic MPQArchive = SCOPE.GetVariable("MPQArchive");
            dynamic archive = null;
            dynamic files = null;
            dynamic contents = null;
            dynamic versions = null;
            try
            {
                archive = MPQArchive(rep);
                files = archive.extract();
                contents = archive.header["user_data_header"]["content"];

                //versions = SCOPE.GetVariable("versions");
                versions = SCOPE.GetVariable("versions");
            }
            catch
            {
                Program.Log("No MPQArchive for " + id, 3);
                FailCleanup(rep);
                return;
            }
            dynamic header = null;
            try
            {
                lock (_locker)
                {
                    header = versions.latest().decode_replay_header(contents);
                }
            }
            catch (Exception e)
            {
                Program.Log("No header for " + id + ": " + e.Message, 3);
                FailCleanup(rep);
                return;
            }

            if (header != null)
            {
                Program.Log("Loading s2protocol header finished", 3);
                var baseBuild = header["m_version"]["m_baseBuild"];
                dynamic protocol = null;
                try
                {
                    protocol = versions.build(baseBuild);
                }
                catch
                {
                    Program.Log("No protocol found for " + id);
                    FailCleanup(rep);
                    return;
                }
                Program.Log("Loading s2protocol protocol finished", 3);

                /**
                // init
                var init_enc = archive.read_file("replay.initData");
                dynamic init_dec = null;
                try
                {
                    init_dec = protocol.decode_replay_initdata(init_enc);
                }
                catch
                {
                    Program.Log("No Init version for " + id);
                    FailCleanup(rep);
                    return;
                }
                Program.Log("Loading s2protocol init finished");

                GetInit(rep, init_dec);
                **/

                // details
                var details_enc = archive.read_file("replay.details");
                dynamic details_dec = null;
                try
                {
                    details_dec = protocol.decode_replay_details(details_enc);
                }
                catch
                {
                    Program.Log("No Version for " + id, 3);
                    FailCleanup(rep);
                    return;
                }
                Program.Log("Loading s2protocol details finished", 3);

                //s2replay replay = s2parse.GetDetails(rep, details_dec);
                dsreplay replay = DSparse.GetDetails(rep, details_dec);

                // trackerevents
                var trackerevents_enc = archive.read_file("replay.tracker.events");
                dynamic trackerevents_dec = null;
                try
                {
                    trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);
                    Program.Log("Loading trackerevents success", 3);
                }
                catch
                {
                    Program.Log("No tracker version for " + id, 3);
                    FailCleanup(rep);
                    return;
                }
                Program.Log("Loading s2protocol trackerevents finished", 3);

                replay = DSparse.GetTrackerevents(rep, trackerevents_dec, replay);
                //s2parse.GetTrackerevents(rep, protocol.decode_replay_tracker_events(trackerevents_enc));

                Interlocked.Increment(ref REPID);
                replay.ID = REPID;
                //if (!replaysng.ContainsKey(repid)) replaysng.TryAdd(repid, replay);
                //Save(Program.myJson_file, replay);
                SaveDS(Program.myJson_file, replay);

            }

            Interlocked.Increment(ref TOTAL_DONE);
            double wr = 0;
            if (TOTAL > 0) wr = TOTAL_DONE * 100 / TOTAL;
            wr = Math.Round(wr, 2);

            if (TOTAL_DONE >= TOTAL)
            {
                DateTime end = DateTime.UtcNow;
                TimeSpan timeDiff = end - START;
                Console.WriteLine(timeDiff.TotalSeconds);
                END = end;
                if (REDO.Count > 0)
                {
                    Console.WriteLine("REDO: " + REDO.Count);
                    //RedoScan();
                }
                else
                {
                    //Stop_decode();
                }
            }

            Interlocked.Decrement(ref THREADS);
        }



        public void Save(string out_file, s2replay rep)
        {

            TextWriter writer = null;
            _readWriteLock.EnterWriteLock();
            try
            {
                var repjson = JsonConvert.SerializeObject(rep);

                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);

            }
            catch
            {
                Program.Log("Failed writing to json :(", 3);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            _readWriteLock.ExitWriteLock();

        }

        public void SaveDS(string out_file, dsreplay rep)
        {

            TextWriter writer = null;
            _readWriteLock.EnterWriteLock();
            try
            {
                var repjson = JsonConvert.SerializeObject(rep);

                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);

            }
            catch
            {
                Program.Log("Failed writing to json :(", 3);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            _readWriteLock.ExitWriteLock();

        }

        private void FailCleanup(string replay_file)
        {
            //if (SKIP.ContainsKey(rep)) SKIP[rep]++;
            //else SKIP.TryAdd(rep, 1);

            if (!REDO.ContainsKey(replay_file))
                REDO.TryAdd(replay_file, 1);
            Interlocked.Increment(ref TOTAL_DONE);
            Interlocked.Decrement(ref THREADS);
        }
    }


}
