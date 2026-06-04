using System;
using System.Collections.Generic;

namespace Models
{
    // מודל של שאלה אחת במשחק.
    // ה־API מחזיר את האובייקט הזה יחד עם אפשרויות התשובה שלו.
    public class Question
    {
        // המפתח הראשי של השאלה.
        public int QuestionID { get; set; }

        // הטקסט שהשחקן רואה במסך.
        public string QuestionText { get; set; } = "";

        // מזהה הקטגוריה של השאלה.
        public int QuestionTypeID { get; set; }

        // רמת הקושי של השאלה, למשל easy / medium / hard.
        public string Difficulty { get; set; } = "easy";

        // מזהה המשתמש שיצר את השאלה.
        public int CreatedBy { get; set; }

        // כמה זמן מותר לענות על השאלה, בשניות.
        public int TimeLimitSec { get; set; } = 15;

        // זמן ההתחלה של השאלה, אם היא כבר הופעלה.
        public DateTime? StartedAt { get; set; }

        // רשימת אפשרויות התשובה שמוצגות ב־UI.
        public List<QuestionOption> Options { get; set; } = new();
    }
}
