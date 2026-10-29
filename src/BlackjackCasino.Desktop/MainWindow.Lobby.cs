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
        {lobby!.Visibility=Visibility.Collapsed;EmbeddedSlots_Click(this,new RoutedEventArgs());});
        modeRegistry.Register(new("midnight-manor","Midnight Manor","Beyond the moonlit doors","Five haunted reels. Free Spins. A Manor whose rooms remember your choices.","#9170AC","☾","Video slots",()=>new ManorBackdrop {Scene="Exterior",Opacity=.85}),()=>
        {lobby!.Visibility=Visibility.Collapsed;EmbeddedManor_Click(this,new RoutedEventArgs());});
        lobby=new CasinoLobbyView(modeRegistry,OpenProgression);
        Grid.SetRow(lobby,1); Grid.SetColumnSpan(lobby,2);Panel.SetZIndex(lobby,900);CasinoRoot.Children.Add(lobby);
        TableModeText.Text="Casino lobby";
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
