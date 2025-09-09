using System.Diagnostics;
using System.Numerics;

namespace BitcoderCZ.Fancade.Runtime.Utils;

/// <summary>
/// A helper class for calculating the projection matrix of the fancade camera and screen to world/world to screen.
/// </summary>
public sealed class FcCamera
{
    private Vector3 _position;
    private float _range = 80f;

    /// <summary>
    /// Initializes a new instance of the <see cref="FcCamera"/> class.
    /// </summary>
    /// <param name="screen">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    public FcCamera(ScreenInfo screen)
    {
        Set(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(45f * (MathF.PI / 180f), 45f * (MathF.PI / 180f), 0f), 80f, false, screen);
    }

    /// <summary>
    /// Gets a value indicating whether the camera is in perspective or orthographic mode.
    /// </summary>
    /// <value><see langword="true"/> if the camera is in perspective mode;
    /// <see langword="false"/> if the camera is in orthographic mode.</value>
    public bool Perspective { get; internal set; }

    /// <summary>
    /// Gets the vertical fov of the camera.
    /// </summary>
    /// <value>The vertical field of view of the camera.</value>
    public float VerticalFov { get; internal set; }

    /// <summary>
    /// Gets the horizontal fov of the camera.
    /// </summary>
    /// <value>The horizontal field of view of the camera.</value>
    public float HorizontalFov { get; internal set; }

    /// <summary>
    /// Gets a value indicating how much is the projection orthographic.
    /// </summary>
    /// <value>A value between 0 and 1, where 0 is perspective and 1 is orthographic.</value>
    public float Ortho { get; internal set; }

    /// <summary>
    /// Gets the camera's focus point.
    /// </summary>
    /// <value>The camera's focus point.</value>
    public Vector3 Focus { get; internal set; }

    /// <summary>
    /// Gets the camera's rotation.
    /// </summary>
    /// <value>The camera's rotation.</value>
    public Quaternion Rotation { get; internal set; }

    /// <summary>
    /// Gets the auto distance.
    /// </summary>
    /// <value>Auto distance.</value>
    public float DistanceAuto { get; internal set; }

    /// <summary>
    /// Gets a vector facing to the right of the camera's view direction.
    /// </summary>
    /// <value>A vector facing to the right of the camera's view direction.</value>
    public Vector3 Right { get; internal set; }

    /// <summary>
    /// Gets a vector facing up relative to the camera's view direction.
    /// </summary>
    /// <value>A vector facing up relative to the camera's view direction.</value>
    public Vector3 Up { get; internal set; }

    /// <summary>
    /// Gets a vector facing along the camera's view direction.
    /// </summary>
    /// <value>A vector facing along the camera's view direction.</value>
    public Vector3 Forward { get; internal set; }

    /// <summary>
    /// Gets <see cref="DistanceAuto"/>, but updated in <see cref="Step(ScreenInfo)"/>.
    /// </summary>
    /// <value><see cref="DistanceAuto"/>, but updated in <see cref="Step(ScreenInfo)"/>.</value>
    public float Distance { get; internal set; }

    /// <summary>
    /// Gets the camera's zoom.
    /// </summary>
    /// <value>The camera's zoom.</value>
    public float Zoom { get; internal set; }

    /// <summary>
    /// Gets the camera's world position.
    /// </summary>
    /// <value>The camera's world position.</value>
    public Vector3 WorldPos { get; internal set; }

    /// <summary>
    /// Gets the world to camera matrix.
    /// </summary>
    /// <value>The world to camera matrix.</value>
    public Matrix4x4 WorldToCameraMatrix { get; internal set; }

    /// <summary>
    /// Gets the camera's projection matrix.
    /// </summary>
    /// <value>The camera's projection matrix.</value>
    public Matrix4x4 ProjectionMatrix { get; internal set; }

    /// <summary>
    /// Gets the combination of <see cref="WorldToCameraMatrix"/> and <see cref="ProjectionMatrix"/> matrix.
    /// </summary>
    /// <value>The combination of <see cref="WorldToCameraMatrix"/> and <see cref="ProjectionMatrix"/> matrix.</value>
    public Matrix4x4 WorldViewpointMatrix { get; internal set; }

    /// <summary>
    /// Gets the inverse of <see cref="WorldViewpointMatrix"/>.
    /// </summary>
    /// <value>Inverse of <see cref="WorldViewpointMatrix"/>.</value>
    public Matrix4x4 WorldViewpointMatrixInverted { get; internal set; }

