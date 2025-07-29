using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoderCZ.Fancade.Editing.Scripting.Exceptions;

/// <summary>
/// An <see cref="Exception"/> thrown when goto recursion is encountered.
/// </summary>
public sealed class GotoRecursionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GotoRecursionException"/> class.
    /// </summary>
    /// <param name="firstGotoLabel">The label the first goto in the chanin jumped to.</param>
    /// <param name="encounteredLabels">All of the encountered labels in the chanin.</param>
    public GotoRecursionException(string firstGotoLabel, string[] encounteredLabels)
        : base($"Goto recursion was detected, the first goto jumps to '{firstGotoLabel}'; Encountered labels: {string.Join(", ", encounteredLabels)}.")
    {
        FirstGotoLabel = firstGotoLabel;
        EncounteredLabels = encounteredLabels;
    }

    /// <summary>
    /// Gets the label the first goto in the chanin jumped to.
    /// </summary>
    /// <value>The label the first goto in the chanin jumped to.</value>
    public string FirstGotoLabel { get; }

    /// <summary>
    /// Gets all of the encountered labels in the chanin.
    /// </summary>
    /// <value>All of the encountered labels in the chanin.</value>
    public string[] EncounteredLabels { get; }
}
