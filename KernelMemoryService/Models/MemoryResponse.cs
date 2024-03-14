using Microsoft.KernelMemory;

namespace KernelMemoryService.Models;

public record class MemoryResponse(string Question, string Answer, IEnumerable<Citation> Citations);
