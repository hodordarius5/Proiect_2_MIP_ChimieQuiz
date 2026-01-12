using ChimieQuiz.Pages;

namespace ChimieQuiz;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
		Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
		Routing.RegisterRoute(nameof(ResultsPage), typeof(ResultsPage));
    }
}