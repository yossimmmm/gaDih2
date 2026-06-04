using System;
using System.Collections.Generic;

namespace Models
{
    // מודל של שאלה בטריוויה.
    // החדר מקבל רשימת שאלות, וה-API מעביר אותה ל-MAUI כמבנה JSON.
    public class Question
    {
        // מזהה השאלה במסד הנתונים.
        public int QuestionID { get; set; }

        // הטקסט של השאלה עצמה.
        public string QuestionText { get; set; } = "";

        // מזהה סוג השאלה.
        public int QuestionTypeID { get; set; }

        // רמת קושי כמו easy / medium / hard.
        public string Difficulty { get; set; } = "easy";

        // מי יצר את השאלה.
        public int CreatedBy { get; set; }

        // כמה שניות מותר לענות על השאלה.
        public int TimeLimitSec { get; set; } = 15;

        // מתי השאלה התחילה לרוץ.
        public DateTime? StartedAt { get; set; }

        // אפשרויות התשובה לשאלה.
        public List<QuestionOption> Options { get; set; } = new();
    }
}
