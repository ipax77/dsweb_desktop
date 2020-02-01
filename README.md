# dsweb_desktop

# sc2dsstats

sc2dsstats is a dotnet core – blazor - electron app for analyzing your Starcraft 2 Direct Strike Replays. It generates some Graphs showing the win rate, synergy, mvp and damage output of each commander. There is also a matchmaking system built in. 

To install the app just download and install the setup.exe: 
https://github.com/ipax77/dsweb_desktop/releases/latest/

![sample graph](/images/dsweb_desktop.png)

# Dependencies
* s2decode - Decoding Starcraft 2 Direct Strike Replays (https://github.com/ipax77/s2decode)
* pax.s2decode - IronPython Parser using s2decode https://git.scytec.de/pax77/paxgame
* paxgamelib - Basic simulation of game mechanics https://git.scytec.de/pax77/paxgame

# Acknowledgements
* Chart.js (https://github.com/chartjs) used for the radar Chart
* s2protocol (https://github.com/Blizzard/s2protocol) used for decoding the replays
* trueskill (https://github.com/sublee/trueskill) used for matchmaking
* IronPython (https://ironpython.net/) to run s2protocol within C#
And all other packages used but not mentioned here.


# License

Copyright (c) 2019, Philipp Hetzner
Open sourced under the GNU General Public License version 3. See the included LICENSE file for more information.

