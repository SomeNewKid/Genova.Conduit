// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Genova.Common.Attributes;
using Genova.Conduit.Embeddings;
using Genova.Conduit.Storage;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that creates <see cref="VectorRecord"/> instances
/// from text chunks and their corresponding embeddings, upserts them into an
/// <see cref="IVectorStore"/>, and stores the records in the <see cref="PipelineContext"/>.
/// </summary>
/// <remarks>
/// This step expects that a previous step has placed a list of text chunks and an
/// <see cref="EmbeddingResponse"/> into the pipeline context using well-known keys.
/// The number of chunks must match the number of embeddings.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class UpsertVectorRecordsStep : IPipelineStep
{
    private readonly IVectorStore _vectorStore;
    private readonly string _chunksKey;
    private readonly string _embeddingsKey;
    private readonly string _vectorRecordsKey;
    private readonly string _idPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertVectorRecordsStep"/> class.
    /// </summary>
    /// <param name="vectorStore">
    /// The vector store into which records will be upserted.
    /// </param>
    /// <param name="chunksKey">
    /// The key in <see cref="PipelineContext.Items"/> that contains the text chunks
    /// as an <see cref="IList{String}"/>.
    /// </param>
    /// <param name="embeddingsKey">
    /// The key in <see cref="PipelineContext.Items"/> that contains the
    /// <see cref="EmbeddingResponse"/> produced for those chunks.
    /// </param>
    /// <param name="vectorRecordsKey">
    /// The key under which the created <see cref="VectorRecord"/> collection will
    /// be stored in <see cref="PipelineContext.Items"/>.
    /// </param>
    /// <param name="idPrefix">
    /// The prefix used when generating record identifiers. The final identifier
    /// will be this prefix concatenated with the chunk index (for example,
    /// <c>"doc-123-"</c> + <c>"0"</c>).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="vectorStore"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any string parameter is <c>null</c> or whitespace.
    /// </exception>
    public UpsertVectorRecordsStep(
        IVectorStore vectorStore,
        string chunksKey,
        string embeddingsKey,
        string vectorRecordsKey,
        string idPrefix)
    {
        if (vectorStore == null)
        {
            throw new ArgumentNullException(nameof(vectorStore));
        }

        if (string.IsNullOrWhiteSpace(chunksKey))
        {
            throw new ArgumentException("Chunks key must be non-empty.", nameof(chunksKey));
        }

        if (string.IsNullOrWhiteSpace(embeddingsKey))
        {
            throw new ArgumentException("Embeddings key must be non-empty.", nameof(embeddingsKey));
        }

        if (string.IsNullOrWhiteSpace(vectorRecordsKey))
        {
            throw new ArgumentException("Vector records key must be non-empty.", nameof(vectorRecordsKey));
        }

        if (string.IsNullOrWhiteSpace(idPrefix))
        {
            throw new ArgumentException("Id prefix must be non-empty.", nameof(idPrefix));
        }

        _vectorStore = vectorStore;
        _chunksKey = chunksKey;
        _embeddingsKey = embeddingsKey;
        _vectorRecordsKey = vectorRecordsKey;
        _idPrefix = idPrefix;
    }

    /// <summary>
    /// Executes the step by creating vector records from the chunks and embeddings,
    /// upserting them into the vector store, and storing the records in the context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the context does not contain the expected chunks or embeddings,
    /// or when their counts do not match.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Retrieve chunks from the context.
        object? rawChunks = context.GetItem<object>(_chunksKey);
        if (rawChunks == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain any text chunks under key '{_chunksKey}'.");
        }

        IList<string>? chunks = rawChunks as IList<string>;
        if (chunks == null)
        {
            throw new InvalidOperationException(
                $"Context item '{_chunksKey}' is not an IList<string>.");
        }

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Context item '{_chunksKey}' contains zero chunks to upsert.");
        }

        // Retrieve embeddings from the context.
        object? rawEmbeddings = context.GetItem<object>(_embeddingsKey);
        if (rawEmbeddings == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain an EmbeddingResponse under key '{_embeddingsKey}'.");
        }

        EmbeddingResponse? embeddingResponse = rawEmbeddings as EmbeddingResponse;
        if (embeddingResponse == null)
        {
            throw new InvalidOperationException(
                $"Context item '{_embeddingsKey}' is not an EmbeddingResponse.");
        }

        if (embeddingResponse.Embeddings == null ||
            embeddingResponse.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                $"EmbeddingResponse under key '{_embeddingsKey}' does not contain any embeddings.");
        }

        if (embeddingResponse.Embeddings.Count != chunks.Count)
        {
            throw new InvalidOperationException(
                $"The number of embeddings ({embeddingResponse.Embeddings.Count}) does not match " +
                $"the number of chunks ({chunks.Count}).");
        }

        // Create vector records.
        IList<VectorRecord> records = new List<VectorRecord>(chunks.Count);

        for (int i = 0; i < chunks.Count; i++)
        {
            string chunkText = chunks[i];
            Embedding embedding = embeddingResponse.Embeddings[i];

            if (embedding.Values == null)
            {
                throw new InvalidOperationException(
                    $"Embedding at index {i} has a null Values collection.");
            }

            string id =
                _idPrefix + i.ToString(CultureInfo.InvariantCulture);

            VectorRecord record = new VectorRecord
            {
                Id = id,
                Embedding = embedding.Values,
                Metadata = new Dictionary<string, object?>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["text"] = chunkText,
                },
            };

            records.Add(record);
        }

        // Upsert into vector store.
        await _vectorStore.UpsertAsync(records, cancellationToken)
            .ConfigureAwait(false);

        // Store the records in the context for downstream steps.
        context.SetItem(_vectorRecordsKey, records);
    }
}
