using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Editing.Scripting.Settings;

/// <summary>
/// Represents the type of a joystick.
/// </summary>
public enum JoystickType
{
    /// <summary>
    /// Outputs XZ vector values perpendicular to camera direction (while assuming that the screen is always facing straight to a certain axis).
    /// </summary>
    XZ,

    /// <summary>
    /// Outputs XY vector values regardless of where the camera is facing.
    /// </summary>
    Screen,
}
