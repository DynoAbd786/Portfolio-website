using ClientData = website.Data;

namespace website.api.Services
{
    // Feeds server-rendered pages straight from IProjectService, so a page
    // render doesn't make an HTTP call back to this same server.
    public class ApiProjectSource : ClientData.IProjectSource
    {
        private readonly IProjectService _projectService;

        public ApiProjectSource(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<List<ClientData.Project>> GetProjectsAsync()
        {
            var projects = await _projectService.GetProjectsAsync();
            return projects.Select(p => new ClientData.Project
            {
                Title = p.Title,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                ProjectUrl = p.ProjectUrl,
                GitHubUrl = p.GitHubUrl,
                Tags = p.Tags,
                Category = (ClientData.ProjectCategory)p.Category,
                IsFeatured = p.IsFeatured,
                IsInDevelopment = p.IsInDevelopment
            }).ToList();
        }
    }
}
