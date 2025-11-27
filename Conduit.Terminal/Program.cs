// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Steps;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Terminal;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("=== Genova.Conduit Responses API Corny Joke + Local DateTime Demo ===");
        Console.Write("Enter a topic (e.g. Work, Home, Relationships): ");
        string? topic = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(topic))
        {
            Console.WriteLine("No topic provided. Exiting.");
            return;
        }

        string? apiKey = Environment.GetEnvironmentVariable("openai-genova-api-key");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Environment variable 'openai-genova-api-key' is not set.");
            Console.WriteLine("Please set it to your OpenAI API key and run again.");
            return;
        }

        // Create the Responses-based chat client.
        IChatClient chatClient = new OpenAiResponseClient(apiKey, "gpt-4o-mini");

        // Register local tools in an in-memory registry.
        ITool[] tools =
        [
            new LocalDateTimeTool()
        ];

        IToolRegistry toolRegistry = new InMemoryToolRegistry(tools);

        // Build the pipeline steps:
        // 1. Generate a corny joke about the topic (using Responses API).
        // 2. Invoke the LocalDateTime tool and store the result in the context.
        IPipelineStep jokeStep = new CornyJokeStep(chatClient, topic.Trim());
        IPipelineStep dateTimeStep = new InvokeToolStep(
            toolRegistry,
            "LocalDateTime",
            "LocalDateTimeArguments",
            "CurrentDateTime");

        SimplePipeline pipeline = new (jokeStep, dateTimeStep);

        // Prepare the pipeline context.
        PipelineContext context = new (ExecutionEnvironment.Application);
        context.SetItem("Topic", topic.Trim());

        IDictionary<string, object?> dateTimeArguments = new Dictionary<string, object?>
        {
            ["kind"] = "local",  // or "utc"
            ["format"] = "G"    // general date/time pattern
        };

        context.SetItem("LocalDateTimeArguments", dateTimeArguments);

        try
        {
            await pipeline.ExecuteAsync(context, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while running the pipeline:");
            Console.WriteLine(ex.Message);
            return;
        }

        string? joke = context.GetItem<string>("CornyJoke");
        string? currentDateTime = context.GetItem<string>("CurrentDateTime");

        Console.WriteLine();
        Console.WriteLine("Here is your corny joke (from /v1/responses):");
        Console.WriteLine("--------------------------------------------");
        if (string.IsNullOrWhiteSpace(joke))
        {
            Console.WriteLine("(No joke was generated.)");
        }
        else
        {
            Console.WriteLine(joke);
        }

        Console.WriteLine();
        Console.WriteLine("Local date and time:");
        Console.WriteLine("--------------------");
        if (string.IsNullOrWhiteSpace(currentDateTime))
        {
            Console.WriteLine("(No date/time was generated.)");
        }
        else
        {
            Console.WriteLine(currentDateTime);
        }
    }
}
