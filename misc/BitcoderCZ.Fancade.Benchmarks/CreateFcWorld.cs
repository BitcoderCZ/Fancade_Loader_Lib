using System.Numerics;
using BenchmarkDotNet.Attributes;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Runtime;
using BitcoderCZ.Fancade.Runtime.Simulated.Bullet;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Benchmarks;

[MemoryDiagnoser]
public class CreateFcWorld
{
    private Game game;
    private ushort levelId;
    private MyCtx ctx = new();
    private Interpreter runner;

    [GlobalSetup]
    public void Load()
    {
        using (var file = File.OpenRead("/home/bitcoder/Downloads/rick.fcg"))
        {
            game = Game.LoadCompressed(file);
        }

        levelId = game.Prefabs.FirstOrDefault(prefab => prefab.Name is "New Level").Id;

        var ast = FcAST.Parse(game.Prefabs, levelId);
        runner = new Interpreter(ast, ctx);
    }

    [Benchmark]
    public FcWorld Create()
    {
        return FcWorld.Create(levelId, game.Prefabs, ctx, fullCtx => runner, false);
    }

    [Benchmark]
    public FcWorld CreateSingleThread()
    {
        return FcWorld.Create(levelId, game.Prefabs, ctx, fullCtx => runner, false, createMultiThreaded: false);
    }

    private sealed class MyCtx : IRuntimeContext
    {
        public Vector2 ScreenSize => new Vector2(1920f, 1080f);

        public Vector3 Accelerometer => new Vector3(0f, -9.8f, 0f);

        public long CurrentFrame { get; private set; }

        public bool TakingBoxArt { get; internal set; }

        public void StepFrame()
            => CurrentFrame++;

        public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
        {
        }

        public FcConstraint AddConstraint(FcObject @base, FcObject part, Vector3? pivot, EnvironmentPosition blockPosition)
            => FcConstraint.Null;

        public void AddForce(FcObject @object, Vector3? force, Vector3? applyAt, Vector3? torque, EnvironmentPosition blockPosition)
        {
        }

        public void AdjustVolumePitch(float channel, float? volume, float? pitch)
        {
        }

        public void AngularLimits(FcConstraint constraint, Vector3 lower, Vector3 upper)
        {
        }

        public void AngularSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping)
        {
        }

        public void AngularMotor(FcConstraint constraint, Vector3 speed, Vector3 force)
        {
        }

        public FcObject CreateObject(FcObject original, EnvironmentPosition blockPosition)
            => default;

        public void DestroyObject(FcObject @object)
        {
        }

        public bool GetButtonPressed(ButtonType type, EnvironmentPosition blockPosition)
            => false;

        public Vector3 GetJoystickDirection(JoystickType type, EnvironmentPosition blockPosition)
            => default;

        public FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId)
            => default;

        public (Vector3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object, EnvironmentPosition blockPosition)
            => default;

        public float GetRandomValue(float min, float max)
            => default;

        public (Vector3 Min, Vector3 Max) GetSize(FcObject @object)
            => default;

        public (Vector3 Velocity, Vector3 Spin) GetVelocity(FcObject @object)
            => default;

        public void LinearLimits(FcConstraint constraint, Vector3 lower, Vector3 upper)
        {
        }

        public void LinearMotor(FcConstraint constraint, Vector3 speed, Vector3 force)
        {
        }

        public void LinearSpring(FcConstraint constraint, Vector3 stiffness, Vector3 damping)
        {
        }

        public void Lose(int delay)
        {
        }

        public void MenuItem(Variable? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
        {
        }

        public float PlaySound(float volume, float pitch, bool loop, FcSound sound)
            => 0f;

        public (bool Hit, Vector3 HitPos, FcObject HitObj) Raycast(Vector3 from, Vector3 to)
            => default;

        public (Vector3 WorldNear, Vector3 WorldFar) ScreenToWorld(Vector2 screenPos)
            => default;

        public void SetBounciness(FcObject @object, float bounciness, EnvironmentPosition blockPosition)
        {
        }

        public void SetCamera(Vector3? position, Quaternion? rotation, float? range, bool perspective)
        {
        }

        public void SetFriction(FcObject @object, float friction, EnvironmentPosition blockPosition)
        {
        }

        public void SetGravity(Vector3 gravity, EnvironmentPosition blockPosition)
        {
        }

        public void SetLight(Vector3? position, Quaternion? rotation)
        {
        }

        public void SetLocked(FcObject @object, Vector3? position, Vector3? rotation, EnvironmentPosition blockPosition)
        {
        }

        public void SetMass(FcObject @object, float mass, EnvironmentPosition blockPosition)
        {
        }

        public void SetPosition(FcObject @object, Vector3? position, Quaternion? rotation, EnvironmentPosition blockPosition)
        {
        }

        public void SetRandomSeed(float seed)
        {
        }

        public void SetScore(float? score, float? coins, Ranking ranking)
        {
        }

        public void SetVelocity(FcObject @object, Vector3? velocity, Vector3? spin, EnvironmentPosition blockPosition)
        {
        }

        public void SetVisible(FcObject @object, bool visible)
        {
        }

        public void StopSound(float channel)
        {
        }

        public bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out Vector3 normal)
        {
            secondObject = default;
            impulse = default;
            normal = default;
            return false;
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
            => default;
    }
}