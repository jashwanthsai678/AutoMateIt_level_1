using NumberGuessingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NumberGuessingApp.Services
{
    public interface IGameService
    {
        Level? GetLevel(int levelId);
        GuessResponse CheckGuess(int levelId, int guess);
        ExecuteCodeResponse ExecutePythonCode(int levelId, string code);
        List<Level> GetAllLevels();
        void ResetGame();
        Dictionary<int, LevelProgress> GetProgress();
    }

    public class GameService : IGameService
    {
        private readonly Dictionary<int, Level> _levels;
        private readonly Dictionary<int, LevelProgress> _progress;
        private readonly Random _random;

        public GameService()
        {
            _random = new Random();
            _levels = new Dictionary<int, Level>();
            _progress = new Dictionary<int, LevelProgress>();
            InitializeLevels();
        }

        private void InitializeLevels()
        {
            // Level 1: Basic guessing
            _levels[1] = new Level
            {
                LevelId = 1,
                Title = "Manual Guessing",
                Description = "Guess the number between 1-100",
                Instructions = "Try to guess the number manually. The target number is fixed for this session.",
                TargetNumber = _random.Next(1, 101),
                IsSolved = false
            };

            // Level 2: Simple loop
            _levels[2] = new Level
            {
                LevelId = 2,
                Title = "Linear Search",
                Description = "Find the number using a simple loop",
                Instructions = "Write Python-like pseudocode to find the number using linear search.",
                TargetNumber = _random.Next(1, 101),
                IsSolved = false
            };

            // Level 3: Binary search
            _levels[3] = new Level
            {
                LevelId = 3,
                Title = "Binary Search",
                Description = "Find the number using binary search algorithm",
                Instructions = "Write Python-like pseudocode that implements binary search to find the number efficiently.",
                TargetNumber = _random.Next(1, 101),
                IsSolved = false
            };

            // Initialize progress
            foreach (var level in _levels.Values)
            {
                _progress[level.LevelId] = new LevelProgress
                {
                    LevelId = level.LevelId,
                    ManualCompleted = false,
                    AutomatedCompleted = false,
                    Attempts = 0
                };
            }
        }

        public Level? GetLevel(int levelId)
        {
            return _levels.ContainsKey(levelId) ? _levels[levelId] : null;
        }

        public List<Level> GetAllLevels()
        {
            return _levels.Values.ToList();
        }

        public Dictionary<int, LevelProgress> GetProgress()
        {
            return _progress;
        }

        public GuessResponse CheckGuess(int levelId, int guess)
        {
            if (!_levels.ContainsKey(levelId))
                return new GuessResponse { IsCorrect = false, Message = "Level not found" };

            var level = _levels[levelId];
            _progress[levelId].Attempts++;

            if (guess == level.TargetNumber)
            {
                _progress[levelId].ManualCompleted = true;
                _levels[levelId].IsSolved = true;
                return new GuessResponse
                {
                    IsCorrect = true,
                    Message = "🎉 Congratulations! You guessed correctly!",
                    ActualNumber = level.TargetNumber
                };
            }

            string hint = guess < level.TargetNumber ? "Too low!" : "Too high!";
            return new GuessResponse
            {
                IsCorrect = false,
                Message = $"{hint} Try again.",
                ActualNumber = null
            };
        }

        public ExecuteCodeResponse ExecutePythonCode(int levelId, string code)
        {
            var response = new ExecuteCodeResponse();

            if (!_levels.ContainsKey(levelId))
            {
                response.Error = "Level not found";
                response.IsSuccess = false;
                return response;
            }

            var level = _levels[levelId];

            try
            {
                // Simulate Python code execution (simplified version)
                response = SimulatePythonExecution(level.TargetNumber, code);

                if (response.NumberFound == true)
                {
                    _progress[levelId].AutomatedCompleted = true;
                    _levels[levelId].IsSolved = true;
                }

                // Analyze time complexity
                response.TimeComplexity = AnalyzeTimeComplexity(code);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.Error = $"Simulation error: {ex.Message}";
                response.IsSuccess = false;
            }

            return response;
        }

        private ExecuteCodeResponse SimulatePythonExecution(int targetNumber, string code)
        {
            var response = new ExecuteCodeResponse();
            var outputLines = new List<string>();
            bool numberFound = false;

            // Convert code to lowercase for easier parsing
            var cleanCode = code.ToLower();
            var lines = code.Split('\n');

            // Simple simulation logic
            if (cleanCode.Contains("for") && cleanCode.Contains("range"))
            {
                // Simulate linear search
                for (int i = 1; i <= 100; i++)
                {
                    if (i == targetNumber)
                    {
                        outputLines.Add($"Found the number: {i}");
                        numberFound = true;
                        break;
                    }
                    else if (cleanCode.Contains("print") && cleanCode.Contains("guess"))
                    {
                        outputLines.Add($"Checking: {i}");
                    }
                }

                if (!numberFound && cleanCode.Contains("not found"))
                {
                    outputLines.Add("Number not found in range");
                }
            }
            else if ((cleanCode.Contains("while") && cleanCode.Contains("low") && cleanCode.Contains("high")) ||
                     (cleanCode.Contains("binary") && cleanCode.Contains("search")))
            {
                // Simulate binary search
                int low = 1, high = 100, attempts = 0;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    attempts++;

                    if (cleanCode.Contains("print") && (cleanCode.Contains("attempt") || cleanCode.Contains("guess")))
                    {
                        outputLines.Add($"Attempt {attempts}: Guessing {mid}");
                    }

                    if (mid == targetNumber)
                    {
                        outputLines.Add($"Found the number {mid} in {attempts} attempts!");
                        numberFound = true;
                        break;
                    }
                    else if (mid < targetNumber)
                    {
                        low = mid + 1;
                        if (cleanCode.Contains("print") && cleanCode.Contains("low"))
                        {
                            outputLines.Add($"Guess {mid}: Too low");
                        }
                    }
                    else
                    {
                        high = mid - 1;
                        if (cleanCode.Contains("print") && cleanCode.Contains("high"))
                        {
                            outputLines.Add($"Guess {mid}: Too high");
                        }
                    }
                }
            }
            else
            {
                // Simple code execution simulation
                if (cleanCode.Contains($"print({targetNumber})") ||
                    cleanCode.Contains($"print(\"{targetNumber}\")") ||
                    cleanCode.Contains($"print('{targetNumber}')"))
                {
                    outputLines.Add($"{targetNumber}");
                    numberFound = true;
                }
                else if (cleanCode.Contains("print"))
                {
                    outputLines.Add("Code executed successfully");
                }
            }

            response.Output = string.Join("\n", outputLines);
            response.NumberFound = numberFound;

            return response;
        }

        private string AnalyzeTimeComplexity(string code)
        {
            var cleanCode = code.ToLower();

            // Check for binary search patterns
            if ((cleanCode.Contains("mid") && cleanCode.Contains("high") && cleanCode.Contains("low") &&
                 cleanCode.Contains("while") && (cleanCode.Contains("//") || cleanCode.Contains("/ 2"))) ||
                (cleanCode.Contains("binary") && cleanCode.Contains("search")))
            {
                return "O(log n) - Binary Search (Efficient!)";
            }
            // Check for linear search
            else if ((cleanCode.Contains("for") && cleanCode.Contains("range")) ||
                     (cleanCode.Contains("while") && cleanCode.Contains("<=")))
            {
                // Check for nested loops
                var forCount = Regex.Matches(cleanCode, @"for\s+\w+\s+in").Count;
                var whileCount = Regex.Matches(cleanCode, @"while\s+[^:]+:").Count;

                if (forCount + whileCount > 1)
                {
                    return "O(n²) - Nested loops detected";
                }

                return "O(n) - Linear search";
            }
            // Check for constant time
            else if (!cleanCode.Contains("for") && !cleanCode.Contains("while"))
            {
                return "O(1) - Constant time operation";
            }

            return "O(?) - Complexity not determined";
        }

        public void ResetGame()
        {
            InitializeLevels();
        }
    }
}