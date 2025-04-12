using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Runtime.Exceptions;
using MathUtils.Vectors;

namespace FancadeLoaderLib.Runtime.Functions.Math;

public sealed class BreakRotationFunction : UnaryFunction
{
    private static readonly byte3 XPos = TerminalDef.GetOutPosition(0, 2, 3);
    private static readonly byte3 YPos = TerminalDef.GetOutPosition(1, 2, 3);
    private static readonly byte3 ZPos = TerminalDef.GetOutPosition(2, 2, 3);

    public BreakRotationFunction(RuntimeTerminal input)
        : base(input)
    {
    }

    public override TerminalOutput GetTerminalOutput(byte3 terminalPos, IRuntimeContext context)
    {
        var q = Input.GetOutput(context).GetValue(context).Quaternion;

        if (terminalPos == XPos)
        {
            float pitchSin = 2.0f * ((q.W * q.Y) - (q.Z * q.X));

            float pitch;
            if (pitchSin > 1.0f)
            {
                pitch = MathF.PI / 2; // 90 degrees
            }
            else if (pitchSin < -1.0f)
            {
                pitch = -MathF.PI / 2; // -90 degrees
            }
            else
            {
                pitch = MathF.Asin(pitchSin);
            }

            return new TerminalOutput(new RuntimeValue(pitch * (180f / MathF.PI)));
        }
        else if (terminalPos == YPos)
        {
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float ww = q.W * q.W;

            return new TerminalOutput(new RuntimeValue(MathF.Atan2(2.0f * ((q.Y * q.Z) + (q.W * q.X)), ww + xx - yy - zz) * (180f / MathF.PI)));
        }
        else if (terminalPos == ZPos)
        {
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float ww = q.W * q.W;

            return new TerminalOutput(new RuntimeValue(MathF.Atan2(2.0f * ((q.X * q.Y) + (q.W * q.Z)), ww - xx - yy + zz) * (180f / MathF.PI)));
        }
        else
        {
            throw new InvalidTerminalException(terminalPos);
        }
    }
}
