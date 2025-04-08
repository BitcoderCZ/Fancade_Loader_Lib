using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Functions;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime;

public sealed partial class AST
{
    private static class FunctionCreation
    {
        public static IFunction? CreateFunction(ushort id, ushort3 pos, ParseContext ctx)
        {
            switch (id)
            {
                case 16 or 20 or 24 or 28 or 32:
                    return new InspectFunction(pos, ctx.GetConnectedTerminal(pos, TerminalDef.GetInPosition(0, 2)));
                case 36:
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Float, out object? value) ? (float)value : 0f));
                    }

                case 38: // vec3
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? (float3)value : float3.Zero));
                    }

                case 42: // rot
                    {
                        return new LiteralFunction(new RuntimeValue(ctx.TryGetSettingOfType(pos, 0, SettingType.Vec3, out object? value) ? (float3)value : float3.Zero));
                    }

                case 449:
                    return new LiteralFunction(new RuntimeValue(true));
                case 451:
                    return new LiteralFunction(new RuntimeValue(false));
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

                default:
                    return null;
            }
        }
    }
}