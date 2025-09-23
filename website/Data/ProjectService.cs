using System.Net.Http.Json;

namespace website.Data
{
    public class ProjectService
    {
        private readonly HttpClient _httpClient;

        public ProjectService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
                return response ?? new List<Project>();
            }
            catch
            {
                // Fallback to empty list if API is unavailable
                return new List<Project>();
            }
        }

        public async Task<List<Project>> GetFeaturedProjectsAsync()
        {
            var allProjects = await GetProjectsAsync();

            // Get featured projects from each category (up to 6 per category)
            var featuredProjects = new List<Project>();

            // Featured Personal Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p.Category == ProjectCategory.Personal && p.IsFeatured)
                .Take(6));

            // Featured Professional Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p.Category == ProjectCategory.Professional && p.IsFeatured)
                .Take(6));

            // Featured Academic Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p.Category == ProjectCategory.Academic && p.IsFeatured)
                .Take(6));

            return featuredProjects;
        }
    }
}