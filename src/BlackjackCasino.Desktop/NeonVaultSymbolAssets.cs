using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class NeonVaultSymbolAssets
{
    private static readonly IReadOnlyDictionary<NeonVaultSymbol, ImageSource> Images = Build();

    internal static ImageSource Get(NeonVaultSymbol symbol) => Images[symbol];

    private static IReadOnlyDictionary<NeonVaultSymbol, ImageSource> Build() =>
        new Dictionary<NeonVaultSymbol, ImageSource>
        {
            [NeonVaultSymbol.Jack] = Decode(NeonVaultSymbolData.Jack),
            [NeonVaultSymbol.Queen] = Decode(NeonVaultSymbolData.Queen),
            [NeonVaultSymbol.King] = Decode(NeonVaultSymbolData.King),
            [NeonVaultSymbol.Ace] = Decode(NeonVaultSymbolData.Ace),
            [NeonVaultSymbol.PulseCore] = Decode(NeonVaultSymbolData.PulseCore),
            [NeonVaultSymbol.NeonChip] = Decode(NeonVaultSymbolData.NeonChip),
            [NeonVaultSymbol.SecurityEye] = Decode(NeonVaultSymbolData.SecurityEye),
            [NeonVaultSymbol.Wild] = Decode(NeonVaultSymbolData.Wild),
            [NeonVaultSymbol.Scatter] = Decode(NeonVaultSymbolData.Scatter),
            [NeonVaultSymbol.VaultKey] = Decode(NeonVaultSymbolData.VaultKey),
        };

    private static ImageSource Decode(string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

internal static partial class NeonVaultSymbolData
{
}
