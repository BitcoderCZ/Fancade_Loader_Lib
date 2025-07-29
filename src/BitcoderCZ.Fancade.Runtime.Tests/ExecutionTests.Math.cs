using BitcoderCZ.Fancade.Editing.Scripting;
using BitcoderCZ.Fancade.Runtime.Tests.AssertUtils;
using BitcoderCZ.Fancade.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static BitcoderCZ.Fancade.Editing.Scripting.CodeWriter.Expressions;

namespace BitcoderCZ.Fancade.Runtime.Tests;

public partial class ExecutionTests
{
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

    private static async Task TestExpression(CodeWriter.IExpression expression, object expected)
    {
        var writer = CreateWriter();

        writer.Inspect(expression);

        var compiled = Compile(writer);

        await Assert.That(compiled).Inspects([new(expected) { Count = 1, }], runFor: 1);
    }
}
