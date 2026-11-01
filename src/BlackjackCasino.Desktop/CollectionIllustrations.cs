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
    private sealed record ArtManifest(int Version, CollectionArtAtlas[] Atlases, CollectionIllustration[] Cards);
    private static readonly ArtManifest Manifest = ReadManifest();
    private static readonly Dictionary<string, CollectionIllustration> Definitions = Manifest.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
    private static readonly Dictionary<string, CollectionArtAtlas> AtlasDefinitions = Manifest.Atlases.ToDictionary(a => a.Id, StringComparer.Ordinal);
    private static readonly Dictionary<string, BitmapImage> Atlases = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, CroppedBitmap> Images = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    internal static IReadOnlyCollection<CollectionIllustration> All => Definitions.Values;
    internal static CollectionIllustration Definition(CollectionCard card) => Definitions.TryGetValue(card.Id, out var art)
        ? art : throw new InvalidDataException("Missing card illustration: " + card.Id);

    internal static CroppedBitmap ImageFor(CollectionCard card)
    {
        lock (Gate)
        {
            if (Images.TryGetValue(card.Id, out CroppedBitmap? image)) return image;
            CollectionIllustration art = Definition(card);
            if (!Atlases.TryGetValue(art.Atlas, out BitmapImage? atlas))
            {
                CollectionArtAtlas entry = AtlasDefinitions[art.Atlas];
                var uri = new Uri("pack://application:,,,/BlackjackCasino.Desktop;component/Assets/CollectionArt/" + entry.File);
                using Stream stream = Application.GetResourceStream(uri)?.Stream
                    ?? throw new InvalidDataException("Missing illustration atlas: " + entry.File);
                atlas = new BitmapImage();
                atlas.BeginInit(); atlas.CacheOption = BitmapCacheOption.OnLoad; atlas.StreamSource = stream; atlas.EndInit(); atlas.Freeze();
                if (atlas.PixelWidth != entry.Width || atlas.PixelHeight != entry.Height)
                    throw new InvalidDataException("Illustration atlas dimensions changed: " + entry.File);
                Atlases.Add(art.Atlas, atlas);
            }
            image = new CroppedBitmap(atlas, new Int32Rect(art.X, art.Y, art.Width, art.Height));
            image.Freeze(); Images.Add(card.Id, image);
            return image;
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
