namespace FancadeLoaderLib.Editing.Scripting.Settings;

/// <summary>
/// Specifies how the price of an item increases, used by menu item.
/// </summary>
public enum PriceIncrease
{
    /// <summary>
    /// The item is free.
    /// </summary>
    Free = 0,

    /// <summary>
    /// The price is always 10.
    /// </summary>
    Fixed10 = 1,

    /// <summary>
    /// The price is always 100.
    /// </summary>
    Fixed100 = 4,

    /// <summary>
    /// The price is always 1000.
    /// </summary>
    Fixed1000 = 7,

    /// <summary>
    /// The price is always 10 000.
    /// </summary>
    Fixed10000 = 10,

    /// <summary>
    /// The price starts at 10, then 20, 30, 40, ...
    /// </summary>
    Linear10 = 2,

    /// <summary>
    /// The price starts at 100, then 200, 300, 400, ...
    /// </summary>
    Linear100 = 5,

    /// <summary>
    /// The price starts at 1000, then 2000, 3000, 4000, ...
    /// </summary>
    Linear1000 = 8,

    /// <summary>
    /// The price starts at 10, then 20, 40, 80, ...
    /// </summary>
    Double10 = 3,

    /// <summary>
    /// The price starts at 100, then 200, 400, 800, ...
    /// </summary>
    Double100 = 6,

    /// <summary>
    /// The price starts at 1000, then 2000, 4000, 8000, ...
    /// </summary>
    Double1000 = 9,
}
