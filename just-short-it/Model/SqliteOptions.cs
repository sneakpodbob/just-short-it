namespace JustShortIt.Model;

/// <summary>
/// Configures SQLite persistence settings for the application.
/// </summary>
/// <param name="Path">
/// Relative or absolute path to the SQLite database file. Relative paths are resolved from the application base directory.
/// </param>
public record SqliteOptions(string Path = "data/justshortit.db");