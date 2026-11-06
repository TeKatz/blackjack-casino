using System.Windows;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Isolated emergency WPF lobby artwork. Neither CasinoModeRegistry nor the
/// normal React lobby knows about, constructs or references these view types.
/// </summary>
internal static class NativeLobbyArtwork
{
    internal static FrameworkElement? Create(string modeId) => modeId switch
    {
        "blackjack" => new LobbyGameArtwork(false),
        "midnight-reels" => new LobbyGameArtwork(true),
        "neon-vault" => new NeonVaultArtwork(),
        "nine-lives" => new NineLivesLobbyArtwork(),
        "midnight-manor" => new ManorBackdrop { Scene = "Exterior", Opacity = .85 },
        "casino-empire" => new EmpireFloorArtwork(),
        "midnight-merge" => new MergeLobbyArtwork(),
        "chill-lounge" => new LoungeGameArtwork(false),
        "kniffel" => new LoungeGameArtwork(true),
        _ => null,
    };
}
