using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף המשחק עצמו.
// משתמש ב-roomCode וב-RoomPlayerID שנשמרו ב-MobileSessionState אחרי יצירת/הצטרפות לחדר.
public partial class PlayPage : ContentPage
{
    private readonly TriviaApiClient api;
    private readonly MobileSessionState session;

    public PlayPage()
    {
        InitializeComponent();
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
        OptionsView.ItemsSource = Array.Empty<QuestionOptionRow>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateRoomLabel();
        RenderQuestion();
    }

    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
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

            var count = 10;
            _ = int.TryParse(QuestionCountEntry.Text, out count);
            if (count <= 0)
                count = 10;

            // #start-game #game #play #api-fetch
            // StartGame שולח userId של ה-host, roomCode וכמות שאלות.
            // השרת בוחר שאלות ושומר אותן ל-room_questions.
            var result = await api.StartGameAsync(
                session.CurrentUser!.UserId,
                session.CurrentRoom!.RoomCode,
                count);

            if (!result.Success || result.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: start failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            StatusLabel.Text = "Status: game started.";
            await LoadCurrentQuestionAsync();
        });
    }

    private async void OnLoadQuestionClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load question", LoadCurrentQuestionAsync);
    }

    private async Task LoadCurrentQuestionAsync()
    {
        if (!EnsureCanUseRoom())
            return;

        // #question #current-question #game #play #api-fetch
        // מבקשים מהשרת את השאלה הפעילה. אם אין עוד שאלות, השרת מחזיר Finished=true.
        var result = await api.GetCurrentQuestionAsync(session.CurrentRoom!.RoomCode);
        if (!result.Success || result.Data is null)
        {
            StatusLabel.Text = $"Status: question failed - {result.Message}";
            return;
        }

        if (result.Data.Finished)
        {
            session.CurrentQuestion = null;
            session.SelectedOption = null;
            RenderQuestion();
            StatusLabel.Text = "Status: game finished. Load scoreboard.";
            return;
        }

        session.CurrentQuestion = result.Data.Question;
        session.SelectedOption = null;
        RenderQuestion();
        StatusLabel.Text = "Status: question loaded.";
    }

    private void OnOptionSelected(object? sender, SelectionChangedEventArgs e)
    {
        // #answer #question
        // בחירת תשובה רק שומרת את האופציה בזיכרון.
        // היא לא נשמרת ב-DB עד לחיצה על Submit Answer.
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
                session.CurrentRoom!.RoomCode,
                session.CurrentPlayer!.RoomPlayerID,
                session.CurrentQuestion.QuestionID,
                session.SelectedOption.OptionID);

            if (!result.Success || result.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: answer failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            StatusLabel.Text = $"Status: {result.Data.Message}";
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
        var result = await api.GetScoreboardAsync(session.CurrentRoom.RoomCode);
        if (!result.Success || result.Data is null)
        {
            ScoreboardLabel.Text = $"Scoreboard: failed - {result.Message}";
            return;
        }

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

        return true;
    }

    private void UpdateRoomLabel()
    {
        RoomLabel.Text = session.CurrentRoom is null
            ? "Room: none"
            : $"Room: {session.CurrentRoom.RoomName} ({session.CurrentRoom.RoomCode}), playerId={session.CurrentPlayer?.RoomPlayerID}";
    }

    private void RenderQuestion()
    {
        var question = session.CurrentQuestion;
        QuestionLabel.Text = question is null
            ? "Question: -"
            : $"Question: {question.QuestionText}";
        TimerLabel.Text = question is null
            ? "Time limit: -"
            : $"Time limit: {question.TimeLimitSec} seconds";
        OptionsView.ItemsSource = question is null
            ? Array.Empty<QuestionOptionRow>()
            : question.Options;
        OptionsView.SelectedItem = null;
    }
}
