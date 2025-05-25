using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Runtime.Compiled;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using TUnit.Assertions.AssertConditions;

namespace FancadeLoaderLib.Runtime.Tests.AssertUtils;

// TODO: allow using Bullet
internal sealed class InspectsValueAssertCondition(InspectAssertExpected[] Expected, int RunFor, TimeSpan Timeout, bool AllowOnlyExpectedInspects) : BaseAssertCondition<AST>
{
    protected override string GetExpectation()
    {
        StringBuilder builder = new StringBuilder();

        if (AllowOnlyExpectedInspects)
        {
            builder.Append("to inspect only: ");
        }
        else
        {
            builder.Append("to inspect: ");
        }

        for (int i = 0; i < Expected.Length; i++)
        {
            if (i != 0)
            {
                builder.Append(", ");
            }

            var expected = Expected[i];

            builder.Append($"'{expected.Value}' of type {expected.Type}");

            if (expected.Position is not null)
            {
                builder.Append($" at {expected.Position}");
            }

            if (expected.BoxArt is { } boxArt)
            {
                builder.Append(boxArt ? " when taking box art" : " when not taking box art");
            }

            if (expected.Count is not null)
            {
                builder.Append($" {expected.Count} {(expected.Count == 1 ? "time" : "times")} in total");
            }

            if (expected.FrameCount is not null)
            {
                builder.Append($" {expected.FrameCount} {(expected.Count == 1 ? "time" : "times")} per frame");
            }

            if (expected.Frequency is not null)
            {
                builder.Append($" {expected.Frequency}");
            }

            if (expected.Order is not null)
            {
                builder.Append($" with order {expected.Order}");
            }
        }

        return builder.ToString();
    }

    protected override ValueTask<AssertionResult> GetResult(AST? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail("ast was null");
        }

        Debug.Assert(exception is null);

