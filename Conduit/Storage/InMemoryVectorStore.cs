// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents an in-memory implementation of <see cref="IVectorStore"/>
/// that holds a limited number of vector records and evicts the oldest
/// records first when the capacity is exceeded.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InMemoryVectorStore : InMemoryStoreBase<VectorRecord>, IVectorStore
{
    private const int DefaultCapacity = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryVectorStore"/> class
    /// with the default capacity.
    /// </summary>
    public InMemoryVectorStore()
        : base(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryVectorStore"/> class
    /// with the specified capacity.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of vector records to store. When the capacity is exceeded,
    /// the oldest records (by last updated timestamp) are removed first.
    /// </param>
    public InMemoryVectorStore(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    /// Adds or updates vector records in the store.
    /// </summary>
    /// <param name="records">The records to upsert.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="records"/> is <c>null</c>.
    /// </exception>
    public Task UpsertAsync(
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource tcs = new ();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        foreach (VectorRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(record.Id))
            {
                throw new ArgumentException(
                    "VectorRecord must have a non-empty Id.",
                    nameof(records));
            }

            if (record.Embedding == null)
            {
                throw new ArgumentException(
                    "VectorRecord must have a non-null Embedding.",
                    nameof(records));
            }

            SetValue(record.Id, record);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a similarity search against the store using the specified embedding,
    /// returning at most <paramref name="topK"/> results.
    /// </summary>
    /// <param name="embedding">The query embedding.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is a collection of search results ordered by similarity.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="embedding"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="topK"/> is less than or equal to zero.
    /// </exception>
    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> embedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                topK,
                "topK must be greater than zero.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<IReadOnlyList<VectorSearchResult>> tcs = new();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        IReadOnlyList<VectorSearchResult> allResults = GetSortedResults(embedding);

        if (allResults.Count > topK)
        {
            List<VectorSearchResult> truncated = new(topK);
            for (int i = 0; i < topK; i++)
            {
                truncated.Add(allResults[i]);
            }

            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(truncated);
        }

        return Task.FromResult(allResults);
    }

    /// <summary>
    /// Performs a similarity search against the store using the specified embedding,
    /// returning all results whose similarity score is greater than or equal to
    /// <paramref name="minConfidence"/>, limited to at most <paramref name="maxResults"/> items.
    /// </summary>
    /// <param name="embedding">The query embedding.</param>
    /// <param name="minConfidence">
    /// The minimum cosine similarity score required for a result to be included.
    /// Expected range is between 0.0 and 1.0. A typical starting value is 0.2.
    /// </param>
    /// <param name="maxResults">
    /// The maximum number of results to return after applying the confidence filter.
    /// Expected to be greater than zero. A typical starting value is 5.
    /// </param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is a collection of search results ordered by similarity.
    /// The collection may be empty if no records meet the minimum confidence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="embedding"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minConfidence"/> is outside the range 0.0 to 1.0,
    /// or when <paramref name="maxResults"/> is less than or equal to zero.
    /// </exception>
    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> embedding,
        float minConfidence = 0.2f,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (minConfidence < 0.0f || minConfidence > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minConfidence),
                minConfidence,
                "minConfidence must be between 0.0 and 1.0.");
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                maxResults,
                "maxResults must be greater than zero.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<IReadOnlyList<VectorSearchResult>> tcs = new();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        IReadOnlyList<VectorSearchResult> allResults = GetSortedResults(embedding);

        List<VectorSearchResult> filtered = [];

        for (int i = 0; i < allResults.Count; i++)
        {
            VectorSearchResult result = allResults[i];

            if (result.Score >= minConfidence)
            {
                filtered.Add(result);
            }
        }

        if (filtered.Count > maxResults)
        {
            List<VectorSearchResult> truncated = new(maxResults);
            for (int i = 0; i < maxResults; i++)
            {
                truncated.Add(filtered[i]);
            }

            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(truncated);
        }

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(filtered);
    }

    private static double ComputeCosineSimilarity(
        IReadOnlyList<float> x,
        IReadOnlyList<float> y)
    {
        if (x.Count != y.Count || x.Count == 0)
        {
            return 0.0d;
        }

        double dot = 0.0d;
        double normX = 0.0d;
        double normY = 0.0d;

        for (int i = 0; i < x.Count; i++)
        {
            double xi = x[i];
            double yi = y[i];

            dot += xi * yi;
            normX += xi * xi;
            normY += yi * yi;
        }

        if (normX <= 0.0d || normY <= 0.0d)
        {
            return 0.0d;
        }

        double denominator = Math.Sqrt(normX) * Math.Sqrt(normY);
        if (denominator <= 0.0d)
        {
            return 0.0d;
        }

        return dot / denominator;
    }

    /// <summary>
    /// Computes cosine similarity between the query embedding and all stored records,
    /// and returns the results sorted in descending order of similarity.
    /// </summary>
    private List<VectorSearchResult> GetSortedResults(IReadOnlyList<float> embedding)
    {
        IList<VectorRecord> allRecords = GetAllValues();

        List<VectorSearchResult> results = [];

        for (int i = 0; i < allRecords.Count; i++)
        {
            VectorRecord record = allRecords[i];

            if (record.Embedding == null)
            {
                continue;
            }

            double score = ComputeCosineSimilarity(embedding, record.Embedding);

            VectorSearchResult result = new ()
            {
                Record = record,
                Score = score,
            };

            results.Add(result);
        }

        results.Sort(
            (left, right) =>
            {
                // Sort descending by score.
                return right.Score.CompareTo(left.Score);
            });

        return results;
    }
}
