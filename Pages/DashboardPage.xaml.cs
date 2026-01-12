using System.Collections.ObjectModel;
using System.Text.Json;
using ChimieQuiz.Models;
using Microsoft.Maui.Storage;

namespace ChimieQuiz.Pages;

public partial class DashboardPage : ContentPage
{
    public ObservableCollection<ChapterCard> Chapters { get; } = new();

    private List<Question> _allQuestions = new();

    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadQuestionsAsync();
    }
    private async Task LoadQuestionsAsync()
    {
        try
        {
            InfoLabel.Text = "Loading questions...";

            using var stream = await FileSystem.OpenAppPackageFileAsync("Data/chimie_questions.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var items = JsonSerializer.Deserialize<List<Question>>(json) ?? new List<Question>();
            _allQuestions = items.Where(q => q.IsValid).ToList();

            var chapterGroups = _allQuestions
                .Where(q => !string.IsNullOrWhiteSpace(q.Chapter))
                .GroupBy(q => q.Chapter!.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            Chapters.Clear();

            foreach (var g in chapterGroups)
            {
                var key = $"progress_{g.Key}";
                var raw = Preferences.Get(key, "");
                var p = string.IsNullOrWhiteSpace(raw)
                    ? new ChapterProgress()
                    : (JsonSerializer.Deserialize<ChapterProgress>(raw) ?? new ChapterProgress());

                Chapters.Add(new ChapterCard
                {
                    Name = g.Key,
                    QuestionCount = g.Count(),
                    Attempts = p.Attempts,
                    BestPercent = p.BestPercent,
                    LastPercent = p.LastPercent,
                    LastAttemptAt = p.LastAttemptAt
                });
            }

            InfoLabel.Text = $"Questions loaded: {_allQuestions.Count} · Chapters: {Chapters.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load questions: {ex.Message}", "OK");
            InfoLabel.Text = "Failed to load questions.";
        }
    }

    private async void OnStartChapterClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;

        var chapter = btn.CommandParameter as string;
        if (string.IsNullOrWhiteSpace(chapter))
            return;

        // safety: verify chapter has questions
        var count = _allQuestions.Count(q => q.Chapter == chapter);
        if (count == 0)
        {
            await DisplayAlertAsync("Notice", "No questions found for this chapter.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(QuizPage)}?chapter={Uri.EscapeDataString(chapter)}");
    }

        public class ChapterCard
        {
            public string Name { get; set; } = "";
            public int QuestionCount { get; set; }

            public int Attempts { get; set; }
            public int BestPercent { get; set; }
            public int LastPercent { get; set; }
            public string LastAttemptAt { get; set; } = "";

            public string Subtitle =>
                Attempts == 0
                ? $"{QuestionCount} questions · No attempts yet"
                : $"{QuestionCount} questions · Best {BestPercent}% · Attempts {Attempts} · Last {LastPercent}% ({LastAttemptAt})";
        }
}