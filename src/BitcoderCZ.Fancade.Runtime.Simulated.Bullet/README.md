# BitcoderCZ.Fancade.Runtime.Simulated.Bullet

Run fancade games with bullet physics.

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
var runner = FcWorld.Create(idOfLevelToExecute, game.Prefabs, ctx, fullCtx =>
{
    // create script runner: Interpreter or FcAstCompiler
    return new Interpreter(ast, fullCtx);
});

// execute 1s (60 frames)
for (var i = 0; i < 60; i++)
{
    var lateUpdate = runner.RunFrame();

    // run late update blocks
    lateUpdate();

    // or simply:
    // runner.RunFrame(1f / 60f);
}

class MyCtx : IRuntimeContextBase
{
    public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
    {
        var msg = $"[INSP] {value.GetValueOfType(type)}, {(variableName is not null ? $"Var: {variableName}, " : string.Empty)}Pos: {inspectBlockPosition}, Type: {type}";
        Console.WriteLine(msg);
    }

    // ...
}
```