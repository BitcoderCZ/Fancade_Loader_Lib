using BitcoderCZ.Fancade.Editing.Scripting.Settings;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BitcoderCZ.Fancade.Runtime.Input;

/// <summary>
/// Provides a way of getting touches and swipes.
/// </summary>
public interface IFcInputHandler
{
    /// <summary>
    /// Gets the position of a touch.
    /// </summary>
    /// <param name="state">State of the touch to detect.</param>
    /// <param name="fingerIndex">The finger whose touch should be detected.</param>
    /// <param name="touchPos">Position of the touch.</param>
    /// <returns><see langword="true"/> if the specified touch is active; otherwise, <see langword="false"/>.</returns>
    bool TryGetTouch(TouchState state, int fingerIndex, out Vector2 touchPos);

    /// <summary>
    /// Gets the direction of a swipe.
    /// </summary>
    /// <param name="direction">Direction of the swipe.</param>
    /// <returns><see langword="true"/> if a swipe is active; otherwise, <see langword="false"/>.</returns>
    bool TryGetSwipe(out Vector3 direction);
}
