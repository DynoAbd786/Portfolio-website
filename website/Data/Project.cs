namespace website.Data
{
    public class Project
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ProjectUrl { get; set; } = "";
        public string[] Tags { get; set; } = System.Array.Empty<string>();
    }
}
