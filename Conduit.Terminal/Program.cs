// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Genova.Common.Utilities;
using Genova.Conduit.Chats;
using Genova.Conduit.Embeddings;
using Genova.Conduit.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Genova.Conduit.Terminal;

internal static class Program
{
    private const string SnapshotResourceName = "Data.vector-snapshot.json";
    private const string ConversationId = "terminal-it-service-desk-demo";

    /// <summary>
    /// The entry point for the terminal application that demonstrates
    /// a retrieval-augmented, multi-turn IT Service Desk assistant
    /// with conversation memory.
    /// </summary>
    private static async Task Main()
    {
        IHost host = CreateHostBuilder().Build();

        IEmbeddingClient embeddingClient =
            host.Services.GetRequiredService<IEmbeddingClient>();

        IChatClient chatClient =
            host.Services.GetRequiredService<IChatClient>();

        IVectorStore vectorStore =
            host.Services.GetRequiredService<IVectorStore>();

        IConversationStore conversationStore =
            host.Services.GetRequiredService<IConversationStore>();

        Console.WriteLine("=== Genova.Conduit IT Service Desk RAG Chat ===");
        Console.WriteLine("Loading embedded vector snapshot...");
        Console.WriteLine();

        bool loaded = await LoadSnapshotIntoVectorStoreAsync(vectorStore, CancellationToken.None);

        if (!loaded)
        {
            Console.WriteLine("ERROR: Unable to load the embedded vector snapshot.");
            return;
        }

        Console.WriteLine("Vector snapshot loaded successfully.");
        Console.WriteLine();
        Console.WriteLine("Type 'exit' to end the chat.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("You: ");
            string? userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                Console.WriteLine("No input provided. Exiting.");
                break;
            }

            if (string.Equals(userInput.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting chat.");
                break;
            }

            // Handle a single turn of the chat.
            await HandleChatTurnAsync(
                userInput,
                embeddingClient,
                chatClient,
                vectorStore,
                conversationStore,
                CancellationToken.None);

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Creates and configures the application host, including dependency injection
    /// for HTTP clients, the embedding client, the chat client, the vector store,
    /// and the conversation store.
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                // Raise System.Net.Http.HttpClient logs to Warning to suppress the INFO lines
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

                // Optional: silence only the specific named client shown in your output
                // logging.AddFilter("System.Net.Http.HttpClient.Genova.Conduit.OpenAI.Embeddings", Microsoft.Extensions.Logging.LogLevel.Warning);
            })
            .ConfigureServices(services =>
            {
                // HTTP clients for OpenAI endpoints.
                services.AddHttpClient("Genova.Conduit.OpenAI.Embeddings");
                services.AddHttpClient("Genova.Conduit.OpenAI.Responses");

                // Embedding client (OpenAI embeddings).
                services.AddSingleton<IEmbeddingClient>(sp =>
                {
                    IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();

                    string? apiKey =
                        Environment.GetEnvironmentVariable("openai-genova-api-key");

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new InvalidOperationException(
                            "Environment variable 'openai-genova-api-key' is not set.");
                    }

                    return new OpenAiEmbeddingClient(factory, apiKey);
                });

                // Chat client (OpenAI Responses API).
                services.AddSingleton<IChatClient>(sp =>
                {
                    IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();

                    string? apiKey =
                        Environment.GetEnvironmentVariable("openai-genova-api-key");

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new InvalidOperationException(
                            "Environment variable 'openai-genova-api-key' is not set.");
                    }

                    return new OpenAiResponseClient(factory, apiKey, "gpt-4o-mini");
                });

                // In-memory vector store (populated from snapshot on startup).
                services.AddSingleton<IVectorStore>(sp =>
                {
                    InMemoryVectorStore store = new InMemoryVectorStore();
                    return store;
                });

