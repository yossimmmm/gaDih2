namespace Models
{
    // סוג/קטגוריה של שאלות טריוויה.
    // חדרים יכולים לסנן שאלות לפי הקטגוריה הזאת.
    public class QuestionType
    {
        // המפתח הראשי של הקטגוריה.
        public int QuestionTypeID { get; set; }

        // שם התצוגה של הקטגוריה.
        public string TypeName { get; set; } = "";
    }
}
