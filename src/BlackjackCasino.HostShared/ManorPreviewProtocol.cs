using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

/// <summary>
/// V2 local WebView2 protocol. Only C# calculates outcomes and commits the journal.
/// Every mutating request is conditional on a durable revision and carries a replay ID.
/// </summary>
internal static class ManorPreviewProtocol
{
    internal const int Version = 2;
    internal const string VirtualHost = "midnight-manor.local";
    internal const string EntryPoint = "https://midnight-manor.local/index.html";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal sealed record Command(string Type,string RequestId,long? ExpectedRevision=null,decimal? Bet=null,int? Room=null);
    internal sealed record Room(int Id,string Name,int X,int Y,string Area,bool Visited,bool Available,bool Locked);
    // Authoritative presentation metadata, not client-side navigation rules.
    internal sealed record Passage(int From,int To,bool RequiresKey);
    internal sealed record WinningLineDetail(int Number,int Count,int[] Rows,string Amount);
    internal sealed record PaytableEntry(string Symbol,string Name,int Three,int Four,int Five);
    internal sealed record RulesInfo(int Version,PaytableEntry[] Symbols,int[][] Paylines);
    internal sealed record RunSnapshot(string Status,string Message,string GhostClue,int PlayerRoom,
        int Turn,int ExitTarget,string Current,string Banked,bool HasCandle,bool HasKey,bool HasCharm,
        Room[] Rooms,Passage[] Passages);
    // Actual, already-settled C# payout; never synthesized by the 3D frontend.
    internal sealed record RunConclusion(string Status,string Message,string Multiplier,string Award);
    internal sealed record Snapshot(
        string Bankroll,string Bet,string SelectedBet,string CycleReturned,string Payout,
        string[] Cells,int[] WinningCells,bool CycleActive,int FreeSpinsRemaining,string Feature,
        bool AnimationsEnabled,bool HasCommittedSpin,string Revision,bool CanSpin,bool CanFreeSpin,
        bool LastSpinFree,int Scatters,int Doors,int WinningLines,RunSnapshot? Run,
        string? SaveNotice,WinningLineDetail[] WinningLineDetails,string Language,RulesInfo Rules,
        bool GlobalSoundEnabled,bool GlobalMusicEnabled,double GlobalMusicVolume,
        RunSnapshot? PracticeRun=null,RunConclusion? LastRun=null);