                // In-memory conversation store (for chat history).
                services.AddSingleton<IConversationStore>(sp =>
                {
                    InMemoryConversationStore store = new InMemoryConversationStore();
                    return store;
                });
            });

        return builder;
    }

    /// <summary>
    /// Handles a single turn of the chat:
    /// embeds the user input, retrieves relevant chunks, builds a prompt
    /// with conversation history and context, and prints the assistant's reply.
    /// </summary>
    private static async Task HandleChatTurnAsync(
        string userInput,
        IEmbeddingClient embeddingClient,
        IChatClient chatClient,
        IVectorStore vectorStore,
        IConversationStore conversationStore,
        CancellationToken cancellationToken)
    {
        // 1. Load or create conversation.
        Conversation? conversation =
            await conversationStore.GetConversationAsync(ConversationId, cancellationToken)
                .ConfigureAwait(false);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = ConversationId,
                Messages = new List<ChatMessage>()
            };
        }

        // Append the new user message to the conversation.
        ChatMessage userMessage = new ChatMessage
        {
            Role = ChatMessageRole.User,
            Content = userInput
        };

        conversation.Messages.Add(userMessage);

        // 2. Embed the current user input as the query.
        EmbeddingResponse queryEmbedding =
            await EmbedUserQueryAsync(embeddingClient, userInput, cancellationToken)
                .ConfigureAwait(false);

        // 3. Search the vector store for similar chunks.
        IReadOnlyList<VectorSearchResult> searchResults =
            await SearchVectorStoreAsync(vectorStore, queryEmbedding, 3, cancellationToken)
                .ConfigureAwait(false);

        // 4. Build context from search results.
        string contextText = BuildContextFromSearchResults(searchResults);

        if (!string.IsNullOrWhiteSpace(contextText))
        {
            Console.WriteLine();
            Console.WriteLine("Retrieved internal chunks (from private vector store):");
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine(contextText);
        }

        // 5. Ask the assistant using both conversation history and retrieved context.
        ChatResponse answer =
            await AskAssistantWithHistoryAndContextAsync(
                    chatClient,
                    conversation,
                    contextText,
                    cancellationToken)
                .ConfigureAwait(false);

        string assistantText =
            answer.Message != null && !string.IsNullOrWhiteSpace(answer.Message.Content)
                ? answer.Message.Content
                : "(No answer was generated.)";

        // Append assistant reply to conversation and save.
        ChatMessage assistantMessage = new ChatMessage
        {
            Role = ChatMessageRole.Assistant,
            Content = assistantText
        };

        conversation.Messages.Add(assistantMessage);

        await conversationStore.SaveConversationAsync(conversation, cancellationToken)
            .ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine("Assistant:");
        Console.WriteLine(assistantText);
    }

    /// <summary>
    /// Loads the embedded vector snapshot from the assembly resources and populates
    /// the provided <see cref="IVectorStore"/> with its records.
    /// </summary>
    private static async Task<bool> LoadSnapshotIntoVectorStoreAsync(
        IVectorStore vectorStore,
        CancellationToken cancellationToken)
    {
        Assembly assembly = typeof(VectorRecord).Assembly;

        using Stream? stream =
            FileHelper.GetEmbeddedResourceStream(assembly, SnapshotResourceName);

        if (stream == null)
        {
            Console.WriteLine(
                $"ERROR: Embedded resource '{SnapshotResourceName}' was not found " +
                $"in assembly '{assembly.FullName}'.");
            return false;
        }

        VectorStoreSnapshot snapshot =
            await VectorStoreSnapshotSerializer.ImportAsync(stream, cancellationToken)
                .ConfigureAwait(false);

        if (snapshot.Records == null || snapshot.Records.Count == 0)
        {
            Console.WriteLine("WARNING: Snapshot contains no vector records.");
        }

        await vectorStore.UpsertAsync(snapshot.Records, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Generates an embedding for the user's query using the embedding client.
    /// </summary>
    private static async Task<EmbeddingResponse> EmbedUserQueryAsync(
        IEmbeddingClient embeddingClient,
        string userQuery,
        CancellationToken cancellationToken)
    {
        IList<string> inputs = new List<string> { userQuery };

        EmbeddingRequest request = new EmbeddingRequest
        {
            Inputs = inputs,
            ModelId = null // Use default embedding model.
        };

        EmbeddingResponse response =
            await embeddingClient.GenerateEmbeddingsAsync(request, cancellationToken)
                .ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Performs a similarity search against the vector store using the first embedding
    /// in the provided <see cref="EmbeddingResponse"/> as the query vector.
    /// </summary>
    private static async Task<IReadOnlyList<VectorSearchResult>> SearchVectorStoreAsync(
        IVectorStore vectorStore,
        EmbeddingResponse queryEmbedding,
        int topK,
        CancellationToken cancellationToken)
    {
        if (queryEmbedding.Embeddings == null ||
            queryEmbedding.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "Query embedding response does not contain any embeddings.");
        }

        Embedding embedding = queryEmbedding.Embeddings[0];

        if (embedding.Values == null)
        {
            throw new InvalidOperationException(
                "Query embedding has a null Values collection.");
        }

        IReadOnlyList<VectorSearchResult> results =
            await vectorStore.SearchAsync(embedding.Values, .2f, 3, cancellationToken)
                .ConfigureAwait(false);

        return results;
    }

    /// <summary>
    /// Builds a human-readable context string from the vector search results
    /// by concatenating the <c>text</c> metadata of each record.
    /// </summary>
    private static string BuildContextFromSearchResults(
        IReadOnlyList<VectorSearchResult> results)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < results.Count; i++)
        {
            VectorSearchResult result = results[i];
            VectorRecord record = result.Record;

            string text = string.Empty;

            if (record.Metadata != null &&
                record.Metadata.TryGetValue("text", out object? value) &&
                value != null)
            {
                if (value is string stringValue)
                {
                    if (!string.IsNullOrWhiteSpace(stringValue))
                    {
                        text = stringValue.Trim();
                    }
                }
                else if (value is JsonElement jsonElement &&
                         jsonElement.ValueKind == JsonValueKind.String)
                {
                    string? fromJson = jsonElement.GetString();
                    if (!string.IsNullOrWhiteSpace(fromJson))
                    {
                        text = fromJson.Trim();
                    }
                }
                text += $" {result.Score}";
            }

            builder.Append('[');
            builder.Append(i + 1);
            builder.Append("] ");
            builder.AppendLine(text);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Asks the assistant (via the OpenAI Responses API) to answer using
    /// the current conversation history and the provided internal context text.
    /// </summary>
    private static async Task<ChatResponse> AskAssistantWithHistoryAndContextAsync(
        IChatClient chatClient,
        Conversation conversation,
        string contextText,
        CancellationToken cancellationToken)
    {
        ChatRequest request = new ChatRequest
        {
            ModelId = null,
            MaxTokens = 512,
            Temperature = 0.2
        };

        // High-level instructions about how to use context and conversation history.
        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content =
                    "You are an internal IT Service Desk assistant. " +
                    "Answer the user's questions as accurately and concisely as possible. " +
                    "Use the internal IT documents provided when they are relevant. " +
                    "Use the conversation history to maintain context. " +
                    "If the answer cannot be found in the internal documents, " +
                    "you may answer based on your general knowledge, " +
                    "but prefer internal policies when there is any conflict."
            });

        if (!string.IsNullOrWhiteSpace(contextText))
        {
            request.Messages.Add(
                new ChatMessage
                {
                    Role = ChatMessageRole.System,
                    Content =
                        "Internal IT documents (may or may not be relevant to the current question):\n" +
                        contextText
                });
        }

        // Add the conversation history (user and assistant messages).
        if (conversation.Messages != null)
        {
            foreach (ChatMessage message in conversation.Messages)
            {
                // Clone messages to avoid mutating stored instances inadvertently.
                ChatMessage copy = new ChatMessage
                {
                    Role = message.Role,
                    Content = message.Content
                };

                request.Messages.Add(copy);
            }
        }

        ChatResponse response =
            await chatClient.GenerateAsync(request, cancellationToken)
                .ConfigureAwait(false);

        return response;
    }
}
