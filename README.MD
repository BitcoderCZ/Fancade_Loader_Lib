# Fancade Loader Lib
C# lib for loading and saving games for [Fancade](https://www.fancade.com/)

## Usage
```csharp
using FancadeLoaderLib;
using FancadeLoaderLib.Raw;

// Load game
Game game;
using (FileStream fs = File.OpenRead("game.fcg"))
{
	game = Game.LoadCompressed(fs);
}

// Add a level
Prefab level = Prefab.CreateLevel(RawGame.CurrentNumbStockPrefabs, "Level 1");

level.Blocks.SetBlock(new int3(0, 0, 0), 1);

game.Prefabs.AddPrefab(level);

// Save game
using (FileStream fs = File.OpenWrite("game.fcg"))
{
	game.SaveCompressed(fs);
}
```

### CodeWriter
```csharp
using FancadeLoaderLib;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Builders;
using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Utils;
using MathUtils.Vectors;
using static FancadeLoaderLib.Editing.Scripting.CodeWriter.Expressions;

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

 using (FileStream fs = File.OpenWrite("game.fcg"))
 {
     builder.Build(int3.Zero).SaveCompressed(fs);
 }

 void InspectResult()
 {
     writer.Inspect(Variable(c));
 }
```