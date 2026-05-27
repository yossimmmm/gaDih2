using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DBL
{
    public class QuestionTypeDB
    {
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<List<QuestionType>> GetAllAsync()
        {
            var result = new List<QuestionType>();

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT question_type_id, type_name
FROM question_types
ORDER BY type_name ASC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new QuestionType
                    {
                        QuestionTypeID = reader.GetInt32(0),
                        TypeName = reader.GetString(1)
                    });
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching question types: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching question types: {ex.Message}", ex);
            }
        }
    }
}
