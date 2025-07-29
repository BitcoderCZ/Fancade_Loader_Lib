namespace BitcoderCZ.Fancade.Editing.Scripting.Settings;

/// <summary>
/// Determines how players are ranked, used by set score.
/// </summary>
public enum Ranking
{
    /// <summary>
    /// The player with the highest score is ranked first, score is displayed as a number.
    /// </summary>
    MostPoints = 2,

    /// <summary>
    /// The player with the lowest score is ranked first, score is displayed as a number.
    /// </summary>
    FewestPoints = 3,

    /// <summary>
    /// The player with the lowest score is ranked first, score is displayed as time.
    /// </summary>
    FastestTime = 4,

    /// <summary>
    /// The player with the highest score is ranked first, score is displayed as time.
    /// </summary>
    LongestTime = 5,
}
