// <copyright file="CodeWriter.Expressions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using MathUtils.Vectors;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FancadeLoaderLib.Runtime.Tests")]

namespace FancadeLoaderLib.Editing.Scripting;

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1615 // Element return value should be documented
public sealed partial class CodeWriter
{
    /// <summary>
    /// Blocks that don't have execution terminals.
    /// </summary>
    public static class Expressions
    {
        private interface IMultiOutputExpression
        {
            SignalType GetType(int outputIndex);

            ITerminal WriteTo(CodeWriter writer, int outputIndex);
        }

        /// <summary>
        /// A "null" expression, nothing is written.
        /// </summary>
        public static IExpression None()
            => NoneExpression.Instance;

        private sealed class NoneExpression : IExpression
        {
            public static readonly NoneExpression Instance = new();

            private NoneExpression()
            {
            }

            public SignalType Type => SignalType.Error;

            public ITerminal WriteTo(CodeWriter writer)
                => NopTerminal.Instance;
        }

        /// <summary>
        /// Wraps an <see cref="ITerminal"/> as an <see cref="IExpression"/>.
        /// </summary>
        /// <param name="terminal">The terminal to wrap.</param>
        public static IExpression WrapTerminal(ITerminal terminal)
            => new TerminalWrapperExpression(terminal);

        private sealed class TerminalWrapperExpression : IExpression
        {
            private readonly ITerminal _terminal;

            public TerminalWrapperExpression(ITerminal terminal)
            {
                _terminal = terminal;
            }

            public SignalType Type => _terminal.SignalType;

            public ITerminal WriteTo(CodeWriter writer)
                => _terminal;
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Game.ScreenSize"/> block.
        /// </summary>
        public static (IExpression Width, IExpression Height) ScreenSize()
        {
            var expression = new ScreenSizeExpression();

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class ScreenSizeExpression : IMultiOutputExpression
        {
            private Block? _block;

            public SignalType GetType(int outputIndex)
                => SignalType.Float;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                _block ??= writer._codePlacer.PlaceBlock(StockBlocks.Game.ScreenSize);

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Game.Accelerometer"/> block.
        /// </summary>
        public static IExpression Accelerometer()
            => new AccelerometerExpression();

        private sealed class AccelerometerExpression : IExpression
        {
            public SignalType Type => SignalType.Vec3;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Game.Accelerometer);

                return new BlockTerminal(block, "Direction");
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Game.CurrentFrame"/> block.
        /// </summary>
        public static IExpression CurrentFrame()
            => new CurrentFrameExpression();

        private sealed class CurrentFrameExpression : IExpression
        {
            public SignalType Type => SignalType.Float;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Game.CurrentFrame);

                return new BlockTerminal(block, "Counter");
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Objects.GetPos"/> block.
        /// </summary>
        /// <param name="object">The object whose position and rotation should be retrived.</param>
        public static (IExpression Position, IExpression Rotation) GetPos(IExpression @object)
        {
            var expression = new GetPosExpression(@object);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class GetPosExpression : IMultiOutputExpression
        {
            private readonly IExpression _object;
            private Block? _block;

            public GetPosExpression(IExpression @object)
            {
                _object = @object;
            }

            public SignalType GetType(int outputIndex)
                => outputIndex switch
                {
                    0 => SignalType.Vec3,
                    1 => SignalType.Rot,
                    _ => throw new UnreachableException(),
                };

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Objects.GetPos);

                    using (writer.ExpressionBlock())
                    {
                        var @object = _object.WriteTo(writer);

                        writer._codePlacer.Connect(@object, new BlockTerminal(_block, "Object"));
                    }
                }

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Objects.Raycast"/> block.
        /// </summary>
        /// <param name="from">From position of the ray.</param>
        /// <param name="to">To position of the ray.</param>
        public static (IExpression Hit, IExpression HitPos, IExpression HitObj) Raycast(IExpression from, IExpression to)
        {
            var expression = new RaycastExpression(from, to);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1), new MultiOutputExpression(expression, 2));
        }

        private sealed class RaycastExpression : IMultiOutputExpression
        {
            private readonly IExpression _from;
            private readonly IExpression _to;
            private Block? _block;

