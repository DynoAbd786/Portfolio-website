namespace website.Data
{
    // Where project data comes from. Pages render on the server, so the host
    // supplies an implementation that reads the API's data directly.
    public interface IProjectSource
    {
        Task<List<Project>> GetProjectsAsync();
    }
}
