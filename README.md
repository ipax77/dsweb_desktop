# dsweb_desktop

# sc2dsstats

sc2dsstats is a dotnet core – blazor - electron app for analyzing your Starcraft 2 Direct Strike Replays. It generates some Graphs showing the win rate, synergy, mvp and damage output of each commander. There is also a matchmaking system built in. 

To install the app just download and install the setup.exe: 
https://github.com/ipax77/dsweb_desktop/releases/download/latest/dsweb_electron6-Setup-latest.exe

![sample graph](/images/dsweb_desktop.png)

# Dependencies / Links
dsmm - Matchmaking Server (https://github.com/ipax77/dsmm)
dsweb - Global stats (https://github.com/ipax77/dsweb)
c# wpf version & REST API server (https://github.com/ipax77/sc2dsstats)


# Acknowledgements
Chart.js (https://github.com/chartjs) used for the radar Chart
s2protocol (https://github.com/Blizzard/s2protocol) used for decoding the replays
trueskill (https://github.com/sublee/trueskill) used for matchmaking
IronPython (https://ironpython.net/) to run s2protocol within C#
And all other packages used but not mentioned here.


# License

Copyright (c) 2019, Philipp Hetzner
Open sourced under the GNU General Public License version 3. See the included LICENSE file for more information.

