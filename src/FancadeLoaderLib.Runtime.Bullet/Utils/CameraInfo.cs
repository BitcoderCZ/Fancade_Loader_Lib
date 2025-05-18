using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;

internal sealed class CameraInfo
{
    public CameraInfo(ScreenInfo screen)
    {
        Set(null, null, null, false, screen);
    }

    public bool Perspective { get; internal set; }

    public float VerticalFov { get; internal set; }
    public float HorizontalFov { get; internal set; }
    public float Ortho { get; internal set; }
    public Vector3 Focus { get; internal set; }
    public Quaternion WorldQuat { get; internal set; }
    public float DistAuto { get; internal set; }

    public Vector3 Right { get; internal set; }
    public Vector3 Up { get; internal set; }
    public Vector3 Forward { get; internal set; }
    public float Dist { get; internal set; }
    public float Zoom { get; internal set; }
    public Vector3 WorldPos { get; internal set; }

    public Matrix4x4 WorldViewpoint { get; internal set; }
    public Matrix4x4 WorldViewpointInverted { get; internal set; }

    public void Set(Vector3? pos, Quaternion? rot, float? range, bool perspective, ScreenInfo screen)
    {
        Vector3 vPos = pos ?? Vector3.Zero;
        Quaternion vRot = rot ?? Quaternion.CreateFromYawPitchRoll(45f * (MathF.PI / 180f), 45f * (MathF.PI / 180f), 0f);
        float vRange = range ?? 80f;
        Perspective = perspective;

        if (!perspective)
        {
            Ortho = 1f;

            Focus = vPos;
            WorldQuat = vRot;
            DistAuto = vRange;

            const float MaxVerticalFov = 15f * (MathF.PI / 180f); // 0.267f
            VerticalFov = screen.AspectRatio * MaxVerticalFov;
            if (MaxVerticalFov <= VerticalFov)
            {
                VerticalFov = MaxVerticalFov;
            }

            HorizontalFov = VerticalFov / screen.AspectRatio;
        }
        else
        {
            Ortho = 0f;

            WorldQuat = vRot;

            if (range is not null)
            {
                HorizontalFov = vRange * (MathF.PI / 180f);
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

            if (1.0 <= aspectRatioAdjustedFov)
            {
                VerticalFov = fovVerticalTemp / aspectRatioAdjustedFov;
                HorizontalFov = fovHorizontalTemp / aspectRatioAdjustedFov;
            }

            DistAuto = 15f;

            if (pos is not null)
            {
                Focus = vPos + Vector3.Transform(Vector3.UnitZ, WorldQuat) * DistAuto;
            }
        }
    }

    public void Step(ScreenInfo screen)
    {
        Right = Vector3.Transform(Vector3.UnitX, WorldQuat);
        Up = Vector3.Transform(Vector3.UnitY, WorldQuat);
        Forward = Vector3.Transform(Vector3.UnitZ, WorldQuat);
        Dist = DistAuto + 0.0f;
        Zoom = Dist * 0.1f;
        WorldPos = Focus - Forward * Dist;

        Matrix4x4 mat_world_v = Matrix4x4.CreateTranslation(-WorldPos) * Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(WorldQuat));

        Matrix4x4 proj;
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

                proj = Matrix4x4Utils.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, Near, Far);
            }
            else
            {
                float right = Zoom;
                float left = -Zoom;
                float top = Zoom / screen.AspectRatio;
                float bottom = -Zoom / screen.AspectRatio;

                proj = Matrix4x4Utils.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, Near, Far);
            }
        }
        else
        {
            float orthoInv = 1f - Ortho;
            float depthOffset = Ortho + Ortho + orthoInv * 0.1f + orthoInv * 0.1f;
            float vfovTan = MathF.Tan(VerticalFov * 0.5f);

            if (Ortho == 0f)
            {
                // perspective left handed
                proj = new Matrix4x4(
                    screen.AspectRatio * (1f / vfovTan), 0f, 0f, 0f,
                    0f, 1f / vfovTan, 0f, 0f,
                    0f, 0f, (depthOffset + 400f) / (400f - depthOffset), 1f,
                    0f, 0f, (depthOffset * -800f) / (400f - depthOffset), 0f);
            }
            else
            {
                float negZoom = -Zoom;
                float orthoWidth = screen.AspectRatio * Zoom;
                float orthoWidthDiff = orthoWidth - screen.AspectRatio * negZoom;
                float zoomDouble = Zoom + Zoom;
                float zoomDiff = Zoom - Zoom;

                // perspective left handed
                proj = new Matrix4x4(
                    Ortho * (2f / zoomDouble) + orthoInv * screen.AspectRatio * (1f / vfovTan), 0f, 0f, 0f,
                    0f, orthoInv * (1f / vfovTan) + Ortho * (2f / orthoWidthDiff), 0f, 0f,
                    0f, 0f, Ortho * 0.0050251256f + orthoInv * ((depthOffset + 400f) / (400f - depthOffset)), orthoInv,
                    -Ortho * (zoomDiff / zoomDouble), -Ortho * ((orthoWidth + screen.AspectRatio * negZoom) / orthoWidthDiff), Ortho * -1.0100503f + orthoInv * ((depthOffset * -800f) / (400f - depthOffset)), Ortho);
            }
        }

        WorldViewpoint = mat_world_v * proj;
        bool inverted = Matrix4x4.Invert(WorldViewpoint, out var worldViewpointInverted);
        Debug.Assert(inverted);
        WorldViewpointInverted = worldViewpointInverted;
    }

    public (Vector3 Near, Vector3 Far) ScreenToWorld(Vector2 screenPos, ScreenInfo screen)
    {
        float ndcX = (2.0f * screenPos.X) / screen.Width - 1.0f;
        float ndcY = 1.0f - (2.0f * screenPos.Y) / screen.Height;

        Vector4 clipNear = new Vector4(ndcX, ndcY, 0f, 1.0f);
        Vector4 clipFar = new Vector4(ndcX, ndcY, Perspective ? 1f : 2f, 1.0f);

        Vector4 worldNearH = Vector4.Transform(clipNear, WorldViewpointInverted);
        Vector4 worldFarH = Vector4.Transform(clipFar, WorldViewpointInverted);

        Vector3 worldNear = new Vector3(
            worldNearH.X / worldNearH.W,
            worldNearH.Y / worldNearH.W,
            worldNearH.Z / worldNearH.W
        );

        Vector3 worldFar = new Vector3(
            worldFarH.X / worldFarH.W,
            worldFarH.Y / worldFarH.W,
            worldFarH.Z / worldFarH.W
        );

        return (worldNear, worldFar);
    }

    public Vector2 WorldToScreen(Vector3 worldPos, ScreenInfo screen)
    {
        Vector4 worldPos4 = new Vector4(worldPos, 1f);

        Vector4 transformed = Vector4.Transform(worldPos4, WorldViewpoint);

        float screenX = (screen.Width * 0.5f) + ((transformed.X / transformed.W) * 0.5f * screen.Width);
        float screenY = (screen.Height * 0.5f) - ((transformed.Y / transformed.W) * 0.5f * screen.Height);

        return new Vector2(screenX, screenY);
    }
}
