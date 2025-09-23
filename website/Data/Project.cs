namespace website.Data
{
    public enum ProjectCategory
    {
        Personal,
        Professional,
        Academic
    }

    public class Project
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ProjectUrl { get; set; } = "";
        public string[] Tags { get; set; } = System.Array.Empty<string>();
        public ProjectCategory Category { get; set; } = ProjectCategory.Personal;
        public bool IsFeatured { get; set; } = false;

        // Keep for backward compatibility
        public bool IsPersonalProject => Category == ProjectCategory.Personal;
    }
}
