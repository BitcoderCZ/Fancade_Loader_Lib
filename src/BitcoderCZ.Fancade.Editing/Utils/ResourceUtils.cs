// <copyright file="ResourceUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Reflection;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class ResourceUtils
{
    private static Assembly? _assembly;

    public static Stream GetResource(string name)
        => (_assembly ??= Assembly.GetExecutingAssembly()).GetManifestResourceStream("BitcoderCZ.Fancade.Editing." + name) ?? throw new FileNotFoundException($"Resource '{name}' wasn't found.");
}
