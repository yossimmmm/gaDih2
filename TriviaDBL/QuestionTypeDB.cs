using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DBL
{
    public class QuestionTypeDB
    {
        // מחרוזת חיבור למסד
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<List<QuestionType>> GetAllAsync()
        {
            // אוסף תוצאות להחזרה לשכבת ה-UI
            var result = new List<QuestionType>();

            try
            {
                // פתיחת חיבור למסד
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // שליפת כל סוגי השאלות לפי סדר אלפביתי
                const string sql = @"
SELECT question_type_id, type_name
FROM question_types
ORDER BY type_name ASC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // מיפוי שורת SQL לאובייקט QuestionType
                    result.Add(new QuestionType
                    {
                        QuestionTypeID = reader.GetInt32(0),
                        TypeName = reader.GetString(1)
                    });
                }

                // החזרת רשימת קטגוריות מלאה
                return result;
            }
            catch (MySqlException ex)
            {
                // עטיפת שגיאת מסד להודעה ברורה לשכבות עליונות
                throw new Exception($"Database error while fetching question types: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // טיפול שגיאה כללית
                throw new Exception($"Error fetching question types: {ex.Message}", ex);
            }
        }
    }
}
