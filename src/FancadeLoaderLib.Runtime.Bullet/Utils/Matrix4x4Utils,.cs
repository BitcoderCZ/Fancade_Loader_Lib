using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;
internal static class Matrix4x4Utils
{
    public static Matrix4x4 CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
#if NET8_0_OR_GREATER
        => Matrix4x4.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane);
#else
        => Impl.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, zNearPlane, zFarPlane).AsM4x4();
#endif

#if !NET8_0_OR_GREATER
    // https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Numerics/Matrix4x4.Impl.cs,12c3a6764eb5201f
    private struct Impl : IEquatable<Impl>
    {
        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Matrix4x4 AsM4x4() 
            => ref Unsafe.As<Impl, Matrix4x4>(ref this);

        private const float BillboardEpsilon = 1e-4f;
        private const float BillboardMinAngle = 1.0f - (0.1f * (MathF.PI / 180.0f)); // 0.1 degrees
        private const float DecomposeEpsilon = 0.0001f;

        public Vector4 X;
        public Vector4 Y;
        public Vector4 Z;
        public Vector4 W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Impl CreateOrthographicOffCenterLeftHanded(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
        {
            // This implementation is based on the DirectX Math Library XMMatrixOrthographicOffCenterLH method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathMatrix.inl

            float reciprocalWidth = 1.0f / (right - left);
            float reciprocalHeight = 1.0f / (top - bottom);
            float range = 1.0f / (zFarPlane - zNearPlane);

            Impl result;

            result.X = new Vector4(reciprocalWidth + reciprocalWidth, 0, 0, 0);
            result.Y = new Vector4(0, reciprocalHeight + reciprocalHeight, 0, 0);
            result.Z = new Vector4(0, 0, range, 0);
            result.W = new Vector4(
                -(left + right) * reciprocalWidth,
                -(top + bottom) * reciprocalHeight,
                -range * zNearPlane,
                1
            );

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
                => (obj is Matrix4x4 other) && Equals(in Unsafe.As<Matrix4x4, Impl>(ref other));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(in Impl other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

#pragma warning disable IDE0022 // Use expression body for method
            return X.Equals(other.X)
                && Y.Equals(other.Y)
                && Z.Equals(other.Z)
                && W.Equals(other.W);
#pragma warning restore IDE0022 // Use expression body for method
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        readonly bool IEquatable<Impl>.Equals(Impl other) => Equals(in other);
    }
#endif
}
