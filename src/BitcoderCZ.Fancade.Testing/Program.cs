using System.Diagnostics;
using System.Numerics;
using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime;
using BitcoderCZ.Fancade.Runtime.Simulated.Bullet;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;

Game game;
using (var file = File.OpenRead("/home/bitcoder/Downloads/XVEHG2U1IG9RW77C.fcg"))
{
    game = Game.LoadCompressed(file);
}

ushort levelId = game.Prefabs.FirstOrDefault(prefab => prefab.Name is "Level 3").Id;

Console.WriteLine("Parsing ast");
var ast = FcAST.Parse(game.Prefabs, levelId);

var ctx = new MyRuntimeCtx();
Console.WriteLine("Building level");
FcWorld world;
//while (true)
//{
    world = FcWorld.Create(levelId, game.Prefabs, ctx, fullCtx => new Interpreter(ast, fullCtx, timeout: Timeout.InfiniteTimeSpan), true);
//}

for (int i = 0; i < 60; i++)
{
    Console.WriteLine($"Running frame {i}");
    world.RunFrame(timeStep: 1f / 60f);

    ctx.Camera.Step(MyRuntimeCtx.ScreenInfo);
    Thread.Sleep(1);
}

Console.WriteLine("Done");

class MyRuntimeCtx : IRuntimeContextBase
{
    public static readonly ScreenInfo ScreenInfo = new ScreenInfo(1920f, 1080f);

    public readonly FcCamera Camera = new FcCamera(ScreenInfo);

    public Vector2 ScreenSize => new Vector2(1920, 1080);

    public Vector3 Accelerometer => new Vector3(0, -9.8f, 0);

    public bool TakingBoxArt => false;

    public void AdjustVolumePitch(float channel, float? volume, float? pitch)
    {
    }

    public bool GetButtonPressed(ButtonType type, EnvironmentPosition blockPosition)
    {
        return false;
    }

    public Vector3 GetJoystickDirection(JoystickType type, EnvironmentPosition blockPosition)
    {
        return default;
    }

    public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
    {
    }

    public void Lose(int delay)
    {
    }

    public void MenuItem(Variable? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
    {
    }

    public float PlaySound(float volume, float pitch, bool loop, FcSound sound)
    {
        return -1f;
    }

    public (Vector3 WorldNear, Vector3 WorldFar) ScreenToWorld(Vector2 screenPos)
        => Camera.ScreenToWorld(screenPos, ScreenInfo);

    public void SetCamera(Vector3? position, Quaternion? rotation, float? range, bool perspective)
        => Camera.Set(position, rotation, range, perspective, ScreenInfo);

    public void SetLight(Vector3? position, Quaternion? rotation)
    {
    }

    public void SetScore(float? score, float? coins, Ranking ranking)
    {
    }

    public void StopSound(float channel)
    {
    }

    public bool TryGetSwipe(out Vector3 direction)
    {
        direction = default;
        return false;
    }

    public bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos)
    {
        touchPos = default;
        return false;
    }

    public void Win(int delay)
    {
    }

    public Vector2 WorldToScreen(Vector3 worldPos)
        => Camera.WorldToScreen(worldPos, ScreenInfo);
}