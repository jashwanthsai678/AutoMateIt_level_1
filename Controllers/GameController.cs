using Microsoft.AspNetCore.Mvc;
using NumberGuessingApp.Models;
using NumberGuessingApp.Services;

namespace NumberGuessingApp.Controllers
{
    [ApiController]
    [Route("api/problems")]
    public class GameController : ControllerBase
    {
        private readonly IProblemService _svc;
        public GameController(IProblemService svc) => _svc = svc;

        [HttpGet("{problemId}")]
        public IActionResult GetProblem(int problemId)
        {
            var p = _svc.GetProblem(problemId);
            if (p == null) return NotFound(new { message = $"Problem {problemId} not found." });
            return Ok(p);
        }

        [HttpPost("{problemId}/manual")]
        public IActionResult CheckManual(int problemId, [FromBody] ManualAnswerRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Value))
                return BadRequest(new { message = "Value is required." });
            return Ok(_svc.CheckManualAnswer(problemId, req.Value));
        }

        [HttpPost("{problemId}/code")]
        public IActionResult RunCode(int problemId, [FromBody] CodeRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Code))
                return BadRequest(new { message = "Code cannot be empty." });
            return Ok(_svc.ExecuteCode(problemId, req.Code));
        }

        [HttpGet("{problemId}/hints")]
        public IActionResult GetHints(int problemId)
        {
            var hints = _svc.GetHints(problemId);
            if (hints == null) return NotFound(new { message = $"Problem {problemId} not found." });
            return Ok(new { hints });
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _svc.ResetAll();
            return Ok(new { message = "All problems reset." });
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "OK", timestamp = DateTime.UtcNow });
    }
}
