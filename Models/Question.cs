using System.Text.Json.Serialization;

namespace ChimieQuiz.Models;

public class Question
{
    public int Id { get; set; }
    public string Chapter { get; set; } = "";
    public string Text { get; set; } = "";

    public List<string> Options { get; set; } = new();

    public int CorrectIndex { get; set; } // 0..4

    [JsonIgnore]
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Chapter) &&
        !string.IsNullOrWhiteSpace(Text) &&
        Options is { Count: 5 } &&
        CorrectIndex >= 0 && CorrectIndex < 5;
}