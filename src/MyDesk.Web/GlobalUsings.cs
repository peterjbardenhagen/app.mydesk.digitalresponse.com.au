// Disambiguate HotChocolate.Path (GraphQL field path) from System.IO.Path.
// All existing code references mean filesystem paths, so we alias project-wide.
global using Path = System.IO.Path;
