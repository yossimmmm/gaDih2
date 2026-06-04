namespace Models
{
    // שורת שחקן מצטיין לרשימת הדירוג הגלובלית.
    public class TopPlayerRow : PlayerStatsBase
    {
        // שם המשתמש שמופיע בטבלת הדירוג.
        public string Username { get; set; } = "";

        // מספר המשחקים הכולל ששחקן שיחק.
        public int GamesPlayed { get; set; }

        // מספר המשחקים ששחקן ניצח.
        public int Wins { get; set; }
    }
}
