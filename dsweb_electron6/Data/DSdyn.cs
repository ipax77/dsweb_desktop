using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace dsweb_electron6.Data
{
    public class DSdyn
    {
        public ObservableCollection<CmdrIcon> CmdrIcons { get; set; } = new ObservableCollection<CmdrIcon>();
        public List<CmdrIcon> ModifiedItems { get; set; }
        public string chartdata { get; set; }
        public static int gameid { get; set; } = 0;

        public DSdyn()
        {
            //CmdrIcons.CollectionChanged += CmdrIconsChanged;
            //foreach (string race in DSdata.s_races)
            foreach (string race in DSdata.s_races_cmdr)
            {
                CmdrIcon icon = new CmdrIcon(race, false);
                CmdrIcons.Add(icon);
            }

        }

        public int GetChecked()
        {
            int i = 0;
            foreach (var icon in CmdrIcons)
            {
                i++;
                if (icon.IsChecked == true)
                {
                    return i;
                }
            }
            return i;
        }

        void CmdrIconsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CmdrIcon newItem in e.NewItems)
                {
                    ModifiedItems.Add(newItem);

                    //Add listener for each item on PropertyChanged event
                    newItem.PropertyChanged += this.OnItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CmdrIcon oldItem in e.OldItems)
                {
                    ModifiedItems.Add(oldItem);

                    oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                }
            }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CmdrIcon item = sender as CmdrIcon;
            if (item != null)
                ModifiedItems.Add(item);
        }



    }

    public class DSdyn_filteroptions : INotifyPropertyChanged, ICloneable
    {
        private int Duration_value = 5376;
        private int Leaver_value = 2000;
        private int Army_value = 1500;
        private int Kills_value = 1500;
        private int Income_value = 1500;
        private int PlayerCount_value = 6;
        private bool Player_value = false;
        private string Startdate_value = "2019-01-01";
        private string Enddate_value = DateTime.Now.ToString("yyyy-MM-dd");
        private string Interest_value = String.Empty;
        private string Vs_value = String.Empty;
        private bool Matchup_value = false;
        private bool Filter_value = false;
        private string Mode_value = "Winrate";
        private bool BeginAtZero_value = false;
        private string Build_value = String.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public int Icons { get; set; } = 0;
        public bool OPT { get; set; } = false;
        public bool DOIT { get; set; } = true;
        public Models.dsfilter fil { get; set; } = new Models.dsfilter();
        public ChartJS Chart { get; set; } = new ChartJS();
        public int Total { get; set; } = 0;

        public int Duration
        {
            get { return this.Duration_value; }
            set
            {
                if (value != this.Duration_value)
                {
                    this.Duration_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Army
        {
            get { return this.Army_value; }
            set
            {
                if (value != this.Army_value)
                {
                    this.Army_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Leaver
        {
            get { return this.Leaver_value; }
            set
            {
                if (value != this.Leaver_value)
                {
                    this.Leaver_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Kills
        {
            get { return this.Kills_value; }
            set
            {
                if (value != this.Kills_value)
                {
                    this.Kills_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Income
        {
            get { return this.Income_value; }
            set
            {
                if (value != this.Income_value)
                {
                    this.Income_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int PlayerCount
        {
            get { return this.PlayerCount_value; }
            set
            {
                if (value != this.PlayerCount_value)
                {
                    this.PlayerCount_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool Player
        {
            get { return this.Player_value; }
            set
            {
                if (value != this.Player_value)
                {
                    this.Player_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Startdate
        {
            get { return this.Startdate_value; }
            set
            {
                if (value != this.Startdate_value)
                {
                    this.Startdate_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Enddate
        {
            get { return this.Enddate_value; }
            set
            {
                if (value != this.Enddate_value)
                {
                    this.Enddate_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Interest
        {
            get { return this.Interest_value; }
            set
            {
                if (value != this.Interest_value)
                {
                    this.Interest_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Vs
        {
            get { return this.Vs_value; }
            set
            {
                if (value != this.Vs_value)
                {
                    this.Vs_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool Matchup
        {
            get { return this.Matchup_value; }
            set
            {
                if (value != this.Matchup_value)
                {
                    this.Matchup_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool Filter
        {
            get { return this.Filter_value; }
            set
            {
                if (value != this.Filter_value)
                {
                    this.Filter_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Mode
        {
            get { return this.Mode_value; }
            set
            {
                if (value != this.Mode_value)
                {
                    this.Mode_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool BeginAtZero
        {
            get { return this.BeginAtZero_value; }
            set
            {
                if (value != this.BeginAtZero_value)
                {
                    this.BeginAtZero_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Build
        {
            get { return this.Build_value; }
            set
            {
                if (value != this.Build_value)
                {
                    this.Build_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string GenHash()
        {
            string opthash = "";
            foreach (var prop in this.GetType().GetProperties())
            {
                //Console.WriteLine("{0}={1}", prop.Name, prop.GetValue(this, null));
                if (prop.Name == "BeginAtZero") continue;
                else if (prop.Name == "OPT") continue;
                else if (prop.Name == "Icons") continue;
                else if (prop.Name == "DOIT") continue;
                else if (prop.Name == "fil") continue;
                else if (prop.Name == "Chart") continue;
                else if (prop.Name == "Total") continue;
                else if (prop.Name == "Ordered") continue;

                if (prop.Name == "Enddate" && prop.GetValue(this, null).ToString() == DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    opthash += prop.Name + "LaSt";
                }
                else
                {
                    opthash += prop.Name + prop.GetValue(this, null).ToString();
                }
            }
            MD5 md5 = new MD5CryptoServiceProvider();
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(opthash);
            return System.BitConverter.ToString(md5.ComputeHash(plainTextBytes));
        }
    }

    public class DSdyn_BuildChecked : INotifyPropertyChanged
    {
        private bool IsChecked_value = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool IsChecked
        {
            get { return this.IsChecked_value; }
            set
            {
                if (value != this.IsChecked_value)
                {
                    this.IsChecked_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_BuildChecked() { }
        public DSdyn_BuildChecked(bool ent)
        {
            IsChecked = ent;
        }
    }

    public class DSdyn_buildoptions : INotifyPropertyChanged
    {
        private string BUILD_value = String.Empty;
        private string BUILD_COMPARE_value = String.Empty;
        private string CMDR_value = String.Empty;
        private string CMDR_VS_value = String.Empty;
        private string BREAKPOINT_value = String.Empty;


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DSdyn_buildoptions()
        {

        }

        public string BUILD
        {
            get { return this.BUILD_value; }
            set
            {
                if (value != this.BUILD_value)
                {
                    this.BUILD_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string BUILD_COMPARE
        {
            get { return this.BUILD_COMPARE_value; }
            set
            {
                if (value != this.BUILD_COMPARE_value)
                {
                    this.BUILD_COMPARE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string CMDR
        {
            get { return this.CMDR_value; }
            set
            {
                if (value != this.CMDR_value)
                {
                    this.CMDR_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string CMDR_VS
        {
            get { return this.CMDR_VS_value; }
            set
            {
                if (value != this.CMDR_VS_value)
                {
                    this.CMDR_VS_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string BREAKPOINT
        {
            get { return this.BREAKPOINT_value; }
            set
            {
                if (value != this.BREAKPOINT_value)
                {
                    this.BREAKPOINT_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    public class DSdyn_databaseoptions : INotifyPropertyChanged
    {
        private bool WINNER_value = false;
        private bool DURATION_value = false;
        private bool MAXLEAVER_value = false;
        private bool MAXKILLSUM_value = false;
        private bool MINKILLSUM_value = false;
        private bool MININCOME_value = false;
        private bool MINARMY_value = false;
        private bool REPLAY_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool ID { get; set; } = true;
        public bool Gametime { get; set; } = true;

        public bool WINNER
        {
            get { return this.WINNER_value; }
            set
            {
                if (value != this.WINNER_value)
                {
                    this.WINNER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool DURATION
        {
            get { return this.DURATION_value; }
            set
            {
                if (value != this.DURATION_value)
                {
                    this.DURATION_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool MAXLEAVER
        {
            get { return this.MAXLEAVER_value; }
            set
            {
                if (value != this.MAXLEAVER_value)
                {
                    this.MAXLEAVER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool MAXKILLSUM
        {
            get { return this.MAXKILLSUM_value; }
            set
            {
                if (value != this.MAXKILLSUM_value)
                {
                    this.MAXKILLSUM_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool REPLAY
        {
            get { return this.REPLAY_value; }
            set
            {
                if (value != this.REPLAY_value)
                {
                    this.REPLAY_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_databaseoptions()
        {
            //var ent = this.GetType().GetProperty("REPLAY").GetValue("REPLAY");
            //ent = false;

        }
    }

    public class DSdyn_options : INotifyPropertyChanged
    {
        private string MODE_value = String.Empty;
        private string STARTDATE_value = String.Empty;
        private string ENDDATE_value = String.Empty;
        private string INTEREST_value = String.Empty;
        private int ICONS_value = 0;
        private bool PLAYER_value = false;
        private bool BEGINATZERO_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string MODE
        {
            get { return this.MODE_value; }
            set
            {
                if (value != this.MODE_value)
                {
                    this.MODE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string STARTDATE
        {
            get { return this.STARTDATE_value; }
            set
            {
                if (value != this.STARTDATE_value)
                {
                    this.STARTDATE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string ENDDATE
        {
            get { return this.ENDDATE_value; }
            set
            {
                if (value != this.ENDDATE_value)
                {
                    this.ENDDATE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string INTEREST
        {
            get { return this.INTEREST_value; }
            set
            {
                if (value != this.INTEREST_value)
                {
                    this.INTEREST_value = value;
                    //NotifyPropertyChanged();
                }
            }
        }
        public int ICONS
        {
            get { return this.ICONS_value; }
            set
            {
                if (value != this.ICONS_value)
                {
                    this.ICONS_value = value;
                    //NotifyPropertyChanged();
                }
            }
        }
        public bool PLAYER
        {
            get { return this.PLAYER_value; }
            set
            {
                if (value != this.PLAYER_value)
                {
                    this.PLAYER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool BEGINATZERO
        {
            get { return this.BEGINATZERO_value; }
            set
            {
                if (value != this.BEGINATZERO_value)
                {
                    this.BEGINATZERO_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_options()
        {
            MODE = "Winrate";
            STARTDATE = "0";
            ENDDATE = "0";
        }
    }

    public class CmdrIcon : INotifyPropertyChanged
    {
        private bool IsChecked_value = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public CmdrIcon(string _ID, bool _IsChecked)
        {
            ID = _ID;
            IsChecked = _IsChecked;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string ID { get; set; }
        public bool IsChecked
        {
            get
            {
                return this.IsChecked_value;
            }
            set
            {
                if (value != this.IsChecked_value)
                {
                    this.IsChecked_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

    }
}
