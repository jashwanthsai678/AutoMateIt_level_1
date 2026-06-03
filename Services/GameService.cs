using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using NumberGuessingApp.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace NumberGuessingApp.Services
{
    // Runtime state per problem — never exposed via API
    internal class ProblemSession
    {
        public int ProblemId { get; init; }
        public bool IsGuessMode { get; init; }
        public int Target { get; init; }                         // GuessMode only
        public Dictionary<string, object> Data { get; init; } = new(); // plain C# types
        public Func<object?, bool>? Validate { get; init; }
        public bool ManualSolved { get; set; }
        public bool CodeSolved { get; set; }
        public int Attempts { get; set; }
    }

    public interface IProblemService
    {
        List<Topic> GetTopics();
        List<Problem> GetProblemsForTopic(int topicId);
        Problem? GetProblem(int problemId);
        ManualAnswerResponse CheckManualAnswer(int problemId, string value);
        CodeResponse ExecuteCode(int problemId, string code);
        List<string>? GetHints(int problemId);
        void ResetAll();
    }

    public class ProblemService : IProblemService
    {
        private Dictionary<int, ProblemSession> _sessions = new();
        private readonly Random _rng = new();

        // ── Topic Catalog (static) ────────────────────────────────────────────
        private static readonly List<Topic> TopicCatalog = new()
        {
            new Topic { TopicId=1, Name="Searching",     Icon="🔍", IsAvailable=true,  Description="Linear search, binary search and beyond" },
            new Topic { TopicId=2, Name="Arrays",        Icon="📊", IsAvailable=true,  Description="Traversal, find, and manipulation problems" },
            new Topic { TopicId=3, Name="Strings",       Icon="🔤", IsAvailable=true,  Description="String operations and pattern recognition" },
            new Topic { TopicId=4, Name="Sorting",       Icon="🔀", IsAvailable=true,  Description="Bubble, insertion, merge and more" },
            new Topic { TopicId=5, Name="Linked Lists",  Icon="🔗", IsAvailable=false, Description="Node traversal and pointer manipulation" },
            new Topic { TopicId=6, Name="Trees",         Icon="🌳", IsAvailable=false, Description="BST, DFS, BFS and tree traversals" },
            new Topic { TopicId=7, Name="Graphs",        Icon="🕸️", IsAvailable=false, Description="DFS, BFS and shortest path algorithms" },
            new Topic { TopicId=8, Name="Tries",         Icon="🌲", IsAvailable=false, Description="Prefix trees and autocomplete" },
        };

        // ── Problem Catalog (static metadata only) ────────────────────────────
        private static readonly List<Problem> ProblemCatalog = new()
        {
            // Searching
            new Problem
            {
                ProblemId=1, TopicId=1, Title="Linear Search", Difficulty="Easy",
                Description="Find a hidden number by checking every value one by one.",
                Instructions="A number is hidden between 1 and 100. Call <code>guess(n)</code> — it returns <code>'too_low'</code>, <code>'too_high'</code>, or <code>'correct'</code>. Check each number starting from 1.",
                ExampleCode="# Linear Search\nfor i in range(1, 101):\n    result = guess(i)\n    if result == \"correct\":\n        print(f\"Found the number: {i}\")\n        break\n    else:\n        print(f\"Checking {i}: {result}\")"
            },
            new Problem
            {
                ProblemId=2, TopicId=1, Title="Binary Search", Difficulty="Medium",
                Description="Find a hidden number efficiently by halving the search range each step.",
                Instructions="A number is hidden between 1 and 100. Call <code>guess(n)</code> — it returns <code>'too_low'</code>, <code>'too_high'</code>, or <code>'correct'</code>. Use binary search — you should find it in at most 7 guesses!",
                ExampleCode="# Binary Search\nlow = 1\nhigh = 100\nattempts = 0\n\nwhile low <= high:\n    mid = (low + high) // 2\n    attempts += 1\n    result = guess(mid)\n\n    if result == \"correct\":\n        print(f\"Found {mid} in {attempts} attempts!\")\n        break\n    elif result == \"too_low\":\n        low = mid + 1\n        print(f\"{mid}: too low\")\n    else:\n        high = mid - 1\n        print(f\"{mid}: too high\")"
            },

            // Arrays
            new Problem
            {
                ProblemId=3, TopicId=2, Title="Find Maximum", Difficulty="Easy",
                Description="Find the largest element in an array.",
                Instructions="An array <code>arr</code> is provided. Traverse it and call <code>answer(max_value)</code> with the largest element.",
                ExampleCode="# Find Maximum\nmax_val = arr[0]\nfor x in arr:\n    if x > max_val:\n        max_val = x\n\nprint(f\"Maximum: {max_val}\")\nanswer(max_val)"
            },
            new Problem
            {
                ProblemId=4, TopicId=2, Title="Find Minimum", Difficulty="Easy",
                Description="Find the smallest element in an array.",
                Instructions="An array <code>arr</code> is provided. Traverse it and call <code>answer(min_value)</code> with the smallest element.",
                ExampleCode="# Find Minimum\nmin_val = arr[0]\nfor x in arr:\n    if x < min_val:\n        min_val = x\n\nprint(f\"Minimum: {min_val}\")\nanswer(min_val)"
            },
            new Problem
            {
                ProblemId=5, TopicId=2, Title="Two Sum", Difficulty="Medium",
                Description="Find two indices in the array whose values add up to a target.",
                Instructions="Array <code>arr</code> and integer <code>target</code> are provided. Find two different indices <code>i</code> and <code>j</code> where <code>arr[i] + arr[j] == target</code>. Call <code>answer([i, j])</code>.",
                ExampleCode="# Two Sum\nfor i in range(len(arr)):\n    for j in range(i + 1, len(arr)):\n        if arr[i] + arr[j] == target:\n            print(f\"Indices {i} and {j}: {arr[i]} + {arr[j]} = {target}\")\n            answer([i, j])\n            break"
            },

            // Strings
            new Problem
            {
                ProblemId=6, TopicId=3, Title="Palindrome Check", Difficulty="Easy",
                Description="Determine if a string reads the same forwards and backwards.",
                Instructions="A string <code>s</code> is provided. Call <code>answer(True)</code> if it is a palindrome, or <code>answer(False)</code> if it is not.",
                ExampleCode="# Palindrome Check\nreversed_s = s[::-1]\nif s == reversed_s:\n    print(f'\"{s}\" is a palindrome')\n    answer(True)\nelse:\n    print(f'\"{s}\" is NOT a palindrome')\n    answer(False)"
            },
            new Problem
            {
                ProblemId=7, TopicId=3, Title="Reverse a String", Difficulty="Easy",
                Description="Return the characters of a string in reverse order.",
                Instructions="A string <code>s</code> is provided. Reverse it manually (without using <code>[::-1]</code> for the challenge!) and call <code>answer(reversed_string)</code>.",
                ExampleCode="# Reverse a String\nreversed_s = \"\"\nfor ch in s:\n    reversed_s = ch + reversed_s\n\nprint(f\"Original: {s}\")\nprint(f\"Reversed: {reversed_s}\")\nanswer(reversed_s)"
            },
            new Problem
            {
                ProblemId=8, TopicId=3, Title="Count Vowels", Difficulty="Easy",
                Description="Count how many vowel characters are in a string.",
                Instructions="A string <code>s</code> is provided. Count vowels (a, e, i, o, u — case-insensitive) and call <code>answer(count)</code>.",
                ExampleCode="# Count Vowels\nvowels = \"aeiouAEIOU\"\ncount = 0\nfor ch in s:\n    if ch in vowels:\n        count += 1\n\nprint(f\"Vowels in '{s}': {count}\")\nanswer(count)"
            },

            // Sorting
            new Problem
            {
                ProblemId=9, TopicId=4, Title="Bubble Sort", Difficulty="Medium",
                Description="Sort an array in ascending order using the bubble sort algorithm.",
                Instructions="An array <code>arr</code> is provided. Sort it using bubble sort (swap adjacent elements that are out of order) and call <code>answer(arr)</code> with the sorted result.",
                ExampleCode="# Bubble Sort\nn = len(arr)\nfor i in range(n):\n    for j in range(0, n - i - 1):\n        if arr[j] > arr[j + 1]:\n            arr[j], arr[j + 1] = arr[j + 1], arr[j]\n\nprint(f\"Sorted: {arr}\")\nanswer(arr)"
            },
            new Problem
            {
                ProblemId=10, TopicId=4, Title="Count Occurrences", Difficulty="Easy",
                Description="Count how many times a value appears in an array.",
                Instructions="Array <code>arr</code> and integer <code>target</code> are provided. Count how many times <code>target</code> appears in <code>arr</code> and call <code>answer(count)</code>.",
                ExampleCode="# Count Occurrences\ncount = 0\nfor x in arr:\n    if x == target:\n        count += 1\n\nprint(f\"{target} appears {count} time(s) in {arr}\")\nanswer(count)"
            },
        };

        public ProblemService() => InitSessions();

        private void InitSessions()
        {
            _sessions = new();

            // ── Searching ─────────────────────────────────────────────────────
            _sessions[1] = new ProblemSession { ProblemId = 1, IsGuessMode = true, Target = _rng.Next(1, 101) };
            _sessions[2] = new ProblemSession { ProblemId = 2, IsGuessMode = true, Target = _rng.Next(1, 101) };

            // ── Arrays ────────────────────────────────────────────────────────
            var a3 = RandArr(10, 10, 99);
            _sessions[3] = new ProblemSession
            {
                ProblemId = 3, IsGuessMode = false,
                Data = { ["arr"] = a3 },
                Validate = ans => SafeInt(ans) == a3.Max()
            };

            var a4 = RandArr(10, 10, 99);
            _sessions[4] = new ProblemSession
            {
                ProblemId = 4, IsGuessMode = false,
                Data = { ["arr"] = a4 },
                Validate = ans => SafeInt(ans) == a4.Min()
            };

            var a5 = RandArr(8, 5, 40);
            var (i5a, i5b) = TwoDistinctIdx(a5.Length);
            int tgt5 = a5[i5a] + a5[i5b];
            _sessions[5] = new ProblemSession
            {
                ProblemId = 5, IsGuessMode = false,
                Data = { ["arr"] = a5, ["target"] = tgt5 },
                Validate = ans =>
                {
                    var idx = SafeIntArr(ans);
                    return idx.Length == 2 && idx[0] != idx[1]
                        && idx[0] >= 0 && idx[1] >= 0
                        && idx[0] < a5.Length && idx[1] < a5.Length
                        && a5[idx[0]] + a5[idx[1]] == tgt5;
                }
            };

            // ── Strings ───────────────────────────────────────────────────────
            bool makePalin = _rng.Next(2) == 0;
            string s6 = makePalin
                ? new[] { "racecar", "level", "noon", "deified", "civic", "radar", "kayak" }[_rng.Next(7)]
                : new[] { "hello", "world", "python", "coding", "array", "stack", "queue" }[_rng.Next(7)];
            bool isPalin = s6 == new string(s6.Reverse().ToArray());
            _sessions[6] = new ProblemSession
            {
                ProblemId = 6, IsGuessMode = false,
                Data = { ["s"] = s6 },
                Validate = ans =>
                {
                    var v = ans?.ToString()?.Trim().ToLower();
                    return v == isPalin.ToString().ToLower() || v == (isPalin ? "1" : "0");
                }
            };

            string s7 = new[] { "algorithm", "binary", "search", "python", "coding", "linked", "graph" }[_rng.Next(7)];
            string rev7 = new string(s7.Reverse().ToArray());
            _sessions[7] = new ProblemSession
            {
                ProblemId = 7, IsGuessMode = false,
                Data = { ["s"] = s7 },
                Validate = ans => ans?.ToString()?.Trim() == rev7
            };

            string s8 = new[] { "automation", "interface", "keyboard", "algorithm", "structure" }[_rng.Next(5)];
            int vowels8 = s8.Count(c => "aeiou".Contains(c));
            _sessions[8] = new ProblemSession
            {
                ProblemId = 8, IsGuessMode = false,
                Data = { ["s"] = s8 },
                Validate = ans => SafeInt(ans) == vowels8
            };

            // ── Sorting ───────────────────────────────────────────────────────
            var a9 = RandArr(8, 1, 50);
            var sorted9 = a9.OrderBy(x => x).ToArray();
            _sessions[9] = new ProblemSession
            {
                ProblemId = 9, IsGuessMode = false,
                Data = { ["arr"] = a9 },
                Validate = ans => SafeIntArr(ans).SequenceEqual(sorted9)
            };

            var a10base = RandArr(6, 1, 10);
            int rep10 = a10base[_rng.Next(6)];
            var a10 = a10base.Concat(new[] { rep10, rep10 }).OrderBy(_ => _rng.Next()).ToArray();
            int cnt10 = a10.Count(x => x == rep10);
            _sessions[10] = new ProblemSession
            {
                ProblemId = 10, IsGuessMode = false,
                Data = { ["arr"] = a10, ["target"] = rep10 },
                Validate = ans => SafeInt(ans) == cnt10
            };
        }

        // ── Public API ────────────────────────────────────────────────────────

        public List<Topic> GetTopics() =>
            TopicCatalog.Select(t =>
            {
                var probs = ProblemCatalog.Where(p => p.TopicId == t.TopicId).ToList();
                int solved = probs.Count(p =>
                    _sessions.TryGetValue(p.ProblemId, out var s) && (s.ManualSolved || s.CodeSolved));
                return t with { ProblemCount = probs.Count, SolvedCount = solved };
            }).ToList();

        public List<Problem> GetProblemsForTopic(int topicId) =>
            ProblemCatalog
                .Where(p => p.TopicId == topicId)
                .Select(Enrich)
                .ToList();

        public Problem? GetProblem(int problemId)
        {
            var def = ProblemCatalog.FirstOrDefault(p => p.ProblemId == problemId);
            return def == null ? null : Enrich(def);
        }

        public ManualAnswerResponse CheckManualAnswer(int problemId, string value)
        {
            if (!_sessions.TryGetValue(problemId, out var s))
                return new ManualAnswerResponse { IsCorrect = false, Message = "Problem not found.", Feedback = "error" };

            s.Attempts++;

            if (s.IsGuessMode)
            {
                if (!int.TryParse(value.Trim(), out int g) || g < 1 || g > 100)
                    return new ManualAnswerResponse { IsCorrect = false, Message = "Enter a number between 1 and 100.", Feedback = "invalid" };

                if (g == s.Target) { s.ManualSolved = true; return new ManualAnswerResponse { IsCorrect = true, Message = "Correct! Well done.", Feedback = "correct" }; }
                var fb = g < s.Target ? "too_low" : "too_high";
                return new ManualAnswerResponse { IsCorrect = false, Message = g < s.Target ? "Too low! Try a higher number." : "Too high! Try a lower number.", Feedback = fb };
            }
            else
            {
                var parsed = ParseRaw(value);
                bool ok = s.Validate?.Invoke(parsed) ?? false;
                if (ok) s.ManualSolved = true;
                return new ManualAnswerResponse { IsCorrect = ok, Message = ok ? "Correct! Great work." : "Not quite. Try again.", Feedback = ok ? "correct" : "wrong" };
            }
        }

        public CodeResponse ExecuteCode(int problemId, string code)
        {
            if (!_sessions.TryGetValue(problemId, out var s))
                return new CodeResponse { Error = "Problem not found.", IsSuccess = false };

            var result = s.IsGuessMode ? RunGuessMode(s.Target, code) : RunAnswerMode(s, code);

            if (result.IsSuccess && result.IsCorrect == true)
                s.CodeSolved = true;

            if (result.IsSuccess)
                result.TimeComplexity = AnalyzeComplexity(code);

            return result;
        }

        public List<string>? GetHints(int problemId)
        {
            if (!_sessions.TryGetValue(problemId, out var s)) return null;
            return problemId switch
            {
                1 or 2 => GuessHints(s.Target),
                3 => new List<string> { $"The array has {ArrLen(s, "arr")} elements", $"Values range from {ArrMin(s, "arr")} to {ArrMax(s, "arr")}" },
                4 => new List<string> { $"The array has {ArrLen(s, "arr")} elements", $"Values range from {ArrMin(s, "arr")} to {ArrMax(s, "arr")}" },
                5 => new List<string> { "Try a nested loop to check every pair of indices", $"The target sum is {s.Data["target"]}", $"The array has {ArrLen(s, "arr")} elements" },
                6 => new List<string> { "Compare the string with its reverse", $"The string \"{s.Data["s"]}\" has {((string)s.Data["s"]).Length} characters" },
                7 => new List<string> { "Build the result by prepending each character", $"Original string: \"{s.Data["s"]}\"" },
                8 => new List<string> { "Vowels are: a, e, i, o, u (both upper and lower case)", $"The string is: \"{s.Data["s"]}\"" },
                9 => new List<string> { "Compare each adjacent pair and swap if the left is larger", "You need one full pass per element (n passes total)" },
                10 => new List<string> { $"You are counting how many times {s.Data["target"]} appears", "A simple loop and counter is enough" },
                _ => new List<string>()
            };
        }

        public void ResetAll() => InitSessions();

        // ── Execution ─────────────────────────────────────────────────────────

        private CodeResponse RunGuessMode(int target, string code)
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            bool solved = false; int guessCount = 0;

            Func<object, string> guessFunc = obj =>
            {
                if (guessCount >= 200) throw new Exception("Exceeded 200 guesses — check for infinite loops.");
                int g; try { g = Convert.ToInt32(obj); } catch { throw new Exception($"guess() expects an integer."); }
                if (g < 1 || g > 100) throw new Exception($"guess() argument must be 1–100, got: {g}");
                guessCount++;
                if (g == target) { solved = true; return "correct"; }
                return g < target ? "too_low" : "too_high";
            };
            scope.SetVariable("guess", guessFunc);

            return Capture(engine, scope, code, () => solved, () => guessCount);
        }

        private CodeResponse RunAnswerMode(ProblemSession session, string code)
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            bool answered = false, isCorrect = false;

            Action<object?> answerFunc = obj =>
            {
                if (answered) throw new Exception("answer() can only be called once.");
                answered = true;
                isCorrect = session.Validate?.Invoke(obj) ?? false;
            };
            scope.SetVariable("answer", answerFunc);

            // Inject problem data — always fresh Python lists so in-place mutation is safe
            foreach (var (key, val) in session.Data)
                scope.SetVariable(key, val is int[] arr ? ToPyList(engine, arr) : val);

            return Capture(engine, scope, code, () => isCorrect, () => 0);
        }

        private static CodeResponse Capture(ScriptEngine engine, ScriptScope scope, string code,
            Func<bool> isCorrect, Func<int> guessCount)
        {
            var ms = new MemoryStream();
            var w = new StreamWriter(ms, Encoding.UTF8) { AutoFlush = true };
            engine.Runtime.IO.SetOutput(ms, w);
            engine.Runtime.IO.SetErrorOutput(ms, w);

            Exception? err = null;
            var t = new Thread(() => { try { engine.Execute(code, scope); } catch (Exception ex) { err = ex; } });
            t.IsBackground = true; t.Start();

            if (!t.Join(TimeSpan.FromSeconds(5)))
                return new CodeResponse { Error = "Execution timed out (5 second limit). Check for infinite loops.", IsSuccess = false };

            w.Flush();
            var output = Encoding.UTF8.GetString(ms.ToArray()).TrimStart('﻿').TrimEnd();
            ms.Dispose();

            if (err != null)
                return new CodeResponse { Output = output, Error = FmtErr(err), IsSuccess = false, GuessCount = guessCount() };

            return new CodeResponse
            {
                Output = string.IsNullOrWhiteSpace(output) ? "(no output)" : output,
                IsSuccess = true, IsCorrect = isCorrect(), GuessCount = guessCount()
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Problem Enrich(Problem def)
        {
            _sessions.TryGetValue(def.ProblemId, out var s);
            return def with
            {
                IsSolved = s != null && (s.ManualSolved || s.CodeSolved),
                ProblemData = BuildVisibleData(def.ProblemId, s)
            };
        }

        private static object? BuildVisibleData(int id, ProblemSession? s)
        {
            if (s == null) return null;
            return id switch
            {
                1 or 2 => new { type = "guess", range = "1 to 100" },
                3 or 4 or 9 => new { type = "answer", arr = (int[])s.Data["arr"] },
                5 => new { type = "answer", arr = (int[])s.Data["arr"], target = s.Data["target"] },
                6 or 7 or 8 => new { type = "answer", s = s.Data["s"] },
                10 => new { type = "answer", arr = (int[])s.Data["arr"], target = s.Data["target"] },
                _ => null
            };
        }

        private static List<string> GuessHints(int n) =>
            new()
            {
                n % 2 == 0 ? "The number is even" : "The number is odd",
                n > 50 ? "The number is greater than 50" : "The number is 50 or less",
                n % 5 == 0 ? "The number is divisible by 5" : "The number is not divisible by 5",
                n % 3 == 0 ? "The number is divisible by 3" : "The number is not divisible by 3",
            };

        private int[] RandArr(int len, int min, int max) =>
            Enumerable.Range(0, len).Select(_ => _rng.Next(min, max + 1)).ToArray();

        private (int, int) TwoDistinctIdx(int len)
        {
            int a = _rng.Next(len), b;
            do { b = _rng.Next(len); } while (b == a);
            return (a, b);
        }

        // Build a real Python list via the engine — avoids any IronPython internal type dependencies
        private static object ToPyList(ScriptEngine engine, int[] arr)
        {
            var tmp = engine.CreateScope();
            tmp.SetVariable("_d", arr);
            engine.Execute("_r = list(_d)", tmp);
            return tmp.GetVariable("_r");
        }

        private static int SafeInt(object? o) { try { return Convert.ToInt32(o); } catch { return int.MinValue; } }

        private static int[] SafeIntArr(object? o)
        {
            try
            {
                if (o is int[] intArr) return intArr;
                if (o is System.Collections.IList list)
                    return list.Cast<object>().Select(x => Convert.ToInt32(x)).ToArray();
                if (o is System.Collections.IEnumerable ie and not string)
                    return ie.Cast<object>().Select(x => Convert.ToInt32(x)).ToArray();
                var s = o?.ToString()?.Trim().TrimStart('[').TrimEnd(']') ?? "";
                return s.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
            }
            catch { return Array.Empty<int>(); }
        }

        private static object? ParseRaw(string s)
        {
            s = s.Trim();
            if (s.Equals("true",  StringComparison.OrdinalIgnoreCase)) return true;
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            if (int.TryParse(s, out int i)) return i;
            var inner = s.TrimStart('[').TrimEnd(']');
            if (inner.Contains(','))
                try { return inner.Split(',').Select(x => int.Parse(x.Trim())).ToArray(); } catch { }
            return s;
        }

        private static string FmtErr(Exception ex)
        {
            if (ex is Microsoft.Scripting.SyntaxErrorException syn) return $"SyntaxError (line {syn.Line}): {syn.Message}";
            return ex.InnerException != null ? $"{ex.Message}\n{ex.InnerException.Message}" : ex.Message;
        }

        private static string AnalyzeComplexity(string code)
        {
            var c = code.ToLower();
            if (c.Contains("mid") && c.Contains("high") && c.Contains("low") && c.Contains("while") && (c.Contains("// 2") || c.Contains("//2")))
                return "O(log n) — Binary Search";
            if (c.Contains("for") || c.Contains("while"))
            {
                int loops = Regex.Matches(c, @"for\s+\w+\s+in").Count + Regex.Matches(c, @"while\s+[^:]+:").Count;
                return loops > 1 ? "O(n²) — Nested loops" : "O(n) — Linear";
            }
            return "O(1) — Constant time";
        }

        private int ArrLen(ProblemSession s, string key) => ((int[])s.Data[key]).Length;
        private int ArrMin(ProblemSession s, string key) => ((int[])s.Data[key]).Min();
        private int ArrMax(ProblemSession s, string key) => ((int[])s.Data[key]).Max();
    }
}