    /// <summary>
    /// Sets <see cref="Focus"/>, <see cref="Rotation"/>, <see cref="DistanceAuto"/>, <see cref="Ortho"/>, vertical and horizontal fov.
    /// Rest of the properties are updated when <see cref="Step(ScreenInfo)"/> (to support then fancade behavior that screen to world/world to screen are delayed by 1 frame).
    /// </summary>
    /// <param name="position">The new position of the camera; or <see langword="null"/>, if the position should not be changed.</param>
    /// <param name="rotation">The new rotation of the camera; or <see langword="null"/>, if the rotation should not be changed.</param>
    /// <param name="range">The new range of the camera; or <see langword="null"/>, if the range should not be changed.</param>
    /// <param name="perspective">The new projection type of the camera; or <see langword="null"/>, if the projection type should not be changed.</param>
    /// <param name="screen">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    public void Set(Vector3? position, Quaternion? rotation, float? range, bool? perspective, ScreenInfo screen)
    {
        if (perspective is not null)
        {
            Perspective = perspective.Value;
        }

        var newPosition = position ?? _position;
        var newRotation = rotation ?? Rotation;
        float newRange = range ?? _range;

        _position = newPosition;
        _range = newRange;

        if (!Perspective)
        {
            Ortho = 1f;

            Focus = newPosition;
            Rotation = newRotation;
            DistanceAuto = newRange;

            const float MaxVerticalFov = 15f * (MathF.PI / 180f); // 0.267f
            VerticalFov = screen.AspectRatio * MaxVerticalFov;
            if (VerticalFov >= MaxVerticalFov)
            {
                VerticalFov = MaxVerticalFov;
            }

            HorizontalFov = VerticalFov / screen.AspectRatio;
        }
        else
        {
            Ortho = 0f;

            Rotation = newRotation;

            if (range is not null)
            {
                HorizontalFov = newRange * (MathF.PI / 180f);
            }
            else
            {
                HorizontalFov = 60f * (MathF.PI / 180f);
            }

            float fovHorizontalTemp;
            float fovVerticalTemp;
            if (!screen.Portrait)
            {
                float temp = HorizontalFov / screen.AspectRatio;
                VerticalFov = HorizontalFov;
                HorizontalFov = temp;

                fovHorizontalTemp = VerticalFov;
                fovVerticalTemp = HorizontalFov;
            }
            else
            {
                VerticalFov = screen.AspectRatio * HorizontalFov;

                fovHorizontalTemp = HorizontalFov;
                fovVerticalTemp = VerticalFov;
            }

            float aspectRatioAdjustedFov = fovVerticalTemp / 3.054326f;

            if (aspectRatioAdjustedFov >= 1.0)
            {
                VerticalFov = fovVerticalTemp / aspectRatioAdjustedFov;
                HorizontalFov = fovHorizontalTemp / aspectRatioAdjustedFov;
            }

            DistanceAuto = 15f;

            if (position is not null)
            {
                Focus = newPosition + (Vector3.Transform(Vector3.UnitZ, Rotation) * DistanceAuto);
            }
        }
    }

