namespace DeepSpace.Core.Entities;

public class SiteSettings
{
    public int Id { get; set; }
    public string SiteTitle { get; set; } = "Deep Space";
    public string SeoDescription { get; set; } =
        "Deep Space: astronomy, physics, and space exploration news based on verified sources.";
    public string SeoKeywords { get; set; } =
        "space, physics, astronomy, kosmos, astrofizyka, deep space";
    public string? FaviconPath { get; set; }
    public string HomePageH1 { get; set; } = "Deep Space";
    public string HomePageSubtitle { get; set; } =
        "Astronomia i fizyka bez clickbaitu. Tylko potwierdzone fakty.";
}
