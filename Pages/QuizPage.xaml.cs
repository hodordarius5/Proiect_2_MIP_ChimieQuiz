using System.Text.Json;
using ChimieQuiz.Models;


namespace ChimieQuiz.Pages;

[QueryProperty(nameof(Chapter), "chapter")]
[QueryProperty(nameof(OnlyIds), "ids")]
public partial class QuizPage : ContentPage
{
    public string Chapter { get; set; } = "";
    public string OnlyIds { get; set; } = "";

    private List<Question> _questions = new();
    private int _index = 0;

    // QuestionId -> SelectedIndex (0..4)
    private readonly Dictionary<int, int> _selected = new();

    public QuizPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_questions.Count == 0) // evită reload dacă revii înapoi
            await LoadChapterAsync();
    }

        private async Task LoadChapterAsync()
    {
        try
        {
            // 1) Citește întrebările din JSON din pachetul aplicației
            using var stream = await FileSystem.OpenAppPackageFileAsync("Data/chimie_questions.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var all = JsonSerializer.Deserialize<List<Question>>(json) ?? new List<Question>();

            // 2) Dacă avem ids (Retry greșite), construim un set de ID-uri
            var idsSet = new HashSet<int>();
            if (!string.IsNullOrWhiteSpace(OnlyIds))
            {
                foreach (var s in OnlyIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(s, out var id))
                        idsSet.Add(id);
                }
            }

            // 3) Filtrăm întrebările după capitol + validare + (opțional) după idsSet
            _questions = all
                .Where(q => q.IsValid)
                .Where(q => q.Chapter == Chapter)
                .Where(q => idsSet.Count == 0 || idsSet.Contains(q.Id))
                .ToList();

            HeaderLabel.Text = Chapter;

            // 4) Dacă nu există întrebări, ieșim frumos
            if (_questions.Count == 0)
            {
                await DisplayAlertAsync("Atenție", "Nu există întrebări pentru capitolul selectat.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // 5) Reset la început de quiz
            _index = 0;
            _selected.Clear(); // foarte important ca să nu rămână răspunsuri vechi

            ShowQuestion();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Eroare", $"Nu pot încărca întrebările: {ex.Message}", "OK");
        }
    }

    private void ShowQuestion()
    {
        var q = _questions[_index];

        ProgressLabel.Text = $"{_index + 1}/{_questions.Count}";
        QuestionLabel.Text = q.Text;

        OptionA.Text = $"A) {q.Options[0]}";
        OptionB.Text = $"B) {q.Options[1]}";
        OptionC.Text = $"C) {q.Options[2]}";
        OptionD.Text = $"D) {q.Options[3]}";
        OptionE.Text = $"E) {q.Options[4]}";

        // reset “highlight”
        ResetOptionVisuals();

        // dacă întrebarea avea deja un răspuns ales (ex: dacă adaugi Back later),
        // îl reafișăm selectat
        if (_selected.TryGetValue(q.Id, out var alreadySelected))
        {
            HighlightSelected(alreadySelected);
            NextButton.IsEnabled = true;
        }
        else
        {
            NextButton.IsEnabled = false;
        }
    }

    private void OnOptionClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;

        int selectedIndex =
            btn == OptionA ? 0 :
            btn == OptionB ? 1 :
            btn == OptionC ? 2 :
            btn == OptionD ? 3 : 4;

        var q = _questions[_index];
        _selected[q.Id] = selectedIndex;

        // IMPORTANT: NU calculăm scor aici, NU afișăm corect/greșit aici
        ResetOptionVisuals();
        HighlightSelected(selectedIndex);

        NextButton.IsEnabled = true;
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        if (_index < _questions.Count - 1)
        {
            _index++;
            ShowQuestion();
            return;
        }

        // Final: trimitem doar răspunsurile selectate; Results calculează corect/greșit
        var answers = _selected.Select(kvp => new QuizAnswer
        {
            QuestionId = kvp.Key,
            SelectedIndex = kvp.Value,
            IsCorrect = false // ResultsPage îl calculează
        }).ToList();

        var payload = JsonSerializer.Serialize(answers);

        await Shell.Current.GoToAsync(
            $"{nameof(ResultsPage)}?chapter={Uri.EscapeDataString(Chapter)}&answers={Uri.EscapeDataString(payload)}"
        );
    }

    private void ResetOptionVisuals()
    {
        // revenim la look-ul default (OptionButton)
        // (Style rămâne, doar resetăm culori/border dacă ai schimbat înainte)
        OptionA.BorderColor = (Color)Application.Current.Resources["BorderColor"];
        OptionB.BorderColor = (Color)Application.Current.Resources["BorderColor"];
        OptionC.BorderColor = (Color)Application.Current.Resources["BorderColor"];
        OptionD.BorderColor = (Color)Application.Current.Resources["BorderColor"];
        OptionE.BorderColor = (Color)Application.Current.Resources["BorderColor"];

        OptionA.BorderWidth = 1;
        OptionB.BorderWidth = 1;
        OptionC.BorderWidth = 1;
        OptionD.BorderWidth = 1;
        OptionE.BorderWidth = 1;
    }

    private void HighlightSelected(int idx)
    {
        // doar evidențiem selecția (fără “corect/greșit”)
        var accent = (Color)Application.Current.Resources["AccentPink"];

        Button b = idx switch
        {
            0 => OptionA,
            1 => OptionB,
            2 => OptionC,
            3 => OptionD,
            _ => OptionE
        };

        b.BorderColor = accent;
        b.BorderWidth = 2;
    }
}