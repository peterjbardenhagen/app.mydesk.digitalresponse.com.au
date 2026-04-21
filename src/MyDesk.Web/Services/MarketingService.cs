using Microsoft.Extensions.Options;

namespace MyDesk.Web.Services;

public class MarketingOptions
{
    public string AssetsRoot         { get; set; } = "";
    public string CompanyProfilesRoot { get; set; } = "";
    public string WebsiteUrl         { get; set; } = "";
    public string CompanyName        { get; set; } = "Techlight";
}

public class MarketingAsset
{
    public string Key         { get; set; } = "";     // logical id used in URLs
    public string Title       { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Icon        { get; set; } = "";     // MudBlazor icon
    public string? PreviewUrl { get; set; }           // for images rendered in-page
    public List<MarketingFile> Files { get; set; } = new();
}

public class MarketingFile
{
    public string Label    { get; set; } = "";       // e.g. "SVG", "PNG (2x)"
    public string Extension { get; set; } = "";
    public string FullPath { get; set; } = "";
    public long   SizeBytes { get; set; }
    public string FormattedSize => SizeBytes switch
    {
        >= 1_048_576 => $"{SizeBytes / 1_048_576.0:N1} MB",
        >= 1024       => $"{SizeBytes / 1024.0:N0} KB",
        _             => $"{SizeBytes} B"
    };
}

/// <summary>
/// Curates Techlight's marketing assets (logos, brand guidelines, company profile).
/// Files are served through a secure endpoint — not exposed directly.
/// </summary>
public class MarketingService
{
    private readonly MarketingOptions _opts;
    private readonly ILogger<MarketingService> _logger;

