// <copyright file="StructuredCodeGraphPositioner.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade.Editing.Scripting.Positioners;

/// <summary>
/// Provides functionality to layout a <see cref="CodeGraph"/> into <see cref="PositionedCodeGraph"/>.
/// Places nodes on the ground (Y position is always 0), from +Z to -Z, respecting code scopes.
/// </summary>
public static class StructuredCodeGraphPositioner
{
    /// <summary>
    /// Computes the 3D positions for all nodes and connections in a <see cref="CodeGraph"/> according to the given layout options.
    /// Places nodes on the ground, from +Z to -Z, respecting code scopes.
    /// </summary>
    /// <param name="graph">The <see cref="CodeGraph"/> to layout.</param>
    /// <param name="layoutOptions">Options controlling the layout.</param>
    /// <returns>A <see cref="PositionedCodeGraph"/> representing the positioned nodes and connections.</returns>
    public static PositionedCodeGraph Layout(CodeGraph graph, LayoutOptions? layoutOptions = null)
    {
        var layoutOptionsVal = layoutOptions ?? LayoutOptions.Defaut;

        layoutOptionsVal.Validate();

        var scopeLayouts = CalculateAllLayouts(graph.RootScope, layoutOptionsVal);

        var scopeDepth = new int[graph.RootScope.GetHorizontalSize()];

        var rootLayout = scopeLayouts[graph.RootScope];

        var nodes = new PositionedNode[graph.NodeCount];
        int depth = ApplyLayout(graph.RootScope, 0, new int3(rootLayout.WidthLeft, 0, 0), scopeLayouts, nodes, scopeDepth, layoutOptionsVal);

        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var newOffset = node.Offset + new int3(0, 0, depth);
            Debug.Assert(newOffset.X >= 0);
            Debug.Assert(newOffset.Y >= 0);
            Debug.Assert(newOffset.Z >= 0);
            nodes[i] = new(newOffset, node.Type, node._settings, node._index);
        }

