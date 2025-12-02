// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Agents;
using Genova.Conduit.Chats;
using Genova.Conduit.Terminal.Agents;
using Genova.Conduit.Terminal.Tools;
using Genova.Conduit.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Genova.Conduit.Terminal;

internal static class Program
{
    private const string AgentId = "math-agent-1";

    /// <summary>
    /// The entry point for the terminal application that demonstrates
    /// a long-running MathAgent orchestrated via BasicAgentOrchestrator.
    /// </summary>
    private static async Task Main()
    {
        IHost host = CreateHostBuilder().Build();

        IAgentOrchestrator orchestrator =
            host.Services.GetRequiredService<IAgentOrchestrator>();

        IAgentStateStore stateStore =
            host.Services.GetRequiredService<IAgentStateStore>();

        IAgent agent =
            host.Services.GetRequiredService<IAgent>();

        Console.WriteLine("=== Genova.Conduit Math Agent Demo ===");
        Console.WriteLine("Enter a simple algebraic question (e.g., 'What is the sum of 2 and 3?').");
        Console.WriteLine();

        Console.Write("Question: ");
        string? userQuestion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            Console.WriteLine("No question provided. Exiting.");
            return;
        }

        // Initialize agent state with the user's question.
        AgentState? existingState =
            await stateStore.GetAsync(AgentId, CancellationToken.None)
                .ConfigureAwait(false);

        AgentState state = existingState ?? new AgentState
        {
            AgentId = AgentId
        };

        state.Data["UserQuestion"] = userQuestion.Trim();
        await stateStore.SaveAsync(state, CancellationToken.None)
            .ConfigureAwait(false);

        Console.WriteLine();
        Console.WriteLine("Starting MathAgent...");
        Console.WriteLine("Press Ctrl+C to cancel.");
        Console.WriteLine();

        using CancellationTokenSource cts = new ();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        while (!cts.IsCancellationRequested)
        {
            AgentRunResult result;

            try
            {
                result =
                    await orchestrator.RunOnceAsync(AgentId, cts.Token)
                        .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Agent orchestrator failure: {ex.Message}");
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{timestamp}] Status: {result.Status}  Message: {result.Message}");

            if (result.Status == AgentRunStatus.Completed ||
                result.Status == AgentRunStatus.Failed)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }
        }

        Console.WriteLine();
        Console.WriteLine("MathAgent has finished its work.");
        Console.WriteLine();

        AgentState? finalState =
            await stateStore.GetAsync(AgentId, CancellationToken.None)
                .ConfigureAwait(false);

        if (finalState != null &&
            finalState.Data.TryGetValue("FinalAnswer", out object? rawAnswer) &&
            rawAnswer is string finalAnswer &&
            !string.IsNullOrWhiteSpace(finalAnswer))
        {
            Console.WriteLine("Final Answer:");
            Console.WriteLine(finalAnswer);
        }
        else
        {
            Console.WriteLine("No final answer was produced.");
        }
    }

    /// <summary>
    /// Creates and configures the application host, including dependency injection
    /// for HTTP clients, the chat client, the tool registry, the math agent,
    /// the agent state store, and the agent orchestrator.
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                // Suppress noisy HttpClient INFO logs; keep warnings and above.
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            })
            .ConfigureServices(services =>
            {
                // HTTP client for OpenAI Responses API.
                services.AddHttpClient("Genova.Conduit.OpenAI.Responses");

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

                // Tool registry with the four math tools.
                services.AddSingleton<IToolRegistry>(sp =>
                {
                    ITool[] tools =
                    [
                        new AdditionTool(),
                        new SubtractionTool(),
                        new MultiplicationTool(),
                        new DivisionTool(),
                        new IncrementTool(),
                        new FileExistsTool()
                    ];

                    InMemoryToolRegistry registry = new (tools);
                    return registry;
                });


                // In-memory agent state store.
                services.AddSingleton<IAgentStateStore>(sp =>
                {
                    InMemoryAgentStateStore store = new ();
                    return store;
                });

                // MathAgent.
                services.AddSingleton<IAgent>(sp =>
                {
                    IChatClient chatClient = sp.GetRequiredService<IChatClient>();
                    IToolRegistry toolRegistry = sp.GetRequiredService<IToolRegistry>();

                    MathAgent agent = new (AgentId, chatClient, toolRegistry);
                    return agent;
                });

                // BasicAgentOrchestrator with a single math agent.
                services.AddSingleton<IAgentOrchestrator>(sp =>
                {
                    IAgentStateStore stateStore = sp.GetRequiredService<IAgentStateStore>();
                    IAgent agent = sp.GetRequiredService<IAgent>();

                    Dictionary<string, IAgent> agents =
                        new(StringComparer.OrdinalIgnoreCase)
                        {
                            [agent.Id] = agent
                        };

                    BasicAgentOrchestrator orchestrator =
                        new (stateStore, agents);

                    return orchestrator;
                });
            });

        return builder;
    }
}
