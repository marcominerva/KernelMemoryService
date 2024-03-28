namespace KernelMemoryService.Models;

public record Search(string Text, IEnumerable<Tag> Tags);

