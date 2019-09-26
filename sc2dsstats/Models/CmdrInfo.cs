using System;
using System.Collections.Generic;

namespace sc2dsstats.Models
{
    public class CmdrInfo
    {
        public string Cmdr { get; set; }
        public int Games { get; set; } = 0;
        public int Matchups { get; set; } = 0;
        public string Winrate { get; set; } = "0";
        public string AverageGameDuration { get; set; } = "0";
        public string FilterInfo { get; set; } = "";
        public Dictionary<string, int> CmdrCount { get; set; } = new Dictionary<string, int>();
    }

    public class FilterInfo
    {
        public int GAMES { get; set; }
        public int GAMESf { get; set; }
        public int FILTERED { get; set; }
        public int Beta { get; set; }
        public int Hots { get; set; }
        public int Playercount { get; set; } = 0;
        public int Gamemodes { get; set; } = 0;
        public int Gametime { get; set; }
        public int Duration { get; set; }
        public int Leaver { get; set; }
        public int Killsum { get; set; }
        public int Army { get; set; }
        public int Income { get; set; }
        public int Std { get; set; }
        public int Total { get; set; }
        public double WR { get; set; }

        public FilterInfo()
        {
            GAMES = 0;
            FILTERED = 0;
            Beta = 0;
            Hots = 0;
            Gametime = 0;
            Duration = 0;
            Leaver = 0;
            Killsum = 0;
            Army = 0;
            Income = 0;
            Std = 0;
        }

        public string Info()
        {
            string bab = "Und es war Sommer";
            if (this.GAMES != 0)
            {


                /**
                bab = this.FILTERED + " / " + this.GAMES + " filtered (" + filtered.ToString() + "%)";
                bab += Environment.NewLine;
                double f = FilterRate(this.Beta);
                bab += "Beta: " + this.Beta + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Hots);
                bab += "Hots: " + this.Hots + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Gametime);
                bab += "Gametime: " + this.Gametime + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Duration);
                bab += "Duration: " + this.Duration + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Leaver);
                bab += "Leaver: " + this.Leaver + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Killsum);
                bab += "Killsum: " + this.Killsum + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Army);
                bab += "Army: " + this.Army + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Income);
                bab += "Income: " + this.Income + " (" + f + "%)" + Environment.NewLine;
                f = FilterRate(this.Std);
                bab += "Std: " + this.Std + " (" + f + "%)" + Environment.NewLine;
                **/

                if (this.FILTERED == 0)
                {
                    FILTERED = Beta + Hots + Gametime + Duration + Leaver + Killsum + Army + Income + Std;
                }
                GAMESf = GAMES;
                double filtered = FilterRate(this.FILTERED);
                GAMESf = GAMES;
                bab = this.FILTERED + " / " + this.GAMES + " replays filtered (" + filtered.ToString() + "%)";
                bab += Environment.NewLine;
                double f = FilterRate(this.Beta);
                //bab += "Beta: " + this.Beta + " (" + f + "%)" + "; ";
                f = FilterRate(this.Hots);
                //bab += "Hots: " + this.Hots + " (" + f + "%)" + "; ";
                f = FilterRate(this.Playercount);
                bab += "Playercount: " + this.Playercount + " (" + f + "%)" + "; ";
                f = FilterRate(this.Gamemodes);
                bab += "Gamemodes: " + this.Gamemodes + " (" + f + "%)" + "; ";
                f = FilterRate(this.Gametime);
                bab += "Gametime: " + this.Gametime + " (" + f + "%)" + "; ";
                f = FilterRate(this.Duration);
                bab += "Duration: " + this.Duration + " (" + f + "%)" + "; ";
                f = FilterRate(this.Leaver);
                bab += "Leaver: " + this.Leaver + " (" + f + "%)" + "; ";
                f = FilterRate(this.Killsum);
                bab += "Killsum: " + this.Killsum + " (" + f + "%)" + "; ";
                f = FilterRate(this.Army);
                bab += "Army: " + this.Army + " (" + f + "%)" + "; ";
                f = FilterRate(this.Income);
                bab += "Income: " + this.Income + " (" + f + "%)" + "; ";
                f = FilterRate(this.Std);
                //bab += "Std: " + this.Std + " (" + f + "%)" + "; ";

            }
            return bab;
        }

        public double FilterRate(int f)
        {
            double i = 0;
            double games = this.GAMESf;
            if (games > 0)
            {
                i = f * 100 / games;
                i = Math.Round(i, 2);
            }
            GAMESf -= f;
            return i;
        }

    }
}
