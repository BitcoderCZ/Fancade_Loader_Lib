using System.Collections.Specialized;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Loader;
using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Compiled;
using BitcoderCZ.Fancade.Runtime.Simulated.Bullet;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;
using TUnit.Assertions.Core;

namespace BitcoderCZ.Fancade.Runtime.Tests.Common;

public sealed class AstRunnerTester
{
    private static readonly IEnumerable<Func<AstRunnerTester, IRuntimeContext, AssemblyLoadContext, (IAstRunner Runner, string RunnerName)>> runnerFactories =
        [
            (tester, ctx, loadCtx) => (new Interpreter(tester._ast, ctx, tester._options.Timeout), "Interpreter"),
            (tester, ctx, loadCtx) => (FcAstCompiler.Compile(tester._ast, ctx, new(loadCtx)
            {
                Timeout = tester._options.Timeout,
                StatementExecutionMode = FcAstCompiler.StatementExecutionMode.StateMachine,
                HumanReadable = true,
            })!, "AstStateMachine"),
            (tester, ctx, loadCtx) => (FcAstCompiler.Compile(tester._ast, ctx, new(loadCtx)
            {
                Timeout = tester._options.Timeout,
                StatementExecutionMode = FcAstCompiler.StatementExecutionMode.DirectCalls,
                HumanReadable = true,
            })!, "AstDirectCalls"),
        ];

    private readonly FcAST _ast;
    private readonly Options _options;
    private readonly PhysicsConfig? _physicsConfig;
    private readonly OrderedDictionary<InspectAssertExpected, AssertionResult?> _expectedInspects = [];
    private readonly Lock _runLock = new();

    private bool _ran;
    private AssertionResult? _nonExpectedAssert;

    private AstRunnerTester(FcAST ast, Options options, PhysicsConfig? physicsConfig)
    {
        _ast = ast;
        _options = options;
        _physicsConfig = physicsConfig;
    }

    public static AstRunnerTester Create(FcAST ast, Options? options = null)
        => new AstRunnerTester(ast, options ?? new(), null);

    public static AstRunnerTester CreatePhysics(FcAST ast, PrefabList prefabs, ushort? levelId = null, Options? options = null)
        => new AstRunnerTester(ast, options ?? new(), new(levelId ?? RawGame.CurrentNumbStockPrefabs, prefabs));

    public static AstRunnerTester Create(PrefabList prefabs, ushort? levelId = null, Options? options = null)
    {
        prefabs.AddImplicitConnections();

        levelId ??= RawGame.CurrentNumbStockPrefabs;
        return Create(FcAST.Parse(prefabs, levelId.Value), options);
    }

    public static AstRunnerTester CreatePhysics(PrefabList prefabs, ushort? levelId = null, Options? options = null)
    {
        prefabs.AddImplicitConnections();

        levelId ??= RawGame.CurrentNumbStockPrefabs;
        return CreatePhysics(FcAST.Parse(prefabs, levelId.Value), prefabs, levelId, options);
    }

    public static AstRunnerTester Create(BlockBuilder builder, ushort? levelId = null, Options? options = null)
        => Create(new PrefabList([(Prefab)builder.Build(int3.Zero)]), levelId, options);

    public static AstRunnerTester CreatePhysics(BlockBuilder builder, ushort? levelId = null, Options? options = null)
        => CreatePhysics(new PrefabList([(Prefab)builder.Build(int3.Zero)]), levelId, options);

    public static AstRunnerTester Create(CodeWriter writer, ushort? levelId = null, Options? options = null)
    {
        writer.Flush();
        return Create(writer.Placer.Builder, levelId, options);
    }

    public static AstRunnerTester CreatePhysics(CodeWriter writer, ushort? levelId = null, Options? options = null)
    {
        writer.Flush();
        return CreatePhysics(writer.Placer.Builder, levelId, options);
    }

    public static AstRunnerTester Create(CodeWriter writer, PrefabList prefabs, ushort? levelId = null, Options? options = null)
    {
        writer.Flush();
        var prefab = (Prefab)writer.Placer.Builder.Build(int3.Zero);

        Debug.Assert(prefabs.ContainsPrefab(prefab.Id));
        prefabs.AddImplicitConnections();

        return Create(prefabs, levelId ?? prefab.Id, options);
    }

    public static AstRunnerTester CreatePhysics(CodeWriter writer, PrefabList prefabs, ushort? levelId = null, Options? options = null)
    {
        writer.Flush();
        var prefab = (Prefab)writer.Placer.Builder.Build(int3.Zero);

        Debug.Assert(prefabs.ContainsPrefab(prefab.Id));
        prefabs.AddImplicitConnections();

        return CreatePhysics(prefabs, levelId ?? prefab.Id, options);
    }

