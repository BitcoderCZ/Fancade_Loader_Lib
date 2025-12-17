# BitcoderCZ.Fancade.Runtime.Compiled

Transpile fancade scripts into C# and run them.

> [!WARNING]  
> This package is in beta. APIs may be changed or removed.

## Sample usage
```csharp
// load game
Game game;
using (var fs = File.OpenRead("game.fcg"))
{
    game = Game.LoadCompressed(fs);
}

// build ast
var idOfLevelToExecute = RawGame.CurrentNumbStockPrefabs;
var ast = FcAST.Parse(game.Prefabs, idOfLevelToExecute);

// create runner
var ctx = new MyCtx();
var runner = FcAstCompiler.Compile(ast, ctx, new FcAstCompiler.Options(AssemblyLoadContext.Default)
{
    StatementExecutionMode = FcAstCompiler.StatementExecutionMode.StateMachine,
    TerminalInfos = PrefabTerminalInfo.Create(game.Prefabs),
});

// if you want to view the transpiled code:
//FcAstCompiler.TryCompile(ast, ctx, new FcAstCompiler.Options(AssemblyLoadContext.Default)
//{
//    StatementExecutionMode = FcAstCompiler.StatementExecutionMode.StateMachine,
//    TerminalInfos = PrefabTerminalInfo.Create(game.Prefabs),
//    HumanReadable = true,
//}, out var code, out var runner, out var diagnostics);

// execute 1s (60 frames)
for (var i = 0; i < 60; i++)
{
    var lateUpdate = runner.RunFrame();

    // run late update blocks
    lateUpdate();

    ctx.CurrentFrame++;
}

class MyCtx : IRuntimeContext
{
    public long CurrentFrame { get; set; }

    public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
    {
        var msg = $"[INSP] {value.GetValueOfType(type)}, {(variableName is not null ? $"Var: {variableName}, " : string.Empty)}Pos: {inspectBlockPosition}, Type: {type}";
        Console.WriteLine(msg);
    }

    // ...
}
```