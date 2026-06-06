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

        // אובייקטים שהמחלקות היורשות משתמשות בהם כדי לעבוד מול ה-DB.
        // conn מחזיק חיבור פעיל, cmd מחזיק את פקודת ה-SQL, ו-reader קורא את התוצאות.
        protected MySqlConnection? conn;
        protected MySqlCommand? cmd;
        protected MySqlDataReader? reader;
        // #db #connection #command #reader
        // דפוס השימוש כאן הוא ברמת SQL גולמית: חיבור, פקודה, וקורא תוצאות.
    }
}
