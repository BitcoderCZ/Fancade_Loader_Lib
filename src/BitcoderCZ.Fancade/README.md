# BitcoderCZ.Fancade

C# lib for loading, saving and modifying games for Fancade.

## Usage
```csharp
using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Raw;

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