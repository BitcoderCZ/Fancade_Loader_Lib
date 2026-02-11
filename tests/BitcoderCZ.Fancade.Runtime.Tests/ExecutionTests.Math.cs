using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Runtime.Tests.Common;
using System.Numerics;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
    [Test]
    public async Task DefaultRotation_Is_Identity()
        => await TestExpression(Variable(new Editing.Variable("a", SignalType.Rot)), Quaternion.Identity);

    [Test]
    [Arguments(3.5f)]
    [Arguments(0f)]
    [Arguments(-7f)]
    public Task Negate_ProducesCorrectOutput(float input)
        => TestExpression(Negate(Number(input)), -input);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task Not_ProducesCorrectOutput(bool input)
        => TestExpression(Not(Truth(input)), !input);

    //[Test]
    //[Repeat(2)]
    //public Task Inverse_ProducesCorrectOutput()
    //{
    //    Quaternion input = Quaternion.CreateFromYawPitchRoll(System.Random.Shared.NextSingle() * float.Pi * 2f, System.Random.Shared.NextSingle() * float.Pi * 2f, System.Random.Shared.NextSingle() * float.Pi * 2f);

    //    return TestExpression(Inverse(CodeWriter.Expressions.Rotation(input.GetEuler())), Quaternion.Inverse(input).GetEuler().ToFloat3());
    //}

    [Test]
    [Arguments(1, 2)]
    [Arguments(-1, -2)]
    public Task Add_Number_ProducesCorrectOutput(float value1, float value2)
        => TestExpression(AddNumbers(Literal(value1), Literal(value2)), value1 + value2);

    [Test]
    [Arguments(1, 2, 3, 4, 5, 6)]
    [Arguments(-1, -2, -3, -4, -5, -6)]
    public async Task Add_Vector_ProducesCorrectOutput(float value1X, float value1Y, float value1Z, float value2X, float value2Y, float value2Z)
    {
        var value1 = new Vector3(value1X, value1Y, value1Z);
        var value2 = new Vector3(value2X, value2Y, value2Z);

        await TestExpression(AddVectors(Literal(value1), Literal(value2)), value1 + value2);
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(45, 90, -45)]
    public async Task Make_Rotation_ProducesCorrectOutput(float x, float y, float z)
    {
        const float DegToRad = MathF.PI / 180f;

        await TestExpression(MakeRotation(Literal(x), Literal(y), Literal(z)), Quaternion.CreateFromYawPitchRoll(y * DegToRad, x * DegToRad, z * DegToRad));
    }

    private static async Task TestExpression(CodeWriter.IExpression expression, object expected)
    {
        var writer = new CodeWriter(new CodeGraph.Builder());

        writer.Inspect(expression);

        var tester = AstRunnerTester.Create(writer);

        await Assert.That(tester).Inspects(new(expected) { Count = 2, FrameCount = 1, });
    }
}
