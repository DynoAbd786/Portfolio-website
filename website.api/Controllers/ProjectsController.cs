using Microsoft.AspNetCore.Mvc;
using website.api.Models;
using website.api.Services;

namespace website.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Project>>> GetProjects()
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("by-url")]
        public async Task<ActionResult<Project>> GetProjectByUrl([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest("URL parameter is required");
                }

                var project = await _projectService.GetProjectByUrlAsync(url);

                if (project == null)
                {
                    return NotFound($"Project with URL '{url}' not found");
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("categories/{category}")]
        public async Task<ActionResult<List<Project>>> GetProjectsByCategory(ProjectCategory category)
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                var filteredProjects = projects.Where(p => p.Category == category).ToList();
                return Ok(filteredProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}