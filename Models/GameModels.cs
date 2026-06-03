using System.Text.Json.Serialization;

namespace NumberGuessingApp.Models
{
    // ── Topics ────────────────────────────────────────────────────────────────
    public record Topic
    {
        public int TopicId { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Icon { get; init; } = "";
        public bool IsAvailable { get; init; }
        public int ProblemCount { get; init; }
        public int SolvedCount { get; init; }
    }

    // ── Problems ──────────────────────────────────────────────────────────────
    public record Problem
    {
        public int ProblemId { get; init; }
        public int TopicId { get; init; }
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Difficulty { get; init; } = "";
        public string ManualInstructions { get; init; } = "";
        public string Instructions { get; init; } = "";
        public string ExampleCode { get; init; } = "";
        public string ScaffoldCode { get; init; } = "";
        public int? PrerequisiteId { get; init; }
        public bool IsSolved { get; init; }
        public bool IsLocked { get; init; }
        public object? ProblemData { get; init; }
    }

    // ── Requests ──────────────────────────────────────────────────────────────
    public class ManualAnswerRequest
    {
        public string Value { get; set; } = "";
    }

    public class CodeRequest
    {
        public string Code { get; set; } = "";
    }

    // ── Responses ─────────────────────────────────────────────────────────────
    public class ManualAnswerResponse
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; } = "";
        public string Feedback { get; set; } = "";
    }

    public class CodeResponse
    {
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public bool IsSuccess { get; set; }
        public bool? IsCorrect { get; set; }
        public string TimeComplexity { get; set; } = "";
        public int GuessCount { get; set; }
    }
}
