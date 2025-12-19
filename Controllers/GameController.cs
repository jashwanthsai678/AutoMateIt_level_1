using Microsoft.AspNetCore.Mvc;
using NumberGuessingApp.Models;
using NumberGuessingApp.Services;

namespace NumberGuessingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpGet("levels")]
        public IActionResult GetLevels()
        {
            var levels = _gameService.GetAllLevels();
            return Ok(levels);
        }

        [HttpGet("level/{id}")]
        public IActionResult GetLevel(int id)
        {
            var level = _gameService.GetLevel(id);
            if (level == null)
                return NotFound(new { message = $"Level {id} not found" });
            return Ok(level);
        }

        [HttpGet("progress")]
        public IActionResult GetProgress()
        {
            var progress = _gameService.GetProgress();
            return Ok(progress);
        }

        [HttpPost("guess")]
        public IActionResult CheckGuess([FromBody] GuessRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid request" });

            var response = _gameService.CheckGuess(request.LevelId, request.Guess);
            return Ok(response);
        }

        [HttpPost("execute")]
        public IActionResult ExecuteCode([FromBody] ExecuteCodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { message = "Code cannot be empty" });

            var response = _gameService.ExecutePythonCode(request.LevelId, request.Code);
            return Ok(response);
        }

        [HttpPost("reset")]
        public IActionResult ResetGame()
        {
            _gameService.ResetGame();
            return Ok(new { message = "Game reset successfully" });
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "OK",
                timestamp = DateTime.UtcNow,
                message = "Number Guessing API is running"
            });
        }
    }
}