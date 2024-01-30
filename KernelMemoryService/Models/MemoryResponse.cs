using Microsoft.KernelMemory;

namespace KernelMemoryService.Models;

public record class MemoryResponse(string Answer, IEnumerable<Citation> Citations);
