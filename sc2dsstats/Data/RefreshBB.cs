using paxgame3.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace paxgame3.Client.Data
{
    public class RefreshBB : INotifyPropertyChanged
    {
        private bool Update_value = false;
        private BBuild BestBuild_value;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public BBuild Bplayer { get; set; }
        public BBuild Bopp { get; set; }
        public BBuild WorstBuild { get; set; }
        public Stats BestStats { get; set; }
        public Stats WorstStats { get; set; }
        public Stats BestStatsOpp { get; set; }
        public Stats WorstStatsOpp { get; set; }
        public int TOTAL = 0;
        public int TOTAL_DONE = 0;

        public BBuild BestBuild
        {
            get { return this.BestBuild_value; }
            set
            {
                if (value != this.BestBuild_value)
                {
                    this.BestBuild_value = value;
                    NotifyPropertyChanged();
                }
            }
        }


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
