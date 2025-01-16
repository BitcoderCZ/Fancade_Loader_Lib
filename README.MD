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
Prefab level = Prefab.CreateLevel("Level 1");

level.Blocks.SetBlock(0, 0, 0, 1);

game.Prefabs.Add(level);

// Save game
using (FileStream fs = File.OpenWrite("game.fcg"))
{
	game.SaveCompressed(fs);
}
```
