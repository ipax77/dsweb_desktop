using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dsweb_electron6;

namespace dsweb_electron6.Models
{
    public class UserConfig
    {
        public string WorkDir { get; set; } = Program.workdir;
        public List<string> Players { get; set; } = new List<string>();
        public List<string> Replays { get; set; } = new List<string>();
        public int Cores { get; set; } = 2;
        public bool Autoupdate { get; set; } = true;
        public bool Autoscan { get; set; } = true;
        public bool Autoupload { get; set; } = false;
        public bool Uploadcredential { get; set; } = false;
        public bool MMcredential { get; set; } = false;
        public string Version { get; set; } = "v0.5";
        public DateTime LastUpload { get; set; } = new DateTime(2018, 1, 1);
    }
}
