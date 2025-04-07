using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Functions;

public sealed class GetVariableFunction : IFunction
{
    private readonly int _variableId;
#if DEBUG
    private readonly SignalType _type;
#endif

    public GetVariableFunction(int variableId, SignalType type)
    {
        _variableId = variableId;

        if (type == SignalType.Error || type.IsPointer())
        {
            ThrowArgumentException($"{nameof(type)} cannot be {nameof(SignalType.Error)} or pointer.", nameof(type));
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
                Debug.Assert(name is "Truth", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            case SignalType.Obj:
                Debug.Assert(name is "Object", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            case SignalType.Con:
                Debug.Assert(name is "Constraint", $"{nameof(name)} should be valid for {nameof(_type)}");
                break;
            default:
                Debug.Fail($"{nameof(_type)} is invalid.");
                break;
        }
#endif

        return new TerminalOutput(new VariableReference(_variableId, 0));
    }
}
