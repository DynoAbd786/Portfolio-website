namespace website.Data
{
    // One slide of an ImageGallery. Description may contain inline HTML (authored in the page).
    public class GalleryItem
    {
        public string? ImagePath { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