            public RaycastExpression(IExpression from, IExpression to)
            {
                _from = from;
                _to = to;
            }

            public SignalType GetType(int outputIndex)
                => outputIndex switch
                {
                    0 => SignalType.Bool,
                    1 => SignalType.Vec3,
                    2 => SignalType.Obj,
                    _ => throw new UnreachableException(),
                };

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Objects.Raycast);

                    using (writer.ExpressionBlock())
                    {
                        var from = _from.WriteTo(writer);
                        var to = _to.WriteTo(writer);

                        writer._codePlacer.Connect(from, new BlockTerminal(_block, "From"));
                        writer._codePlacer.Connect(to, new BlockTerminal(_block, "To"));
                    }
                }

                return new BlockTerminal(_block, 2 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Objects.GetSize"/> block.
        /// </summary>
        /// <param name="object">The object whose size should be retrived.</param>
        public static (IExpression Min, IExpression Max) GetSize(IExpression @object)
        {
            var expression = new GetSizeExpression(@object);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class GetSizeExpression : IMultiOutputExpression
        {
            private readonly IExpression _object;
            private Block? _block;

            public GetSizeExpression(IExpression @object)
            {
                _object = @object;
            }

            public SignalType GetType(int outputIndex)
                => SignalType.Vec3;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Objects.GetSize);

                    using (writer.ExpressionBlock())
                    {
                        var @object = _object.WriteTo(writer);

                        writer._codePlacer.Connect(@object, new BlockTerminal(_block, "Object"));
                    }
                }

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Physics.GetVelocity"/> block.
        /// </summary>
        /// <param name="object">The object whose velocity should be retrived.</param>
        public static (IExpression Velocity, IExpression Spin) GetVelocity(IExpression @object)
        {
            var expression = new GetVelocityExpression(@object);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class GetVelocityExpression : IMultiOutputExpression
        {
            private readonly IExpression _object;
            private Block? _block;

            public GetVelocityExpression(IExpression @object)
            {
                _object = @object;
            }

            public SignalType GetType(int outputIndex)
                => SignalType.Vec3;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Physics.GetVelocity);

                    using (writer.ExpressionBlock())
                    {
                        var @object = _object.WriteTo(writer);

                        writer._codePlacer.Connect(@object, new BlockTerminal(_block, "Object"));
                    }
                }

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Negate"/> block.
        /// </summary>
        /// <param name="num">The number to be negated.</param>
        public static IExpression Negate(IExpression num)
            => new UnaryExpression(num, StockBlocks.Math.Negate);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Not"/> block.
        /// </summary>
        /// <param name="tru">The truth to be inverted.</param>
        public static IExpression Not(IExpression tru)
            => new UnaryExpression(tru, StockBlocks.Math.Not);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Inverse"/> block.
        /// </summary>
        /// <param name="rot">The rotation to be inverted.</param>
        public static IExpression Inverse(IExpression rot)
            => new UnaryExpression(rot, StockBlocks.Math.Inverse);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Add_Number"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression AddNumbers(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Add_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Add_Vector"/> block.
        /// </summary>
        /// <param name="vec1">The first vector.</param>
        /// <param name="vec2">The second vector.</param>
        public static IExpression AddVectors(IExpression vec1, IExpression vec2)
            => new BinaryExpression(vec1, vec2, StockBlocks.Math.Add_Vector);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Subtract_Number"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression SubtractNumbers(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Subtract_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Subtract_Vector"/> block.
        /// </summary>
        /// <param name="vec1">The first vector.</param>
        /// <param name="vec2">The second vector.</param>
        public static IExpression SubtractVectors(IExpression vec1, IExpression vec2)
            => new BinaryExpression(vec1, vec2, StockBlocks.Math.Subtract_Vector);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Multiply_Number"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression Multiply(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Multiply_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Multiply_Vector"/> block.
        /// </summary>
        /// <param name="vec">The vector.</param>
        /// <param name="num">The number.</param>
        public static IExpression Scale(IExpression vec, IExpression num)
            => new BinaryExpression(vec, num, StockBlocks.Math.Multiply_Vector);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Rotate_Vector"/> block.
        /// </summary>
        /// <param name="vec">The vector.</param>
        /// <param name="rot">The rotation.</param>
        public static IExpression Rotate(IExpression vec, IExpression rot)
            => new BinaryExpression(vec, rot, StockBlocks.Math.Rotate_Vector);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Multiply_Rotation"/> block.
        /// </summary>
        /// <param name="rot1">The first rotation.</param>
        /// <param name="rot2">The second rotation.</param>
        public static IExpression Combine(IExpression rot1, IExpression rot2)
            => new BinaryExpression(rot1, rot2, StockBlocks.Math.Multiply_Rotation);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Divide_Number"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression Divide(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Divide_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Modulo_Number"/> block.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        public static IExpression Modulo(IExpression a, IExpression b)
            => new BinaryExpression(a, b, StockBlocks.Math.Modulo_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Power"/> block.
        /// </summary>
        /// <param name="base">The base.</param>
        /// <param name="exponent">The exponent.</param>
        public static IExpression Power(IExpression @base, IExpression exponent)
            => new BinaryExpression(@base, exponent, StockBlocks.Math.Power);

        /// <summary>
        /// Writes the 
        /// Writes the <see cref="StockBlocks.Math.Equals_Number"/> block. block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression EqualsNumbers(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Equals_Number);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Equals_Vector"/> block.
        /// </summary>
        /// <param name="vec1">The first vector.</param>
        /// <param name="vec2">The second vector.</param>
        public static IExpression EqualsVectors(IExpression vec1, IExpression vec2)
            => new BinaryExpression(vec1, vec2, StockBlocks.Math.Equals_Vector);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Equals_Object"/> block.
        /// </summary>
        /// <param name="obj1">The first object.</param>
        /// <param name="obj2">The second object.</param>
        public static IExpression EqualsObjects(IExpression obj1, IExpression obj2)
            => new BinaryExpression(obj1, obj2, StockBlocks.Math.Equals_Object);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Equals_Bool"/> block.
        /// </summary>
        /// <param name="tru1">The first truth.</param>
        /// <param name="tru2">The second truth.</param>
        public static IExpression EqualsTruths(IExpression tru1, IExpression tru2)
            => new BinaryExpression(tru1, tru2, StockBlocks.Math.Equals_Bool);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.LogicalAnd"/> block.
        /// </summary>
        /// <param name="tru1">The first truth.</param>
        /// <param name="tru2">The second truth.</param>
        public static IExpression And(IExpression tru1, IExpression tru2)
            => new BinaryExpression(tru1, tru2, StockBlocks.Math.LogicalAnd);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.LogicalOr"/> block.
        /// </summary>
        /// <param name="tru1">The first truth.</param>
        /// <param name="tru2">The second truth.</param>
        public static IExpression Or(IExpression tru1, IExpression tru2)
            => new BinaryExpression(tru1, tru2, StockBlocks.Math.LogicalOr);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Less"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression LessThan(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Less);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Greater"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression GreaterThan(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Greater);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Random"/> block.
        /// </summary>
        /// <param name="min">The minimum number (inclusive).</param>
        /// <param name="max">The maximum number (exclusive).</param>
        public static IExpression Random(IExpression min, IExpression max)
            => new BinaryExpression(min, max, StockBlocks.Math.Random);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Min"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression Min(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Min);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Max"/> block.
        /// </summary>
        /// <param name="num1">The first number.</param>
        /// <param name="num2">The second number.</param>
        public static IExpression Max(IExpression num1, IExpression num2)
            => new BinaryExpression(num1, num2, StockBlocks.Math.Max);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Sin"/> block.
        /// </summary>
        /// <param name="num">Angle in degrees.</param>
        public static IExpression Sin(IExpression num)
            => new UnaryExpression(num, StockBlocks.Math.Sin);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Cos"/> block.
        /// </summary>
        /// <param name="num">Angle in degrees.</param>
        public static IExpression Cos(IExpression num)
            => new UnaryExpression(num, StockBlocks.Math.Cos);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Round"/> block.
        /// </summary>
        /// <param name="number">The number to round.</param>
        public static IExpression Round(IExpression number)
            => new UnaryExpression(number, StockBlocks.Math.Round);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Floor"/> block.
        /// </summary>
        /// <param name="number">The number to floor.</param>
        public static IExpression Floor(IExpression number)
            => new UnaryExpression(number, StockBlocks.Math.Floor);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Ceiling"/> block.
        /// </summary>
        /// <param name="number">The number to ceil.</param>
        public static IExpression Ceiling(IExpression number)
            => new UnaryExpression(number, StockBlocks.Math.Ceiling);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Absolute"/> block.
        /// </summary>
        /// <param name="num">The number whose absolute value should be retrived.</param>
        public static IExpression Absolute(IExpression num)
            => new UnaryExpression(num, StockBlocks.Math.Absolute);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Logarithm"/> block.
        /// </summary>
        /// <param name="number">The number whose logarithm value should be retrived.</param>
        /// <param name="base">Base of the logarithm.</param>
        public static IExpression Logarithm(IExpression number, IExpression @base)
            => new BinaryExpression(number, @base, StockBlocks.Math.Logarithm);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Normalize"/> block.
        /// </summary>
        /// <param name="vector">The vector that should be normalized.</param>
        public static IExpression Normalize(IExpression vector)
            => new UnaryExpression(vector, StockBlocks.Math.Normalize);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.DotProduct"/> block.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        public static IExpression DotProduct(IExpression vector1, IExpression vector2)
            => new BinaryExpression(vector1, vector2, StockBlocks.Math.DotProduct);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.CrossProduct"/> block.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        public static IExpression CrossProduct(IExpression vector1, IExpression vector2)
            => new BinaryExpression(vector1, vector2, StockBlocks.Math.CrossProduct);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Distance"/> block.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        public static IExpression Distance(IExpression vector1, IExpression vector2)
            => new BinaryExpression(vector1, vector2, StockBlocks.Math.Distance);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Lerp"/> block.
        /// </summary>
        /// <param name="from">The start rotation.</param>
        /// <param name="to">The end rotation.</param>
        /// <param name="amount">How far between <paramref name="from"/> and <paramref name="to"/> to transition (0 - 1).</param>
        public static IExpression Lerp(IExpression from, IExpression to, IExpression amount)
            => new LerpExpression(from, to, amount);

        private sealed class LerpExpression : IExpression
        {
            private readonly IExpression _from;
            private readonly IExpression _to;
            private readonly IExpression _amount;

            public LerpExpression(IExpression from, IExpression to, IExpression amount)
            {
                _from = from;
                _to = to;
                _amount = amount;
            }

            public SignalType Type => SignalType.Rot;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Math.Lerp);

                using (writer.ExpressionBlock())
                {
                    writer._codePlacer.Connect(_from.WriteTo(writer), new BlockTerminal(block, "From"));
                    writer._codePlacer.Connect(_to.WriteTo(writer), new BlockTerminal(block, "To"));
                    writer._codePlacer.Connect(_amount.WriteTo(writer), new BlockTerminal(block, "Amount"));
                }

                return new BlockTerminal(block, "Rotation");
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.AxisAngle"/> block.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">How much to rotate (in degrees).</param>
        public static IExpression AxisAngle(IExpression axis, IExpression angle)
            => new BinaryExpression(axis, angle, StockBlocks.Math.AxisAngle);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.ScreenToWorld"/> block.
        /// </summary>
        /// <param name="screenX">The x position in screen space.</param>
        /// <param name="screenY">The y position in screen space.</param>
        public static (IExpression WorldNear, IExpression WorldFar) ScreenToWorld(IExpression screenX, IExpression screenY)
        {
            var expression = new ScreenToWorldExpression(screenX, screenY);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class ScreenToWorldExpression : IMultiOutputExpression
        {
            private readonly IExpression _screenX;
            private readonly IExpression _screenY;
            private Block? _block;

            public ScreenToWorldExpression(IExpression screenX, IExpression screenY)
            {
                _screenX = screenX;
                _screenY = screenY;
            }

            public SignalType GetType(int outputIndex)
                => SignalType.Vec3;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Math.ScreenToWorld);

                    using (writer.ExpressionBlock())
                    {
                        writer._codePlacer.Connect(_screenX.WriteTo(writer), new BlockTerminal(_block, "Screen X"));
                        writer._codePlacer.Connect(_screenY.WriteTo(writer), new BlockTerminal(_block, "Screen Y"));
                    }
                }

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.WorldToScreen"/> block.
        /// </summary>
        /// <param name="worldPos">Position in world space.</param>
        public static (IExpression ScreenX, IExpression ScreenY) WorldToScreen(IExpression worldPos)
        {
            var expression = new WorldToScreenExpression(worldPos);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1));
        }

