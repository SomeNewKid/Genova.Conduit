// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Terminal;

/// <summary>
/// The main program class for the Corny Joke demo application.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry point of the application.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private static async Task Main()
    {
        Console.WriteLine("=== Genova.Conduit Corny Joke Demo ===");
        Console.Write("Enter a topic (e.g. Work, Home, Relationships): ");
        string? topic = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(topic))
        {
            Console.WriteLine("No topic provided. Exiting.");
            return;
        }

        // Read API key from environment variable
        string? apiKey = Environment.GetEnvironmentVariable("openai-genova-api-key");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Environment variable 'openai-genova-api-key' is not set.");
            Console.WriteLine("Please set it to your OpenAI API key and run again.");
            return;
        }

        // Create a chat model client and a single-step pipeline
        OpenAiChatModelClient chatClient = new (apiKey, modelId: "gpt-4o-mini");
        CornyJokeStep jokeStep = new (chatClient, topic.Trim());
        SimplePipeline pipeline = new (jokeStep);

        // Build the context and run the pipeline
        PipelineContext context = new (ExecutionEnvironment.Application);
        context.SetItem("Topic", topic.Trim());

        try
        {
            await pipeline.ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while generating the joke:");
            Console.WriteLine(ex.Message);
            return;
        }

        // Retrieve the joke from the context
        string? joke = context.GetItem<string>("CornyJoke");

        Console.WriteLine();
        Console.WriteLine("Here is your corny joke:");
        Console.WriteLine("------------------------");
        Console.WriteLine(string.IsNullOrWhiteSpace(joke)
            ? "(No joke was generated.)"
            : joke);
    }
}
