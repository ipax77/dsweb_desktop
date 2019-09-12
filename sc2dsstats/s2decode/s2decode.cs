using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Text.Json;
using s2decode.Models;
using sc2dsstats.Models;
using sc2dsstats;

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
        public ConcurrentDictionary<string, int> REDO { get; set; } = new ConcurrentDictionary<string, int>();

        Dictionary<string, string> ReplayFolder { get; set; } = new Dictionary<string, string>();

        public ScriptEngine LoadEngine(int ID, Dictionary<string, string> repfolder)
        {
            ReplayFolder = repfolder;
            if (ENGINE != null) return ENGINE;
            Program.Log("Loading Engine ..");
            REPID = ID + 1;
            string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            EXEDIR = exedir;
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
            Program.Log("Loading Engine comlete.");
            return engine;
        }


        public dsreplay DecodePython(Object stateInfo, bool toJson = true, bool GetDetails = false)
        {
            Interlocked.Increment(ref THREADS);
            //Console.WriteLine("Threads running: " + THREADS);
            dsreplay replay = null;
            string rep = (string)stateInfo;
            string id = Path.GetFileNameWithoutExtension(rep);
            Program.Log("Working on rep ..");
            Program.Log("Loading s2protocol ..");
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
                Program.Log("No MPQArchive for " + id);
                FailCleanup(rep);
                return null;
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
                Program.Log("No header for " + id + ": " + e.Message);
                FailCleanup(rep);
                return null;
            }

            if (header != null)
            {
                Program.Log("Loading s2protocol header finished");
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
                    return null;
                }
                Program.Log("Loading s2protocol protocol finished");

                
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
                    return null;
                }
                Program.Log("Loading s2protocol init finished");

                s2parse.GetInit(rep, init_dec);
                

                // details
                var details_enc = archive.read_file("replay.details");
                dynamic details_dec = null;
                try
                {
                    details_dec = protocol.decode_replay_details(details_enc);
                }
                catch
                {
                    Program.Log("No Version for " + id);
                    FailCleanup(rep);
                    return null;
                }
                Program.Log("Loading s2protocol details finished");

                //s2replay replay = s2parse.GetDetails(rep, details_dec);
                replay = DSparseNG.GetDetails(rep, details_dec);

                // trackerevents
                var trackerevents_enc = archive.read_file("replay.tracker.events");
                dynamic trackerevents_dec = null;
                try
                {
                    trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);
                    Program.Log("Loading trackerevents success");
                }
                catch
                {
                    Program.Log("No tracker version for " + id);
                    FailCleanup(rep);
                    return null;
                }
                Program.Log("Loading s2protocol trackerevents finished");

                replay = DSparseNG.GetTrackerevents(rep, trackerevents_dec, replay, GetDetails);
                //s2parse.GetTrackerevents(rep, protocol.decode_replay_tracker_events(trackerevents_enc));
                Interlocked.Increment(ref REPID);
                replay.ID = REPID;
                replay.REPLAY = ReplayFolder[Path.GetDirectoryName(rep)] + "/" + id;
                replay.Init();
                //if (!replaysng.ContainsKey(repid)) replaysng.TryAdd(repid, replay);
                //Save(Program.myJson_file, replay);
                if (toJson == true)
                    SaveDS(Program.myJson_file, replay);
                
            }

            Interlocked.Increment(ref TOTAL_DONE);

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
            return replay;
        }



        public void Save(string out_file, s2replay rep)
        {

            TextWriter writer = null;
            _readWriteLock.EnterWriteLock();
            try
            {
                //var repjson = JsonConvert.SerializeObject(rep);
                var repjson = JsonSerializer.Serialize(rep);
                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);

            }
            catch
            {
                Program.Log("Failed writing to json :(");
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
            //TextWriter writer2 = null;

            /**
            ReplayDetails details = new ReplayDetails();
            details.MIDDLE = rep.MIDDLE;
            foreach (dsplayer pl in rep.PLAYERS.OrderBy(o => o.REALPOS)) 
            {
                PlayerDetails pld = new PlayerDetails();
                pld.SPAWNS = pl.SPAWNS;
                pld.STATS = pl.STATS;
                pld.REALPOS = pl.REALPOS;
                details.PLAYERS.Add(pld);
            }

            var details_json = JsonSerializer.Serialize(details);
            **/

            _readWriteLock.EnterWriteLock();
            try
            {
                //var repjson = JsonConvert.SerializeObject(rep);
                var repjson = JsonSerializer.Serialize(rep);
                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);

                //writer2 = new StreamWriter(Program.myDetails_file);
                //writer2.Write(details_json + Environment.NewLine);

            }
            catch (Exception e)
            {
                Program.Log("Failed writing to json :(");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
                //if (writer2 != null)
                //    writer2.Close();

            }
            _readWriteLock.ExitWriteLock();

        }

        private void FailCleanup(string replay_file)
        {
            //if (SKIP.ContainsKey(rep)) SKIP[rep]++;
            //else SKIP.TryAdd(rep, 1);

            if (!REDO.ContainsKey(replay_file))
                REDO.TryAdd(replay_file, 1);
            else
                REDO[replay_file]++;

            //Interlocked.Increment(ref TOTAL_DONE);
            Interlocked.Decrement(ref THREADS);
        }
    }


}
