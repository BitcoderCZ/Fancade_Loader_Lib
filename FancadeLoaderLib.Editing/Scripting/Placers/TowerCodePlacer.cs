// <copyright file="TowerCodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Builders;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

public sealed class TowerCodePlacer : IScopedCodePlacer
{
	private readonly List<Block> _blocks = new List<Block>(256);

	private readonly BlockBuilder _builder;
	private int _maxHeight = 20;
	private bool _inHighlight = false;
	private int _statementDepth = 0;

	public TowerCodePlacer(BlockBuilder builder)
	{
		_builder = builder;
	}

	public enum Move
	{
		X,
		Z,
	}

	public int MaxHeight
	{
		get => _maxHeight;
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException($"{nameof(MaxHeight)} must be larger than 0.", nameof(value));
			}

			_maxHeight = value;
		}
	}

	public bool SquarePlacement { get; set; } = true;

	public Block PlaceBlock(BlockDef blockDef)
	{
		Block block;

		if (_inHighlight)
		{
			block = new Block(blockDef, int3.Zero);
			_builder.AddHighlightedBlock(block);
		}
		else
		{
			block = new Block(blockDef, int3.Zero);
			_blocks.Add(block);
		}

		return block;
	}

	public void EnterStatementBlock()
		=> _statementDepth++;

	public void ExitStatementBlock()
	{
		const int move = 4;

		_statementDepth--;

		if (_statementDepth < 0)
		{
			throw new InvalidOperationException("Must be in a statement to exit one.");
		}

		if (_statementDepth <= 0 && _blocks.Count > 0)
		{
			// https://stackoverflow.com/a/17974
			int width = (_blocks.Count + MaxHeight - 1) / MaxHeight;

			if (SquarePlacement)
			{
				width = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(width)));
			}

			width *= move;

			int3 bPos = int3.Zero;

			for (int i = 0; i < _blocks.Count; i++)
			{
				_blocks[i].Position = bPos;
				bPos.Y++;

				if (bPos.Y > MaxHeight)
				{
					bPos.Y = 0;
					bPos.X += move;

					if (bPos.X >= width)
					{
						bPos.X = 0;
						bPos.Z += move;
					}
				}
			}

			_builder.AddBlockSegments(_blocks);

			_blocks.Clear();
		}
	}

	public void EnterExpressionBlock()
	{
	}

	public void ExitExpressionBlock()
	{
	}

	public void EnterHighlight()
		=> _inHighlight = true;

	public void ExitHightlight()
		=> _inHighlight = false;
}