        private sealed class WorldToScreenExpression : IMultiOutputExpression
        {
            private readonly IExpression _worldPos;
            private Block? _block;

            public WorldToScreenExpression(IExpression worldPos)
            {
                _worldPos = worldPos;
            }

            public SignalType GetType(int outputIndex)
                => SignalType.Float;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(StockBlocks.Math.WorldToScreen);

                    using (writer.ExpressionBlock())
                    {
                        writer._codePlacer.Connect(_worldPos.WriteTo(writer), new BlockTerminal(_block, "World Near"));
                    }
                }

                return new BlockTerminal(_block, 1 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.LineVsPlane"/> block.
        /// </summary>
        /// <param name="lineFrom">Line's starting position.</param>
        /// <param name="lineTo">Line's end position.</param>
        /// <param name="planePoint">A point on the plane.</param>
        /// <param name="planeNormal">A vector perpendicular to the plane (the up direction from the plane's surface).</param>
        public static IExpression LineVsPlane(IExpression lineFrom, IExpression lineTo, IExpression planePoint, IExpression planeNormal)
            => new LineVsPlaneExpression(lineFrom, lineTo, planePoint, planeNormal);

        private sealed class LineVsPlaneExpression : IExpression
        {
            private readonly IExpression _lineFrom;
            private readonly IExpression _lineTo;
            private readonly IExpression _planePoint;
            private readonly IExpression _planeNormal;

            public LineVsPlaneExpression(IExpression lineFrom, IExpression lineTo, IExpression planePoint, IExpression planeNormal)
            {
                _lineFrom = lineFrom;
                _lineTo = lineTo;
                _planePoint = planePoint;
                _planeNormal = planeNormal;
            }

            public SignalType Type => SignalType.Vec3;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Math.LineVsPlane);

                using (writer.ExpressionBlock())
                {
                    writer._codePlacer.Connect(_lineFrom.WriteTo(writer), new BlockTerminal(block, "Line From"));
                    writer._codePlacer.Connect(_lineTo.WriteTo(writer), new BlockTerminal(block, "Line To"));
                    writer._codePlacer.Connect(_planePoint.WriteTo(writer), new BlockTerminal(block, "Plane Point"));
                    writer._codePlacer.Connect(_planeNormal.WriteTo(writer), new BlockTerminal(block, "Plane Normal"));
                }

                return new BlockTerminal(block, "Intersection");
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.LookRotation"/> block.
        /// </summary>
        /// <param name="direction">The direction to point in.</param>
        /// <param name="up">The up direction.</param>
        public static IExpression LookRotation(IExpression direction, IExpression up)
            => new BinaryExpression(direction, up, StockBlocks.Math.LookRotation);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Make_Vector"/> block.
        /// </summary>
        /// <param name="x">The x component of the vector.</param>
        /// <param name="y">The y component of the vector.</param>
        /// <param name="z">The z component of the vector.</param>
        public static IExpression MakeVector(IExpression x, IExpression y, IExpression z)
            => new MakeVectorExpression(x, y, z, true);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Make_Rotation"/> block.
        /// </summary>
        /// <param name="x">The x component of the rotation (in degrees).</param>
        /// <param name="y">The y component of the rotation (in degrees).</param>
        /// <param name="z">The z component of the rotation (in degrees).</param>
        public static IExpression MakeRotation(IExpression x, IExpression y, IExpression z)
            => new MakeVectorExpression(x, y, z, false);

        private sealed class MakeVectorExpression : IExpression
        {
            private readonly IExpression _x;
            private readonly IExpression _y;
            private readonly IExpression _z;
            private readonly bool _vector;

            public MakeVectorExpression(IExpression x, IExpression y, IExpression z, bool vector)
            {
                _x = x;
                _y = y;
                _z = z;
                _vector = vector;
            }

            public SignalType Type => _vector ? SignalType.Vec3 : SignalType.Rot;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(_vector ? StockBlocks.Math.Make_Vector : StockBlocks.Math.Break_Vector);

                using (writer.ExpressionBlock())
                {
                    writer._codePlacer.Connect(_x.WriteTo(writer), new BlockTerminal(block, "X"));
                    writer._codePlacer.Connect(_y.WriteTo(writer), new BlockTerminal(block, "Y"));
                    writer._codePlacer.Connect(_z.WriteTo(writer), new BlockTerminal(block, "Z"));
                }

                return new BlockTerminal(block, 0);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Break_Vector"/> block.
        /// </summary>
        /// <param name="vector">The vector to break.</param>
        public static (IExpression X, IExpression Y, IExpression Z) BreakVector(IExpression vector)
        {
            var expression = new BreakVecRotExpression(vector, true);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1), new MultiOutputExpression(expression, 2));
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Math.Break_Rotation"/> block.
        /// </summary>
        /// <param name="rotation">The rotation to break.</param>
        public static (IExpression X, IExpression Y, IExpression Z) BreakRotation(IExpression rotation)
        {
            var expression = new BreakVecRotExpression(rotation, false);

            return (new MultiOutputExpression(expression, 0), new MultiOutputExpression(expression, 1), new MultiOutputExpression(expression, 2));
        }

        private sealed class BreakVecRotExpression : IMultiOutputExpression
        {
            private readonly IExpression _input;
            private readonly bool _vector;
            private Block? _block;

            public BreakVecRotExpression(IExpression input, bool vector)
            {
                _input = input;
                _vector = vector;
            }

            public SignalType GetType(int outputIndex)
                => SignalType.Float;

            public ITerminal WriteTo(CodeWriter writer, int outputIndex)
            {
                if (_block is null)
                {
                    _block = writer._codePlacer.PlaceBlock(_vector ? StockBlocks.Math.Break_Vector : StockBlocks.Math.Break_Rotation);

                    using (writer.ExpressionBlock())
                    {
                        ITerminal terminal = _input.WriteTo(writer);
                        writer._codePlacer.Connect(terminal, new BlockTerminal(_block, 3));
                    }
                }

                return new BlockTerminal(_block, 2 - outputIndex);
            }
        }

        /// <summary>
        /// Writes the <see cref="StockBlocks.Values.Number"/> block.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static IExpression Number(float value)
            => new LiteralExpression(value, SignalType.Float);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Values.Vector"/> block.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static IExpression Vector(Vector3 value)
            => new LiteralExpression(new float3(value.X, value.Y, value.Z), SignalType.Vec3);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Values.Rotation"/> block.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static IExpression Rotation(Vector3 value)
            => new LiteralExpression(new Rotation(new float3(value.X, value.Y, value.Z)), SignalType.Rot);

        /// <summary>
        /// Writes the <see cref="StockBlocks.Values.True"/> or <see cref="StockBlocks.Values.False"/> block.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static IExpression Truth(bool value)
            => new LiteralExpression(value, SignalType.Bool);

        internal static IExpression Literal(object value)
            => new LiteralExpression(value, value switch
            {
                float => SignalType.Float,
                bool => SignalType.Bool,
                float3 => SignalType.Vec3,
                FancadeLoaderLib.Rotation => SignalType.Rot,
                _ => throw new UnreachableException(),
            });

        internal sealed class LiteralExpression : IExpression
        {
            internal readonly object _value;
            internal readonly SignalType _type;

            public LiteralExpression(object value, SignalType type)
            {
                Debug.Assert(!type.IsPointer(), $"{nameof(type)} shouldn't be pointer.");

                _value = value;
                _type = type;
            }

            public SignalType Type => _type;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(_type switch
                {
                    SignalType.Float => StockBlocks.Values.Number,
                    SignalType.Vec3 => StockBlocks.Values.Vector,
                    SignalType.Rot => StockBlocks.Values.Rotation,
                    SignalType.Bool => (_value is bool b && b) ? StockBlocks.Values.True : StockBlocks.Values.False,
                    _ => throw new UnreachableException(),
                });

                if (_type is not SignalType.Bool)
                {
                    writer._codePlacer.SetSetting(block, 0, _value);
                }

                return new BlockTerminal(block, 0);
            }
        }

