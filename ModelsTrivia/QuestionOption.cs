namespace Models
{
    // אפשרות תשובה אחת בתוך שאלה.
    public class QuestionOption
    {
        // מזהה פנימי של האפשרות.
        public int OptionID { get; set; }

        // לאיזו שאלה האפשרות שייכת.
        public int QuestionID { get; set; }

        // הטקסט שהשחקן רואה ולוחץ עליו.
        public string OptionText { get; set; } = "";

        // האם זו התשובה הנכונה.
        public bool IsCorrect { get; set; }
    }
}
