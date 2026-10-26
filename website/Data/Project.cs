namespace website.Data
{
    public enum ProjectCategory
    {
        Personal,
        Professional,
        Academic
    }

    // The kind of work, for filtering; a project can sit in several. Order is the filter order.
    public enum ProjectField
    {
        AiMl,
        VisionGraphics,
        ScientificBiomedical,
        CloudDistributed,
        SystemsInfrastructure,
        ProductBusiness
    }

    public class Project
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ProjectUrl { get; set; } = "";
        public string GitHubUrl { get; set; } = "";
        public string[] Tags { get; set; } = System.Array.Empty<string>();
        public ProjectCategory Category { get; set; } = ProjectCategory.Personal;
        public bool IsFeatured { get; set; } = false;
        public bool IsInDevelopment { get; set; } = false;
        public ProjectField[] Fields { get; set; } = System.Array.Empty<ProjectField>();
        // One sentence on what was built or achieved
        public string Result { get; set; } = "";
    }
}