    /// <summary>
    /// Updates the rest of the properties not set by <see cref="Set(Vector3?, Quaternion?, float?, bool?, ScreenInfo)"/>.
    /// </summary>
    /// <param name="screen">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    public void Step(ScreenInfo screen)
    {
        Right = Vector3.Transform(Vector3.UnitX, Rotation);
        Up = Vector3.Transform(Vector3.UnitY, Rotation);
        Forward = Vector3.Transform(Vector3.UnitZ, Rotation);
        Distance = DistanceAuto + 0.0f;
        Zoom = Distance * 0.1f;
        WorldPos = Focus - (Forward * Distance);

        Matrix4x4 matWorldV = Matrix4x4.CreateTranslation(-WorldPos) * Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Rotation));

        Matrix4x4 projection;
        if (Ortho == 1f)
        {
            const float Near = 1f;
            const float Far = 200f;

            if (screen.Landscape)
            {
                float right = Zoom / screen.AspectRatio;
                float left = -Zoom / screen.AspectRatio;
                float top = Zoom;
                float bottom = -Zoom;

                projection = Matrix4x4Utils.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, Near, Far);
            }
            else
            {
                float right = Zoom;
                float left = -Zoom;
                float top = Zoom / screen.AspectRatio;
                float bottom = -Zoom / screen.AspectRatio;

                projection = Matrix4x4Utils.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, Near, Far);
            }
        }
        else
        {
            float orthoInv = 1f - Ortho;
            float depthOffset = Ortho + Ortho + (orthoInv * 0.1f) + (orthoInv * 0.1f);
            float vFovTan = MathF.Tan(VerticalFov * 0.5f);

            if (Ortho == 0f)
            {
                // perspective left handed
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
                projection = new Matrix4x4(
                    screen.AspectRatio * (1f / vFovTan), 0f, 0f, 0f,
                    0f, 1f / vFovTan, 0f, 0f,
                    0f, 0f, (depthOffset + 400f) / (400f - depthOffset), 1f,
                    0f, 0f, (depthOffset * -800f) / (400f - depthOffset), 0f);
#pragma warning restore SA1117 // Parameters should be on same line or separate lines
            }
            else
            {
                float negZoom = -Zoom;
                float orthoWidth = screen.AspectRatio * Zoom;
                float orthoWidthDiff = orthoWidth - (screen.AspectRatio * negZoom);
                float zoomDouble = Zoom + Zoom;
                float zoomDiff = Zoom - Zoom;

                // perspective left handed
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
                projection = new Matrix4x4(
                    (Ortho * (2f / zoomDouble)) + (orthoInv * screen.AspectRatio * (1f / vFovTan)), 0f, 0f, 0f,
                    0f, (orthoInv * (1f / vFovTan)) + (Ortho * (2f / orthoWidthDiff)), 0f, 0f,
                    0f, 0f, (Ortho * 0.0050251256f) + (orthoInv * ((depthOffset + 400f) / (400f - depthOffset))), orthoInv,
                    -Ortho * (zoomDiff / zoomDouble), -Ortho * ((orthoWidth + (screen.AspectRatio * negZoom)) / orthoWidthDiff), (Ortho * -1.0100503f) + (orthoInv * ((depthOffset * -800f) / (400f - depthOffset))), Ortho);
#pragma warning restore SA1117 // Parameters should be on same line or separate lines
            }
        }

        WorldToCameraMatrix = matWorldV;
        ProjectionMatrix = projection;
        WorldViewpointMatrix = matWorldV * projection;
        bool inverted = Matrix4x4.Invert(WorldViewpointMatrix, out var worldViewpointInverted);
        Debug.Assert(inverted, $"{nameof(WorldViewpointMatrix)} inversion should always succeed.");
        WorldViewpointMatrixInverted = worldViewpointInverted;
    }

    /// <summary>
    /// Gets the near and far positions for a given screen coordinate.
    /// </summary>
    /// <param name="screenPos">The screen position.</param>
    /// <param name="screen">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    /// <returns>The near and far positions.</returns>
    public (Vector3 Near, Vector3 Far) ScreenToWorld(Vector2 screenPos, ScreenInfo screen)
    {
        float ndcX = ((2.0f * screenPos.X) / screen.Width) - 1.0f;
        float ndcY = 1.0f - ((2.0f * screenPos.Y) / screen.Height);

        Vector4 clipNear = new Vector4(ndcX, ndcY, 0f, 1.0f);
        Vector4 clipFar = new Vector4(ndcX, ndcY, Perspective ? 1f : 2f, 1.0f);

        Vector4 worldNearH = Vector4.Transform(clipNear, WorldViewpointMatrixInverted);
        Vector4 worldFarH = Vector4.Transform(clipFar, WorldViewpointMatrixInverted);

        Vector3 worldNear = new Vector3(
            worldNearH.X / worldNearH.W,
            worldNearH.Y / worldNearH.W,
            worldNearH.Z / worldNearH.W);

        Vector3 worldFar = new Vector3(
            worldFarH.X / worldFarH.W,
            worldFarH.Y / worldFarH.W,
            worldFarH.Z / worldFarH.W);

        return (worldNear, worldFar);
    }

    /// <summary>
    /// Gets the position of a world coordinate when projected on the screen.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="screen">A <see cref="ScreenInfo"/> instance representing the current viewport.</param>
    /// <returns>Position of <paramref name="worldPos"/> on the screen.</returns>
    public Vector2 WorldToScreen(Vector3 worldPos, ScreenInfo screen)
    {
        Vector4 worldPos4 = new Vector4(worldPos, 1f);

        Vector4 transformed = Vector4.Transform(worldPos4, WorldViewpointMatrix);

        float screenX = (screen.Width * 0.5f) + ((transformed.X / transformed.W) * 0.5f * screen.Width);
        float screenY = (screen.Height * 0.5f) - ((transformed.Y / transformed.W) * 0.5f * screen.Height);

        return new Vector2(screenX, screenY);
    }
}
