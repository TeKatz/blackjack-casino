using System.Windows;
using System.Windows.Controls;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly CasinoModeRegistry modeRegistry=new();
    private CasinoLobbyView? lobby;
    internal CasinoModeRegistry Modes=>modeRegistry;
    private void InitializeLobby()
    {
        InitializeEmpire();
        modeRegistry.Register(new("blackjack","Blackjack","The velvet tables","A quiet seat, a fresh shoe and your next decision. Solo or with the house regulars.","#537D66","♠","Table games",()=>new LobbyGameArtwork(false)),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Blackjack · your table";});
        modeRegistry.Register(new("midnight-reels","Midnight Reels","The golden floor","Three reels, a centre payline and a little old-school casino charm.","#C69A51","7 7 7","Classic slots",()=>new LobbyGameArtwork(true)),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Midnight Reels · classic slots";EmbeddedSlots_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("neon-vault","Neon Vault","The high-voltage floor","Five reels, 1,024 ways and Vault Break Free Spins. High Roller stakes up to $250K.","#55DDF8","◇","Video slots",()=>new NeonVaultArtwork()),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Neon Vault · high roller slots";EmbeddedNeonVault_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("nine-lives","Nine Lives","The moonlit rooftop","Five lines, Pounce Wilds and nine bonus spins. Chase the 5,000× Full Grid Frenzy.","#C95CFF","9","Video slots",()=>new NineLivesLobbyArtwork()),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Nine Lives · high risk slots";EmbeddedNineLives_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("midnight-manor","Midnight Manor","Beyond the moonlit doors","Five haunted reels. Free Spins. A Manor whose rooms remember your choices.","#9170AC","☾","Video slots",()=>new ManorBackdrop {Scene="Exterior",Opacity=.85}),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Midnight Manor · video slots";EmbeddedManor_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("casino-empire", "Casino Empire", "Your name above the door",
            "Build your midnight landmark with lasting upgrades, idle income and owner dividends.",
            "#CBA66B", "♜", "Idle casino", () => new EmpireFloorArtwork()),
            OpenEmpire,
            canOpen: () => !EmpireDetachedOpen,
            secondaryOpen: OpenEmpireDetached,
            secondaryLabel: "OPEN IN NEW WINDOW",
            canSecondaryOpen: () => !EmpirePageOpen);
        modeRegistry.Register(new("chill-lounge", "Chill Lounge", "A quieter corner",
            "Offline games at your own pace. Settle into Sudoku or Midnight Solitaire — no wagers, no rush.",
            "#7FB49F", "✦", "Relaxed games", () => new LoungeGameArtwork(false)), OpenLounge);
        modeRegistry.Register(new("kniffel", "Midnight Dice", "The dice lounge",
            "Five dice and your score sheet. Play solo or join a casino table with three AI opponents.",
            "#9DD6B3", "⚄", "Table games", () => new LoungeGameArtwork(true)), OpenKniffel);
        lobby=new CasinoLobbyView(modeRegistry,OpenProgression);
        lobby.IsVisibleChanged += (_, _) => LobbyButton.Visibility = lobby.IsVisible ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetRow(lobby,1); Grid.SetColumnSpan(lobby,2);Panel.SetZIndex(lobby,900);CasinoRoot.Children.Add(lobby);
        LobbyButton.Visibility=Visibility.Collapsed;
        TableModeText.Text="Casino lobby";

        // InitializeLobby is invoked from PresentationLoaded, so registering another Loaded
        // handler here would be too late. Configure the embedded surface synchronously now.
        ConfigureEmbeddedGameSurface();
    }
    private void ConfigureEmbeddedGameSurface()
    {
        EnsureEmbeddedGameHost();
        if(embeddedGameOverlay is null) return;
        Grid.SetRow(embeddedGameOverlay,0);
        Grid.SetRowSpan(embeddedGameOverlay,2);
        embeddedGameOverlay.IsVisibleChanged-=EmbeddedGameOverlay_IsVisibleChanged;
        embeddedGameOverlay.IsVisibleChanged+=EmbeddedGameOverlay_IsVisibleChanged;
        UpdateHostHeaderVisibility(embeddedGameOverlay.IsVisible);
    }
    private void EmbeddedGameOverlay_IsVisibleChanged(object sender,DependencyPropertyChangedEventArgs e)
        => UpdateHostHeaderVisibility((bool)e.NewValue);
    private void UpdateHostHeaderVisibility(bool embeddedGameVisible)
    {
        if(CasinoHeader.Parent is UIElement hostHeader)
        {
            hostHeader.Visibility=embeddedGameVisible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
    private void Lobby_Click(object sender,RoutedEventArgs e)
    {
        if(session.RoundInProgress) {StatusText.Text="Finish your hand before returning to the lobby.";return;}
        CloseEmbeddedGame();
        if(lobby is not null) lobby.Visibility=Visibility.Visible;
        LobbyButton.Visibility=Visibility.Collapsed;
        TableModeText.Text="Casino lobby";
    }
    private void OpenProgression(string tab)
    {
        OpenProgressionPage(tab);
    }
}
