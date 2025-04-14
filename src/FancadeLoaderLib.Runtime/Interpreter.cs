using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using FancadeLoaderLib.Runtime.Syntax;
using FancadeLoaderLib.Runtime.Syntax.Math;
using FancadeLoaderLib.Runtime.Syntax.Values;
using FancadeLoaderLib.Runtime.Syntax.Variables;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using static FancadeLoaderLib.Runtime.Utils.ThrowHelper;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime;

public sealed class Interpreter
{
    private static readonly byte3 PosOut02 = TerminalDef.GetOutPosition(0, 2, 2);
    private static readonly byte3 PosOut12 = TerminalDef.GetOutPosition(1, 2, 2);

    private static readonly byte3 PosOut03 = TerminalDef.GetOutPosition(0, 2, 3);
    private static readonly byte3 PosOut13 = TerminalDef.GetOutPosition(1, 2, 3);
    private static readonly byte3 PosOut23 = TerminalDef.GetOutPosition(2, 2, 3);

    private readonly AST _ast;
    private readonly IRuntimeContext _ctx;

    private readonly VariableAccessor _variableAccessor;

    public Interpreter(AST ast, IRuntimeContext ctx)
    {
        ThrowIfNull(ast, nameof(ast));
        ThrowIfNull(ast, nameof(ctx));

        _ast = ast;
        _ctx = ctx;

        _variableAccessor = new VariableAccessor(_ast.GlobalVariables.Concat(_ast.Variables[ast.PrefabId]));
    }

    public Action RunFrame()
    {
        Queue<(ushort3 BlockPosition, byte3 TerminalPos)> lateUpdateQueue = new Queue<(ushort3 BlockPosition, byte3 TerminalPos)>();

        foreach (var entryPoint in _ast.EntryPoints)
        {
            Execute(entryPoint, lateUpdateQueue);
        }

        return () =>
        {
            while (lateUpdateQueue.TryDequeue(out var entryPoint))
            {
                Execute(entryPoint, null);
            }
        };
    }

