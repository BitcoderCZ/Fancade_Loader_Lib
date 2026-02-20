// <copyright file="TowerCodeGraphPositioner.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Editing.Scripting.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade.Editing.Scripting.Positioners;

/// <summary>
/// Provides functionality to layout a <see cref="CodeGraph"/> into <see cref="PositionedCodeGraph"/>.
/// Places nodes in towers, ignores code scopes.
/// </summary>
public static class TowerCodeGraphPositioner
{
    /// <summary>
    /// Computes the 3D positions for all nodes and connections in a <see cref="CodeGraph"/> according to the given layout options.
    /// Places nodes in towers, ignores scopes.
    /// </summary>
    /// <param name="graph">The <see cref="CodeGraph"/> to layout.</param>
    /// <param name="layoutOptions">Options controlling the layout.</param>
    /// <returns>A <see cref="PositionedCodeGraph"/> representing the positioned nodes and connections.</returns>
    public static PositionedCodeGraph Layout(CodeGraph graph, LayoutOptions? layoutOptions = null)
        => Layout([graph], layoutOptions);

    /// <summary>
    /// Computes the 3D positions for all nodes and connections in the <see cref="CodeGraph"/>s according to the given layout options.
    /// Places nodes in towers, ignores scopes.
    /// </summary>
    /// <param name="graphs">The <see cref="CodeGraph"/>s to layout.</param>
    /// <param name="layoutOptions">Options controlling the layout.</param>
    /// <returns>A <see cref="PositionedCodeGraph"/> representing the positioned nodes and connections.</returns>
    public static PositionedCodeGraph Layout(ReadOnlySpan<CodeGraph> graphs, LayoutOptions? layoutOptions = null)
    {
        LayoutOptions layoutOptionsVal = layoutOptions ?? LayoutOptions.Defaut;
        layoutOptionsVal.Validate();

        return CodeGraphPositionHelpers.LayoutGraphs(graphs, LayoutNodes, layoutOptionsVal);
    }

    /// <summary>
    /// Computes the 3D positions for all nodes and connections in the <see cref="CodeGraph"/>s according to the given layout options.
    /// Places nodes in towers, ignores scopes.
    /// </summary>
    /// <param name="graphs">The <see cref="CodeGraph"/>s to layout.</param>
    /// <param name="layoutOptions">Options controlling the layout.</param>
    /// <returns>A <see cref="PositionedCodeGraph"/> representing the positioned nodes and connections.</returns>
    [OverloadResolutionPriority(-1)]
    public static PositionedCodeGraph Layout(IEnumerable<CodeGraph> graphs, LayoutOptions? layoutOptions = null)
    {
        LayoutOptions layoutOptionsVal = layoutOptions ?? LayoutOptions.Defaut;
        layoutOptionsVal.Validate();

        return CodeGraphPositionHelpers.LayoutGraphs(graphs, LayoutNodes, layoutOptionsVal);
    }

    private static int3 LayoutNodes(CodeGraph graph, Span<PositionedNodeData> nodes, int3 offset, LayoutOptions layoutOptions)
    {
        var info = new LayoutInfo(graph, graph.NodeCount, layoutOptions);

        int nodeIndex = 0;
        int yLevel = 0;
        int towerIndex = 0;
        int2 basePos = int2.Zero;
        foreach (var node in graph._nodes)
        {
            if (node.Type is null or { Prefab: { Id: 0 } })
            {
                nodes[nodeIndex] = default;
                nodeIndex++;
                continue;
            }

            var position = offset + new int3(basePos.X, yLevel, basePos.Y);
            nodes[nodeIndex] = new PositionedNodeData(node.Type, node.Settings, (short3)position);

            nodeIndex++;
            yLevel++;
            if (yLevel >= layoutOptions.MaximumTowerHeight)
            {
                yLevel = 0;
                towerIndex++;
                basePos = GetTowerCoords(towerIndex, info.TowerX) * layoutOptions.TowerSpacing;
            }
        }

        return new int3(((info.TowerX - 1) * layoutOptions.TowerSpacing) + Prefab.MaxSize, Math.Min(layoutOptions.MaximumTowerHeight, graph.NodeCount), ((info.TowerZ - 1) * layoutOptions.TowerSpacing) + Prefab.MaxSize);
    }

    private static int2 GetTowerCoords(int towerIndex, int towersX)
        => int2.FromIndex(towerIndex, towersX);

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
        /// Initializes a new instance of the <see cref="LayoutOptions"/> struct.
        /// </summary>
        public LayoutOptions()
        {
        }

        /// <summary>
        /// Gets the maximum height of a code tower, before starting another one, default is 6, minimum is 1.
        /// </summary>
        /// <value>Gets the maximum height of a code tower.</value>
        public int MaximumTowerHeight { get; init; } = 6;

        /// <summary>
        /// Gets the spacing between the code towers, default is 4, minimum is 4.
        /// </summary>
        /// <value>Spacing between the code towers.</value>
        public int TowerSpacing { get; init; } = 4;

        /// <summary>
        /// Gets the <see cref="TowerCodeGraphPositioner.PlacementMode"/> option, default is <see cref="PlacementMode.Square"/>.
        /// </summary>
        /// <value>Gets the <see cref="TowerCodeGraphPositioner.PlacementMode"/> option.</value>
        public PlacementMode PlacementMode { get; init; } = PlacementMode.Square;

        internal void Validate()
        {
            ThrowHelper.ThrowIfLessThan(MaximumTowerHeight, 1);
            ThrowHelper.ThrowIfLessThan(TowerSpacing, 1);
            ThrowHelper.ThrowIfGreaterThanOrEqualToOrNegative((int)PlacementMode, 3);
        }
    }

    /// <summary>
    /// Determines how the code towers are placed.
    /// </summary>
    public enum PlacementMode
    {
        /// <summary>
        /// Places the towers in a square.
        /// </summary>
        Square,

        /// <summary>
        /// Places the towers in a line going in the X axis.
        /// </summary>
        LineX,

        /// <summary>
        /// Places the towers in a line going in the Z axis.
        /// </summary>
        LineZ,
    }

    private readonly struct LayoutInfo
    {
        public readonly int TowerCount;
        public readonly int TowerX;
        public readonly int TowerZ;
        public readonly CodeGraph Graph;

        public LayoutInfo(CodeGraph graph, int nodeCount, LayoutOptions options)
        {
            Graph = graph;

            TowerCount = (nodeCount + options.MaximumTowerHeight - 1) / options.MaximumTowerHeight;

            switch (options.PlacementMode)
            {
                case PlacementMode.Square:
                    TowerX = (int)MathF.Ceiling(MathF.Sqrt(TowerCount));
                    TowerZ = (int)MathF.Ceiling((float)TowerCount / TowerX);
                    break;

                case PlacementMode.LineX:
                    TowerX = TowerCount;
                    TowerZ = 1;
                    break;

                case PlacementMode.LineZ:
                    TowerX = 1;
                    TowerZ = TowerCount;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }
}