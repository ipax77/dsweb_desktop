﻿using paxgame3.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using pax.s2decode.Models;

namespace paxgame3.Client.Data
{
    public class RefreshPl : INotifyPropertyChanged
    {
        private bool Update_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Dictionary<int, Player> Players { get; set; } = new Dictionary<int, Player>();
        public Dictionary<int, dsplayer> dsPlayers { get; set; } = new Dictionary<int, dsplayer>();

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