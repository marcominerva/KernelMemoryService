namespace KernelMemoryService.Models;

/// <summary>
/// Represents a tag that can be attached to a document during the import phase.
/// </summary>
public class UploadTag
{
    public required string Name { get; set; }

    public required string Value { get; set; }

    public static bool TryParse(string? text, out UploadTag tag)
    {
        var parts = text?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts?.Length != 2)
        {
            tag = default!;
            return false;
        }

        tag = new UploadTag { Name = parts[0], Value = parts[1] };
        return true;
    }
}
