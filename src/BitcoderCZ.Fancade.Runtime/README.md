# BitcoderCZ.Fancade.Runtime

Allows running fancade scripts.

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

// create interpreter
var ctx = new MyCtx();
var interpreter = new Interpreter(ast, ctx);

// execute 1s (60 frames)
for (var i = 0; i < 60; i++)
{
    var lateUpdate = interpreter.RunFrame();

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