namespace Models
{
    // סוג שאלה.
    // משמש לסינון, בחירה, או שיוך של שאלות לחדר מסוים.
    public class QuestionType
    {
        // מזהה פנימי של סוג השאלה.
        public int QuestionTypeID { get; set; }

        // שם סוג השאלה.
        public string TypeName { get; set; } = "";
    }
}
