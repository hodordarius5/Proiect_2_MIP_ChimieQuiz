namespace ChimieQuiz.Models;

public class QuizAnswer
{
    public int QuestionId { get; set; }
    public int SelectedIndex { get; set; } // 0..4

    public bool IsCorrect { get; set; }
}