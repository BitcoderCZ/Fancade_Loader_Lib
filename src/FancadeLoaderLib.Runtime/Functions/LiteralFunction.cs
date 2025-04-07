using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class LiteralFunction : IFunction
{
    private readonly RuntimeValue _value;
#if DEBUG
    private readonly SignalType _type;
#endif

    public LiteralFunction(RuntimeValue value, SignalType type)
    {
        _value = value;

        if (type is not (SignalType.Float or SignalType.Vec3 or SignalType.Rot or SignalType.Bool))
        {
            ThrowArgumentException($"{nameof(type)} must be {nameof(SignalType.Float)} or {nameof(SignalType.Vec3)} or {nameof(SignalType.Rot)} or {nameof(SignalType.Bool)}.", nameof(type));
        }

        _type = type;
    }

    public TerminalOutput GetTerminalValue(string name, IRuntimeContext context)
    {
#if DEBUG
        switch (_type)
        {
            case SignalType.Float:
                Debug.Assert(name is "Number", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            case SignalType.Vec3:
                Debug.Assert(name is "Vector", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            case SignalType.Rot:
                Debug.Assert(name is "Rotation", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            case SignalType.Bool:
                Debug.Assert(name is "True" or "False", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            default:
                Debug.Fail($"{nameof(_type)} is invalid.");
                break;
        }
#endif

        return new TerminalOutput(_value);
    }
}