        if (Expected.Any(expected => expected.BoxArt is not null))
        {
            var res = Run(actualValue, false);
            return res.IsPassed
                ? Run(actualValue, true)
                : res;
        }
        else
        {
            return Run(actualValue);
        }
    }

    private AssertionResult Run(AST ast, bool boxArt = false)
    {
        IEnumerable<Func<AST, IRuntimeContext, IAstRunner>> runnerFactories = [(ast, ctx) => new Interpreter(ast, ctx, Timeout), (ast, ctx) => AstCompiler.Compile(AstCompiler.Parse(ast, Timeout), ctx)!];

        Queue<Inspect> inspectQueue = new();

        foreach (var factory in runnerFactories)
        {
            var ctx = new InspectRuntimeContext(inspectQueue)
            {
                TakingBoxArt = boxArt,
            };

            var runner = factory(ast, ctx);

            var res = Run(runner, inspectQueue, ctx);

            if (!res.IsPassed)
            {
                return res;
            }

            Debug.Assert(inspectQueue.Count == 0);
        }

        return AssertionResult.Passed;
    }

    private AssertionResult Run(IAstRunner runner, Queue<Inspect> inspectQueue, InspectRuntimeContext ctx)
    {
        int[] matchedCount = new int[Expected.Length];
        int[] matchedThisFrame = new int[Expected.Length];

        for (int frame = 0; frame < RunFor; frame++)
        {
            Debug.Assert(inspectQueue.Count == 0);

            var lateUpdate = runner.RunFrame();
            lateUpdate();

            InspectAssertExpected? lastOrderedInspect = null;

            while (inspectQueue.TryDequeue(out var inspect))
            {
                bool matched = false;

                for (int i = 0; i < Expected.Length; i++)
                {
                    var expected = Expected[i];

                    if (inspect.Type == expected.Type && Equals(inspect.Value, expected.Value, expected.Type) &&
                        (expected.Position is null || inspect.InspectBlockPosition == expected.Position) &&
                        (expected.BoxArt is null || ctx.TakingBoxArt == expected.BoxArt))
                    {
                        matched = true;

                        switch (expected.Frequency)
                        {
                            case InspectFrequency.OnlyOnOneFrame:
                                if (matchedCount[i] != matchedThisFrame[i])
                                {
                                    return AssertionResult.Fail($"{expected} was inspected on multiple frames");
                                }

                                break;
                        }

                        if (expected.Order is { } order)
                        {
                            if (lastOrderedInspect is { } lastInspect && lastInspect.Order > order)
                            {
                                return AssertionResult.Fail($"{expected} was inspected after {lastInspect}");
                            }

                            lastOrderedInspect = expected;
                        }

                        matchedCount[i]++;
                        matchedThisFrame[i]++;
                    }
                }

                if (AllowOnlyExpectedInspects && !matched)
                {
                    object inspectVal = inspect.Value.GetValueOfType(inspect.Type);
                    string inspected = inspectVal switch
                    {
                        float f => f.ToString("0.###"),
                        float3 f3 => f3.ToString("0.###"),
                        Quaternion q => q.GetEuler().ToString("0.###"),
                        _ => inspectVal?.ToString() ?? "null",
                    };

                    return AssertionResult.Fail($"non expected inspect occured, '{inspected}' at pos {inspect.InspectBlockPosition}");
                }
            }

            for (int i = 0; i < Expected.Length; i++)
            {
                var expected = Expected[i];

                if (expected.Frequency is InspectFrequency.EveryFrame && matchedThisFrame[i] == 0)
                {
                    return AssertionResult.Fail($"{expected} was not inspected on frame {frame}");
                }

                if (expected.FrameCount is { } frameCount && matchedThisFrame[i] != frameCount)
                {
                    return AssertionResult.Fail($"{expected} was inspected on frame {matchedThisFrame[i]} {(matchedThisFrame[i] == 1 ? "time" : "times")} on frame {frame}");
                }
            }

            matchedThisFrame.AsSpan().Clear();

            ctx.StepFrame();
        }

        for (int i = 0; i < Expected.Length; i++)
        {
            var expected = Expected[i];

            if (expected.Count is { } count && matchedCount[i] > count)
            {
                return AssertionResult.Fail($"{expected} was inspected {matchedCount[i]} {(matchedCount[i] == 1 ? "time" : "times")}");
            }
        }

        return AssertionResult.Passed;
    }

    private static bool Equals(RuntimeValue a, object b, SignalType type)
    {
        const float MaxDeltaNumber = Runtime.Constants.EqualsNumbersMaxDiff;
        const float MaxDeltaVector = Runtime.Constants.EqualsVectorsMaxDiff;
        const float MaxDeltaRotation = 0.001f;

        return type switch
        {
            SignalType.Float => MathF.Abs(a.Float - (float)b) < MaxDeltaNumber,
            SignalType.Vec3 => (a.Float3 - (float3)b).LengthSquared < MaxDeltaVector,
            SignalType.Rot => Equals(a.Quaternion, ((Rotation)b).Value),
            SignalType.Bool => a.Bool == (bool)b,
            SignalType.Obj => (FcObject)a.Int == (FcObject)b,
            SignalType.Con => (FcConstraint)a.Int == (FcConstraint)b,
            _ => throw new UnreachableException(),
        };

        // TODO: does this even work?
        static bool Equals(Quaternion a, float3 bEuler)
        {
            const float DegToRad = MathF.PI / 180f;

            Quaternion b = Quaternion.CreateFromYawPitchRoll(bEuler.Y * DegToRad, bEuler.X * DegToRad, bEuler.Z * DegToRad);

            return MathF.Abs(a.X - b.X) < MaxDeltaRotation &&
                MathF.Abs(a.Y - b.Y) < MaxDeltaRotation &&
                MathF.Abs(a.Z - b.Z) < MaxDeltaRotation &&
                MathF.Abs(a.W - b.W) < MaxDeltaRotation;
        }
    }

    private record struct Inspect(RuntimeValue Value, SignalType Type, string? VariableName, ushort PrefabId, ushort3 InspectBlockPosition);

    private sealed class InspectRuntimeContext : IRuntimeContext
    {
        private readonly Queue<Inspect> _inspectQueue;
        private readonly FcRandom _rng = new();

        public InspectRuntimeContext(Queue<Inspect> inspectQueue)
        {
            Debug.Assert(inspectQueue is not null);

            _inspectQueue = inspectQueue;
        }

        public float2 ScreenSize => new float2(1920f, 1080f);

        public float3 Accelerometer => new float3(0f, -9.8f, 0f);

        public long CurrentFrame { get; private set; }

        public bool TakingBoxArt { get; internal set; }

        public void StepFrame()
            => CurrentFrame++;

        public void InspectValue(RuntimeValue value, SignalType type, string? variableName, ushort prefabId, ushort3 inspectBlockPosition)
            => _inspectQueue.Enqueue(new(value, type, variableName, prefabId, inspectBlockPosition));

        public FcConstraint AddConstraint(FcObject @base, FcObject part, float3? pivot)
            => FcConstraint.Null;

        public void AddForce(FcObject @object, float3? force, float3? applyAt, float3? torque)
        {
        }

        public void AdjustVolumePitch(float channel, float? volume, float? pitch)
        {
        }

        public void AngularLimits(FcConstraint constraint, float3 lower, float3 upper)
        {
        }

        public void AngularMotor(FcConstraint constraint, float3 speed, float3 force)
        {
        }

        public void AngularSpring(FcConstraint constraint, float3 stiffness, float3 damping)
        {
        }

        public FcObject CreateObject(FcObject original)
            => default;

        public void DestroyObject(FcObject @object)
        {
        }

        public bool GetButtonPressed(ButtonType type)
            => false;

        public float3 GetJoystickDirection(JoystickType type)
            => default;

        public FcObject GetObject(int3 position, byte3 voxelPosition, ushort prefabId)
            => default;

        public (float3 Position, Quaternion Rotation) GetObjectPosition(FcObject @object)
            => default;

        public float GetRandomValue(float min, float max)
        {
            float val = _rng.NextSingle(min, max);
            return val;
        }

        public (float3 Min, float3 Max) GetSize(FcObject @object)
            => default;

        public (float3 Velocity, float3 Spin) GetVelocity(FcObject @object)
            => default;

        public void LinearLimits(FcConstraint constraint, float3 lower, float3 upper)
        {
        }

        public void LinearMotor(FcConstraint constraint, float3 speed, float3 force)
        {
        }

        public void LinearSpring(FcConstraint constraint, float3 stiffness, float3 damping)
        {
        }

        public void Lose(int delay)
        {
        }

        public void MenuItem(VariableReference? variable, FcObject picture, string name, MaxBuyCount maxBuyCount, PriceIncrease priceIncrease)
        {
        }

        public float PlaySound(float volume, float pitch, FcSound sound)
            => 0f;

        public (bool Hit, float3 HitPos, FcObject HitObj) Raycast(float3 from, float3 to)
            => default;

        public (float3 WorldNear, float3 WorldFar) ScreenToWorld(float2 screenPos)
            => default;

        public void SetBounciness(FcObject @object, float bounciness)
        {
        }

        public void SetCamera(float3? position, Quaternion? rotation, float? range, bool perspective)
        {
        }

        public void SetFriction(FcObject @object, float friction)
        {
        }

        public void SetGravity(float3 gravity)
        {
        }

        public void SetLight(float3? position, Quaternion? rotation)
        {
        }

        public void SetLocked(FcObject @object, float3? position, float3? rotation)
        {
        }

        public void SetMass(FcObject @object, float mass)
        {
        }

        public void SetPosition(FcObject @object, float3? position, Quaternion? rotation)
        {
        }

        public void SetRandomSeed(float seed)
            => _rng.SetSeed(seed);

        public void SetScore(float? score, float? coins, Ranking ranking)
        {
        }

        public void SetVelocity(FcObject @object, float3? velocity, float3? spin)
        {
        }

        public void SetVisible(FcObject @object, bool visible)
        {
        }

        public void StopSound(float channel)
        {
        }

        public bool TryGetCollision(FcObject firstObject, out FcObject secondObject, out float impulse, out float3 normal)
        {
            secondObject = default;
            impulse = default;
            normal = default;
            return false;
        }

        public bool TryGetSwipe(out float3 direction)
        {
            direction = default;
            return false;
        }

        public bool TryGetTouch(TouchState state, int fingerIndex, out float2 touchPos)
        {
            touchPos = default;
            return false;
        }

        public void Win(int delay)
        {
        }

        public float2 WorldToScreen(float3 worldPos)
            => default;
    }
}
