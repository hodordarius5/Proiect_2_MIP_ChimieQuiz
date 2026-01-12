namespace ChimieQuiz.Models;

public class ChapterProgress
{
    public int Attempts { get; set; }
    public int BestPercent { get; set; }
    public int LastPercent { get; set; }
    public string LastAttemptAt { get; set; } = "";
}