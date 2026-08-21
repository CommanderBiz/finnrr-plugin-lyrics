using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Finnrr.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Finnrr;

/// <summary>
/// Runs after every library scan and automatically syncs lyrics for newly added audio items.
/// This is the Finnrr differentiator: new music (e.g. Bridge rips) gets synced lyrics
/// without waiting for the scheduled task.
/// </summary>
public class LyricSyncPostScanTask : ILibraryPostScanTask
{
    private const int QueryPageLimit = 100;

    private static readonly BaseItemKind[] ItemKinds = [BaseItemKind.Audio];
    private static readonly MediaType[] MediaTypes = [MediaType.Audio];
    private static readonly SourceType[] SourceTypes = [SourceType.Library];
    private static readonly DtoOptions DtoOptions = new(false);

    private readonly ILibraryManager _libraryManager;
    private readonly ILyricManager _lyricManager;
    private readonly RetryStateStore _retryStateStore;
    private readonly ILogger<LyricSyncPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricSyncPostScanTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public LyricSyncPostScanTask(
        ILibraryManager libraryManager,
        ILyricManager lyricManager,
        IApplicationPaths applicationPaths,
        ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _lyricManager = lyricManager;
        _retryStateStore = new RetryStateStore(applicationPaths, loggerFactory.CreateLogger<RetryStateStore>());
        _logger = loggerFactory.CreateLogger<LyricSyncPostScanTask>();
    }

    /// <inheritdoc />
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configuration = FinnrrPlugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!configuration.EnableAutoSyncOnScan)
        {
            _logger.LogInformation("Finnrr auto-sync is disabled; skipping.");
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var retryState = await _retryStateStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        // Newest items first — a fresh scan surfaces the most recently added music at the top.
        var query = new InternalItemsQuery
        {
            Recursive = true,
            IsVirtualItem = false,
            IncludeItemTypes = ItemKinds,
            DtoOptions = DtoOptions,
            MediaTypes = MediaTypes,
            SourceTypes = SourceTypes,
            Limit = QueryPageLimit,
            OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) }
        };

        var totalCount = _libraryManager.GetCount(query);
        if (totalCount == 0)
        {
            _logger.LogInformation("Finnrr auto-sync: no audio items found.");
            return;
        }

        var maxTracks = Math.Max(configuration.AutoSyncMaxTracksPerScan, 1);
        var processedCount = 0;
        var fetchedCount = 0;

        for (var startIndex = 0; startIndex < totalCount && processedCount < maxTracks; startIndex += QueryPageLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            query.StartIndex = startIndex;
            var result = _libraryManager.GetItemsResult(query);

            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (processedCount >= maxTracks)
                {
                    break;
                }

                if (item is not Audio audioItem)
                {
                    continue;
                }

                processedCount++;
                progress.Report(100d * processedCount / maxTracks);

                var itemKey = audioItem.Id.ToString("N", CultureInfo.InvariantCulture);
                if (retryState.Entries.TryGetValue(itemKey, out var entry)
                    && entry.NextRetryUtc > nowUtc)
                {
                    // Backoff: don't hammer lrclib.net for tracks that recently had no lyrics.
                    continue;
                }

                try
                {
                    var existingLyrics = await _lyricManager.GetLyricsAsync(audioItem, cancellationToken).ConfigureAwait(false);
                    if (existingLyrics is not null)
                    {
                        continue;
                    }

                    var results = await _lyricManager.SearchLyricsAsync(audioItem, true, cancellationToken).ConfigureAwait(false);
                    if (results.Count == 0)
                    {
                        // Leave retry bookkeeping to the scheduled task.
                        continue;
                    }

                    await _lyricManager.DownloadLyricsAsync(audioItem, results[0].Id, cancellationToken).ConfigureAwait(false);
                    fetchedCount++;
                    _logger.LogInformation("Finnrr auto-sync: fetched lyrics for {Name}", audioItem.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Finnrr auto-sync: failed for {Path}", audioItem.Path);
                }
            }
        }

        _logger.LogInformation(
            "Finnrr auto-sync complete: {FetchedCount} tracks got lyrics ({ProcessedCount} checked).",
            fetchedCount,
            processedCount);
    }
}