        return new PositionedCodeGraph(nodes, CollectionsMarshal.AsSpan(graph._regions), CollectionsMarshal.AsSpan(graph._connections), new int3(rootLayout.GetTotalWidth(layoutOptionsVal.PaddingX), rootLayout.Height, depth));
    }

    private static int ApplyLayout(CodeScope scope, int layer, int3 origin, Dictionary<CodeScope, ScopeLayout> scopeLayouts, PositionedNode[] nodes, int[] scopeDepth, LayoutOptions layoutOptions)
    {
        // todo: track depth per X level, only move when necesary
        var thisLayout = scopeLayouts[scope];

        switch (scope.Type)
        {
            case ScopeType.Statement:
                origin.X += thisLayout.WidthLeft;
                layer += scope.GetMaxExpressionDepth();
                break;
            case ScopeType.Expression:
                origin.X -= thisLayout.Width;
                break;
        }

        origin.Z -= (scope.FirstNodeSize ?? int3.One).Z - 1;

        Debug.Assert(origin.X >= 0);

        int3 currentPos = origin;
        var childEnumerator = scope._children.GetEnumerator();
        CodeScope? currentChild = childEnumerator.MoveNext() ? childEnumerator.Current : null;

        int depth = 0;
        int nodeIndex = 0;
        foreach (var node in scope._nodes)
        {
            nodes[node._index] = new PositionedNode(currentPos, node.Type, node._settings, node._index);

            int nodeZOffset = node.Type.Size.Z - 1;
            Debug.Assert(nodeZOffset >= 0);

            int zSize = node.Type.Size.Z;

            if (currentChild is not null && currentChild.DeclaringNodeIndex == nodeIndex)
            {
                int3 statementPos = currentPos + new int3(thisLayout.Width + layoutOptions.PaddingX, 0, layoutOptions.StatementDepthOffset + nodeZOffset);
                int3 expressionPos = currentPos - new int3(layoutOptions.PaddingX, 0, -nodeZOffset);
                int statementZSize = 0;
                int expressionZSize = 0;

                do
                {
                    int3 childOrigin = default;
                    int childLayer = default;
                    switch (currentChild.Type)
                    {
                        case ScopeType.Statement:
                            childOrigin = statementPos;
                            childLayer = layer + 1;
                            break;
                        case ScopeType.Expression:
                            childOrigin = expressionPos;
                            childLayer = layer - 1;
                            break;
                    }

                    int childZSize = ApplyLayout(currentChild, childLayer, childOrigin, scopeLayouts, nodes, scopeDepth, layoutOptions);

                    switch (currentChild.Type)
                    {
                        case ScopeType.Statement:
                            statementPos.Z -= childZSize;
                            statementZSize += childZSize;
                            break;
                        case ScopeType.Expression:
                            expressionPos.Z -= childZSize;
                            expressionZSize += childZSize;
                            break;
                    }

                    currentChild = childEnumerator.MoveNext() ? childEnumerator.Current : null;
                } while (currentChild is not null && currentChild.DeclaringNodeIndex == nodeIndex);

                zSize = Math.Max(zSize, Math.Max(statementZSize, expressionZSize));
            }

            currentPos.Z -= zSize + layoutOptions.PaddingZ;
            depth += zSize + layoutOptions.PaddingZ;
            nodeIndex++;
        }

        Debug.Assert(!childEnumerator.MoveNext());
        Debug.Assert(currentChild is null);

        return depth;
    }

    private static Dictionary<CodeScope, ScopeLayout> CalculateAllLayouts(CodeScope scope, LayoutOptions layoutOptions)
    {
        var map = new Dictionary<CodeScope, ScopeLayout>(32);

        CalculateAllLayouts(scope, map.Add, layoutOptions);

        return map;
    }

    private static ScopeLayout CalculateAllLayouts(CodeScope scope, Action<CodeScope, ScopeLayout> onLayoutCalculated, LayoutOptions layoutOptions)
    {
        var thisLayout = CalculateLayoutNodesOnly(scope, layoutOptions.PaddingZ);

        int statementWidth = 0;
        int expressionWidth = 0;
        int height = 0;

        foreach (var child in scope.Children)
        {
            var childLayout = CalculateAllLayouts(child, onLayoutCalculated, layoutOptions);

            switch (child.Type)
            {
                case ScopeType.Statement:
                    statementWidth = Math.Max(statementWidth, childLayout.GetTotalWidth(layoutOptions.PaddingX));
                    break;
                case ScopeType.Expression:
                    expressionWidth = Math.Max(expressionWidth, childLayout.GetTotalWidth(layoutOptions.PaddingX));
                    break;
            }

            height = Math.Max(height, childLayout.Height);
        }

        if (statementWidth is not 0)
        {
            statementWidth += layoutOptions.PaddingX;
        }

        if (expressionWidth is not 0)
        {
            expressionWidth += layoutOptions.PaddingX;
        }

        var layout = new ScopeLayout(thisLayout.Width, expressionWidth, statementWidth, Math.Max(thisLayout.Height, height));

        onLayoutCalculated(scope, layout);

        return layout;
    }

    private static ScopeLayout CalculateLayoutNodesOnly(CodeScope scope, int paddingZ = 1)
    {
        Debug.Assert(paddingZ >= 0);

        int width = 0;
        int height = 0;

        foreach (var node in scope.Nodes)
        {
            height = Math.Max(height, node.Type.Size.Y);
            width = Math.Max(width, node.Type.Size.X);
        }

        return new ScopeLayout(width, 0, 0, height);
    }

    /// <summary>
    /// Options for controlling how nodes and scopes are laid out in a <see cref="CodeGraph"/>.
    /// </summary>
    public readonly struct LayoutOptions
    {
        /// <summary>
        /// Gets the default <see cref="LayoutOptions"/>.
        /// </summary>
        /// <value>A <see cref="LayoutOptions"/> instance with the default values.</value>
        public static LayoutOptions Defaut => new();

        /// <summary>
        /// Gets a compact <see cref="LayoutOptions"/>.
        /// </summary>
        /// <value>A <see cref="LayoutOptions"/> instance with minimum spacing.</value>
        public static LayoutOptions Compact => new() { PaddingZ = 0, StatementDepthOffset = 0, };

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutOptions"/> struct.
        /// </summary>
        public LayoutOptions()
        {
        }

        /// <summary>
        /// Gets the padding between nodes on the X axis, default is 1, minimum is 1.
        /// </summary>
        /// <value>Padding between nodes on the X axis.</value>
        public int PaddingX { get; init; } = 1;

        /// <summary>
        /// Gets the padding between nodes on the Z axis, default is 1, minimum is 0.
        /// </summary>
        /// <value>Padding between nodes on the Z axis.</value>
        public int PaddingZ { get; init; } = 1;

        /// <summary>
        /// Gets the offset along the Z axis of child statement scopes, default is 1, minimum is 0.
        /// </summary>
        /// <value>Offset along the Z axis of child statement scopes.</value>
        public int StatementDepthOffset { get; init; } = 1;

        internal void Validate()
        {
            ThrowHelper.ThrowIfLessThan(PaddingX, 1);
            ThrowHelper.ThrowIfNegative(PaddingZ);
            ThrowHelper.ThrowIfNegative(StatementDepthOffset);
        }
    }

    private readonly struct ScopeLayout
    {
        public ScopeLayout(int width, int widthLeft, int widthRight, int height)
        {
            Width = width;
            WidthLeft = widthLeft;
            WidthRight = widthRight;
            Height = height;
        }

        public int Width { get; }

        public int WidthLeft { get; }

        public int WidthRight { get; }

        public int Height { get; }

        public int GetTotalWidth(int paddingX)
        {
            int widthLeft = WidthLeft is 0 ? 0 : WidthLeft + paddingX;
            int widthRight = WidthLeft is 0 ? 0 : WidthRight + WidthRight;
            return widthLeft + Width + widthRight;
        }
    }
}