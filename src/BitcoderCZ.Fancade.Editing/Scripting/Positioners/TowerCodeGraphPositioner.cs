// <copyright file="TowerCodeGraphPositioner.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.InteropServices;
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
    {
        LayoutOptions layoutOptionsVal = layoutOptions ?? LayoutOptions.Defaut;
        layoutOptionsVal.Validate();

        var nodes = new List<PositionedNodeData>(graph.NodeCount);
        CollectionsMarshal.SetCount(nodes, graph.NodeCount);
        var info = new LayoutInfo(graph, nodes, layoutOptionsVal);

        int3 size = new int3(((info.TowerX - 1) * layoutOptionsVal.TowerSpacing) + Prefab.MaxSize, Math.Min(layoutOptionsVal.MaximumTowerHeight, graph.NodeCount), ((info.TowerZ - 1) * layoutOptionsVal.TowerSpacing) + Prefab.MaxSize);

        // todo: this can now just enumerate the nodes in order, instead of using scopes
        ApplyLayout(graph.RootScope, ref info);

        return PositionedCodeGraph.Create(nodes, CollectionsMarshal.AsSpan(graph._regions), CollectionsMarshal.AsSpan(graph._connections), size);
    }

    private static void ApplyLayout(CodeScope scope, ref LayoutInfo info)
    {
        var nodes = scope._nodes.GetEnumerator();

        for (; info.TowerIndex < info.TowerCount; info.TowerIndex++)
        {
            var basePos = GetTowerCoords(info.TowerIndex, info.TowerX) * info.Options.TowerSpacing;

            for (; info.YLevel < info.Options.MaximumTowerHeight; info.YLevel++, info.NodeIndex++)
            {
                if (!nodes.MoveNext())
                {
                    goto breakLabel;
                }

                var handle = nodes.Current;
                if (handle == NodeHandle.Null)
                {
                    info.YLevel--;
                    continue;
                }

                var node = info.Graph.GetNode(handle, out var nodeSettings);

                var position = new short3(basePos.X, info.YLevel, basePos.Y);
                info.Nodes[handle._index] = new PositionedNodeData(node.Type, nodeSettings, position);
            }

            info.YLevel = 0;
        }

    breakLabel:

        foreach (var child in scope.Children)
        {
            ApplyLayout(child, ref info);
        }
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
        /// Gets the maximum height of a code tower, before starting another one, default is 20, minimum is 1.
        /// </summary>
        /// <value>Gets the maximum height of a code tower.</value>
        public int MaximumTowerHeight { get; init; } = 20;

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

    private struct LayoutInfo
    {
        public readonly LayoutOptions Options;
        public readonly int TowerCount;
        public readonly int TowerX;
        public readonly int TowerZ;
        public readonly CodeGraph Graph;
        public readonly List<PositionedNodeData> Nodes;

        public int NodeIndex;
        public int TowerIndex;
        public int YLevel;

        public LayoutInfo(CodeGraph graph, List<PositionedNodeData> nodes, LayoutOptions options)
        {
            Graph = graph;
            Nodes = nodes;
            Options = options;

            TowerCount = (nodes.Count + options.MaximumTowerHeight - 1) / options.MaximumTowerHeight;

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