# Fancade Loader Lib
C# lib for loading and saving games for [Fancade](https://www.fancade.com/)

## Usage
```csharp
using FancadeLoaderLib;

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
