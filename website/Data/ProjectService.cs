namespace website.Data
{
    public class ProjectService
    {
        private readonly IProjectSource _source;

        public ProjectService(IProjectSource source)
        {
            _source = source;
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                var projects = await _source.GetProjectsAsync();
                // Filter out any null entries
                return projects.Where(p => p != null).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectService] Error loading projects: {ex.GetType().Name} - {ex.Message}");
                return new List<Project>();
            }
        }

        public async Task<Project?> GetProjectByUrlAsync(string url)
        {
            var projects = await GetProjectsAsync();
            return projects.FirstOrDefault(p => p.ProjectUrl == url);
        }

        // The project after this one in data order, wrapping round to the first
        public static Project GetNextProject(IReadOnlyList<Project> all, string url)
        {
            var index = all.ToList().FindIndex(p => p.ProjectUrl == url);
            return all[(index + 1) % all.Count];
        }

        public async Task<List<Project>> GetFeaturedProjectsAsync()
        {
            var allProjects = await GetProjectsAsync();

            // Get featured projects from each category (up to 6 per category)
            var featuredProjects = new List<Project>();

            // Featured Personal Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p != null && p.Category == ProjectCategory.Personal && p.IsFeatured)
                .Take(6));

            // Featured Professional Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p != null && p.Category == ProjectCategory.Professional && p.IsFeatured)
                .Take(6));

            // Featured Academic Projects
            featuredProjects.AddRange(allProjects
                .Where(p => p != null && p.Category == ProjectCategory.Academic && p.IsFeatured)
                .Take(6));

            return featuredProjects;
        }
    }
}
