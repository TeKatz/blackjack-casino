using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed record PlayingCardTheme(
        Color FaceA,
        Color FaceB,
        Color Border,
        Color BlackInk,
        Color RedInk,
        Color BackA,
        Color BackB,
        Color BackAccent,
        Color Ornament,
        bool DoubleFrame = false,
        bool DarkFace = false, string Mark = "♠", string Pattern = "plain", bool Premium = false)
{

    internal static PlayingCardTheme Resolve(string? variant) => variant switch
    {
        "card-art-deco" => new(
            Color.FromRgb(247, 239, 213), Color.FromRgb(228, 216, 181), Color.FromRgb(54, 46, 34),
            Color.FromRgb(28, 28, 26), Color.FromRgb(151, 43, 47), Color.FromRgb(27, 29, 31), Color.FromRgb(10, 12, 15),
            Color.FromRgb(207, 171, 90), Color.FromRgb(179, 143, 67), DoubleFrame: true, Mark: "◇"),
        "card-crimson-velvet" => new(
            Color.FromRgb(250, 239, 224), Color.FromRgb(231, 216, 207), Color.FromRgb(128, 57, 68),
            Color.FromRgb(38, 31, 34), Color.FromRgb(139, 35, 50), Color.FromRgb(102, 25, 39), Color.FromRgb(41, 13, 22),
            Color.FromRgb(216, 192, 185), Color.FromRgb(168, 91, 102), DoubleFrame: true, Mark: "◆"),
        "card-emerald-house" => new(
            Color.FromRgb(244, 241, 221), Color.FromRgb(218, 228, 205), Color.FromRgb(54, 104, 72),
            Color.FromRgb(25, 45, 34), Color.FromRgb(162, 47, 54), Color.FromRgb(18, 75, 49), Color.FromRgb(7, 34, 24),
            Color.FromRgb(225, 192, 99), Color.FromRgb(175, 143, 67), DoubleFrame: true, Mark: "♣"),
        "card-midnight-blue" => new(
            Color.FromRgb(232, 239, 246), Color.FromRgb(203, 216, 232), Color.FromRgb(76, 103, 139),
            Color.FromRgb(27, 43, 67), Color.FromRgb(154, 53, 69), Color.FromRgb(26, 51, 88), Color.FromRgb(9, 20, 42),
            Color.FromRgb(184, 207, 233), Color.FromRgb(126, 158, 197), DoubleFrame: true, Mark: "☾"),
        "card-midnight-sovereign" => new(
            Color.FromRgb(31, 32, 34), Color.FromRgb(13, 15, 18), Color.FromRgb(214, 169, 76),
            Color.FromRgb(239, 217, 166), Color.FromRgb(222, 101, 104), Color.FromRgb(16, 16, 19), Color.FromRgb(5, 7, 10),
            Color.FromRgb(242, 198, 96), Color.FromRgb(224, 176, 76), DoubleFrame: true, DarkFace: true, Mark: "✦"),
        "card-linen" => Make("#FAF4E7", "#E8DDC7", "#A89A80", "#343531", "#A33F42", "#C6B594", "#8A795C", "#FFF5DE", "#A99977", "◇", "weave"),
        "card-harbor" => Make("#F4F9FD", "#DCE9F2", "#507A9E", "#183B5B", "#B33F56", "#245578", "#122E4B", "#B7DCEB", "#6F9FBC", "≋", "lattice"),
        "card-rosewood" => Make("#FFF1DC", "#E5CCAE", "#B47959", "#3D2825", "#9E3742", "#683A34", "#2E191B", "#E4B18A", "#BD8666", "◆", "diamonds", true),
        "card-jade-filigree" => Make("#EFF7E8", "#CFDECC", "#92945F", "#1D463D", "#AC4A4B", "#306A58", "#112F2B", "#E1CF91", "#A8B680", "❖", "fans", true),
        "card-celestial-opal" => Make("#F9F1FF", "#D9EAEF", "#A8BEDD", "#383359", "#9B3763", "#514B79", "#163E4D", "#D7FFF5", "#B9ABD8", "✧", "stars", true, false, true),
        "card-solar-eclipse" => Make("#29232B", "#100F18", "#D2AF69", "#F8E5B9", "#FF9FA2", "#251D25", "#080D18", "#FFE0A0", "#CEAF72", "☀", "eclipse", true, true, true),
        _ => new(
            Color.FromRgb(248, 248, 245), Color.FromRgb(238, 238, 232), Color.FromRgb(216, 216, 210),
            Color.FromRgb(23, 26, 24), Color.FromRgb(184, 41, 49), Color.FromRgb(22, 61, 41), Color.FromRgb(10, 35, 25),
            Color.FromRgb(212, 168, 79), Color.FromRgb(190, 190, 180)),
    };


    private static PlayingCardTheme Make(string a, string b, string edge, string black, string red,
        string backA, string backB, string accent, string ornament, string mark, string pattern,
        bool doubleFrame = false, bool darkFace = false, bool premium = false) =>
        new(C(a), C(b), C(edge), C(black), C(red), C(backA), C(backB), C(accent), C(ornament), doubleFrame, darkFace, mark, pattern, premium);

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
