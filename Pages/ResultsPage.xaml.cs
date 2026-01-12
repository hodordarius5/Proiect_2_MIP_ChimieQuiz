using System.Collections.ObjectModel;
using System.Text.Json;
using ChimieQuiz.Models;
using Microsoft.Maui.Storage;

namespace ChimieQuiz.Pages;

[QueryProperty(nameof(Chapter), "chapter")]
[QueryProperty(nameof(AnswersJson), "answers")]
public partial class ResultsPage : ContentPage
{
    public string Chapter { get; set; } = "";
    public string AnswersJson { get; set; } = "";

    public ObservableCollection<WrongItem> WrongItems { get; } = new();

    private List<int> _wrongIds = new();

    public ResultsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAndRenderAsync();
    }

    private async Task LoadAndRenderAsync()
    {
        try
        {
            // 1) Parse answers payload
            var decoded = Uri.UnescapeDataString(AnswersJson ?? "");
            var answers = JsonSerializer.Deserialize<List<QuizAnswer>>(decoded) ?? new List<QuizAnswer>();

            // 2) Load questions again
            using var stream = await FileSystem.OpenAppPackageFileAsync("Data/chimie_questions.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var allQuestions = JsonSerializer.Deserialize<List<Question>>(json) ?? new List<Question>();
            var questionsById = allQuestions
                .Where(q => q.IsValid)
                .ToDictionary(q => q.Id, q => q);

            // 3) Compute correctness + build wrong list
            WrongItems.Clear();
            _wrongIds.Clear();

            int total = answers.Count;
            int correct = 0;

            foreach (var a in answers)
            {
                if (!questionsById.TryGetValue(a.QuestionId, out var q))
                    continue;

                bool isCorrect = a.SelectedIndex == q.CorrectIndex;
                if (isCorrect)
                {
                    correct++;
                    continue;
                }

                _wrongIds.Add(q.Id);

                WrongItems.Add(new WrongItem
                {
                    QuestionId = q.Id,
                    QuestionText = q.Text,
                    YourAnswerText = $"Răspunsul tău: {Letter(a.SelectedIndex)}) {q.Options[a.SelectedIndex]}",
                    CorrectAnswerText = $"Corect: {Letter(q.CorrectIndex)}) {q.Options[q.CorrectIndex]}"
                });
            }

            int wrong = total - correct;
            int percent = total == 0 ? 0 : (int)Math.Round((double)correct * 100.0 / total);

            var key = $"progress_{Chapter}";
            var raw = Preferences.Get(key, "");

            ChapterProgress p;
            if (string.IsNullOrWhiteSpace(raw))
            {
                p = new ChapterProgress();
            }
            else
            {
                p = JsonSerializer.Deserialize<ChapterProgress>(raw) ?? new ChapterProgress();
            }

            p.Attempts += 1;
            p.LastPercent = percent;
            p.BestPercent = Math.Max(p.BestPercent, percent);
            p.LastAttemptAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            Preferences.Set(key, JsonSerializer.Serialize(p));

            // 4) UI
            TitleLabel.Text = $"Rezultate – {Chapter}";
            ScoreLabel.Text = $"Scor: {correct}/{total}";
            PercentLabel.Text = $"Procent: {percent}%";
            CountsLabel.Text = $"Răspunse: {total} · Greșite: {wrong}";

            NoWrongLabel.IsVisible = wrong == 0;
            RetryWrongButton.IsVisible = wrong > 0;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Eroare", $"Nu pot calcula rezultatele: {ex.Message}", "OK");
        }
    }

    private static string Letter(int idx) => idx switch
    {
        0 => "A",
        1 => "B",
        2 => "C",
        3 => "D",
        _ => "E"
    };

    private async void OnRetryWrongClicked(object sender, EventArgs e)
    {
        if (_wrongIds.Count == 0)
            return;

        // Trimitem lista de ID-uri greșite către QuizPage
        var ids = string.Join(",", _wrongIds);
        await Shell.Current.GoToAsync($"{nameof(QuizPage)}?chapter={Uri.EscapeDataString(Chapter)}&ids={Uri.EscapeDataString(ids)}");
    }

    private async void OnDashboardClicked(object sender, EventArgs e)
{
   await Shell.Current.GoToAsync("//DashboardPage");
}

    public class WrongItem
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string YourAnswerText { get; set; } = "";
        public string CorrectAnswerText { get; set; } = "";
    }
}