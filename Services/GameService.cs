using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using NumberGuessingApp.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace NumberGuessingApp.Services
{
    internal class ProblemSession
    {
        public int ProblemId { get; init; }
        public bool IsGuessMode { get; init; }
        public int Target { get; init; }
        public Dictionary<string, object> Data { get; init; } = new();
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

        // ── Topic Catalog ─────────────────────────────────────────────────────
        private static readonly List<Topic> TopicCatalog = new()
        {
            new Topic { TopicId=1, Name="Searching",    Icon="🔍", IsAvailable=true,  Description="Linear search, binary search and beyond" },
            new Topic { TopicId=2, Name="Arrays",       Icon="📊", IsAvailable=true,  Description="Traversal, find, and manipulation problems" },
            new Topic { TopicId=3, Name="Strings",      Icon="🔤", IsAvailable=true,  Description="String operations and pattern recognition" },
            new Topic { TopicId=4, Name="Sorting",      Icon="🔀", IsAvailable=true,  Description="Bubble, insertion, merge and more" },
            new Topic { TopicId=5, Name="Linked Lists", Icon="🔗", IsAvailable=false, Description="Node traversal and pointer manipulation" },
            new Topic { TopicId=6, Name="Trees",        Icon="🌳", IsAvailable=false, Description="BST, DFS, BFS and tree traversals" },
            new Topic { TopicId=7, Name="Graphs",       Icon="🕸️", IsAvailable=false, Description="DFS, BFS and shortest path algorithms" },
            new Topic { TopicId=8, Name="Tries",        Icon="🌲", IsAvailable=false, Description="Prefix trees and autocomplete" },
        };

        // ── Problem Catalog ───────────────────────────────────────────────────
        private static readonly List<Problem> ProblemCatalog = new()
        {
            // ─ Searching ─────────────────────────────────────────────────────
            new Problem
            {
                ProblemId=1, TopicId=1, Title="The Patient Hunt", Difficulty="Easy",
                Description="A number is hiding. Your only move? Check every possibility, one by one, starting from 1. Slow — but it always works. Can you automate the patience?",
                ManualInstructions="A number is hidden between 1 and 100. Guess numbers starting from 1 and work your way up — no directional hints! Notice how many guesses it takes.",
                Instructions="A number is hidden between 1–100. Call <code>guess(n)</code> — returns <code>'too_low'</code>, <code>'too_high'</code>, or <code>'correct'</code>. Check each number from 1 upward.",
                ExampleCode =
                    "# Linear Search\n" +
                    "for i in range(1, 101):\n" +
                    "    result = guess(i)\n" +
                    "    if result == \"correct\":\n" +
                    "        print(f\"Found the number: {i}\")\n" +
                    "        break\n" +
                    "    else:\n" +
                    "        print(f\"Checking {i}: {result}\")",
                ScaffoldCode =
                    "# Linear Search — fill in the blanks\n" +
                    "for i in range(___):  # TODO: what range to check?\n" +
                    "    result = guess(i)\n" +
                    "    if result == \"correct\":\n" +
                    "        print(f\"Found: {i}\")\n" +
                    "        ___  # TODO: what to do when found?\n" +
                    "    else:\n" +
                    "        print(f\"{i}: {result}\")"
            },
            new Problem
            {
                ProblemId=2, TopicId=1, Title="Half & Conquer", Difficulty="Medium",
                PrerequisiteId=1,
                Description="What if every wrong guess eliminated half the remaining possibilities? 100 candidates, 7 guesses max. Strategy beats brute force — every single time.",
                ManualInstructions="A number is hidden between 1 and 100. After each guess you will know if it is <strong>Too High</strong> or <strong>Too Low</strong>. Use those clues to halve your range each time — can you find it in 7 guesses or fewer?",
                Instructions="A number is hidden between 1–100. Call <code>guess(n)</code> — returns <code>'too_low'</code>, <code>'too_high'</code>, or <code>'correct'</code>. You should find it in ≤ 7 guesses!",
                ExampleCode =
                    "# Binary Search\n" +
                    "low = 1\n" +
                    "high = 100\n" +
                    "attempts = 0\n\n" +
                    "while low <= high:\n" +
                    "    mid = (low + high) // 2\n" +
                    "    attempts += 1\n" +
                    "    result = guess(mid)\n\n" +
                    "    if result == \"correct\":\n" +
                    "        print(f\"Found {mid} in {attempts} attempts!\")\n" +
                    "        break\n" +
                    "    elif result == \"too_low\":\n" +
                    "        low = mid + 1\n" +
                    "        print(f\"{mid}: too low\")\n" +
                    "    else:\n" +
                    "        high = mid - 1\n" +
                    "        print(f\"{mid}: too high\")",
                ScaffoldCode =
                    "# Binary Search — fill in the blanks\n" +
                    "low = 1\n" +
                    "high = 100\n" +
                    "attempts = 0\n\n" +
                    "while low <= high:\n" +
                    "    mid = ___  # TODO: midpoint of low and high?\n" +
                    "    attempts += 1\n" +
                    "    result = guess(mid)\n\n" +
                    "    if result == \"correct\":\n" +
                    "        print(f\"Found {mid} in {attempts} attempts!\")\n" +
                    "        break\n" +
                    "    elif result == \"too_low\":\n" +
                    "        low = ___  # TODO: how does low change?\n" +
                    "    else:\n" +
                    "        high = ___  # TODO: how does high change?"
            },

            // ─ Arrays ─────────────────────────────────────────────────────────
            new Problem
            {
                ProblemId=3, TopicId=2, Title="Crown the Champion", Difficulty="Easy",
                Description="One value in this array stands above all others. Walk through every element — whoever beats the current champion claims the crown. Can your code always find it?",
                Instructions="Array <code>arr</code> is provided. Traverse it and call <code>answer(max_value)</code> with the largest element.",
                ExampleCode =
                    "# Find Maximum\n" +
                    "max_val = arr[0]\n" +
                    "for x in arr:\n" +
                    "    if x > max_val:\n" +
                    "        max_val = x\n\n" +
                    "print(f\"Maximum: {max_val}\")\n" +
                    "answer(max_val)",
                ScaffoldCode =
                    "# Find Maximum — fill in the blank\n" +
                    "max_val = arr[0]\n" +
                    "for x in arr:\n" +
                    "    if ___:  # TODO: when is x the new maximum?\n" +
                    "        max_val = x\n\n" +
                    "print(f\"Maximum: {max_val}\")\n" +
                    "answer(max_val)"
            },
            new Problem
            {
                ProblemId=4, TopicId=2, Title="Find the Underdog", Difficulty="Easy",
                PrerequisiteId=3,
                Description="The smallest value hides quietly in a crowd of numbers. It never stands out — but it's always there. Can you write code that always finds it?",
                Instructions="Array <code>arr</code> is provided. Traverse it and call <code>answer(min_value)</code> with the smallest element.",
                ExampleCode =
                    "# Find Minimum\n" +
                    "min_val = arr[0]\n" +
                    "for x in arr:\n" +
                    "    if x < min_val:\n" +
                    "        min_val = x\n\n" +
                    "print(f\"Minimum: {min_val}\")\n" +
                    "answer(min_val)",
                ScaffoldCode =
                    "# Find Minimum — fill in the blank\n" +
                    "min_val = arr[0]\n" +
                    "for x in arr:\n" +
                    "    if ___:  # TODO: when is x the new minimum?\n" +
                    "        min_val = x\n\n" +
                    "print(f\"Minimum: {min_val}\")\n" +
                    "answer(min_val)"
            },
            new Problem
            {
                ProblemId=5, TopicId=2, Title="The Perfect Pair", Difficulty="Medium",
                PrerequisiteId=4,
                Description="Somewhere in this array, two values secretly add up to a target. They're hiding in plain sight — but you have to check every combination to be sure.",
                Instructions="Array <code>arr</code> and integer <code>target</code> are provided. Find indices <code>i</code> and <code>j</code> where <code>arr[i] + arr[j] == target</code>. Call <code>answer([i, j])</code>.",
                ExampleCode =
                    "# Two Sum\n" +
                    "for i in range(len(arr)):\n" +
                    "    for j in range(i + 1, len(arr)):\n" +
                    "        if arr[i] + arr[j] == target:\n" +
                    "            print(f\"Indices {i} and {j}: {arr[i]} + {arr[j]} = {target}\")\n" +
                    "            answer([i, j])\n" +
                    "            break",
                ScaffoldCode =
                    "# Two Sum — fill in the blanks\n" +
                    "for i in range(len(arr)):\n" +
                    "    for j in range(___, len(arr)):  # TODO: where should j start?\n" +
                    "        if ___:  # TODO: what is the condition to check?\n" +
                    "            print(f\"Found at indices {i} and {j}\")\n" +
                    "            answer([i, j])\n" +
                    "            break"
            },

            // ─ Strings ────────────────────────────────────────────────────────
            new Problem
            {
                ProblemId=6, TopicId=3, Title="Mirror Mirror", Difficulty="Easy",
                Description="Some words are their own reflection — 'racecar', 'level', 'noon'. Others aren't. Your code must tell the difference by comparing the string to itself.",
                Instructions="String <code>s</code> is provided. Call <code>answer(True)</code> if it is a palindrome, <code>answer(False)</code> if not.",
                ExampleCode =
                    "# Palindrome Check\n" +
                    "reversed_s = s[::-1]\n" +
                    "if s == reversed_s:\n" +
                    "    print(f'\"{s}\" is a palindrome')\n" +
                    "    answer(True)\n" +
                    "else:\n" +
                    "    print(f'\"{s}\" is NOT a palindrome')\n" +
                    "    answer(False)",
                ScaffoldCode =
                    "# Palindrome Check — fill in the blank\n" +
                    "reversed_s = ___  # TODO: how do you reverse a string in Python?\n" +
                    "if s == reversed_s:\n" +
                    "    print(f'\"{s}\" is a palindrome')\n" +
                    "    answer(True)\n" +
                    "else:\n" +
                    "    print(f'\"{s}\" is NOT a palindrome')\n" +
                    "    answer(False)"
            },
            new Problem
            {
                ProblemId=7, TopicId=3, Title="Flip It", Difficulty="Easy",
                PrerequisiteId=6,
                Description="Take a word. Build it backwards, one character at a time from the last letter to the first. No shortcuts — every character earns its place.",
                Instructions="String <code>s</code> is provided. Reverse it manually (build character by character) and call <code>answer(reversed_string)</code>.",
                ExampleCode =
                    "# Reverse a String\n" +
                    "reversed_s = \"\"\n" +
                    "for ch in s:\n" +
                    "    reversed_s = ch + reversed_s\n\n" +
                    "print(f\"Original: {s}\")\n" +
                    "print(f\"Reversed: {reversed_s}\")\n" +
                    "answer(reversed_s)",
                ScaffoldCode =
                    "# Reverse a String — fill in the blank\n" +
                    "reversed_s = \"\"\n" +
                    "for ch in s:\n" +
                    "    reversed_s = ___ + reversed_s  # TODO: what goes in front?\n\n" +
                    "print(f\"Reversed: {reversed_s}\")\n" +
                    "answer(reversed_s)"
            },
            new Problem
            {
                ProblemId=8, TopicId=3, Title="The Vowel Census", Difficulty="Easy",
                PrerequisiteId=7,
                Description="Every string hides vowels among its characters. Walk through each one, check if it belongs, and count the ones that do. One pass. That's the deal.",
                Instructions="String <code>s</code> is provided. Count vowels (a, e, i, o, u — case-insensitive) and call <code>answer(count)</code>.",
                ExampleCode =
                    "# Count Vowels\n" +
                    "vowels = \"aeiouAEIOU\"\n" +
                    "count = 0\n" +
                    "for ch in s:\n" +
                    "    if ch in vowels:\n" +
                    "        count += 1\n\n" +
                    "print(f\"Vowels in '{s}': {count}\")\n" +
                    "answer(count)",
                ScaffoldCode =
                    "# Count Vowels — fill in the blank\n" +
                    "vowels = \"aeiouAEIOU\"\n" +
                    "count = 0\n" +
                    "for ch in s:\n" +
                    "    if ___:  # TODO: check if ch is a vowel\n" +
                    "        count += 1\n\n" +
                    "print(f\"Vowels in '{s}': {count}\")\n" +
                    "answer(count)"
            },

            // ─ Sorting ────────────────────────────────────────────────────────
            new Problem
            {
                ProblemId=9, TopicId=4, Title="Order from Chaos", Difficulty="Medium",
                Description="A jumbled array needs sorting. Your method: compare neighbors, swap the ones out of order, repeat until nothing needs swapping. Simple rule. Powerful result.",
                Instructions="Array <code>arr</code> is provided (6 elements). Sort it using bubble sort — compare adjacent pairs and swap when left > right. Call <code>answer(arr)</code> with the sorted result.",
                ExampleCode =
                    "# Bubble Sort\n" +
                    "n = len(arr)\n" +
                    "for i in range(n):\n" +
                    "    for j in range(0, n - i - 1):\n" +
                    "        if arr[j] > arr[j + 1]:\n" +
                    "            arr[j], arr[j + 1] = arr[j + 1], arr[j]\n\n" +
                    "print(f\"Sorted: {arr}\")\n" +
                    "answer(arr)",
                ScaffoldCode =
                    "# Bubble Sort — fill in the blanks\n" +
                    "n = len(arr)\n" +
                    "for i in range(n):\n" +
                    "    for j in range(0, ___):  # TODO: what is the inner range?\n" +
                    "        if ___:  # TODO: when should we swap adjacent elements?\n" +
                    "            arr[j], arr[j + 1] = arr[j + 1], arr[j]\n\n" +
                    "print(f\"Sorted: {arr}\")\n" +
                    "answer(arr)"
            },
            new Problem
            {
                ProblemId=10, TopicId=4, Title="The Frequency Counter", Difficulty="Easy",
                PrerequisiteId=9,
                Description="A value appears multiple times across this array — hiding in plain sight. Your job: scan every element and count exactly how many times it shows up.",
                Instructions="Array <code>arr</code> and integer <code>target</code> are provided. Count how many times <code>target</code> appears in <code>arr</code> and call <code>answer(count)</code>.",
                ExampleCode =
                    "# Count Occurrences\n" +
                    "count = 0\n" +
                    "for x in arr:\n" +
                    "    if x == target:\n" +
                    "        count += 1\n\n" +
                    "print(f\"{target} appears {count} time(s) in {arr}\")\n" +
                    "answer(count)",
                ScaffoldCode =
                    "# Count Occurrences — fill in the blank\n" +
                    "count = 0\n" +
                    "for x in arr:\n" +
                    "    if ___:  # TODO: when does x match the target?\n" +
                    "        count += 1\n\n" +
                    "print(f\"{target} appears {count} time(s)\")\n" +
                    "answer(count)"
            },
        };

        public ProblemService() => InitSessions();

        private void InitSessions()
        {
            _sessions = new();

            _sessions[1] = new ProblemSession { ProblemId=1, IsGuessMode=true, Target=_rng.Next(1,101) };
            _sessions[2] = new ProblemSession { ProblemId=2, IsGuessMode=true, Target=_rng.Next(1,101) };

            var a3 = RandArr(10, 10, 99);
            _sessions[3] = new ProblemSession { ProblemId=3, IsGuessMode=false, Data={ ["arr"]=a3 }, Validate=ans=>SafeInt(ans)==a3.Max() };

            var a4 = RandArr(10, 10, 99);
            _sessions[4] = new ProblemSession { ProblemId=4, IsGuessMode=false, Data={ ["arr"]=a4 }, Validate=ans=>SafeInt(ans)==a4.Min() };

            var a5 = RandArr(8, 5, 40);
            var (i5a, i5b) = TwoIdx(a5.Length);
            int tgt5 = a5[i5a] + a5[i5b];
            _sessions[5] = new ProblemSession
            {
                ProblemId=5, IsGuessMode=false, Data={ ["arr"]=a5, ["target"]=tgt5 },
                Validate=ans => { var idx=SafeIntArr(ans); return idx.Length==2 && idx[0]!=idx[1] && idx[0]>=0 && idx[1]>=0 && idx[0]<a5.Length && idx[1]<a5.Length && a5[idx[0]]+a5[idx[1]]==tgt5; }
            };

            bool makePalin = _rng.Next(2)==0;
            string s6 = makePalin
                ? new[]{"racecar","level","noon","deified","civic","radar","kayak"}[_rng.Next(7)]
                : new[]{"hello","world","python","coding","array","stack","queue"}[_rng.Next(7)];
            bool isPalin = s6 == new string(s6.Reverse().ToArray());
            _sessions[6] = new ProblemSession { ProblemId=6, IsGuessMode=false, Data={ ["s"]=s6 }, Validate=ans=>{ var v=ans?.ToString()?.Trim().ToLower(); return v==isPalin.ToString().ToLower()||v==(isPalin?"1":"0"); } };

            string s7 = new[]{"algorithm","binary","search","python","coding","linked","graph"}[_rng.Next(7)];
            string rev7 = new string(s7.Reverse().ToArray());
            _sessions[7] = new ProblemSession { ProblemId=7, IsGuessMode=false, Data={ ["s"]=s7 }, Validate=ans=>ans?.ToString()?.Trim()==rev7 };

            string s8 = new[]{"automation","interface","keyboard","algorithm","structure"}[_rng.Next(5)];
            int vc8 = s8.Count(c=>"aeiou".Contains(c));
            _sessions[8] = new ProblemSession { ProblemId=8, IsGuessMode=false, Data={ ["s"]=s8 }, Validate=ans=>SafeInt(ans)==vc8 };

            // 6 elements for bubble sort — keeps manual simulation to ~15 steps
            var a9 = RandArr(6, 1, 30);
            var sorted9 = a9.OrderBy(x=>x).ToArray();
            _sessions[9] = new ProblemSession { ProblemId=9, IsGuessMode=false, Data={ ["arr"]=a9 }, Validate=ans=>SafeIntArr(ans).SequenceEqual(sorted9) };

            var a10base = RandArr(6, 1, 8);
            int rep10 = a10base[_rng.Next(6)];
            var a10 = a10base.Concat(new[]{rep10,rep10}).OrderBy(_=>_rng.Next()).ToArray();
            int cnt10 = a10.Count(x=>x==rep10);
            _sessions[10] = new ProblemSession { ProblemId=10, IsGuessMode=false, Data={ ["arr"]=a10, ["target"]=rep10 }, Validate=ans=>SafeInt(ans)==cnt10 };
        }

        // ── Public API ────────────────────────────────────────────────────────

        public List<Topic> GetTopics() =>
            TopicCatalog.Select(t =>
            {
                var probs = ProblemCatalog.Where(p => p.TopicId == t.TopicId).ToList();
                int solved = probs.Count(p => _sessions.TryGetValue(p.ProblemId, out var s) && (s.ManualSolved || s.CodeSolved));
                return t with { ProblemCount = probs.Count, SolvedCount = solved };
            }).ToList();

        public List<Problem> GetProblemsForTopic(int topicId) =>
            ProblemCatalog.Where(p => p.TopicId == topicId).Select(Enrich).ToList();

        public Problem? GetProblem(int problemId)
        {
            var def = ProblemCatalog.FirstOrDefault(p => p.ProblemId == problemId);
            return def == null ? null : Enrich(def);
        }

        public ManualAnswerResponse CheckManualAnswer(int problemId, string value)
        {
            if (!_sessions.TryGetValue(problemId, out var s))
                return new ManualAnswerResponse { IsCorrect=false, Message="Problem not found.", Feedback="error" };
            s.Attempts++;
            if (s.IsGuessMode)
            {
                if (!int.TryParse(value.Trim(), out int g) || g < 1 || g > 100)
                    return new ManualAnswerResponse { IsCorrect=false, Message="Enter a number between 1 and 100.", Feedback="invalid" };
                if (g == s.Target) { s.ManualSolved=true; return new ManualAnswerResponse { IsCorrect=true, Message="Correct! Well done.", Feedback="correct" }; }
                var fb = g < s.Target ? "too_low" : "too_high";
                return new ManualAnswerResponse { IsCorrect=false, Message=g<s.Target?"Too low! Try higher.":"Too high! Try lower.", Feedback=fb };
            }
            var parsed = ParseRaw(value);
            bool ok = s.Validate?.Invoke(parsed) ?? false;
            if (ok) s.ManualSolved = true;
            return new ManualAnswerResponse { IsCorrect=ok, Message=ok?"Correct!":"Not quite. Try again.", Feedback=ok?"correct":"wrong" };
        }

        public CodeResponse ExecuteCode(int problemId, string code)
        {
            if (!_sessions.TryGetValue(problemId, out var s))
                return new CodeResponse { Error="Problem not found.", IsSuccess=false };
            var result = s.IsGuessMode ? RunGuessMode(s.Target, code) : RunAnswerMode(s, code);
            if (result.IsSuccess && result.IsCorrect == true) s.CodeSolved = true;
            if (result.IsSuccess) result.TimeComplexity = AnalyzeComplexity(code);
            return result;
        }

        public List<string>? GetHints(int problemId)
        {
            if (!_sessions.TryGetValue(problemId, out var s)) return null;
            return problemId switch
            {
                1 or 2 => GuessHints(s.Target),
                3 => new List<string> { $"The array has {ArrLen(s,"arr")} elements", $"Values range from {ArrMin(s,"arr")} to {ArrMax(s,"arr")}" },
                4 => new List<string> { $"The array has {ArrLen(s,"arr")} elements", $"Values range from {ArrMin(s,"arr")} to {ArrMax(s,"arr")}" },
                5 => new List<string> { "Try checking every pair of indices with a nested loop", $"The target sum is {s.Data["target"]}" },
                6 => new List<string> { "Compare the string with its reverse", $"The string \"{s.Data["s"]}\" has {((string)s.Data["s"]).Length} characters" },
                7 => new List<string> { "Build the result by prepending each character", $"Original: \"{s.Data["s"]}\"" },
                8 => new List<string> { "Vowels are: a, e, i, o, u (upper and lower case)", $"The string is: \"{s.Data["s"]}\"" },
                9 => new List<string> { "Compare each adjacent pair — swap when left > right", "You need one full pass per element (n passes total)" },
                10 => new List<string> { $"You are counting how many times {s.Data["target"]} appears", "A simple loop with a counter is enough" },
                _ => new List<string>()
            };
        }

        public void ResetAll() => InitSessions();

        // ── Execution ─────────────────────────────────────────────────────────

        private CodeResponse RunGuessMode(int target, string code)
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            bool solved = false; int gc = 0;
            Func<object, string> guessFunc = obj =>
            {
                if (gc >= 200) throw new Exception("Exceeded 200 guesses — check for infinite loops.");
                int g; try { g = Convert.ToInt32(obj); } catch { throw new Exception("guess() expects an integer."); }
                if (g < 1 || g > 100) throw new Exception($"guess() argument must be 1–100, got: {g}");
                gc++;
                if (g == target) { solved = true; return "correct"; }
                return g < target ? "too_low" : "too_high";
            };
            scope.SetVariable("guess", guessFunc);
            return Capture(engine, scope, code, () => solved, () => gc);
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
                return new CodeResponse { Error="Execution timed out (5 second limit).", IsSuccess=false };
            w.Flush();
            var output = Encoding.UTF8.GetString(ms.ToArray()).TrimStart('﻿').TrimEnd();
            ms.Dispose();
            if (err != null)
                return new CodeResponse { Output=output, Error=FmtErr(err), IsSuccess=false, GuessCount=guessCount() };
            return new CodeResponse { Output=string.IsNullOrWhiteSpace(output)?"(no output)":output, IsSuccess=true, IsCorrect=isCorrect(), GuessCount=guessCount() };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Problem Enrich(Problem def)
        {
            _sessions.TryGetValue(def.ProblemId, out var s);
            bool isLocked = def.PrerequisiteId.HasValue &&
                (!_sessions.TryGetValue(def.PrerequisiteId.Value, out var pre) || !(pre.ManualSolved || pre.CodeSolved));
            return def with
            {
                IsSolved = s != null && (s.ManualSolved || s.CodeSolved),
                IsLocked = isLocked,
                ProblemData = BuildVisibleData(def.ProblemId, s)
            };
        }

        private static object? BuildVisibleData(int id, ProblemSession? s)
        {
            if (s == null) return null;
            return id switch
            {
                1 or 2 => new { type="guess", range="1 to 100" },
                3 or 4 or 9 => new { type="answer", arr=(int[])s.Data["arr"] },
                5 => new { type="answer", arr=(int[])s.Data["arr"], target=s.Data["target"] },
                6 or 7 or 8 => new { type="answer", s=s.Data["s"] },
                10 => new { type="answer", arr=(int[])s.Data["arr"], target=s.Data["target"] },
                _ => null
            };
        }

        private static List<string> GuessHints(int n) => new()
        {
            n%2==0 ? "The number is even" : "The number is odd",
            n>50 ? "The number is greater than 50" : "The number is 50 or less",
            n%5==0 ? "The number is divisible by 5" : "The number is not divisible by 5",
            n%3==0 ? "The number is divisible by 3" : "The number is not divisible by 3",
        };

        private int[] RandArr(int len, int min, int max) =>
            Enumerable.Range(0, len).Select(_ => _rng.Next(min, max+1)).ToArray();

        private (int, int) TwoIdx(int len)
        {
            int a = _rng.Next(len), b;
            do { b = _rng.Next(len); } while (b == a);
            return (a, b);
        }

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
                if (o is int[] ia) return ia;
                if (o is System.Collections.IList l) return l.Cast<object>().Select(x => Convert.ToInt32(x)).ToArray();
                if (o is System.Collections.IEnumerable ie and not string) return ie.Cast<object>().Select(x => Convert.ToInt32(x)).ToArray();
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
            if (inner.Contains(',')) try { return inner.Split(',').Select(x => int.Parse(x.Trim())).ToArray(); } catch { }
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
            if (c.Contains("mid") && c.Contains("high") && c.Contains("low") && c.Contains("while") && (c.Contains("// 2")||c.Contains("//2")))
                return "O(log n) — Binary Search";
            if (c.Contains("for") || c.Contains("while"))
            {
                int loops = Regex.Matches(c, @"for\s+\w+\s+in").Count + Regex.Matches(c, @"while\s+[^:]+:").Count;
                return loops > 1 ? "O(n²) — Nested loops" : "O(n) — Linear";
            }
            return "O(1) — Constant time";
        }

        private int ArrLen(ProblemSession s, string k) => ((int[])s.Data[k]).Length;
        private int ArrMin(ProblemSession s, string k) => ((int[])s.Data[k]).Min();
        private int ArrMax(ProblemSession s, string k) => ((int[])s.Data[k]).Max();
    }
}
