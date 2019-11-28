namespace paxgame3.Client.Models
{
    public class LocalData
    {
        public int GameID { get; set; } = 0;
        public int PlayerID { get; set; } = 0;
        public bool inGame { get; set; } = false;
        public GameHistory Game { get; set; }
        public Player player { get; set; }
    }
}
