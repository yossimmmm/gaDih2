using System;
using System.Collections.Generic;

namespace Models
{
    public class Question
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; } = "";
        public int QuestionTypeID { get; set; }
        public string Difficulty { get; set; } = "easy";
        public int CreatedBy { get; set; }
        public int TimeLimitSec { get; set; } = 15;
        public DateTime? StartedAt { get; set; }

        public List<QuestionOption> Options { get; set; } = new();
    }
}
