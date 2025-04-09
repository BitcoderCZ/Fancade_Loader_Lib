using FancadeLoaderLib.Editing;
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
                    return new InspectFunction(pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
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
                default:
                    throw new NotImplementedException($"Prefab with id {id} is not yet implemented.");
            }
        }
    }
}