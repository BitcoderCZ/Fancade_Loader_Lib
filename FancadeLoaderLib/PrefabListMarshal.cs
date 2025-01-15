// <copyright file="PrefabListMarshal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib;

public static class PrefabListMarshal
{
	public static Span<Prefab> AsSpan(PrefabList list)
		=> CollectionsMarshal.AsSpan(list._list);

	public static Span<PartialPrefab> AsSpan(PartialPrefabList list)
		=> CollectionsMarshal.AsSpan(list._list);
}
