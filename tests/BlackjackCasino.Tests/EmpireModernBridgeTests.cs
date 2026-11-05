using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class EmpireModernBridgeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private static EmpireService Estate()
    {
        var state = EmpireState.Normalize(new() { Started = true, LastAccruedAt = Now, Prestige = 25,
            Credits = EmpireRules.MaximumCredits, Levels = EmpireCatalog.Departments.ToDictionary(r => r.Id, _ => 100),
            Research = new() { ["construction"] = 2, ["compound"] = 1, ["automation"] = 1 } });
        var property = state.Properties[state.ActivePropertyId] with { Projects = new(["executive-offices", "resort-wing", "skyline-property"]) };
        return new(state with { Properties = new(state.Properties) { [state.ActivePropertyId] = property } });
    }
    private static string Message(EmpireModernSession session, string action = "upgrade", string? id = "slots", object? automation = null)
        => JsonSerializer.Serialize(new { version = 1, session = session.Id, expectedRevision = session.Revision,
            requestId = Guid.NewGuid().ToString("D"), action, id, levels = 1, automation });
    private static EmpireModernCommand Command(EmpireModernSession session, string action = "upgrade", string? id = "slots")
        => EmpireModernProtocol.Parse(Message(session, action, id))!;

    [Theory]
    [InlineData("https://midnight-manor.local/empire.html", true)]
    [InlineData("http://midnight-manor.local/empire.html", false)]
    [InlineData("https://midnight-manor.local/lobby.html", false)]
    [InlineData("https://midnight-manor.local/empire.html?x=1", false)]
    [InlineData("https://midnight-manor.local:8443/empire.html", false)]
    [InlineData("https://attacker.local/empire.html", false)]
    [InlineData("https://user@midnight-manor.local/empire.html", false)]
    public void OnlyExactPackagedOriginIsTrusted(string url, bool expected) => Assert.Equal(expected, EmpireModernProtocol.Trusted(url));

    [Fact]
    public void InvalidVersionsIdsNumbersAndOversizeCommandsAreRejected()
    {
        var session = new EmpireModernSession();
        string json = Message(session);
        Assert.NotNull(EmpireModernProtocol.Parse(json));
        foreach (string bad in new[] { json.Replace("\"version\":1", "\"version\":2"), json.Replace("\"slots\"", "\"no-room\""),
            json.Replace("\"levels\":1", "\"levels\":-1"), json.Replace("\"levels\":1", "\"levels\":1.5"),
            json.Replace("\"levels\":1", "\"levels\":101"), json.Replace("\"expectedRevision\":0", "\"expectedRevision\":-1"),
            "[]", "not json", new string('x', 8193) }) Assert.Null(EmpireModernProtocol.Parse(bad));
    }

    [Fact]
    public void DuplicateStaleAndOldSessionRequestsCannotBuyAgain()
    {
        var service = Estate(); var session = new EmpireModernSession();
        EmpireModernCommand command = Command(session);
        int commits = 0;
        EmpireActionResult Commit(EmpireModernCommand c) { commits++; return service.Transact(s => EmpireModernProtocol.Apply(s, c, Now), _ => { }); }
        var first = session.Execute(command, Commit, _ => throw new InvalidOperationException());
        var duplicate = session.Execute(command, Commit, _ => throw new InvalidOperationException());
        Assert.True(first.Success); Assert.Equal(first, duplicate); Assert.Equal(1, commits); Assert.Equal(101, service.Level("slots"));
        Assert.False(session.Execute(command with { RequestId = Guid.NewGuid().ToString("D") }, Commit, _ => null!).Success);
        var reopened = new EmpireModernSession();
        Assert.False(reopened.Execute(command, Commit, _ => null!).Success);
        Assert.Equal(1, commits);
    }

    [Fact]
    public void FailedPersistenceKeepsCreditsAndRevisionAndDoesNotCacheAPurchase()
    {
        var service = Estate(); var session = new EmpireModernSession(); var command = Command(session);
        string before = JsonSerializer.Serialize(service.Snapshot);
        EmpireActionResult Commit(EmpireModernCommand c)
        {
            try { return service.Transact(s => EmpireModernProtocol.Apply(s, c, Now), _ => throw new IOException("disk")); }
            catch (IOException) { return new(false, "Save failed"); }
        }
        Assert.False(session.Execute(command, Commit, _ => null!).Success);
        Assert.Equal(0L, session.Revision);
        Assert.Equal(before, JsonSerializer.Serialize(service.Snapshot));
    }

    [Theory]
    [InlineData("upgrade", "slots")]
    [InlineData("research", "construction")]
    [InlineData("project", "casino-vault")]
    [InlineData("property.buy", "neon-bay")]
    [InlineData("event", "blackjack")]
    [InlineData("contract.accept", "expansion-drive")]
    [InlineData("manager", "elena")]
    public void BridgePreviewIsIsolatedAndMatchesCommittedCore(string action, string id)
    {
        var service = Estate(); var session = new EmpireModernSession(); var command = Command(session, action, id);
        if (action == "manager") command = command with { RoomId = "slots" };
        string before = JsonSerializer.Serialize(service.Snapshot);
        var preview = service.Preview("slots", (candidate, at) => EmpireModernProtocol.Apply(candidate, command, at));
        Assert.Equal(before, JsonSerializer.Serialize(service.Snapshot));
        var result = service.Transact(s => EmpireModernProtocol.Apply(s, command, Now), _ => { });
        Assert.True(result.Success); Assert.True(preview.Success);
        Assert.Equal(preview.BudgetAfter, service.Credits);
        Assert.Equal(preview.PortfolioAfter, service.TotalCreditsPerSecond);
        Assert.Equal(preview.RoomAfter, service.ProductionBreakdown("slots"));
    }

    [Fact]
    public void AutomationUsesExactInvariantDecimalsInGermanCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var session = new EmpireModernSession();
            var config = new { enabled = true, reserve = "1000000.50", spendPercent = "0.125", targetLevel = 110, cheapestMilestone = false, priorities = new[] { "slots", "tables" } };
            var command = EmpireModernProtocol.Parse(Message(session, "automation", null, config));
            Assert.NotNull(command); Assert.Equal(1000000.50m, command.Automation!.Reserve); Assert.Equal(.125m, command.Automation.SpendPercent);
            var service = Estate(); Assert.True(EmpireModernProtocol.Apply(service, command, Now).Success);
            Assert.Equal(config.targetLevel, service.Automation.TargetLevel);
            Assert.Null(EmpireModernProtocol.Parse(Message(session, "automation", null, config with { spendPercent = "0.51" })));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void InspectingARoomDoesNotTurnManagerRemovalIntoAnAssignment()
    {
        var service = Estate();
        Assert.True(service.AssignManager("elena", "hotel", Now).Success);
        var command = Command(new EmpireModernSession(), "manager", "elena") with { InspectRoomId = "hotel" };
        var preview = service.Preview(command.InspectRoomId!, (candidate, at) => EmpireModernProtocol.Apply(candidate, command, at));
        Assert.True(preview.Success);
        Assert.Equal("hotel", service.ManagerAssignments["elena"]);
        Assert.True(EmpireModernProtocol.Apply(service, command, Now).Success);
        Assert.False(service.ManagerAssignments.ContainsKey("elena"));
        Assert.Equal(preview.RoomAfter, service.ProductionBreakdown("hotel"));
    }

    [Fact]
    public void ReadOnlySnapshotPreservesSaveSchemaAndEveryCatalogEntry()
    {
        var service = Estate(); var before = JsonSerializer.Serialize(service.Snapshot);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(EmpireModernSnapshot.Create(service), options));
        var root = doc.RootElement;
        Assert.Equal(15, root.GetProperty("rooms").GetArrayLength());
        Assert.Equal(3, root.GetProperty("floors").GetArrayLength());
        Assert.Equal(EmpirePropertyCatalog.All.Count, root.GetProperty("properties").GetArrayLength());
        Assert.Equal(EmpireResearchCatalog.All.Count, root.GetProperty("research").GetArrayLength());
        Assert.Equal(service.Credits.ToString(CultureInfo.InvariantCulture), root.GetProperty("credits").GetString());
        Assert.Equal(before, JsonSerializer.Serialize(service.Snapshot));
    }
}