    private void Execute((ushort3 BlockPosition, byte3 TerminalPos) entryPoint, Queue<(ushort3 BlockPosition, byte3 TerminalPos)>? lateUpdateQueue)
    {
        Span<byte3> executeNextSpan = stackalloc byte3[16];

        Stack<(ushort3 BlockPosition, byte3 TerminalPos)> executeNext = new();

        executeNext.Push(entryPoint);

        while (executeNext.TryPop(out var item))
        {
            var (blockPos, terminalPos) = item;
            var statement = (StatementSyntax)_ast.Nodes[blockPos];

            int nextCount = 0;
            executeNextSpan[nextCount++] = TerminalDef.AfterPosition;

            // faster than switching on type
            switch (statement.PrefabId)
            {
                // **************************************** Math ****************************************
                case 485:
                    {
                        var randomSeed = (RandomSeedStatementSyntax)statement;
                        if (randomSeed.Seed is not null)
                        {
                            _ctx.SetRandomSeed(GetValue(randomSeed.Seed).Float);
                        }
                    }

                    break;

                // **************************************** Value ****************************************
                case 16 or 20 or 24 or 28 or 32:
                    {
                        var inspect = (InspectStatementSyntax)statement;
                        if (inspect.Input is not null)
                        {
                            _ctx.InspectValue(GetValue(inspect.Input), inspect.Type, _ast.PrefabId, inspect.Position);
                        }
                    }

                    break;

                // **************************************** Variables ****************************************
                case 428 or 430 or 432 or 434 or 436 or 438:
                    {
                        var setVar = (SetVaribleStatementSyntax)statement;

                        if (setVar.Value is not null)
                        {
                            _variableAccessor.SetVariableValue(_variableAccessor.GetVariableId(setVar.Variable), 0, GetValue(setVar.Value));
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException($"Prefab with id {statement.PrefabId} is not yet implemented.");
            }

            foreach (var nextTerminal in executeNextSpan[..nextCount])
            {
                foreach (var connection in statement.OutVoidConnections)
                {
                    if (connection.FromVoxel == nextTerminal)
                    {
                        executeNext.Push((connection.To, (byte3)connection.ToVoxel));
                    }
                }
            }
        }
    }

    private RuntimeValue GetValue(SyntaxTerminal? terminal)
        => GetOutput(terminal).GetValue(_variableAccessor);

    private TerminalOutput GetOutput(SyntaxTerminal? terminal)
    {
        if (terminal is null)
        {
            return TerminalOutput.Disconnected;
        }

        // faster than switching on type
        switch (terminal.Node.PrefabId)
        {
            // **************************************** Math ****************************************
            case 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var unary = (UnaryExpressionSyntax)terminal.Node;
                    var input = GetValue(unary.Input);
                    RuntimeValue value;

                    value = terminal.Node.PrefabId switch
                    {
                        90 => new(-input.Float),
                        144 => new(!input.Bool),
                        440 => new(Quaternion.Inverse(input.Quaternion)),
                        413 => new(MathF.Sin(input.Float)),
                        453 => new(MathF.Cos(input.Float)),
                        184 => new(MathF.Round(input.Float)),
                        186 => new(MathF.Floor(input.Float)),
                        188 => new(MathF.Ceiling(input.Float)),
                        455 => new(MathF.Abs(input.Float)),
                        578 => new(input.Float3.Normalized()),
                        _ => throw new UnreachableException(),
                    };

                    return new TerminalOutput(value);
                }

            case 92 or 96 or 100 or 104 or 108 or 112 or 116 or 120 or 124 or 172 or 457 or 132 or 136 or 140 or 421 or 146 or 417 or 128 or 481 or 168 or 176 or 180 or 580 or 570 or 574 or 190 or 200 or 204:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 2), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var binary = (BinaryExpressionSyntax)terminal.Node;
                    var input1 = GetValue(binary.Input1);

                    // optimize by not getting the second value when not needed
                    switch (terminal.Node.PrefabId)
                    {
                        case 146:
                            return new TerminalOutput(new RuntimeValue(input1.Bool && GetValue(binary.Input2).Bool));
                        case 417:
                            return new TerminalOutput(new RuntimeValue(input1.Bool || GetValue(binary.Input2).Bool));
                    }

                    var input2 = GetValue(binary.Input2);
                    RuntimeValue value;

                    const float EqualsNumbersMaxDiff = 0.001f;
                    const float EqualsVectorsMaxDiff = 1.0000001e-06f;

                    value = terminal.Node.PrefabId switch
                    {
                        92 => new(input1.Float + input2.Float),
                        96 => new(input1.Float3 + input2.Float3),
                        100 => new(input1.Float - input2.Float),
                        104 => new(input1.Float3 - input2.Float3),
                        108 => new(input1.Float * input2.Float),
                        112 => new(input1.Float3 * input2.Float),
                        116 => new(Vector3.Transform(input1.Float3.ToNumerics(), input2.Quaternion).ToFloat3()),
                        120 => new(input1.Quaternion * input2.Quaternion),
                        124 => new(input1.Float / input2.Float),
                        172 => new(input1.Float % input2.Float),
                        457 => new(MathF.Pow(input1.Float, input2.Float)),
                        132 => new(MathF.Abs(input1.Float - input2.Float) < EqualsNumbersMaxDiff),
                        136 => new((input1.Float3 - input2.Float3).LengthSquared < EqualsVectorsMaxDiff),
                        140 => new(input1.Int == input2.Int),
                        421 => new(input1.Bool == input2.Bool),
                        128 => new(input1.Float < input2.Float),
                        481 => new(input1.Float > input2.Float),
                        168 => new(_ctx.GetRandomValue(input1.Float, input2.Float)),
                        176 => new(MathF.Min(input1.Float, input2.Float)),
                        180 => new(MathF.Max(input1.Float, input2.Float)),
                        580 => new(MathF.Log(input1.Float, input2.Float)),
                        570 => new(float3.Dot(input1.Float3, input2.Float3)),
                        574 => new(float3.Cross(input1.Float3, input2.Float3)),
                        190 => new((input1.Float3 - input2.Float3).Length),
                        200 => new(QuaternionUtils.AxisAngle(input1.Float3.ToNumerics(), input2.Float)),
                        204 => new(QuaternionUtils.LookRotation(input1.Float3.ToNumerics(), binary.Input2 is null ? Vector3.UnitY : input2.Float3.ToNumerics())),
                        _ => throw new UnreachableException(),
                    };

                    return new TerminalOutput(value);
                }

            case 194:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lerp = (LerpExpressionSyntax)terminal.Node;

                    return new TerminalOutput(new RuntimeValue(Quaternion.Lerp(GetValue(lerp.From).Quaternion, GetValue(lerp.To).Quaternion, GetValue(lerp.Amount).Float)));
                }

            case 216:
                {
                    var screenToWorld = (ScreenToWorldExpressionSyntax)terminal.Node;

                    var (near, far) = _ctx.ScreenToWorld(new float2(GetValue(screenToWorld.ScreenX).Float, GetValue(screenToWorld.ScreenY).Float));

                    float3 val = default;
                    if (terminal.Position == PosOut02)
                    {
                        val = near;
                    }
                    else if (terminal.Position == PosOut12)
                    {
                        val = far;
                    }
                    else
                    {
                        ThrowInvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 477:
                {
                    var worldToScreen = (WorldToScreenExpressionSyntax)terminal.Node;

                    var screenPos = _ctx.WorldToScreen(GetValue(worldToScreen.WorldPos).Float3);

                    float val = default;
                    if (terminal.Position == PosOut02)
                    {
                        val = screenPos.X;
                    }
                    else if (terminal.Position == PosOut12)
                    {
                        val = screenPos.Y;
                    }
                    else
                    {
                        ThrowInvalidTerminalException(terminal.Position);
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            case 208:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 4), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var lineVsPlane = (LineVsPlaneExpressionSyntax)terminal.Node;

                    Vector3 lineFrom = GetValue(lineVsPlane.LineFrom).Float3.ToNumerics();
                    Vector3 lineTo = GetValue(lineVsPlane.LineTo).Float3.ToNumerics();
                    Vector3 planePoint = GetValue(lineVsPlane.PlanePoint).Float3.ToNumerics();
                    Vector3 planeNormal = GetValue(lineVsPlane.PlaneNormal).Float3.ToNumerics();

                    float t = Vector3.Dot(planePoint - lineFrom, planeNormal) / Vector3.Dot(lineTo - lineFrom, planeNormal);
                    return new TerminalOutput(new RuntimeValue((lineFrom + (t * (lineTo - lineFrom))).ToFloat3()));
                }

            case 150 or 162:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 3), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var makeVecRot = (MakeVecRotExpressionSyntax)terminal.Node;

                    float x = GetValue(makeVecRot.X).Float;
                    float y = GetValue(makeVecRot.Y).Float;
                    float z = GetValue(makeVecRot.Z).Float;

                    return new TerminalOutput(makeVecRot.PrefabId switch
                    {
                        150 => new RuntimeValue(new float3(x, y, z)),
                        162 => new RuntimeValue(Quaternion.CreateFromYawPitchRoll(y, x, z)),
                        _ => throw new UnreachableException(),
                    });
                }

