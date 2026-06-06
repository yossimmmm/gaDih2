using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DBL
{
    public static class SeedData
    {
        // מחרוזת חיבור למסד
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        private sealed class SeedQuestion
        {
            // טקסט השאלה
            public string Text { get; }
            // סוג השאלה (קטגוריה)
            public int TypeId { get; }
            // רמת קושי
            public string Difficulty { get; }
            // אופציות תשובה
            public string[] Options { get; }
            // אינדקס התשובה הנכונה
            public int CorrectIndex { get; }

            public SeedQuestion(string text, int typeId, string difficulty, string[] options, int correctIndex)
            {
                Text = text;
                TypeId = typeId;
                Difficulty = difficulty;
                Options = options;
                CorrectIndex = correctIndex;
            }
        }

        // #seeddata #bootstrap #schema
        public static async Task EnsureSeedQuestionsAsync()
        {
            // פתיחת חיבור וטרנזקציה עבור כל פעולות bootstrap
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // שלב 1: הבטחת מבנה סכימה עדכני
                // הבטחת עמודות/טבלאות נדרשות לסכימה הנוכחית
                await EnsureRoomsIsPublicColumnAsync(conn, (MySqlTransaction)tx);
                await EnsureRoomsQuestionTypeColumnAsync(conn, (MySqlTransaction)tx);
                await EnsureRoomsLastSeenColumnAsync(conn, (MySqlTransaction)tx);
                await EnsureRoomPlayersLastSeenColumnAsync(conn, (MySqlTransaction)tx);
                await EnsureRoomQuestionsStartedAtColumnAsync(conn, (MySqlTransaction)tx);
                await EnsureGameResultsTableAsync(conn, (MySqlTransaction)tx);
                await EnsureUserSessionsTableAsync(conn, (MySqlTransaction)tx);
                await EnsurePasswordResetTokensTableAsync(conn, (MySqlTransaction)tx);
                await EnsureUsersRoleColumnAsync(conn, (MySqlTransaction)tx);
                // שלב 2: הכנסת נתוני בסיס למערכת
                // הבטחת נתוני קטגוריות בסיסיים
                // #seeddata #question-types
                await EnsureQuestionTypesAsync(conn, (MySqlTransaction)tx);
                // יצירת משתמש seed אם אין משתמשים
                // #seeddata #seed-user
                var userId = await GetOrCreateSeedUserAsync(conn, (MySqlTransaction)tx);
                // הזרעת שאלות התחלתיות למערכת
                // #seeddata #questions
                await InsertQuestionsAsync(conn, (MySqlTransaction)tx, userId);

                // אישור סופי לכל פעולות האתחול
                await tx.CommitAsync();
            }
            catch
            {
                // מבצעים rollback במקרה של כשל בכל שלב
                await tx.RollbackAsync();
                throw;
            }
        }

        private static async Task<int> GetQuestionsCountAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // ספירת שאלות קיימות במסד
            const string sql = "SELECT COUNT(*) FROM questions;";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        private static async Task EnsureQuestionTypesAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // הכנסת קטגוריות ברירת מחדל (INSERT IGNORE למניעת כפילויות)
            const string sql = @"
INSERT IGNORE INTO question_types (question_type_id, type_name) VALUES
(1, 'General Knowledge'),
(2, 'Science'),
(3, 'History'),
(4, 'Geography'),
(5, 'Entertainment'),
(6, 'Sports'),
(7, 'Technology'),
(8, 'Math');";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomsIsPublicColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה אם קיימת עמודת is_public בטבלת חדרים
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'rooms'
  AND COLUMN_NAME = 'is_public';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            // יצירת העמודה אם אינה קיימת
            const string alterSql = @"
ALTER TABLE rooms
ADD COLUMN is_public TINYINT(1) NOT NULL DEFAULT 0;";
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomsQuestionTypeColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה/הוספה של question_type_id בטבלת חדרים
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'rooms'
  AND COLUMN_NAME = 'question_type_id';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            const string alterSql = @"
ALTER TABLE rooms
ADD COLUMN question_type_id INT NULL;";
            // העמודה נשמרת nullable כי חדר יכול לעבוד בלי קטגוריה קשיחה
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomQuestionsStartedAtColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה/הוספה של started_at בטבלת room_questions
            // העמודה מאפשרת לעקוב מתי שאלה התחילה לצורך טיימר בצד שרת
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'room_questions'
  AND COLUMN_NAME = 'started_at';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            const string alterSql = @"
ALTER TABLE room_questions
ADD COLUMN started_at DATETIME NULL;";
            // ערך NULL אומר שהשאלה טרם התחילה
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomsLastSeenColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה/הוספה של last_seen בטבלת rooms
            // משמש לניקוי חדרים לא פעילים ולתחזוקת רשימה ציבורית
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'rooms'
  AND COLUMN_NAME = 'last_seen';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            const string alterSql = @"
ALTER TABLE rooms
ADD COLUMN last_seen DATETIME NULL;";
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomPlayersLastSeenColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה/הוספה של last_seen בטבלת room_players
            // משמש לזיהוי שחקנים שהתנתקו/נעלמו מהחדר
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'room_players'
  AND COLUMN_NAME = 'last_seen';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            const string alterSql = @"
ALTER TABLE room_players
ADD COLUMN last_seen DATETIME NULL;";
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureGameResultsTableAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // יצירת טבלת תוצאות משחק אם אינה קיימת
            // הטבלה שומרת תוצאה לכל משתמש בכל חדר לצורכי סטטיסטיקה
            const string sql = @"
CREATE TABLE IF NOT EXISTS game_results (
  game_result_id INT NOT NULL AUTO_INCREMENT,
  room_id INT NOT NULL,
  user_id INT NOT NULL,
  correct_count INT NOT NULL,
  answered_count INT NOT NULL,
  is_winner TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (game_result_id),
  UNIQUE KEY uq_game_results_room_user (room_id, user_id),
  KEY ix_game_results_user (user_id),
  CONSTRAINT fk_game_results_room FOREIGN KEY (room_id) REFERENCES rooms (room_id) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_game_results_user FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE ON UPDATE CASCADE
);";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureUserSessionsTableAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // יצירת טבלת סשנים אם אינה קיימת
            // כל סשן מקושר למשתמש וכולל טוקן ייחודי ותוקף
            const string sql = @"
CREATE TABLE IF NOT EXISTS user_sessions (
  session_id INT NOT NULL AUTO_INCREMENT,
  session_token VARCHAR(64) NOT NULL,
  user_id INT NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at DATETIME NOT NULL,
  last_seen DATETIME NULL,
  PRIMARY KEY (session_id),
  UNIQUE KEY uq_user_sessions_token (session_token),
  KEY ix_user_sessions_user (user_id),
  CONSTRAINT fk_user_sessions_user FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE ON UPDATE CASCADE
);";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsurePasswordResetTokensTableAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // טבלת טוקנים לאיפוס סיסמה (שומרים hash ולא את הטוקן הגולמי)
            // כך גם אם המסד דולף, אי אפשר להשתמש ישירות בטוקן המקורי
            const string sql = @"
CREATE TABLE IF NOT EXISTS password_reset_tokens (
  reset_id INT NOT NULL AUTO_INCREMENT,
  user_id INT NOT NULL,
  token_hash CHAR(64) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at DATETIME NOT NULL,
  used_at DATETIME NULL,
  PRIMARY KEY (reset_id),
  UNIQUE KEY uq_password_reset_tokens_hash (token_hash),
  KEY ix_password_reset_tokens_user (user_id),
  CONSTRAINT fk_password_reset_tokens_user FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE ON UPDATE CASCADE
);";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task EnsureUsersRoleColumnAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // בדיקה/הוספה של עמודת role בטבלת users
            // העמודה נדרשת למנגנון הרשאות User/Admin
            const string checkSql = @"
SELECT COUNT(*)
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'users'
  AND COLUMN_NAME = 'role';";

            await using (var checkCmd = new MySqlCommand(checkSql, conn, tx))
            {
                var obj = await checkCmd.ExecuteScalarAsync();
                if (Convert.ToInt32(obj) > 0)
                    return;
            }

            const string alterSql = @"
ALTER TABLE users
ADD COLUMN role VARCHAR(20) NOT NULL DEFAULT 'User';";
            await using var alterCmd = new MySqlCommand(alterSql, conn, tx);
            await alterCmd.ExecuteNonQueryAsync();
        }

        private static async Task<int> GetOrCreateSeedUserAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            // ניסיון לקחת משתמש ראשון קיים
            const string getSql = "SELECT user_id FROM users ORDER BY user_id LIMIT 1;";
            await using (var getCmd = new MySqlCommand(getSql, conn, tx))
            {
                var obj = await getCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    return Convert.ToInt32(obj);
            }

            // אם אין משתמשים - יוצרים משתמש admin seed
            var passwordHash = PasswordHelper.Hash("admin");
            // משתמש seed משמש בעלות/יצירה ראשונית של שאלות
            const string insertSql = @"
