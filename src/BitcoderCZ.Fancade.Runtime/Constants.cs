// <copyright file="Constants.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Compiled")]
[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Tests")]
[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Tests.Common")]

namespace BitcoderCZ.Fancade.Runtime;

internal static class Constants
{
    public const float EqualsNumbersMaxDiff = 0.001f;
    public const float EqualsVectorsMaxDiff = 1.0000001e-06f;
}