            case 156 or 442:
                {
                    var breakVecRot = (BreakVecRotExpressionnSyntax)terminal.Node;

                    var vecRot = GetValue(breakVecRot.VecRot);

                    float val;
                    switch (breakVecRot.PrefabId)
                    {
                        case 156:
                            var vec = vecRot.Float3;
                            if (terminal.Position == PosOut03)
                            {
                                val = vec.X;
                            }
                            else if (terminal.Position == PosOut13)
                            {
                                val = vec.Y;
                            }
                            else if (terminal.Position == PosOut23)
                            {
                                val = vec.Z;
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            break;
                        case 442:
                            var rot = vecRot.Quaternion;
                            if (terminal.Position == PosOut03)
                            {
                                float pitchSin = 2.0f * ((rot.W * rot.Y) - (rot.Z * rot.X));

                                if (pitchSin > 1.0f)
                                {
                                    val = MathF.PI / 2; // 90 degrees
                                }
                                else if (pitchSin < -1.0f)
                                {
                                    val = -MathF.PI / 2; // -90 degrees
                                }
                                else
                                {
                                    val = MathF.Asin(pitchSin);
                                }
                            }
                            else if (terminal.Position == PosOut13)
                            {
                                float xx = rot.X * rot.X;
                                float yy = rot.Y * rot.Y;
                                float zz = rot.Z * rot.Z;
                                float ww = rot.W * rot.W;

                                val = MathF.Atan2(2.0f * ((rot.Y * rot.Z) + (rot.W * rot.X)), ww + xx - yy - zz);
                            }
                            else if (terminal.Position == PosOut23)
                            {
                                float xx = rot.X * rot.X;
                                float yy = rot.Y * rot.Y;
                                float zz = rot.Z * rot.Z;
                                float ww = rot.W * rot.W;

                                val = MathF.Atan2(2.0f * ((rot.X * rot.Y) + (rot.W * rot.Z)), ww - xx - yy + zz);
                            }
                            else
                            {
                                throw new InvalidTerminalException(terminal.Position);
                            }

                            val *= 180f / MathF.PI; // rad to deg
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    return new TerminalOutput(new RuntimeValue(val));
                }

            // **************************************** Value ****************************************
            case 36 or 38 or 42 or 449 or 451:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, terminal.Node.PrefabId is 38 or 42 ? 2 : 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var literal = (LiteralExpressionSyntax)terminal.Node;

                    return new TerminalOutput(literal.Value);
                }

            // **************************************** Variables ****************************************
            case 46 or 48 or 50 or 52 or 54 or 56:
                {
                    Debug.Assert(terminal.Position == TerminalDef.GetOutPosition(0, 2, 1), $"{nameof(terminal)}.{nameof(terminal.Position)} should be valid.");
                    var getVar = (GetVariableExpressionSyntax)terminal.Node;

                    return new TerminalOutput(new VariableReference(_variableAccessor.GetVariableId(getVar.Variable), 0));
                }

            default:
                throw new NotImplementedException($"Prefab with id {terminal.Node.PrefabId} is not yet implemented.");
        }
    }

    private sealed class VariableAccessor : IVariableAccessor
    {
        private readonly FrozenDictionary<Variable, int> _variableToId;
        private readonly VariableManager _variableManager;

        public VariableAccessor(IEnumerable<Variable> variables)
        {
            int varId = 0;
            _variableToId = variables.ToFrozenDictionary(var => var, _ => varId++);
            _variableManager = new VariableManager(_variableToId.Count);
        }

        public int GetVariableId(Variable variable)
            => _variableToId[variable];

        public RuntimeValue GetVariableValue(int variableId, int index)
            => _variableManager.GetVariableValue(variableId, index);

        public void SetVariableValue(int variableId, int index, RuntimeValue value)
            => _variableManager.SetVariableValue(variableId, index, value);
    }
}
