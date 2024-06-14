namespace KernelMemoryService.Models;

public record class Question(Guid ConversationId, string Text, IEnumerable<Tag> Tags) : Search(Text, Tags);
