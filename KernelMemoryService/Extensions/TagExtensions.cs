using KernelMemoryService.Models;
using Microsoft.KernelMemory;
using TinyHelpers.Extensions;

namespace KernelMemoryService.Extensions;

public static class TagExtensions
{
    public static TagCollection? ToTagCollection(this IEnumerable<Tag>? tags)
    {
        if (tags.IsNullOrEmpty())
        {
            return null;
        }

        var collection = new TagCollection();
        foreach (var tag in tags)
        {
            collection.Add(tag.Name, tag.Value);
        }

        return collection;
    }
}
