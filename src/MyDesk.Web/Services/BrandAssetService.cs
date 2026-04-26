using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;
using System.Text.Json;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for managing brand assets - file uploads, storage, and retrieval
/// </summary>
public class BrandAssetService
{
    private readonly string _assetsRoot;
    private readonly string _settingsFilePath;
    private readonly ILogger<BrandAssetService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PlatformSettingsService _platformSettings;
    private static readonly object _lock = new();

    public BrandAssetService(
        IWebHostEnvironment environment,
        ILogger<BrandAssetService> logger,
        IHttpContextAccessor httpContextAccessor,
        PlatformSettingsService platformSettings)
    {
        _assetsRoot = Path.Combine(environment.WebRootPath, "brand-assets");
        _settingsFilePath = Path.Combine(environment.ContentRootPath, "Data", "brand-assets.json");
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _platformSettings = platformSettings;

        // Ensure directories exist
        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
    }

    /// <summary>
    /// Get all brand assets from settings
    /// </summary>
    public List<BrandAssetFile> GetAllAssets()
    {
        var settings = _platformSettings.Current;
        return settings.BrandAssets.Where(a => a.IsPublic).OrderByDescending(a => a.UploadedAt).ToList();
    }
    
    /// <summary>
    /// Get assets by category
    /// </summary>
    public List<BrandAssetFile> GetAssetsByCategory(string category)
    {
        return GetAllAssets().Where(a => a.Category == category).ToList();
    }

    /// <summary>
    /// Get a single asset by ID
    /// </summary>
    public BrandAssetFile? GetAsset(Guid id)
    {
        return _platformSettings.Current.BrandAssets.FirstOrDefault(a => a.Id == id);
    }

    /// <summary>
    /// Get the full physical path for an asset
    /// </summary>
    public string GetFullPath(BrandAssetFile asset)
    {
        return Path.Combine(_assetsRoot, asset.FilePath);
    }

    /// <summary>
    /// Upload a new brand asset
    /// </summary>
    public async Task<BrandAssetFile> UploadAsync(IBrowserFile file, string category, string description, string uploadedBy)
    {
        var asset = new BrandAssetFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = file.Name,
            FileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}",
            Category = category,
            Description = description,
            ContentType = file.ContentType,
            FileSize = file.Size,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            FilePath = Path.Combine(category, $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}")
        };

        // Create category directory
        var categoryPath = Path.Combine(_assetsRoot, category);
        Directory.CreateDirectory(categoryPath);

        // Save file
        var fullPath = Path.Combine(_assetsRoot, asset.FilePath);
        await using (var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)) // 50MB max
        {
            await using var fileStream = File.Create(fullPath);
            await stream.CopyToAsync(fileStream);
        }

        // Add to settings
        lock (_lock)
        {
            var settings = _platformSettings.Current;
            settings.BrandAssets.Add(asset);
            SaveSettings(settings);
        }

        _logger.LogInformation("Brand asset uploaded: {FileName} ({Category}) by {UploadedBy}", 
            asset.OriginalFileName, category, uploadedBy);

        return asset;
    }

    /// <summary>
    /// Delete a brand asset
    /// </summary>
    public async Task DeleteAsync(Guid id, string deletedBy)
    {
        var asset = GetAsset(id);
        if (asset == null) return;

        // Delete physical file
        var fullPath = GetFullPath(asset);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        // Remove from settings
        lock (_lock)
        {
            var settings = _platformSettings.Current;
            settings.BrandAssets.RemoveAll(a => a.Id == id);
            SaveSettings(settings);
        }

        _logger.LogInformation("Brand asset deleted: {FileName} by {DeletedBy}", 
            asset.OriginalFileName, deletedBy);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get all important links
    /// </summary>
    public List<ImportantLink> GetImportantLinks()
    {
        var settings = _platformSettings.Current;
        var links = settings.ImportantLinks.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToList();
        
        // If no links configured, return defaults
        if (!links.Any())
        {
            links = GetDefaultImportantLinks();
        }
        
        return links;
    }

    /// <summary>
    /// Save important links to settings
    /// </summary>
    public void SaveImportantLinks(List<ImportantLink> links)
    {
        lock (_lock)
        {
            var settings = _platformSettings.Current;
            settings.ImportantLinks = links;
            SaveSettings(settings);
        }
    }

    /// <summary>
    /// Get default important links for new installations
    /// </summary>
    private List<ImportantLink> GetDefaultImportantLinks()
    {
        var companyName = _platformSettings.GetBrandingName();
        var website = _platformSettings.Current.CompanyWebsite;
        
        return new List<ImportantLink>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Google Analytics",
                Description = "Website traffic and user behavior analytics",
                Url = "https://analytics.google.com",
                Icon = "Analytics",
                Category = "Analytics",
                DisplayOrder = 1,
                IsExternal = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Meta Business Suite",
                Description = "Facebook and Instagram business management",
                Url = "https://business.facebook.com",
                Icon = "Facebook",
                Category = "Social",
                DisplayOrder = 2,
                IsExternal = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = $"{companyName} Website",
                Description = "Official company website",
                Url = website,
                Icon = "Language",
                Category = "Website",
                DisplayOrder = 3,
                IsExternal = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Additional Resources",
                Description = "More tools and resources",
                Url = "#",
                Icon = "MoreHoriz",
                Category = "Resources",
                DisplayOrder = 4,
                IsExternal = false
            }
        };
    }

    /// <summary>
    /// Save settings to JSON file
    /// </summary>
    private void SaveSettings(PlatformSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            _platformSettings.InvalidateCache();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save brand asset settings");
            throw;
        }
    }

    /// <summary>
    /// Get MIME type for file
    /// </summary>
    public static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            ".html" or ".htm" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
