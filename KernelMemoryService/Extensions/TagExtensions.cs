using KernelMemoryService.Models;
using Microsoft.KernelMemory;
using TinyHelpers.Extensions;

namespace KernelMemoryService.Extensions;

public static class TagExtensions
{
    public static TagCollection? ToTagCollection(this IEnumerable<UploadTag>? tags)
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

    /// <summary>
    /// Creates a single filter that searches for all the tags in the collection using AND logic.
    /// </summary>
    public static MemoryFilter? ToMemoryFilter(this IEnumerable<Tag>? tags)
    {
        if (tags.IsNullOrEmpty())
        {
            return null;
        }

        var memoryFilter = new MemoryFilter();
        foreach (var tag in tags)
        {
            memoryFilter = memoryFilter.ByTag(tag.Name, tag.Value);
        }

        return memoryFilter;
    }

    /// <summary>
    /// Creates a list of filters that searches for all the tags in the collection using OR logic.
    /// </summary>
    public static ICollection<MemoryFilter>? ToMemoryFilters(this IEnumerable<Tag>? tags)
    {
        if (tags.IsNullOrEmpty())
        {
            return null;
        }

        var memoryFilters = new List<MemoryFilter>();
        foreach (var tag in tags)
        {
            memoryFilters.Add(MemoryFilters.ByTag(tag.Name, tag.Value));
        }

        return memoryFilters;
    }
}