    public void AddExpectedInspect(InspectAssertExpected inspect)
        => _expectedInspects.Add(inspect, null);

    public AssertionResult GetResult(InspectAssertExpected inspect)
    {
        lock (_runLock)
        {
            if (!_ran)
            {
                RunAll();
                _ran = true;
            }
        }

        if (_nonExpectedAssert is { } nonExpectedAssert)
        {
            return nonExpectedAssert;
        }

        return _expectedInspects[inspect] ?? AssertionResult.Passed;
    }

    private void RunAll()
    {
        var assemblyLoadContext = new AssemblyLoadContext("TempTestFcAstCompile", isCollectible: true);

        try
        {
            Queue<Inspect> inspectQueue = new();

            foreach (var factory in runnerFactories)
            {
                var ctx = new InspectRuntimeContext(inspectQueue);

                IAstRunner runner;
                string runnerName = "Unknown";
                if (_physicsConfig is { } physics)
                {
                    runner = FcWorld.Create(physics.LevelId, physics.Prefabs, ctx, physicsCtx =>
                    {
                        var item = factory(this, physicsCtx, assemblyLoadContext);
                        runnerName = item.RunnerName;
                        return item.Runner;
                    });
                }
                else
                {
                    (runner, runnerName) = factory(this, ctx, assemblyLoadContext);
                }

                try
                {
                    Run(runner, runnerName, inspectQueue, ctx);
                }
                finally
                {
                    runner.Dispose();
                }

                Debug.Assert(inspectQueue.Count == 0);
            }
        }
        finally
        {
            assemblyLoadContext.Unload();
        }
    }

    private void Run(IAstRunner runner, string runnerName, Queue<Inspect> inspectQueue, InspectRuntimeContext ctx)
    {
        int[] matchedCount = new int[_expectedInspects.Count];
        int[] matchedThisFrame = new int[_expectedInspects.Count];

        for (int frame = 0; frame < _options.RunFor; frame++)
        {
            Debug.Assert(inspectQueue.Count == 0);

            var lateUpdate = runner.RunFrame();
            lateUpdate();

            InspectAssertExpected? lastOrderedInspect = null;

            while (inspectQueue.TryDequeue(out var inspect))
            {
                bool matched = false;

                for (int i = 0; i < _expectedInspects.Count; i++)
                {
                    var item = _expectedInspects.GetAt(i);
                    if (item.Value is { })
                    {
                        matched = true; // don't double report
                        continue; // already has error
                    }

                    var expected = item.Key;

                    if (inspect.Type == expected.Type && Equals(inspect.Value, expected.Value, expected.Type) &&
                        (expected.Position is null || inspect.InspectBlockPosition == expected.Position))
                    {
                        matched = true;

                        switch (expected.Frequency)
                        {
                            case InspectFrequency.OnlyOnOneFrame:
                                if (matchedCount[i] != matchedThisFrame[i])
                                {
                                    TryReport(i, AssertionResult.Failed($"[{runnerName}] {expected} was inspected on multiple frames"));
                                    continue;
                                }

                                break;
                        }

                        if (expected.Order is { } order)
                        {
                            if (lastOrderedInspect is { } lastInspect && lastInspect.Order > order)
                            {
                                TryReport(i, AssertionResult.Failed($"[{runnerName}] {expected} was inspected after {lastInspect}"));
                                continue;
                            }

                            lastOrderedInspect = expected;
                        }

                        matchedCount[i]++;
                        matchedThisFrame[i]++;
                    }
                }

                if (_options.AllowOnlyExpectedInspects && !matched)
                {
                    object inspectVal = inspect.Value.GetValueOfType(inspect.Type);
                    string inspected = inspectVal switch
                    {
                        float f => f.ToString("0.###"),
                        Vector3 f3 => f3.ToString("0.###"),
                        Quaternion q => $"{q.GetEuler():0.###} ({q:0.###})",
                        FcObject o => o.Value.ToString(),
                        FcConstraint c => c.Value.ToString(),
                        _ => inspectVal?.ToString() ?? "null",
                    };

                    _nonExpectedAssert = AssertionResult.Failed($"[{runnerName}] non expected inspect occurred, '{inspected}' of type {inspect.Type} at pos {inspect.InspectBlockPosition}");
                }
            }

            for (int i = 0; i < _expectedInspects.Count; i++)
            {
                var item = _expectedInspects.GetAt(i);
                if (item.Value is { })
                {
                    continue; // already has error
                }

                var expected = item.Key;

                if (expected.Frequency is InspectFrequency.EveryFrame && matchedThisFrame[i] == 0)
                {
                    TryReport(i, AssertionResult.Failed($"[{runnerName}] {expected} was not inspected on frame {frame}"));
                    continue;
                }

                if (expected.FrameCount is { } frameCount && matchedThisFrame[i] != frameCount)
                {
                    TryReport(i, AssertionResult.Failed($"[{runnerName}] {expected} was inspected {matchedThisFrame[i]} {(matchedThisFrame[i] == 1 ? "time" : "times")} on frame {frame}"));
                    continue;
                }
            }

            matchedThisFrame.AsSpan().Clear();

            ctx.StepFrame();
        }

        for (int i = 0; i < _expectedInspects.Count; i++)
        {
            var expected = _expectedInspects.GetAt(i).Key;

            if (expected.Count is { } count && matchedCount[i] > count)
            {
                TryReport(i, AssertionResult.Failed($"[{runnerName}] {expected} was inspected {matchedCount[i]} {(matchedCount[i] == 1 ? "time" : "times")}"));
                continue;
            }
        }
    }

