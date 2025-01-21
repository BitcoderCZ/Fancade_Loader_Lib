// <copyright file="IScopedCodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Utils;
using System;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

public interface IScopedCodePlacer : ICodePlacer
{
	void EnterStatementBlock();

	virtual IDisposable StatementBlock()
	{
		EnterStatementBlock();
		return new Disposable(ExitStatementBlock);
	}

	void ExitStatementBlock();

	void EnterExpressionBlock();

	virtual IDisposable ExpressionBlock()
	{
		EnterExpressionBlock();
		return new Disposable(ExitExpressionBlock);
	}

	void ExitExpressionBlock();

	void EnterHighlight();

	virtual IDisposable HighlightBlock()
	{
		EnterExpressionBlock();
		return new Disposable(ExitExpressionBlock);
	}

	void ExitHightlight();
}
