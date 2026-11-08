namespace BlackjackCasino.Desktop;

/// <summary>
/// WPF-independent, canonical skin palette for Dice, the shop preview and
/// Adventure. Both the old WPF drawings and modern browser snapshot use this
/// exact contract, so switching desktop shells cannot change equipped skins.
/// </summary>
internal sealed record DicePaletteTokens(
    string Top, string Bottom, string Pips, string Edge,
    string? Glow = null, bool Engraved = false)
{
    internal static DicePaletteTokens For(string variant) => variant switch
    {
        "dice-obsidian" => new("#35404A", "#0C1119", "#F4D483", "#C4A05B"),
        "dice-jade" => new("#4A987D", "#123E36", "#F5EDD5", "#97C7A4"),
        "dice-violet" => new("#9471B7", "#35224D", "#FFE5A5", "#CBA7E7"),
        "dice-ice" => new("#B7DAEE", "#4D7594", "#132F49", "#D5EDFA"),
        "dice-ruby" => new("#AC435E", "#401A30", "#FFF0D2", "#E3A696", "#F06F95"),
        "dice-aurora" => new("#389E97", "#573776", "#E1FFF4", "#8BE5D3", "#86EDDA"),
        "dice-porcelain" => new("#FFFDF4", "#D4DFE9", "#204E83", "#6C93B7"),
        "dice-forest" => new("#527663", "#1A372D", "#F4E8CF", "#97AD8D"),
        "dice-copper" => new("#E0B38B", "#87503B", "#302A29", "#F2CE9E", Engraved: true),
        "dice-sapphire" => new("#3C70A7", "#132642", "#E5F0FD", "#A3BBD7", Engraved: true),
        "dice-astral-opal" => new("#BCAEDB", "#407777", "#172D42", "#E1EBFF", "#B7E8FF", true),
        "dice-solar-crown" => new("#37323B", "#0E1019", "#FFE0A0", "#DDBB75", "#F6CF7F", true),
        _ => new("#FFFCEE", "#D7CBB2", "#2D3730", "#BDA36E"),
    };
}
