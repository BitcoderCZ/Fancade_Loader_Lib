using BitcoderCZ.Fancade.Runtime.Syntax;
using BitcoderCZ.Fancade.Runtime.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// An <see cref="IFcEnvironment"/> implementation.
/// </summary>
public sealed class FcEnvironment : IFcEnvironment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FcEnvironment"/> class.
    /// </summary>
    /// <param name="ast">The <see cref="FcAST"/> representing the environment.</param>
    /// <param name="index">The environment's index.</param>
    /// <param name="outerEnvironmentIndex">Index of the outer environment; or <c>-1</c>, if this is the outer-most environment.</param>
    /// <param name="outerPosition">Position of this environment in the outer environment; or <see cref="int3.Zero"/>, if this is the outer-most environment.</param>
    public FcEnvironment(FcAST ast, int index, int outerEnvironmentIndex, int3 outerPosition)
    {
        Index = index;
        OuterEnvironmentIndex = outerEnvironmentIndex;
        AST = ast;
        OuterPosition = outerPosition;
    }

    /// <summary>
    /// Gets the <see cref="FcAST"/> representing the environment.
    /// </summary>
    /// <value>The <see cref="FcAST"/> representing the environment.</value>
    public FcAST AST { get; }

    /// <inheritdoc/>
    public int Index { get; }

    /// <inheritdoc/>
    public int OuterEnvironmentIndex { get; }

    /// <inheritdoc/>
    public int3 OuterPosition { get; }

    /// <summary>
    /// Gets a <see cref="Dictionary{TKey, TValue}"/> for storing a prefab's data.
    /// </summary>
    /// <value>A <see cref="Dictionary{TKey, TValue}"/> for storing a prefab's data.</value>
    public Dictionary<int3, object> BlockData { get; } = [];

    /// <summary>
    /// Gets the id of the prefab this <see cref="FcEnvironment"/> represents.
    /// </summary>
    /// <value>Id of the prefab this <see cref="FcEnvironment"/> represents.</value>
    public ushort PrefabId => AST.PrefabId;

    internal static List<EntryPoint> GetEntryPointsInExecutionOrder(FcEnvironment[] environments)
    {
        var list = new List<EntryPoint>();

        WriteEntryPointsInExecutionOrder(environments, 0, list);

        return list;
    }

    private static void WriteEntryPointsInExecutionOrder(FcEnvironment[] environments, int index, List<EntryPoint> list)
    {
        var environment = environments[index];
        var ast = environment.AST;
        var entryPointTerminals = ast.EntryPointTerminals;
        var statements = ast.Statements;

        // should also sort by terminal position?
        foreach (var item in ((IEnumerable<SortItem>)[
             ..entryPointTerminals.Select((item, index) => new SortItem(item.BlockPosition, index)),
             .. statements.Values.OfType<CustomStatementSyntax>().Select(item => new SortItem(item.Position))])
                .OrderBy(item => item.Pos, ScriptPositionComparer.Instance))
        {
            if (item.EntryPointIndex is { } entryPointIndex)
            {
                var entryPoint = entryPointTerminals[entryPointIndex];
                list.Add(new EntryPoint(environment.Index, entryPoint.BlockPosition, entryPoint.TerminalPosition));
            }
            else
            {
                var statement = statements[item.Pos];
                var envPos = statement.Position;
                var innerEnvironment = environments.FirstOrDefault(item => item.OuterEnvironmentIndex == index && item.OuterPosition == envPos);
                if (innerEnvironment is not null)
                {
                    WriteEntryPointsInExecutionOrder(environments, innerEnvironment.Index, list);
                }
            }
        }
    }

    private readonly struct SortItem
    {
        public SortItem(int3 pos)
        {
            Pos = pos;
            EntryPointIndex = null;
        }

        public SortItem(int3 pos, int? entryPointIndex)
        {
            Pos = pos;
            EntryPointIndex = entryPointIndex;
        }

        public readonly int3 Pos { get; }

        public readonly int? EntryPointIndex { get; }
    }
}