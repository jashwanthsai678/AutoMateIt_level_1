namespace NumberGuessingApp.Models
{
    public class Level
    {
        public int LevelId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public int TargetNumber { get; set; }
        public bool IsSolved { get; set; }
    }

    public class GuessRequest
    {
        public int Guess { get; set; }
        public int LevelId { get; set; }
    }

    public class GuessResponse
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ActualNumber { get; set; }
    }

    public class ExecuteCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public int LevelId { get; set; }
    }

    public class ExecuteCodeResponse
    {
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string TimeComplexity { get; set; } = string.Empty;
        public bool? NumberFound { get; set; }
    }

    public class LevelProgress
    {
        public int LevelId { get; set; }
        public bool ManualCompleted { get; set; }
        public bool AutomatedCompleted { get; set; }
        public int Attempts { get; set; }
    }
}