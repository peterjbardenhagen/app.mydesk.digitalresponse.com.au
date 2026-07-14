// Disambiguate HotChocolate.Path (GraphQL field path) from System.IO.Path.
// All existing code references mean filesystem paths, so we alias project-wide.
global using Path = System.IO.Path;

// Fix missing type resolution errors that CS8803 previously masked
global using MyDesk.Shared.Services;
global using System.Data;
global using SixLabors.ImageSharp;
global using SixLabors.ImageSharp.Processing;
global using SixLabors.ImageSharp.Formats;
global using SixLabors.ImageSharp.PixelFormats;

// .NET 10 OpenAPI integration (replaces Swashbuckle implicit usings)
global using Microsoft.AspNetCore.OpenApi;
