using Newtonsoft.Json;
using pax.s2decode.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sc2dsstats.Data
{
    public static class DSrest
    {

        public static bool Upload(StartUp startUp, DSreplays dsData)
        {
            return AutoUpload(startUp, dsData);
        }

        public static bool AutoUpload(StartUp startUp, DSreplays dsData)
        {
            string hash = "UndEsWarSommer";
            string hash2 = "UndEsWarWinter";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string names = String.Join(";", startUp.Conf.Players);
                hash = GetHash(sha256Hash, names);
                hash2 = GetHash(sha256Hash, Program.myJson_file);
            }
            var client = new RestClient("https://www.pax77.org:9126");
            //var client = new RestClient("https://192.168.178.28:9001");
            //var client = new RestClient("http://192.168.178.28:9000");
            //var client = new RestClient("https://localhost:44393");

            List<dsreplay> temp = new List<dsreplay>(dsData.Replays);
            string lastrep = "";
            if (temp.Count > 0)
            {
                lastrep = temp.OrderByDescending(o => o.GAMETIME).First().GAMETIME.ToString().Substring(0, 14);
            }

            DSinfo info = new DSinfo();
            info.Name = hash;
            info.Json = hash2;
            info.LastRep = lastrep;
            info.LastUpload = startUp.Conf.LastUpload;
            info.Total = dsData.Replays.Count;
            info.Version = startUp.Conf.Version;

            var restRequest = new RestRequest("/secure/data/autoinfo", Method.POST);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddJsonBody(info);
            var response = client.Execute(restRequest);


            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response.Content.Contains("UpToDate")) return true;
                else lastrep = response.Content;
            }
            else return false;

            lastrep = new String(lastrep.Where(Char.IsDigit).Take(14).ToArray());

            double dlastrep = 0;
            try
            {
                dlastrep = Double.Parse(lastrep);
            }
            catch
            {
                return false;
            }
            if (dlastrep == 0 || startUp.Conf.FullSend == true) temp = new List<dsreplay>(dsData.Replays);
            else temp = new List<dsreplay>(dsData.Replays.Where(x => x.GAMETIME > dlastrep).ToList());

            List<string> anonymous = new List<string>();
            foreach (dsreplay replay in temp)
            {
                Dictionary<string, string> plbackup = new Dictionary<string, string>();
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    string plname = pl.NAME;
                    if (startUp.Conf.Players.Contains(pl.NAME)) pl.NAME = "player";
                    else pl.NAME = "player" + pl.REALPOS.ToString();
                    plbackup[pl.NAME] = plname;
                }
                anonymous.Add(JsonConvert.SerializeObject(replay));

                foreach (dsplayer pl in replay.PLAYERS)
                {
                    if (plbackup.ContainsKey(pl.NAME))
                        pl.NAME = plbackup[pl.NAME];
                }
            }
            string exp_csv = Program.workdir + "\\export.json";
            if (!File.Exists(exp_csv))
            {
                File.Delete(exp_csv);
            }
            File.WriteAllLines(exp_csv, anonymous);
            string exp_csv_gz = exp_csv + ".gz";
            using (FileStream fileToBeZippedAsStream = new FileInfo(exp_csv).OpenRead())
            {
                using (FileStream gzipTargetAsStream = new FileInfo(exp_csv_gz).Create())
                {
                    using (GZipStream gzipStream = new GZipStream(gzipTargetAsStream, CompressionMode.Compress))
                    {
                        try
                        {
                            fileToBeZippedAsStream.CopyTo(gzipStream);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            if (startUp.Conf.FullSend == true)
                restRequest = new RestRequest("/secure/data/fullsend/" + hash);
            else
                restRequest = new RestRequest("/secure/data/autoupload/" + hash);
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.Method = Method.POST;
            restRequest.AddHeader("Authorization", "DSupload77");
            restRequest.AddFile("content", exp_csv_gz);
            response = client.Execute(restRequest);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (startUp.Conf.FullSend == true)
                {
                    startUp.Conf.FullSend = false;
                    startUp.Save();
                }
                return true;
            }
            else return false;
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }

    [Serializable]
    public class DSinfo
    {
        public string Name { get; set; }
        public string Json { get; set; }
        public int Total { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastRep { get; set; }
        public string Version { get; set; }
    }
}
