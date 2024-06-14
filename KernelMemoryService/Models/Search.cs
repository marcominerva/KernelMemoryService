namespace KernelMemoryService.Models;

public record class Search(string Text, IEnumerable<Tag> Tags);

