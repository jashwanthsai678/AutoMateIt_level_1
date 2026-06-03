using Microsoft.AspNetCore.Mvc;
using NumberGuessingApp.Services;

namespace NumberGuessingApp.Controllers
{
    [ApiController]
    [Route("api/topics")]
    public class TopicsController : ControllerBase
    {
        private readonly IProblemService _svc;
        public TopicsController(IProblemService svc) => _svc = svc;

        [HttpGet]
        public IActionResult GetTopics() => Ok(_svc.GetTopics());

        [HttpGet("{topicId}/problems")]
        public IActionResult GetProblems(int topicId)
        {
            var problems = _svc.GetProblemsForTopic(topicId);
            if (!problems.Any()) return NotFound(new { message = $"Topic {topicId} not found or has no problems." });
            return Ok(problems);
        }
    }
}
