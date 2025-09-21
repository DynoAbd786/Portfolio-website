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
                var projects = await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
                return projects ?? new List<Project>();
            }
            catch (Exception ex)
            {
                // Log the error and return empty list if API is not available
                Console.WriteLine($"API call failed: {ex.Message}. Make sure the API server is running on the correct port.");
                return new List<Project>();
            }
        }

    }
}