        /// <summary>
        /// Writes the get variable block.
        /// </summary>
        /// <param name="variable">The variable to write.</param>
        public static IExpression Variable(Variable variable)
            => new VariableExpression(variable);

        private sealed class VariableExpression : IExpression
        {
            private readonly Variable _variable;

            public VariableExpression(Variable variable)
            {
                _variable = variable;
            }

            public SignalType Type => _variable.Type.ToPointer();

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Variables.GetVariableByType(_variable.Type));

                writer._codePlacer.SetSetting(block, 0, _variable.Name);

                return new BlockTerminal(block, 0);
            }
        }

        /// <summary>
        /// Writes the list block.
        /// </summary>
        /// <param name="variable">The variable whose index should be changed.</param>
        /// <param name="index">The value to add to <paramref name="variable"/>'s index.</param>
        public static IExpression List(IExpression variable, IExpression index)
            => new ListExpression(variable, index, variable.Type);

        /// <summary>
        /// Writes the list block.
        /// </summary>
        /// <param name="variable">The variable whose index should be changed.</param>
        /// <param name="index">The value to add to <paramref name="variable"/>'s index.</param>
        /// <param name="type">Type of the variable.</param>
        public static IExpression List(IExpression variable, IExpression index, SignalType type)
            => new ListExpression(variable, index, type);

        private sealed class ListExpression : IExpression
        {
            private readonly IExpression _variable;
            private readonly IExpression _index;
            private readonly SignalType _type;

            public ListExpression(IExpression variable, IExpression index, SignalType type)
            {
                _variable = variable;
                _index = index;
                _type = type;
            }

            public SignalType Type => _type.ToPointer();

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(_variable.Type));

                using (writer.ExpressionBlock())
                {
                    writer._codePlacer.Connect(_variable.WriteTo(writer), new BlockTerminal(block, "Variable"));
                    writer._codePlacer.Connect(_index.WriteTo(writer), new BlockTerminal(block, "Index"));
                }

                return new BlockTerminal(block, "Element");
            }
        }

        // it would be inefficient for expresions with multiple outputs to write the block multiple times, this wrapper allows them to "cache" the block
        private sealed class MultiOutputExpression : IExpression
        {
            private readonly IMultiOutputExpression _expression;
            private readonly int _outputIndex;

            public MultiOutputExpression(IMultiOutputExpression expression, int outputIndex)
            {
                _expression = expression;
                _outputIndex = outputIndex;
            }

            public SignalType Type => _expression.GetType(_outputIndex);

            public ITerminal WriteTo(CodeWriter writer)
                => _expression.WriteTo(writer, _outputIndex);
        }

        private sealed class UnaryExpression : IExpression
        {
            private readonly IExpression _inp;
            private readonly BlockDef _def;

            public UnaryExpression(IExpression inp, BlockDef def)
            {
                _inp = inp;
                _def = def;
            }

            public SignalType Type => _def.Terminals[0].SignalType;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(_def);

                using (writer.ExpressionBlock())
                {
                    var inp = _inp.WriteTo(writer);

                    writer._codePlacer.Connect(inp, new BlockTerminal(block, 1));
                }

                return new BlockTerminal(block, 0);
            }
        }

        private sealed class BinaryExpression : IExpression
        {
            private readonly IExpression _inp1;
            private readonly IExpression _inp2;
            private readonly BlockDef _def;

            public BinaryExpression(IExpression inp1, IExpression inp2, BlockDef def)
            {
                _inp1 = inp1;
                _inp2 = inp2;
                _def = def;
            }

            public SignalType Type => _def.Terminals[0].SignalType;

            public ITerminal WriteTo(CodeWriter writer)
            {
                var block = writer._codePlacer.PlaceBlock(_def);

                using (writer.ExpressionBlock())
                {
                    var inp1 = _inp1.WriteTo(writer);
                    var inp2 = _inp2.WriteTo(writer);

                    writer._codePlacer.Connect(inp1, new BlockTerminal(block, 2));
                    writer._codePlacer.Connect(inp2, new BlockTerminal(block, 1));
                }

                return new BlockTerminal(block, 0);
            }
        }
    }
}
#pragma warning restore SA1615 // Element return value should be documented
#pragma warning restore SA1201 // Elements should appear in the correct order
