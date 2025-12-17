# BitcoderCZ.Fancade.Editing

Additional utils for easier editing of fancade games.

### CodeWriter
```csharp
using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Builders;
using BitcoderCZ.Fancade.Editing.Scripting.Placers;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using MathUtils.Vectors;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

// calculate Fibonacci Sequence
var builder = new GameFileBlockBuilder(null, "Level 1", PrefabType.Level);
var placer = new GroundCodePlacer(builder); // TowerCodePlacer produces more compact, but less readable code
var writer = new CodeWriter(placer, new TerminalConnector(builder.Connect));

Variable a = new Variable("a", SignalType.Float);
Variable b = new Variable("b", SignalType.Float);
Variable c = new Variable("c", SignalType.Float);
Variable iter = new Variable("iter", SignalType.Float);

float maxIter = 10;

using (writer.StatementBlock())
{
    writer.If(LessThan(Variable(iter), Number(maxIter)), writer =>
    {
        writer.PlaySensor(writer =>
        {
            writer.SetVariable(a, Number(0f));
            writer.SetVariable(b, Number(1f));

            writer.Inspect(Variable(a));
            writer.Inspect(Variable(b));
        });

        writer.SetVariable(c, AddNumbers(Variable(a), Variable(b)));
        InspectResult();
        writer.SetVariable(a, Variable(b));
        writer.SetVariable(b, Variable(c));

        writer.IncrementNumber(Variable(iter));
    }, null);
}

writer.Flush(); // required if using goto or scoped CodePlacer

using (FileStream fs = File.OpenWrite("game.fcg"))
{
    builder.Build(int3.Zero).SaveCompressed(fs);
}

void InspectResult()
{
    writer.Inspect(Variable(c));
}
```
Results in:
![CodeWriter example result](https://github.com/BitcoderCZ/Fancade_Loader_Lib/blob/main/images/CodeWriter.png?raw=true)