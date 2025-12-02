namespace RagePad.Models;

/// <summary>
/// Stores metadata for each open tab.
/// </summary>
internal sealed class TabData
{
    /// <summary>
    /// Full path to the file, or null if untitled.
    /// </summary>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Whether the tab has unsaved changes.
    /// </summary>
    public bool IsModified { get; set; }
    
    /// <summary>
    /// The number for "Untitled N" naming (0 if saved file).
    /// </summary>
    public int UntitledNumber { get; set; }
    
    /// <summary>
    /// Gets the display name for the tab.
    /// </summary>
    public string DisplayName => FilePath != null 
        ? System.IO.Path.GetFileName(FilePath) 
        : $"Untitled {UntitledNumber}";
}
