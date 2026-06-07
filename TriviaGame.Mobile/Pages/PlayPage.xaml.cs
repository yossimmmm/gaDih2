using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף המשחק עצמו.
// משתמש ב-roomCode וב-RoomPlayerID שנשמרו ב-MobileSessionState אחרי יצירת/הצטרפות לחדר.
public partial class PlayPage : ContentPage
{
    // השירות שדרכו הדף מתחיל משחק, טוען שאלה, שולח תשובה וטוען scoreboard.
    private readonly TriviaApiClient api;

    // מכיל את המשתמש, החדר, RoomPlayer, השאלה והבחירה הנוכחית.
    private readonly MobileSessionState session;

    public PlayPage()
    {
        // טוען את PlayPage.xaml ויוצר את כל הפקדים בעלי x:Name.
        InitializeComponent();

        // אותם services משותפים שהוגדרו ב-MauiProgram.
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();

        // בתחילת הדרך אין אפשרויות תשובה, לכן מציגים אוסף ריק ולא null.
        OptionsView.ItemsSource = Array.Empty<QuestionOptionRow>();
    }

    protected override void OnAppearing()
    {
        // מופעל בכל פעם שנכנסים למסך, כולל אחרי בחירת חדר ב-RoomsPage.
        base.OnAppearing();

        // קורא את מצב החדר והשאלה מה-session ומעתיק אותו ל-UI.
        UpdateRoomLabel();
        RenderQuestion();
    }

    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        // מרכז loading וחריגות לכל פעולות המשחק.
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Status: {actionName} failed - {ex.Message}";
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnStartGameClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("start game", async () =>
        {
            if (!EnsureCanUseRoom())
                return;

            // ברירת המחדל היא 10 שאלות.
            var count = 10;

            // TryParse מחזיר false במקום לזרוק חריגה אם המשתמש כתב טקסט שאינו מספר.
            // _ אומר שאין צורך לשמור את ערך ה-bool שהפונקציה מחזירה.
            _ = int.TryParse(QuestionCountEntry.Text, out count);

            // גם מספר שלילי או אפס מוחלף בברירת המחדל.
            if (count <= 0)
                count = 10;

            // #start-game #game #play #api-fetch
            // StartGame שולח userId של ה-host, roomCode וכמות שאלות.
            // השרת בוחר שאלות ושומר אותן ל-room_questions.
            var result = await api.StartGameAsync(
                // ! אומר לקומפיילר שכבר בדקנו שהערך אינו null בתוך EnsureCanUseRoom.
                session.CurrentUser!.UserId,
                session.CurrentRoom!.RoomCode,
                count);

            if (!result.Success || result.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: start failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            StatusLabel.Text = "Status: game started.";
            // אחרי שהשרת יצר room_questions, טוענים מיד את השאלה הראשונה.
            await LoadCurrentQuestionAsync();
        });
    }

    private async void OnLoadQuestionClicked(object? sender, EventArgs e)
    {
        // method group: מעבירים את הפונקציה עצמה ל-RunUiActionAsync.
        await RunUiActionAsync("load question", LoadCurrentQuestionAsync);
    }

    private async Task LoadCurrentQuestionAsync()
    {
        if (!EnsureCanUseRoom())
            return;

        // #question #current-question #game #play #api-fetch
        // מבקשים מהשרת את השאלה הפעילה. אם אין עוד שאלות, השרת מחזיר Finished=true.
        // roomCode מזהה את המשחק שאת השאלה שלו רוצים לקבל.
        var result = await api.GetCurrentQuestionAsync(session.CurrentRoom!.RoomCode);
        if (!result.Success || result.Data is null)
        {
            StatusLabel.Text = $"Status: question failed - {result.Message}";
            return;
        }

        if (result.Data.Finished)
        {
            // אין עוד שאלה, לכן מוחקים את השאלה והבחירה הישנות מה-session.
            session.CurrentQuestion = null;
            session.SelectedOption = null;

            // מרעננים את הפקדים כך שלא תישאר שאלה ישנה על המסך.
            RenderQuestion();
            StatusLabel.Text = "Status: game finished. Load scoreboard.";
            return;
        }

        // שומרים את השאלה המלאה, כולל Options, כדי ש-Submit יוכל לקרוא QuestionID.
        session.CurrentQuestion = result.Data.Question;

        // כל שאלה חדשה מתחילה בלי תשובה מסומנת.
        session.SelectedOption = null;
        RenderQuestion();
        StatusLabel.Text = "Status: question loaded.";
    }

    private void OnOptionSelected(object? sender, SelectionChangedEventArgs e)
    {
        // #answer #question
        // בחירת תשובה רק שומרת את האופציה בזיכרון.
        // היא לא נשמרת ב-DB עד לחיצה על Submit Answer.
        // as מחזיר QuestionOptionRow אם הפריט הוא מהסוג הנכון, או null אם אין בחירה.
        session.SelectedOption = e.CurrentSelection.FirstOrDefault() as QuestionOptionRow;
        StatusLabel.Text = session.SelectedOption is null
            ? "Status: no option selected."
            : $"Status: selected option {session.SelectedOption.OptionID}.";
    }

