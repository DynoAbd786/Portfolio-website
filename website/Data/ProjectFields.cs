namespace website.Data
{
    // Display names and URL slugs for ProjectField (used by the Projects page filter)
    public static class ProjectFields
    {
        public static string Label(ProjectField field) => field switch
        {
            ProjectField.AiMl => "AI & ML",
            ProjectField.VisionGraphics => "Computer vision & graphics",
            ProjectField.ScientificBiomedical => "Scientific & biomedical",
            ProjectField.CloudDistributed => "Cloud & distributed",
            ProjectField.SystemsInfrastructure => "Systems & infrastructure",
            ProjectField.ProductBusiness => "Product & business",
            _ => field.ToString()
        };

        public static string Slug(ProjectField field) => field switch
        {
            ProjectField.AiMl => "ai-ml",
            ProjectField.VisionGraphics => "vision-graphics",
            ProjectField.ScientificBiomedical => "scientific-biomedical",
            ProjectField.CloudDistributed => "cloud-distributed",
            ProjectField.SystemsInfrastructure => "systems-infrastructure",
            ProjectField.ProductBusiness => "product-business",
            _ => field.ToString().ToLowerInvariant()
        };

        public static ProjectField? FromSlug(string? slug)
        {
            foreach (var field in Enum.GetValues<ProjectField>())
            {
                if (Slug(field) == slug) return field;
            }
            return null;
        }
    }
}