    public MarketingService(IOptions<MarketingOptions> opts, ILogger<MarketingService> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    public string WebsiteUrl => _opts.WebsiteUrl;
    public string CompanyName => _opts.CompanyName;

    public List<MarketingAsset> GetAssets()
    {
        var assets = new List<MarketingAsset>();
        var brand = Path.Combine(_opts.AssetsRoot, "images", "brand");

        // ── Logos ────────────────────────────────────────────────────────────
        assets.Add(BuildAsset(
            key: "logo-full-dark",
            title: "Primary Logo — Dark",
            description: "Full logo lockup on light backgrounds. Use this for most applications.",
            category: "Brand Identity",
            icon: "Bookmarks",
            previewRelative: "/images/brand/techlight-logo-full-dark.svg",
            files: new[]
            {
                (Label: "SVG (vector)",  Path: Path.Combine(brand, "techlight-logo-full-dark.svg")),
                (Label: "PNG (high-res)", Path: Path.Combine(brand, "techlight-logo-full-dark.png")),
            }));

        assets.Add(BuildAsset(
            key: "logo-full-light",
            title: "Primary Logo — Light",
            description: "Full logo lockup on dark backgrounds.",
            category: "Brand Identity",
            icon: "Bookmarks",
            previewRelative: "/images/brand/techlight-logo-full-light.svg",
            files: new[]
            {
                (Label: "SVG (vector)",  Path: Path.Combine(brand, "techlight-logo-full-light.svg")),
                (Label: "PNG (high-res)", Path: Path.Combine(brand, "techlight-logo-full-light.png")),
            }));

        assets.Add(BuildAsset(
            key: "logomark-dark",
            title: "Logomark — Dark",
            description: "Icon-only version for favicons, avatars, and small spaces.",
            category: "Brand Identity",
            icon: "Adjust",
            previewRelative: "/images/brand/techlight-logomark-dark.svg",
            files: new[]
            {
                (Label: "SVG (vector)",  Path: Path.Combine(brand, "techlight-logomark-dark.svg")),
                (Label: "PNG (high-res)", Path: Path.Combine(brand, "techlight-logomark-dark.png")),
            }));

        assets.Add(BuildAsset(
            key: "logomark-light",
            title: "Logomark — Light",
            description: "Icon-only version on dark backgrounds.",
            category: "Brand Identity",
            icon: "Adjust",
            previewRelative: "/images/brand/techlight-logomark-light.svg",
            files: new[]
            {
                (Label: "SVG (vector)",  Path: Path.Combine(brand, "techlight-logomark-light.svg")),
                (Label: "PNG (high-res)", Path: Path.Combine(brand, "techlight-logomark-light.png")),
            }));

        // ── Brand Guidelines ─────────────────────────────────────────────────
        assets.Add(BuildAsset(
            key: "brand-guidelines",
            title: "Brand Guidelines",
            description: "Complete visual identity standards: colours, typography, logo usage.",
            category: "Brand Guidelines",
            icon: "MenuBook",
            files: new[]
            {
                (Label: "HTML",
                 Path: Path.Combine(_opts.AssetsRoot, "techlight-branding.html"))
            }));

        // ── Company Profile ──────────────────────────────────────────────────
        assets.Add(BuildAsset(
            key: "company-profile",
            title: "Company Profile",
            description: "Official corporate profile — who we are, what we do, key projects.",
            category: "Corporate",
            icon: "Article",
            files: new[]
            {
                (Label: "PDF",
                 Path: Path.Combine(_opts.CompanyProfilesRoot, "Company Profile _ Techlight _ PDF.pdf")),
                (Label: "Word",
                 Path: Path.Combine(_opts.CompanyProfilesRoot, "Company Profile _ Techlight _ WORD DOCUMENT.doc")),
            }));

        assets.Add(BuildAsset(
            key: "company-profile-html",
            title: "Interactive Company Profile",
            description: "Web-version of the corporate profile for sharing via link.",
            category: "Corporate",
            icon: "Language",
            files: new[]
            {
                (Label: "HTML",
                 Path: Path.Combine(_opts.AssetsRoot, "techlight-company-profile.html"))
            }));

        assets.Add(BuildAsset(
            key: "project-references",
            title: "Project References",
            description: "Notable past projects and case studies.",
            category: "Corporate",
            icon: "Assignment",
            files: new[]
            {
                (Label: "PDF",
                 Path: Path.Combine(_opts.CompanyProfilesRoot, "Techlight Company Profile Project References.pdf")),
            }));

        // ── Email Signatures ─────────────────────────────────────────────────
        for (int i = 1; i <= 3; i++)
        {
            var num = i.ToString("D2");
            assets.Add(BuildAsset(
                key: $"email-signature-{num}",
                title: $"Email Signature — Style {i}",
                description: "HTML email signature template. Open, copy, and paste into your email client.",
                category: "Stationery",
                icon: "Email",
                files: new[]
                {
                    (Label: "HTML",
                     Path: Path.Combine(_opts.AssetsRoot, $"email-signature-{num}.html")),
                }));
        }

        return assets
            .Where(a => a.Files.Any())   // only show assets whose files exist
            .ToList();
    }

    public MarketingAsset? GetAsset(string key) =>
        GetAssets().FirstOrDefault(a => a.Key == key);

    public MarketingFile? GetFile(string assetKey, string label)
    {
        var asset = GetAsset(assetKey);
        return asset?.Files.FirstOrDefault(f =>
            string.Equals(f.Label, label, StringComparison.OrdinalIgnoreCase));
    }

    private MarketingAsset BuildAsset(
        string key, string title, string description, string category,
        string icon, (string Label, string Path)[] files,
        string? previewRelative = null)
    {
        var asset = new MarketingAsset
        {
            Key = key,
            Title = title,
            Description = description,
            Category = category,
            Icon = icon,
            PreviewUrl = previewRelative != null
                ? $"/api/marketing/preview?path={Uri.EscapeDataString(previewRelative)}"
                : null
        };

        foreach (var (label, fullPath) in files)
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogDebug("Marketing file not found: {Path}", fullPath);
                continue;
            }
            var info = new FileInfo(fullPath);
            asset.Files.Add(new MarketingFile
            {
                Label = label,
                Extension = info.Extension.TrimStart('.').ToUpperInvariant(),
                FullPath = fullPath,
                SizeBytes = info.Length
            });
        }
        return asset;
    }
}
