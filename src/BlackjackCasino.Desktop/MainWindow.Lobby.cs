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
        modeRegistry.Register(new("blackjack","Blackjack","The velvet tables","A quiet seat, a fresh shoe and your next decision. Solo or with the house regulars.","#537D66","♠","Table games",()=>new LobbyGameArtwork(false)),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Blackjack · your table";});
        modeRegistry.Register(new("midnight-reels","Midnight Reels","The golden floor","Three reels, a centre payline and a little old-school casino charm.","#C69A51","7 7 7","Classic slots",()=>new LobbyGameArtwork(true)),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Midnight Reels · classic slots";EmbeddedSlots_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("neon-vault","Neon Vault","The high-voltage floor","Five reels, 1,024 ways, Blackout events, expanding Wilds and Vault Break Free Spins. High Roller access up to $250K.","#55DDF8","◇","Video slots",()=>new NeonVaultArtwork()),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Neon Vault · high roller slots";EmbeddedNeonVault_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("midnight-manor","Midnight Manor","Beyond the moonlit doors","Five haunted reels. Free Spins. A Manor whose rooms remember your choices.","#9170AC","☾","Video slots",()=>new ManorBackdrop {Scene="Exterior",Opacity=.85}),()=>
        {lobby!.Visibility=Visibility.Collapsed;TableModeText.Text="Midnight Manor · video slots";EmbeddedManor_Click(this,new RoutedEventArgs());});
        lobby=new CasinoLobbyView(modeRegistry,OpenProgression);
        Grid.SetRow(lobby,1); Grid.SetColumnSpan(lobby,2);Panel.SetZIndex(lobby,900);CasinoRoot.Children.Add(lobby);
        TableModeText.Text="Casino lobby";

        Loaded += (_,_) =>
        {
            if(embeddedGameOverlay is null) return;
            Grid.SetRow(embeddedGameOverlay,0);
            Grid.SetRowSpan(embeddedGameOverlay,2);
            embeddedGameOverlay.IsVisibleChanged-=EmbeddedGameOverlay_IsVisibleChanged;
            embeddedGameOverlay.IsVisibleChanged+=EmbeddedGameOverlay_IsVisibleChanged;
        };
    }
    private void EmbeddedGameOverlay_IsVisibleChanged(object sender,DependencyPropertyChangedEventArgs e)
    {
        if(CasinoHeader.Parent is UIElement hostHeader)
        {
            hostHeader.Visibility=(bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible;
        }
    }
    private void Lobby_Click(object sender,RoutedEventArgs e)
    {
        if(session.RoundInProgress) {StatusText.Text="Finish your hand before returning to the lobby.";return;}
        CloseEmbeddedGame();
        if(lobby is not null) lobby.Visibility=Visibility.Visible;
        TableModeText.Text="Casino lobby";
    }
    private void OpenProgression(string tab)
    {
        if(progressionController is null) return;
        var window=new ProgressionWindow(progressionController) {Owner=this};
        window.Loaded+=(_,_)=>window.SelectTab(tab);
        window.ShowDialog();RefreshProfileButton();
    }
}
