using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class Camera
{
    public Vector3 Position { get; internal set; }

    public Vector3 Focus { get; internal set; }

    public Vector3 WorldPosition { get; internal set; }

    public Quaternion Rotation { get; internal set; }

    public float Range { get; internal set; }

    public bool Perspective { get; internal set; }
}
