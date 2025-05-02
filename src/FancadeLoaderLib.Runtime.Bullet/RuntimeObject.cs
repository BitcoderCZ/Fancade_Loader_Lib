using BulletSharp;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class RuntimeObject : IDisposable
{
    public RuntimeObject(FcObject id, RigidBody rigidBody, bool userCreated)
    {
        Id = id;
        RigidBody = rigidBody;
        UserCreated = userCreated;
    }

    public FcObject Id { get; }

    public RigidBody RigidBody { get; }

    public bool UserCreated { get; }

    public bool Visible { get; set; } = true;

    public void Dispose()
        => RigidBody.Dispose();
}