INSERT INTO users (username, full_name, email, password_hash, role)
VALUES ('admin', 'Admin', 'admin@example.com', @hash, 'Admin');";
            await using (var insertCmd = new MySqlCommand(insertSql, conn, tx))
            {
                insertCmd.Parameters.AddWithValue("@hash", passwordHash);
                await insertCmd.ExecuteNonQueryAsync();
                return (int)insertCmd.LastInsertedId;
            }
        }

        private static async Task InsertQuestionsAsync(MySqlConnection conn, MySqlTransaction tx, int userId)
        {
            // רשימת שאלות ברירת מחדל למערכת
            // כל איבר כולל טקסט, קטגוריה, קושי, אופציות, ואינדקס נכון
            // הרשימה בנויה לפי קטגוריות כדי לאפשר משחק מאוזן ועשיר
            var questions = new List<SeedQuestion>
            {
                new SeedQuestion(
                    "What is the capital of France?",
                    1,
                    "easy",
                    new[] { "Paris", "London", "Berlin", "Madrid" },
                    0),
                new SeedQuestion(
                    "Who painted the Mona Lisa?",
                    1,
                    "easy",
                    new[] { "Leonardo da Vinci", "Pablo Picasso", "Vincent van Gogh", "Michelangelo" },
                    0),
                new SeedQuestion(
                    "What is the largest planet in our solar system?",
                    2,
                    "easy",
                    new[] { "Jupiter", "Saturn", "Neptune", "Earth" },
                    0),
                new SeedQuestion(
                    "In which year did World War II end?",
                    3,
                    "medium",
                    new[] { "1945", "1944", "1946", "1943" },
                    0),
                new SeedQuestion(
                    "What is the smallest country in the world?",
                    4,
                    "easy",
                    new[] { "Vatican City", "Monaco", "San Marino", "Liechtenstein" },
                    0),
                new SeedQuestion(
                    "Who wrote the Harry Potter series?",
                    5,
                    "easy",
                    new[] { "J.K. Rowling", "Stephen King", "J.R.R. Tolkien", "George R.R. Martin" },
                    0),
                new SeedQuestion(
                    "How many sides does a hexagon have?",
                    8,
                    "easy",
                    new[] { "6", "5", "7", "8" },
                    0),
                new SeedQuestion(
                    "What is the chemical symbol for gold?",
                    2,
                    "easy",
                    new[] { "Au", "Go", "Gd", "Ag" },
                    0),
                new SeedQuestion(
                    "Which ocean is the largest?",
                    4,
                    "easy",
                    new[] { "Pacific Ocean", "Atlantic Ocean", "Indian Ocean", "Arctic Ocean" },
                    0),
                new SeedQuestion(
                    "Who invented the telephone?",
                    7,
                    "medium",
                    new[] { "Alexander Graham Bell", "Thomas Edison", "Nikola Tesla", "Guglielmo Marconi" },
                    0),
                new SeedQuestion(
                    "What is the speed of light in vacuum (approximately)?",
                    2,
                    "hard",
                    new[] { "300,000 km/s", "150,000 km/s", "450,000 km/s", "200,000 km/s" },
                    0),
                new SeedQuestion(
                    "In which country would you find Mount Kilimanjaro?",
                    4,
                    "medium",
                    new[] { "Tanzania", "Kenya", "Uganda", "Rwanda" },
                    0),
                new SeedQuestion(
                    "Who was the first person to walk on the moon?",
                    3,
                    "easy",
                    new[] { "Neil Armstrong", "Buzz Aldrin", "Yuri Gagarin", "John Glenn" },
                    0),
                new SeedQuestion(
                    "What is the largest mammal in the world?",
                    2,
                    "easy",
                    new[] { "Blue Whale", "Elephant", "Giraffe", "Polar Bear" },
                    0),
                new SeedQuestion(
                    "What does CPU stand for?",
                    7,
                    "easy",
                    new[] { "Central Processing Unit", "Computer Personal Unit", "Central Program Utility", "Computer Processing Utility" },
                    0),
                new SeedQuestion(
                    "How many bones are in the human body (adult)?",
                    2,
                    "medium",
                    new[] { "206", "200", "212", "220" },
                    0),
                new SeedQuestion(
                    "What is the hardest natural substance on Earth?",
                    2,
                    "medium",
                    new[] { "Diamond", "Gold", "Platinum", "Quartz" },
                    0),
                new SeedQuestion(
                    "Which planet is known as the Red Planet?",
                    2,
                    "easy",
                    new[] { "Mars", "Venus", "Jupiter", "Saturn" },
                    0),
                new SeedQuestion(
                    "Who composed the Four Seasons?",
                    5,
                    "medium",
                    new[] { "Antonio Vivaldi", "Wolfgang Amadeus Mozart", "Ludwig van Beethoven", "Johann Sebastian Bach" },
                    0),
                new SeedQuestion(
                    "What is the square root of 144?",
                    8,
                    "easy",
                    new[] { "12", "11", "13", "14" },
                    0),
                // ידע כללי נוסף
                new SeedQuestion(
                    "Which language has the most native speakers worldwide?",
                    1,
                    "medium",
                    new[] { "Mandarin Chinese", "Spanish", "English", "Hindi" },
                    0),
                new SeedQuestion(
                    "Which planet is closest to the Sun?",
                    1,
                    "easy",
                    new[] { "Mercury", "Venus", "Earth", "Mars" },
                    0),
                // מדע נוסף
                new SeedQuestion(
                    "What gas do plants absorb from the atmosphere?",
                    2,
                    "easy",
                    new[] { "Carbon dioxide", "Oxygen", "Nitrogen", "Hydrogen" },
                    0),
                new SeedQuestion(
                    "What part of the cell contains genetic material?",
                    2,
                    "medium",
                    new[] { "Nucleus", "Mitochondria", "Ribosome", "Golgi apparatus" },
                    0),
                // היסטוריה נוספת
                new SeedQuestion(
                    "Who was the first President of the United States?",
                    3,
                    "easy",
                    new[] { "George Washington", "Thomas Jefferson", "Abraham Lincoln", "John Adams" },
                    0),
                new SeedQuestion(
                    "The ancient city of Rome was built on how many hills?",
                    3,
                    "medium",
                    new[] { "Seven", "Five", "Nine", "Three" },
                    0),
                // גאוגרפיה נוספת
                new SeedQuestion(
                    "Which continent is the Sahara Desert located in?",
                    4,
                    "easy",
                    new[] { "Africa", "Asia", "Australia", "South America" },
                    0),
                new SeedQuestion(
                    "Which river is the longest in the world?",
                    4,
                    "medium",
                    new[] { "Nile", "Amazon", "Yangtze", "Mississippi" },
                    0),
                // בידור נוסף
                new SeedQuestion(
                    "Which movie features the quote, \"May the Force be with you\"?",
                    5,
                    "easy",
                    new[] { "Star Wars", "Star Trek", "The Matrix", "Blade Runner" },
                    0),
                new SeedQuestion(
                    "Who is known as the \"King of Pop\"?",
                    5,
                    "easy",
                    new[] { "Michael Jackson", "Elvis Presley", "Prince", "Freddie Mercury" },
                    0),
                // ספורט נוסף
                new SeedQuestion(
                    "How many players are on a soccer team on the field?",
                    6,
                    "easy",
                    new[] { "11", "9", "10", "12" },
                    0),
                new SeedQuestion(
                    "Which sport uses a shuttlecock?",
                    6,
                    "easy",
                    new[] { "Badminton", "Tennis", "Squash", "Table Tennis" },
                    0),
                // טכנולוגיה נוספת
                new SeedQuestion(
                    "What does \"HTTP\" stand for?",
                    7,
                    "medium",
                    new[] { "HyperText Transfer Protocol", "High Transfer Text Protocol", "Hyper Transfer Text Process", "HyperText Transmission Process" },
                    0),
                new SeedQuestion(
                    "Which company created the Android operating system?",
                    7,
                    "medium",
                    new[] { "Google", "Apple", "Microsoft", "IBM" },
                    0),
                // מתמטיקה נוספת
                new SeedQuestion(
                    "What is 9 x 8?",
                    8,
                    "easy",
                    new[] { "72", "81", "64", "79" },
                    0),
                new SeedQuestion(
                    "What is the value of pi (to two decimal places)?",
                    8,
                    "easy",
                    new[] { "3.14", "3.41", "3.04", "3.24" },
                    0),
                // עוד ידע כללי
                new SeedQuestion(
                    "How many continents are there on Earth?",
                    1,
                    "easy",
                    new[] { "7", "6", "8", "5" },
                    0),
                new SeedQuestion(
                    "What is the currency of Japan?",
                    1,
                    "easy",
                    new[] { "Yen", "Won", "Dollar", "Euro" },
                    0),
                new SeedQuestion(
                    "Which metal is liquid at room temperature?",
                    1,
                    "medium",
                    new[] { "Mercury", "Iron", "Copper", "Aluminum" },
                    0),
                // עוד מדע
                new SeedQuestion(
                    "What is H2O commonly known as?",
                    2,
                    "easy",
                    new[] { "Water", "Hydrogen", "Oxygen", "Salt" },
                    0),
                new SeedQuestion(
                    "Which gas makes up most of Earth's atmosphere?",
                    2,
                    "medium",
                    new[] { "Nitrogen", "Oxygen", "Carbon dioxide", "Argon" },
                    0),
                new SeedQuestion(
                    "What force keeps planets in orbit around the Sun?",
                    2,
                    "medium",
                    new[] { "Gravity", "Magnetism", "Friction", "Radiation pressure" },
                    0),
                // עוד היסטוריה
                new SeedQuestion(
                    "In which year did the Berlin Wall fall?",
                    3,
                    "medium",
                    new[] { "1989", "1991", "1979", "1985" },
                    0),
                new SeedQuestion(
                    "Who was known as the Maid of Orleans?",
                    3,
                    "medium",
                    new[] { "Joan of Arc", "Marie Antoinette", "Catherine the Great", "Florence Nightingale" },
                    0),
                new SeedQuestion(
                    "Which empire was ruled by Julius Caesar?",
                    3,
                    "easy",
                    new[] { "Roman Empire", "Ottoman Empire", "Mongol Empire", "British Empire" },
                    0),
                // עוד גאוגרפיה
                new SeedQuestion(
                    "What is the capital of Canada?",
                    4,
                    "easy",
                    new[] { "Ottawa", "Toronto", "Vancouver", "Montreal" },
                    0),
                new SeedQuestion(
                    "Which country has the city of Sydney?",
                    4,
                    "easy",
                    new[] { "Australia", "Canada", "United States", "United Kingdom" },
                    0),
                new SeedQuestion(
                    "Which US state is the largest by area?",
                    4,
                    "medium",
                    new[] { "Alaska", "Texas", "California", "Montana" },
                    0),
                // עוד בידור
                new SeedQuestion(
                    "Which TV series features the character Walter White?",
                    5,
                    "easy",
                    new[] { "Breaking Bad", "The Office", "Friends", "Lost" },
                    0),
                new SeedQuestion(
                    "Which Disney movie includes the song 'Let It Go'?",
                    5,
                    "easy",
                    new[] { "Frozen", "Moana", "Tangled", "Cinderella" },
                    0),
                new SeedQuestion(
                    "Who directed the movie 'Jurassic Park'?",
                    5,
                    "medium",
                    new[] { "Steven Spielberg", "James Cameron", "Christopher Nolan", "Peter Jackson" },
                    0),
                // עוד ספורט
                new SeedQuestion(
                    "How many points is a touchdown worth in American football (before extra point)?",
                    6,
                    "easy",
                    new[] { "6", "7", "3", "2" },
                    0),
                new SeedQuestion(
                    "In tennis, what is the term for zero?",
                    6,
                    "easy",
                    new[] { "Love", "Nil", "Zero", "Blank" },
                    0),
                new SeedQuestion(
                    "Which country won the FIFA World Cup in 2018?",
                    6,
                    "medium",
                    new[] { "France", "Croatia", "Germany", "Brazil" },
                    0),
                // עוד טכנולוגיה
                new SeedQuestion(
                    "What does GPU stand for?",
                    7,
                    "easy",
                    new[] { "Graphics Processing Unit", "General Processing Unit", "Graphical Performance Unit", "Global Processing Utility" },
                    0),
                new SeedQuestion(
                    "Which company makes the iPhone?",
                    7,
                    "easy",
                    new[] { "Apple", "Samsung", "Google", "Microsoft" },
                    0),
                new SeedQuestion(
                    "Binary uses which two digits?",
                    7,
                    "easy",
                    new[] { "0 and 1", "1 and 2", "2 and 3", "0 and 2" },
                    0),
                // עוד מתמטיקה
                new SeedQuestion(
                    "What is 7 squared?",
                    8,
                    "easy",
                    new[] { "49", "42", "36", "56" },
                    0),
                new SeedQuestion(
                    "What is 15 percent of 200?",
                    8,
                    "medium",
                    new[] { "30", "20", "25", "35" },
                    0),
                new SeedQuestion(
                    "What is the value of 2 + 2 * 3?",
                    8,
                    "easy",
                    new[] { "8", "12", "10", "6" },
                    0)
            };

            const string qSql = @"
INSERT INTO questions (question_text, question_type_id, difficulty, created_by)
VALUES (@text, @type, @difficulty, @created_by);";

            const string optSql = @"
INSERT INTO question_options (question_id, option_text, is_correct)
VALUES (@qid, @text, @is_correct);";

            foreach (var q in questions)
            {
                // אם שאלה כבר קיימת לא מכניסים שוב
                if (await QuestionExistsAsync(conn, tx, q))
                    continue;

                // הוספת שאלה חדשה
                await using var qCmd = new MySqlCommand(qSql, conn, tx);
                qCmd.Parameters.AddWithValue("@text", q.Text);
                qCmd.Parameters.AddWithValue("@type", q.TypeId);
                qCmd.Parameters.AddWithValue("@difficulty", q.Difficulty);
                qCmd.Parameters.AddWithValue("@created_by", userId);
                await qCmd.ExecuteNonQueryAsync();
                var qid = (int)qCmd.LastInsertedId;

                // הוספת אופציות תשובה לשאלה החדשה
                for (var i = 0; i < q.Options.Length; i++)
                {
                    // אופציה מסומנת נכונה לפי CorrectIndex
                    await using var optCmd = new MySqlCommand(optSql, conn, tx);
                    optCmd.Parameters.AddWithValue("@qid", qid);
                    optCmd.Parameters.AddWithValue("@text", q.Options[i]);
                    optCmd.Parameters.AddWithValue("@is_correct", i == q.CorrectIndex ? 1 : 0);
                    await optCmd.ExecuteNonQueryAsync();
                }
            }

            // בסיום הלולאה, כל השאלות החדשות נשמרות בתוך אותה טרנזקציה
        }

        private static async Task<bool> QuestionExistsAsync(MySqlConnection conn, MySqlTransaction tx, SeedQuestion q)
        {
            // בדיקה האם שאלה כבר קיימת לפי טקסט + קטגוריה
            // כך נמנעת כפילות ב-seed בהרצות חוזרות
            // הפעולה מחזירה true אם נמצאה לפחות התאמה אחת
            const string existsSql = @"
SELECT COUNT(*)
FROM questions
WHERE question_text = @text AND question_type_id = @type;";

            await using var cmd = new MySqlCommand(existsSql, conn, tx);
            cmd.Parameters.AddWithValue("@text", q.Text);
            cmd.Parameters.AddWithValue("@type", q.TypeId);
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj) > 0;
        }
    }
}



