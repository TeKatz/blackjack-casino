using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class ProfileIdentityTests
{
    [Theory]
    [InlineData("  Midnight   Timo  ","Midnight Timo")]
    [InlineData("Élron ✨","Élron ✨")]
    public void NameIsNormalizedAndStored(string input,string expected)
    {
        var state=new PlayerProfileState {DisplayName=ProfileIdentity.Validate(input)};
        Assert.Equal(expected,JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(state))!.DisplayName);
    }
    [Fact]
    public void LegacyProfileKeepsProgressAndGetsSafeName()
    {
        var state=JsonSerializer.Deserialize<PlayerProfileState>("{\"SchemaVersion\":2,\"Coins\":4500,\"Level\":20}")!;
        _=new ProgressionService(state);
        Assert.Equal("Player",state.DisplayName);Assert.Equal(4500,state.Coins);Assert.Equal(20,state.Level);Assert.Equal(5,state.SchemaVersion);
        Assert.Throws<ArgumentException>(()=>ProfileIdentity.Validate("   "));
        Assert.Throws<ArgumentException>(()=>ProfileIdentity.Validate(new string('x',25)));
    }
}