    private static bool Equals(RuntimeValue a, object b, SignalType type)
    {
        const float MaxDeltaNumber = Constants.EqualsNumbersMaxDiff;
        const float MaxDeltaVector = Constants.EqualsVectorsMaxDiff;
        const float MaxDeltaRotationRadians = 0.01f;

        return type switch
        {
            SignalType.Float => MathF.Abs(a.Float - (float)b) < MaxDeltaNumber,
            SignalType.Vec3 => (a.Vector3 - (Vector3)b).LengthSquared() < MaxDeltaVector,
            SignalType.Rot => EqualsRotation(a.Quaternion, b),
            SignalType.Bool => a.Bool == (bool)b,
            SignalType.Obj => (FcObject)a.Int == (FcObject)b,
            SignalType.Con => (FcConstraint)a.Int == (FcConstraint)b,
            _ => throw new UnreachableException(),
        };

        static bool EqualsRotation(Quaternion a, object b)
        {
            const float DegToRad = MathF.PI / 180f;

            Quaternion bQuat;

            if (b is Quaternion q)
            {
                bQuat = q;
            }
            else if (b is Rotation r)
            {
                var e = r.Value;
                bQuat = Quaternion.CreateFromYawPitchRoll(
                    e.Y * DegToRad,
                    e.X * DegToRad,
                    e.Z * DegToRad
                );
            }
            else
            {
                throw new UnreachableException();
            }

            a = Quaternion.Normalize(a);
            bQuat = Quaternion.Normalize(bQuat);

            float dot = MathF.Abs(Quaternion.Dot(a, bQuat));
            dot = MathF.Min(dot, 1f);

            float angle = 2f * MathF.Acos(dot);

            return angle <= MaxDeltaRotationRadians;
        }
    }

    private bool TryReport(int expectedIndex, AssertionResult error)
    {
        Debug.Assert(!error.IsPassed);

        if (_expectedInspects.GetAt(expectedIndex).Value is { })
        {
            return false;
        }

        _expectedInspects.SetAt(expectedIndex, error);

        return true;
    }

    public readonly struct Options
    {
        public int RunFor { get; init; } = 2;
        public TimeSpan Timeout { get; init; } =
#if DEBUG
            TimeSpan.FromSeconds(1000);
#else
            TimeSpan.FromSeconds(4); 
#endif
        public bool AllowOnlyExpectedInspects { get; init; } = true;

        public Options()
        {
        }
    }

    private readonly record struct PhysicsConfig(ushort LevelId, PrefabList Prefabs);

    private record struct Inspect(RuntimeValue Value, SignalType Type, string? VariableName, ushort PrefabId, int3 InspectBlockPosition);

    private sealed class InspectRuntimeContext : IRuntimeContext
    {
        private readonly Queue<Inspect> _inspectQueue;
        private readonly FcRandom _rng = new();

        public InspectRuntimeContext(Queue<Inspect> inspectQueue)
        {
            Debug.Assert(inspectQueue is not null);

            _inspectQueue = inspectQueue;
        }

        public Vector2 ScreenSize => new Vector2(1920f, 1080f);

        public Vector3 Accelerometer => new Vector3(0f, -9.8f, 0f);

        public long CurrentFrame { get; private set; }

        public bool TakingBoxArt { get; internal set; }

        public void StepFrame()
            => CurrentFrame++;

        public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, int3 inspectBlockPosition)
            => _inspectQueue.Enqueue(new(value, type, variableName, prefabId, inspectBlockPosition));

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

        public FcObject CreateObject(FcObject original)
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
        {
            float val = _rng.NextSingle(min, max);
            return val;
        }

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
            => _rng.SetSeed(seed);

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