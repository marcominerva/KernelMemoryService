# Kernel Memory Service

[Kernel Memory](https://github.com/microsoft/kernel-memory) provides a [Service implementation](https://github.com/microsoft/kernel-memory/tree/main/service/Service) that can be used to manage memory settings, ingest data and query for answers. While it is a good solution, in some scenarios it can be too complex.

So, the goal of this repository is to provide a lightweight implementation of Kernel Memory as a Service. This project is quite simple, so it can be easily customized according to your needs and even integrated in existing application with little effort.

### How to use

The service can be directly configured in the [Program.cs file](https://github.com/marcominerva/KernelMemoryService/blob/master/KernelMemoryService/Program.cs). The default implementation uses the following settings:

- Azure OpenAI Service for embeddings and text generation.
- File system for Content Storage, Vector Storage and Orchestration.

The configuration values are stored in the [appsettings.json file](https://github.com/marcominerva/KernelMemoryService/blob/master/KernelMemoryService/appsettings.json).

You can easily change all these options by using any of the [supported backends](https://github.com/microsoft/kernel-memory?tab=readme-ov-file#supported-data-formats-and-backends).

### Conversational support

Embeddings are generated based on a given text. So, in a conversational scenario, it is necessary to keep track of the previous messages in order to generate valid embeddings for a particular question.

For example, suppose we have imported a couple of Wikipedia articles, one about [Taggia](https://en.wikipedia.org/wiki/Taggia) and the other about [Sanremo](https://en.wikipedia.org/wiki/Sanremo), two cities in Italy. Now, we want to ask questions about them (of course, this information is publicly available and known by GPT models, so using embeddings and RAG aren't really necessary, but this is just an example). So, we start with the following:

- How many people live in Taggia?

Using embeddings and RAG, Kernel Memory will generate the correct answer. Now, as we are in a chat context, we ask another question:

- And in Sanremo?

From our point of view, this question is the "continuation" of the chat, so it means "And how many people live in Sanremo?", However, if we directly generate embeddings for "And in Sanremo?", they won't contain anything about the fact we are interested in the population number, so we won't get any result.

To solve this problem, we need to keep track of the previous messages and, when asking a question, we need to reformulate it taking into account the whole conversation. In this way, we can generate the correct embeddings.

The Service automatically handles this scenario by using a Memory Cache and a **ConversationId** associated to each question. Questions and answers are kept in memory, so the Service is able to [reformulate questions](https://github.com/marcominerva/KernelMemoryService/blob/master/KernelMemoryService/Services/ChatService.cs) based on the current chat context before using Kernel Memory.

> **Note**
This isn't the only way to keep track of the conversation context. The Service uses an explicit approach to make it clear how the workflow should work.

Two settings in [appsettings.json file](https://github.com/marcominerva/KernelMemoryService/blob/master/KernelMemoryService/appsettings.json) are used to limit the chat cache:

- _MessageLimit_: specifies how many messages for each conversation must be saved. When this limit is reached, oldest messages are automatically removed.
- _MessageExpiration_: specifies the time interval used to maintain messages in cache, regardless their count.