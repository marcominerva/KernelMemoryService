namespace KernelMemoryService.Models;

public class Tag
{
    public required string Name { get; set; }

    public required string Value { get; set; }

    public static bool TryParse(string? text, out Tag tag)
    {
        var parts = text?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts?.Length != 2)
        {
            tag = default!;
            return false;
        }

        tag = new Tag { Name = parts[0], Value = parts[1] };
        return true;
    }
}
