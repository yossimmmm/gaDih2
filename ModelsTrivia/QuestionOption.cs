namespace Models
{
    // אפשרות תשובה אחת לשאלת טריוויה.
    public class QuestionOption
    {
        // המפתח הראשי של האפשרות.
        public int OptionID { get; set; }

        // המפתח הזר שמקשר את האפשרות לשאלה.
        public int QuestionID { get; set; }

        // הטקסט שמוצג כשאלה אפשרית.
        public string OptionText { get; set; } = "";

        // האם זו האפשרות הנכונה.
        public bool IsCorrect { get; set; }
    }
}