    internal static bool IsTrustedSource(string? source)
    {
        return Uri.TryCreate(source,UriKind.Absolute,out Uri? uri)
            && uri.Scheme==Uri.UriSchemeHttps
            && uri.Host.Equals(VirtualHost,StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && uri.AbsolutePath.Equals("/index.html",StringComparison.Ordinal)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    internal static bool TryParseCommand(string? source,string? body,out Command? command)
    {
        command=null;
        if(!IsTrustedSource(source) || string.IsNullOrWhiteSpace(body) || body.Length>2048)
            return false;
        try
        {
            using JsonDocument document=JsonDocument.Parse(body);
            JsonElement root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object
                || !root.TryGetProperty("protocolVersion",out JsonElement version)
                || version.ValueKind!=JsonValueKind.Number
                || !version.TryGetInt32(out int number) || number!=Version
                || !root.TryGetProperty("type",out JsonElement type) || type.ValueKind!=JsonValueKind.String
                || !root.TryGetProperty("requestId",out JsonElement request) || request.ValueKind!=JsonValueKind.String)
                return false;

            string? name=type.GetString();
            string? id=request.GetString();
            if(id is null || id.Length is <1 or >64
                || id.Any(c=>!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
                return false;

            if(name is "client.ready" or "snapshot.request" or "ui.close" or "ui.ready")
            {
                command=new(name,id);
                return true;
            }
            if(name is not ("spin.paid" or "spin.free" or "run.move" or "run.candle"
                or "run.cashout" or "run.follow" or "run.auto"
                or "practice.start" or "practice.restart" or "practice.exit"
                or "practice.move" or "practice.candle" or "practice.cashout"
                or "practice.follow" or "practice.auto"))
                return false;
            if(!root.TryGetProperty("expectedRevision",out JsonElement revision)
                || revision.ValueKind!=JsonValueKind.String
                || !long.TryParse(revision.GetString(),NumberStyles.None,CultureInfo.InvariantCulture,out long expected)
                || expected<0)
                return false;

            decimal? bet=null;
            int? room=null;
            if(name=="spin.paid")
            {
                if(!root.TryGetProperty("bet",out JsonElement stake) || stake.ValueKind!=JsonValueKind.String)
                    return false;
                string? value=stake.GetString();
                if(value is null || value.Length is <1 or >4 || value.Any(c=>!char.IsAsciiDigit(c))
                    || !decimal.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out decimal wager))
                    return false;
                ManorRules.ValidateBet(wager);
                bet=wager;
            }
            if(name is "run.move" or "practice.move")
            {
                if(!root.TryGetProperty("room",out JsonElement target) || target.ValueKind!=JsonValueKind.Number
                    || !target.TryGetInt32(out int index) || index is <0 or >16)
                    return false;
                room=index;
            }
            command=new(name,id,expected,bet,room);
            return true;
        }
        catch(Exception ex) when(ex is JsonException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    internal static Snapshot CreateSnapshot(ManorSessionState state,bool animationsEnabled,
        decimal selectedBet=25m,string? saveNotice=null,string language="en",
        bool globalSoundEnabled=true,bool globalMusicEnabled=true,double globalMusicVolume=.1d,
        ManorRunState? practice=null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ManorSpin? spin=state.LastSpin;
        string[] cells=spin?.Cells is { Length:15 } real
            ? real.Select(symbol=>symbol.ToString()).ToArray()
            : Enumerable.Range(0,5)
                .SelectMany(reel=>Enumerable.Range(0,3).Select(row=>ManorRules.SymbolAt(reel,reel*5,row).ToString()))
                .ToArray();

        var winningCells=new HashSet<int>();
        if(spin is not null)
            foreach(ManorLineWin win in spin.Wins)
            {
                if(win.Line is <0 or >=20 || win.Count is <0 or >5) continue;
                for(int reel=0;reel<win.Count;reel++)
                    winningCells.Add(reel*3+ManorRules.Paylines[win.Line][reel]);
            }

        RunSnapshot? runSnapshot=state.Run is { Status:ManorRunStatus.Hunting } active
            ? ProjectRun(active) : null;
        RunSnapshot? practiceSnapshot=practice is not null ? ProjectRun(practice) : null;
        RunConclusion? finishedRun=state.Run is { Status:not ManorRunStatus.Hunting } settled
            && state.RunPaid
                ? new(settled.Status.ToString(),settled.Message,
                    Multiplier(settled.PaidMultiplier),Money(state.Bet*settled.PaidMultiplier))
                : null;

        string feature=runSnapshot is not null
            ? "Escape the Manor · choose your path"
            : state.Run is { Status:not ManorRunStatus.Hunting } finished
                ? "Escape the Manor · "+finished.Status
                : state.FreeSpinsRemaining>0
                    ? "Haunted Night · "+state.FreeSpinsRemaining+" free spins saved"
                    : state.CycleActive ? "Saved cycle in progress" : "The house awaits";
        // A running legacy free-spin cycle must keep its persisted payout table.
        int tableVersion=state.CycleActive ? state.PaytableVersion : ManorRules.CurrentPaytableVersion;
        var rules=new RulesInfo(tableVersion,
            ManorRules.SymbolDefinitions(tableVersion)
                .Select(entry=>new PaytableEntry(entry.Symbol.ToString(),entry.Name,
                    entry.Three,entry.Four,entry.Five)).ToArray(),
            ManorRules.Paylines.Select(line=>line.ToArray()).ToArray());
        bool canSpin=!state.CycleActive;
        bool canFree=state.CycleActive && runSnapshot is null && state.FreeSpinsRemaining>0;
        return new Snapshot(Money(state.Bankroll),Money(state.Bet),Money(selectedBet),Money(state.CycleReturned),
            Money(spin?.Payout ?? 0m),cells,winningCells.Order().ToArray(),state.CycleActive,
            state.FreeSpinsRemaining,feature,animationsEnabled,spin is not null,
            state.Revision.ToString(CultureInfo.InvariantCulture),canSpin,canFree,
            spin?.Free ?? false,spin?.Scatters ?? 0,spin?.Doors ?? 0,spin?.Wins.Length ?? 0,
            runSnapshot,saveNotice,
            spin?.Wins.Where(win=>win.Line is >=0 and <20 && win.Count is >=3 and <=5)
                .Select(win=>new WinningLineDetail(win.Line+1,win.Count,
                    ManorRules.Paylines[win.Line].ToArray(),Money(win.Amount))).ToArray()
                ?? [],language=="de" ? "de" : "en",rules,
            globalSoundEnabled,globalMusicEnabled,
            double.IsFinite(globalMusicVolume) ? Math.Clamp(globalMusicVolume,0d,1d) : 0d,
            practiceSnapshot,finishedRun);
    }

    private static RunSnapshot ProjectRun(ManorRunState run)
    {
        var actual=new ManorRun(run);
        int[] available=run.Status==ManorRunStatus.Hunting
            ? ManorMap.Adjacent(run.Player,run.HasKey,player:true) : [];
        return new RunSnapshot(
            run.Status.ToString(),run.Message,actual.GhostClue,run.Player,run.Turn,run.ExitTarget,
            Multiplier(run.Multiplier(run.Current)),Multiplier(run.Multiplier(run.Banked)),
            run.HasCandle,run.HasKey,run.HasCharm,
            ManorMap.Rooms.Where(room=>room.Id<17).Select(room=>new Room(
                room.Id,room.Name,room.X,room.Y,room.Area.ToString(),
                (run.Collected & (1UL<<room.Id))!=0,
                available.Contains(room.Id),
                ManorMap.Locked(run.Player,room.Id) && !run.HasKey
            )).ToArray(),
            ManorMap.Edges.Where(edge=>edge.A<17 && edge.B<17)
                .Select(edge=>new Passage(edge.A,edge.B,ManorMap.Locked(edge.A,edge.B)))
                .ToArray());
    }

    internal static string SerializeSnapshot(Snapshot snapshot,string requestId)=>
        JsonSerializer.Serialize(new { protocolVersion=Version,type="snapshot",requestId,payload=snapshot },Json);

    internal static string SerializeError(string requestId,string message,Snapshot snapshot,bool fatal=false)=>
        JsonSerializer.Serialize(new
        {
            protocolVersion=Version,type="error",requestId,
            payload=new { message,snapshot,fatal }
        },Json);

    private static string Money(decimal amount)=>"$"+amount.ToString("N2",CultureInfo.GetCultureInfo("en-US"));
    private static string Multiplier(decimal amount)=>amount.ToString("0.##",CultureInfo.InvariantCulture)+"×";
}
