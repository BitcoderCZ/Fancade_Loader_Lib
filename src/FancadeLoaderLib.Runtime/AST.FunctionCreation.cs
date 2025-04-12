using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Functions.Control;
using FancadeLoaderLib.Runtime.Functions.Math;
using FancadeLoaderLib.Runtime.Functions.Values;
using FancadeLoaderLib.Runtime.Functions.Variables;
using FancadeLoaderLib.Runtime.Utils;
using MathUtils.Vectors;
using System.Numerics;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    private static class FunctionCreation
    {
        public static IFunction? CreateFunction(ushort id, ushort3 pos, ParseContext ctx)
        {
            switch (id)
            {
                // ******************** Value ********************
                case 16 or 20 or 24 or 28 or 32:
                    return new InspectFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), (SignalType)(((id - 16) / 2) + 2), pos);
                case 36:
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Float, out object? value) ? (float)value : 0f), false);
                    }

                case 38: // vec3
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? (float3)value : float3.Zero), true);
                    }

                case 42: // rot
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? ((float3)value).ToQuatDeg() : Quaternion.Identity), true);
                    }

                case 449:
                    return new LiteralFunction(new RuntimeValue(true), false);
                case 451:
                    return new LiteralFunction(new RuntimeValue(false), false);

                // ******************** Variable ********************
                case 46:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Float));
                    }

                case 48:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Vec3));
                    }

                case 50:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Rot));
                    }

                case 52:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Bool));
                    }

                case 54:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Obj));
                    }

                case 56:
                    {
                        return new GetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Con));
                    }

                case 428:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Float), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 430:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Vec3), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 432:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Rot), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 434:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Bool), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 436:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Obj), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                case 438:
                    {
                        return new SetVariableFunction(ctx.GetVariableId(ctx.TryGetSettingOfType(pos, 0, SettingType.String, out object? varName) ? (string)varName : string.Empty, SignalType.Con), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                    }

                // ******************** Control ********************
                case 234:
                    return new IfFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 238:
                    return new PlaySensorFunction();

                // ******************** Math ********************
                case 90:
                    return new NegateFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 144:
                    return new NotFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 440:
                    return new InverseFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 92:
                    return new AddNumbersFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 96:
                    return new AddVectorsFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 100:
                    return new SubtractNumbersFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 104:
                    return new SubtractVectorsFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 108:
                    return new MultiplyFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 112:
                    return new ScaleFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 116:
                    return new RotateFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 120:
                    return new CombineFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 124:
                    return new DivideFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 172:
                    return new ModuloFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 457:
                    return new PowerFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 132:
                    return new EqualsNumbersFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 136:
                    return new EqualsVectorsFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 140:
                    return new EqualsObjectsFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 421:
                    return new EqualsTruthsFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 146:
                    return new LogicalAndFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 417:
                    return new LogicalOrFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 128:
                    return new LessThanFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 481:
                    return new GreaterThanFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 168:
                    return new RandomFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 485:
                    return new RandomSeedFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 176:
                    return new MinFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 180:
                    return new MaxFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 413:
                    return new SinFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 453:
                    return new CosFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 184:
                    return new RoundFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 186:
                    return new FloorFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 188:
                    return new CeilingFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 455:
                    return new AbsoluteFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 580:
                    return new LogarithmFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 578:
                    return new NormalizeFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 1)));
                case 570:
                    return new DotProductFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 574:
                    return new CrossProductFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 190:
                    return new DistanceFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 194:
                    return new LerpFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 200:
                    return new AxisAngleFunciton(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 216:
                    return new ScreenToWorldFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 477:
                    return new WorldToScreenFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 204:
                    return new LookRotationFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 2)));
                case 208:
                    return new LineVsPlaneFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 4)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(3, 4)));
                case 150:
                    return new MakeVectorFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 156:
                    return new BreakVectorFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));
                case 162:
                    return new MakeRotationFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(1, 3)), ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(2, 3)));
                case 442:
                    return new BreakRotationFunction(ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 3)));
                default:
                    throw new NotImplementedException($"Prefab with id {id} is not yet implemented.");
            }
        }
    }
}