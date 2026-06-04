using MySql.Data.MySqlClient;

namespace DBL
{
    // מחלקת בסיס לעזרי הגישה למסד.
    // מחלקות יורשות משתמשות כאן בטיפוסי החיבור, הפקודה והקורא.
    public class DB
    {
        // מחרוזת חיבור משותפת למסד המקומי.
        // באפליקציה אמיתית עדיף להביא את זה מהקונפיגורציה ולא לקוד מקור.
        protected const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        // אובייקטים לשימוש מחלקות יורשות.
        protected MySqlConnection? conn;
        protected MySqlCommand? cmd;
        protected MySqlDataReader? reader;
    }
}
