using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

internal sealed record CollectionIllustration(string Id, string Atlas, int X, int Y, int Width, int Height, string Subject);
internal sealed record CollectionArtAtlas(string Id, string File, int Width, int Height, string Sha256);

/// <summary>Every stable card ID maps to its own audited illustration region. No shared-symbol fallback.</summary>
internal static class CollectionIllustrations
{
    private const int MaxCachedAtlases = 3;

    private sealed record ArtManifest(int Version, CollectionArtAtlas[] Atlases, CollectionIllustration[] Cards);

    private sealed class AtlasCacheEntry
    {
        internal AtlasCacheEntry(BitmapImage atlas, LinkedListNode<string> recencyNode)
        {
            Atlas = atlas;
            RecencyNode = recencyNode;
        }

        internal BitmapImage Atlas { get; }
        internal LinkedListNode<string> RecencyNode { get; }
        internal Dictionary<string, CroppedBitmap> Images { get; } = new(StringComparer.Ordinal);
    }

    private static readonly ArtManifest Manifest = ReadManifest();
    private static readonly Dictionary<string, CollectionIllustration> Definitions = Manifest.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
    private static readonly Dictionary<string, CollectionArtAtlas> AtlasDefinitions = Manifest.Atlases.ToDictionary(a => a.Id, StringComparer.Ordinal);
    private static readonly Dictionary<string, AtlasCacheEntry> AtlasCache = new(StringComparer.Ordinal);
    private static readonly LinkedList<string> AtlasRecency = new();
    private static readonly object Gate = new();

    internal static IReadOnlyCollection<CollectionIllustration> All => Definitions.Values;
    internal static int AtlasCacheCapacity => MaxCachedAtlases;

    internal static int CachedAtlasCount
    {
        get
        {
            lock (Gate) return AtlasCache.Count;
        }
    }

    internal static int CachedImageCount
    {
        get
        {
            lock (Gate) return AtlasCache.Values.Sum(entry => entry.Images.Count);
        }
    }

    internal static long AtlasLoadCount { get; private set; }
    internal static long AtlasEvictionCount { get; private set; }

    internal static CollectionIllustration Definition(CollectionCard card) => Definitions.TryGetValue(card.Id, out var art)
        ? art : throw new InvalidDataException("Missing card illustration: " + card.Id);

    internal static CroppedBitmap ImageFor(CollectionCard card)
    {
        lock (Gate)
        {
            CollectionIllustration art = Definition(card);
            AtlasCacheEntry entry = GetOrLoadAtlas(art.Atlas);
            if (entry.Images.TryGetValue(card.Id, out CroppedBitmap? image)) return image;

            image = new CroppedBitmap(entry.Atlas, new Int32Rect(art.X, art.Y, art.Width, art.Height));
            image.Freeze();
            entry.Images.Add(card.Id, image);
            return image;
        }
    }

    private static AtlasCacheEntry GetOrLoadAtlas(string atlasId)
    {
        if (AtlasCache.TryGetValue(atlasId, out AtlasCacheEntry? cached))
        {
            Touch(cached);
            return cached;
        }

        CollectionArtAtlas definition = AtlasDefinitions[atlasId];
        BitmapImage atlas = LoadAtlas(definition);
        LinkedListNode<string> recencyNode = AtlasRecency.AddFirst(atlasId);
        var entry = new AtlasCacheEntry(atlas, recencyNode);
        AtlasCache.Add(atlasId, entry);
        AtlasLoadCount++;
        TrimCache();
        return entry;
    }

    private static BitmapImage LoadAtlas(CollectionArtAtlas entry)
    {
        var uri = new Uri("pack://application:,,,/BlackjackCasino.Desktop;component/Assets/CollectionArt/" + entry.File);
        using Stream stream = Application.GetResourceStream(uri)?.Stream
            ?? throw new InvalidDataException("Missing illustration atlas: " + entry.File);
        var atlas = new BitmapImage();
        atlas.BeginInit();
        atlas.CacheOption = BitmapCacheOption.OnLoad;
        atlas.StreamSource = stream;
        atlas.EndInit();
        atlas.Freeze();
        if (atlas.PixelWidth != entry.Width || atlas.PixelHeight != entry.Height)
            throw new InvalidDataException("Illustration atlas dimensions changed: " + entry.File);
        return atlas;
    }

    private static void Touch(AtlasCacheEntry entry)
    {
        if (ReferenceEquals(AtlasRecency.First, entry.RecencyNode)) return;
        AtlasRecency.Remove(entry.RecencyNode);
        AtlasRecency.AddFirst(entry.RecencyNode);
    }

    private static void TrimCache()
    {
        while (AtlasCache.Count > MaxCachedAtlases)
        {
            LinkedListNode<string>? oldest = AtlasRecency.Last;
            if (oldest is null) throw new InvalidOperationException("Collection artwork atlas recency cache is inconsistent.");
            AtlasRecency.RemoveLast();
            if (!AtlasCache.Remove(oldest.Value))
                throw new InvalidOperationException("Collection artwork atlas cache is inconsistent.");
            AtlasEvictionCount++;
        }
    }

    private static ArtManifest ReadManifest()
    {
        using Stream stream = typeof(CollectionIllustrations).Assembly.GetManifestResourceStream("BlackjackCasino.CollectionArt.manifest.json")
            ?? throw new InvalidDataException("The card illustration manifest is missing.");
        ArtManifest manifest = JsonSerializer.Deserialize<ArtManifest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The card illustration manifest is empty.");
        if (manifest.Version != 1 || manifest.Cards.Length != CollectionCatalog.Cards.Count
            || manifest.Cards.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Cards.Length
            || CollectionCatalog.Cards.Any(c => !manifest.Cards.Any(a => a.Id == c.Id)))
            throw new InvalidDataException("The illustration manifest must cover every card exactly once.");
        return manifest;
    }
}
