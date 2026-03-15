using DeepSpace.Core.Entities;
using DeepSpace.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DeepSpace.Web.Pages.Admin;

public class SettingsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private static readonly HashSet<string> AllowedFaviconExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ico",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private const long MaxFaviconSizeBytes = 1024 * 1024;

    public SettingsModel(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public SettingsInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? FaviconFile { get; set; }

    public string CurrentFaviconPath { get; set; } = "/favicon.ico";

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await GetOrCreateSettingsAsync(createIfMissing: false);
        ApplySettingsToInput(settings ?? new SiteSettings());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateFaviconFile();

        if (!ModelState.IsValid)
        {
            var currentSettings = await GetOrCreateSettingsAsync(createIfMissing: false);
            CurrentFaviconPath = string.IsNullOrWhiteSpace(currentSettings?.FaviconPath)
                ? "/favicon.ico"
                : currentSettings.FaviconPath!;
            return Page();
        }

        var settings = await GetOrCreateSettingsAsync(createIfMissing: true) ?? new SiteSettings();

        settings.SiteTitle = Input.SiteTitle.Trim();
        settings.SeoDescription = Input.SeoDescription.Trim();
        settings.SeoKeywords = Input.SeoKeywords.Trim();
        settings.HomePageH1 = Input.HomePageH1.Trim();
        settings.HomePageSubtitle = Input.HomePageSubtitle.Trim();

        if (FaviconFile is { Length: > 0 })
        {
            settings.FaviconPath = await SaveFaviconAsync(FaviconFile, settings.FaviconPath);
        }

        await _context.SaveChangesAsync();

        StatusMessage = "Ustawienia zapisane.";
        return RedirectToPage();
    }

    private async Task<SiteSettings?> GetOrCreateSettingsAsync(bool createIfMissing)
    {
        var settings = await _context.SiteSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();

        if (settings != null || !createIfMissing)
        {
            return settings;
        }

        settings = new SiteSettings();
        _context.SiteSettings.Add(settings);
        return settings;
    }

    private void ApplySettingsToInput(SiteSettings settings)
    {
        Input = new SettingsInputModel
        {
            SiteTitle = settings.SiteTitle,
            SeoDescription = settings.SeoDescription,
            SeoKeywords = settings.SeoKeywords,
            HomePageH1 = settings.HomePageH1,
            HomePageSubtitle = settings.HomePageSubtitle
        };

        CurrentFaviconPath = string.IsNullOrWhiteSpace(settings.FaviconPath)
            ? "/favicon.ico"
            : settings.FaviconPath;
    }

    private void ValidateFaviconFile()
    {
        if (FaviconFile is not { Length: > 0 })
        {
            return;
        }

        var extension = Path.GetExtension(FaviconFile.FileName);
        if (!AllowedFaviconExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(FaviconFile),
                "Dozwolone typy favicon: .ico, .png, .jpg, .jpeg, .webp.");
        }

        if (FaviconFile.Length > MaxFaviconSizeBytes)
        {
            ModelState.AddModelError(nameof(FaviconFile),
                "Plik favicon jest za duży. Maksymalny rozmiar to 1 MB.");
        }
    }

    private async Task<string> SaveFaviconAsync(IFormFile faviconFile, string? previousFaviconPath)
    {
        var extension = Path.GetExtension(faviconFile.FileName).ToLowerInvariant();
        var fileName = $"favicon-{Guid.NewGuid():N}{extension}";

        var faviconDirectory = Path.Combine(_environment.WebRootPath, "uploads", "favicons");
        Directory.CreateDirectory(faviconDirectory);

        var destinationPath = Path.Combine(faviconDirectory, fileName);
        await using (var stream = System.IO.File.Create(destinationPath))
        {
            await faviconFile.CopyToAsync(stream);
        }

        DeletePreviousFavicon(previousFaviconPath, faviconDirectory);

        return $"/uploads/favicons/{fileName}";
    }

    private static void DeletePreviousFavicon(string? previousFaviconPath, string faviconDirectory)
    {
        if (string.IsNullOrWhiteSpace(previousFaviconPath) ||
            !previousFaviconPath.StartsWith("/uploads/favicons/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var previousFileName = Path.GetFileName(previousFaviconPath);
        var previousFilePath = Path.Combine(faviconDirectory, previousFileName);

        if (System.IO.File.Exists(previousFilePath))
        {
            System.IO.File.Delete(previousFilePath);
        }
    }

    public class SettingsInputModel
    {
        [Required]
        [StringLength(120)]
        public string SiteTitle { get; set; } = "Deep Space";

        [Required]
        [StringLength(320)]
        public string SeoDescription { get; set; } =
            "Deep Space: astronomy, physics, and space exploration news based on verified sources.";

        [Required]
        [StringLength(320)]
        public string SeoKeywords { get; set; } =
            "space, physics, astronomy, kosmos, astrofizyka, deep space";

        [Required]
        [StringLength(120)]
        public string HomePageH1 { get; set; } = "Deep Space";

        [Required]
        [StringLength(220)]
        public string HomePageSubtitle { get; set; } =
            "Astronomia i fizyka bez clickbaitu. Tylko potwierdzone fakty.";
    }
}