    private async void OnSubmitAnswerClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("submit answer", async () =>
        {
            if (!EnsureCanUseRoom())
                return;

            // #answer-validation #submit-answer-validation #validation
            if (session.CurrentQuestion is null || session.SelectedOption is null)
            {
                StatusLabel.Text = "Status: load a question and select an answer first.";
                return;
            }

            // #submit-answer #answer #game #play #api-fetch
            // שולחים roomCode, RoomPlayerID, QuestionID ו-OptionID.
            // השרת שומר את התשובה ב-player_answers ומחזיר הודעה.
            var result = await api.SubmitAnswerAsync(
                // מזהה החדר קובע לאיזה משחק שייכת התשובה.
                session.CurrentRoom!.RoomCode,

                // RoomPlayerID קובע איזה שחקן בתוך החדר ענה.
                session.CurrentPlayer!.RoomPlayerID,

                // QuestionID קובע על איזו שאלה ענו.
                session.CurrentQuestion.QuestionID,

                // OptionID קובע איזו תשובה נבחרה.
                session.SelectedOption.OptionID);

            if (!result.Success || result.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: answer failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            StatusLabel.Text = $"Status: {result.Data.Message}";
            // אחרי שמירת התשובה מבקשים מהשרת את השאלה הבאה.
            // אם אין שאלה נוספת, השרת יחזיר Finished=true.
            await LoadCurrentQuestionAsync();
        });
    }

    private async void OnLoadScoreboardClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load scoreboard", LoadScoreboardAsync);
    }

    private async Task LoadScoreboardAsync()
    {
        if (session.CurrentRoom is null)
        {
            ScoreboardLabel.Text = "Scoreboard: no active room.";
            return;
        }

        // #scoreboard #results #game #api-fetch
        // מבקש מהשרת ניקוד של כל השחקנים בחדר.
        // scoreboard מחושב בצד השרת לפי התשובות שנשמרו ב-player_answers.
        var result = await api.GetScoreboardAsync(session.CurrentRoom.RoomCode);
        if (!result.Success || result.Data is null)
        {
            ScoreboardLabel.Text = $"Scoreboard: failed - {result.Message}";
            return;
        }

        // ממירים כל ScoreRow לטקסט nickname: correct/answered.
        ScoreboardLabel.Text = "Scoreboard: " + string.Join(
            " | ",
            result.Data.Rows.Select(r => $"{r.Nickname}: {r.CorrectCount}/{r.AnsweredCount}"));
    }

    private async void OnStatsClicked(object? sender, EventArgs e)
    {
        // #stats #results #navigation
        await Shell.Current.GoToAsync("//stats");
    }

    private bool EnsureCanUseRoom()
    {
        // #login-validation #room-validation #player-validation #validation
        // כדי לשחק צריך גם משתמש, גם חדר, וגם RoomPlayerID.
        if (!session.IsLoggedIn || session.CurrentUser is null)
        {
            StatusLabel.Text = "Status: login first.";
            return false;
        }

        if (session.CurrentRoom is null || session.CurrentPlayer is null)
        {
            StatusLabel.Text = "Status: create or join a room first.";
            return false;
        }

        // true אומר ל-event handler שמותר להמשיך לקריאת ה-API.
        return true;
    }

    private void UpdateRoomLabel()
    {
        // ?. מאפשר להציג RoomPlayerID גם אם CurrentPlayer עדיין null בלי לזרוק חריגה.
        RoomLabel.Text = session.CurrentRoom is null
            ? "Room: none"
            : $"Room: {session.CurrentRoom.RoomName} ({session.CurrentRoom.RoomCode}), playerId={session.CurrentPlayer?.RoomPlayerID}";
    }

    private void RenderQuestion()
    {
        // שומרים את השאלה במשתנה מקומי כדי לפשט את שאר הקוד.
        var question = session.CurrentQuestion;

        // אם question=null מציגים placeholder; אחרת את הטקסט שהגיע מהשרת.
        QuestionLabel.Text = question is null
            ? "Question: -"
            : $"Question: {question.QuestionText}";
        TimerLabel.Text = question is null
            ? "Time limit: -"
            : $"Time limit: {question.TimeLimitSec} seconds";
        // מחברים את רשימת האפשרויות ל-CollectionView.
        // אוסף ריק מונע הצגת אפשרויות של שאלה קודמת.
        OptionsView.ItemsSource = question is null
            ? Array.Empty<QuestionOptionRow>()
            : question.Options;

        // מאפסים סימון ויזואלי של תשובה בכל רינדור.
        OptionsView.SelectedItem = null;
    }
}
