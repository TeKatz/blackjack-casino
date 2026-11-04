using BlackjackCasino.Core.Jigsaw;
namespace BlackjackCasino.Tests;
public sealed class JigsawTests
{
    [Fact] public void ThousandPiecesAreSupported() => Assert.Equal(1000, JigsawGame.Start("neon-city", 1000).Pieces.Count);
}
