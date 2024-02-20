namespace KernelMemoryService.Models;

public record Question(Guid ConversationId, string Text, IEnumerable<Tag> Tags);
