using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernSudokuBridgeTests
{
    private static string Message(ModernSudokuSession session, string action, long? revision = null,
        int? index = null, int? digit = null, string? value = null, string? requestId = null) =>
        JsonSerializer.Serialize(new
        {
            type = "lounge.sudoku.command", version = 1,
            requestId = requestId ?? Guid.NewGuid().ToString("D"),
            session = session.Id, expectedRevision = revision ?? session.Revision,
            action, index, digit, value,
        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

    [Fact]
    public void ProtocolAcceptsOnlyBoundedSudokuIntents()
    {
        var s = new ModernSudokuSession();
        foreach (string payload in new[]
        {
            Message(s, "sudoku.new", value:"Relaxed"), Message(s, "sudoku.resume", value:"Master"),
            Message(s, "sudoku.daily"), Message(s, "sudoku.back"), Message(s, "sudoku.exit"),
            Message(s, "sudoku.value",index:0,digit:9), Message(s, "sudoku.note",index:80,digit:1),
            Message(s, "sudoku.erase",index:20), Message(s, "sudoku.hint",index:-1),
            Message(s, "sudoku.undo"), Message(s, "sudoku.redo"),
            Message(s, "sudoku.timer",value:"false"), Message(s, "sudoku.guides",value:"true"),
        }) Assert.NotNull(ModernSudokuProtocol.Parse(payload));

        foreach (string payload in new[]
        {
            Message(s, "sudoku.new",value:"0"), Message(s, "sudoku.resume",value:"Unrestricted"),
            Message(s, "sudoku.value",index:-1,digit:1), Message(s, "sudoku.value",index:81,digit:1),
            Message(s, "sudoku.note",index:0,digit:10), Message(s, "sudoku.erase",index:0,value:"9"),
            Message(s, "sudoku.hint",index:-2), Message(s, "sudoku.timer",value:"maybe"),
            Message(s, "sudoku.wallet",value:"900000"), Message(s, "sudoku.daily",index:0),
            Message(s, "sudoku.value",index:1,digit:9,revision:-1),
            "{}", "[]", "not-json", new string('X',2049),
        }) Assert.Null(ModernSudokuProtocol.Parse(payload));
    }

    [Fact]
    public void ReceiptsRejectReplaysAndStaleRevisionsBeforeGameExecution()
    {
        var s = new ModernSudokuSession();
        int changes = 0;
        var first = ModernSudokuProtocol.Parse(Message(s,"sudoku.daily"))!;
        (bool Success,string Message) Save(ModernSudokuCommand _) { changes++; return (true,"Saved"); }
        ModernSudokuReply receipt=s.Execute(first,Save);
        Assert.True(receipt.Success);
        Assert.Equal(1,s.Revision);
        Assert.Equal(receipt,s.Execute(first,Save));
        Assert.Equal(1,changes);
        Assert.False(s.Execute(first with {RequestId=Guid.NewGuid().ToString("D")},Save).Success);
        Assert.Equal(1,changes);
        Assert.False(s.Execute(first with {Session=Guid.NewGuid().ToString("D"),RequestId=Guid.NewGuid().ToString("D")},Save).Success);
    }

    [Fact]
    public void FailedSudokuSaveDoesNotMutateLiveBoardOrReward()
    {
        SudokuProgressState saved = new();
        bool fail=false;
        int writes=0;
        var service=new ModernSudokuService(saved,next =>
            {if(fail)throw new IOException("Storage unavailable");saved=next;writes++;},
            () => true,_=>{},()=>true,_=>{},()=>string.Empty);
        var commands=new ModernSudokuSession();
        ModernSudokuReply start=commands.Execute(
            ModernSudokuProtocol.Parse(Message(commands,"sudoku.new",value:"Relaxed"))!,service.Execute);
        Assert.True(start.Success);
        string id=service.PuzzleId!;
        SudokuPuzzle puzzle=SudokuLibrary.Get(id);
        int index=Enumerable.Range(0,81).First(i=>puzzle.Givens[i]=='0');
        Assert.True(SudokuSolver.TrySolve(puzzle.Givens,out string solution));
        int digit=solution[index]-'0';
        fail=true;
        Assert.Throws<IOException>(()=>commands.Execute(
            ModernSudokuProtocol.Parse(Message(commands,"sudoku.value",index:index,digit:digit))!,service.Execute));
        Assert.Equal(puzzle.Givens,saved.Games[id].Board);
        Assert.Equal(id,service.PuzzleId);
        fail=false;
        Assert.True(commands.Execute(ModernSudokuProtocol.Parse(
            Message(commands,"sudoku.value",index:index,digit:digit))!,service.Execute).Success);
        Assert.Equal(digit.ToString(),saved.Games[id].Board[index].ToString());
        Assert.Equal(2,writes);
    }
}
