using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public struct Vector3S
    {
        //
        // Summary:
        //     The X component of the vector.
        public ushort X;

        //
        // Summary:
        //     The Y component of the vector.
        public ushort Y;

        //
        // Summary:
        //     The Z component of the vector.
        public ushort Z;

        //
        // Summary:
        //     Gets a vector whose 3 elements are equal to zero.
        //
        // Returns:
        //     A vector whose three elements are equal to zero (that is, it returns the vector
        //     (0,0,0).
        public static Vector3S Zero
        {
            get {
                return default(Vector3S);
            }
        }

        //
        // Summary:
        //     Gets a vector whose 3 elements are equal to one.
        //
        // Returns:
        //     A vector whose three elements are equal to one (that is, it returns the vector
        //     (1,1,1).
        public static Vector3S One
        {
            get {
                return new Vector3S(1, 1, 1);
            }
        }

        //
        // Summary:
        //     Gets the vector (1,0,0).
        //
        // Returns:
        //     The vector (1,0,0).
        public static Vector3S UnitX
        {
            get {
                return new Vector3S(1, 0, 0);
            }
        }

        //
        // Summary:
        //     Gets the vector (0,1,0).
        //
        // Returns:
        //     The vector (0,1,0).
        public static Vector3S UnitY
        {
            get {
                return new Vector3S(0, 1, 0);
            }
        }

        //
        // Summary:
        //     Gets the vector (0,0,1).
        //
        // Returns:
        //     The vector (0,0,1).
        public static Vector3S UnitZ
        {
            get {
                return new Vector3S(0, 0, 1);
            }
        }

        //
        // Summary:
        //     Returns the hash code for this instance.
        //
        // Returns:
        //     The hash code.
        public override int GetHashCode()
        {
            return X ^ Y ^ Z;
        }

        //
        // Summary:
        //     Returns a value that indicates whether this instance and a specified object are
        //     equal.
        //
        // Parameters:
        //   obj:
        //     The object to compare with the current instance.
        //
        // Returns:
        //     true if the current instance and obj are equal; otherwise, false. If obj is null,
        //     the method returns false.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Vector3S)) {
                return false;
            }

            return Equals((Vector3S)obj);
        }

        //
        // Summary:
        //     Returns the string representation of the current instance using default formatting.
        //
        // Returns:
        //     The string representation of the current instance.
        public override string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        //
        // Summary:
        //     Returns the string representation of the current instance using the specified
        //     format string to format individual elements.
        //
        // Parameters:
        //   format:
        //     A standard or custom numeric format string that defines the format of individual
        //     elements.
        //
        // Returns:
        //     The string representation of the current instance.
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        //
        // Summary:
        //     Returns the string representation of the current instance using the specified
        //     format string to format individual elements and the specified format provider
        //     to define culture-specific formatting.
        //
        // Parameters:
        //   format:
        //     A standard or custom numeric format string that defines the format of individual
        //     elements.
        //
        //   formatProvider:
        //     A format provider that supplies culture-specific formatting information.
        //
        // Returns:
        //     The string representation of the current instance.
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            stringBuilder.Append('<');
            stringBuilder.Append(((IFormattable)X).ToString(format, formatProvider));
            stringBuilder.Append(numberGroupSeparator);
            stringBuilder.Append(' ');
            stringBuilder.Append(((IFormattable)Y).ToString(format, formatProvider));
            stringBuilder.Append(numberGroupSeparator);
            stringBuilder.Append(' ');
            stringBuilder.Append(((IFormattable)Z).ToString(format, formatProvider));
            stringBuilder.Append('>');
            return stringBuilder.ToString();
        }

        //
        // Summary:
        //     Returns the length of this vector object.
        //
        // Returns:
        //     The vector's length.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Length()
        {
            if (Vector.IsHardwareAccelerated) {
                float num = Dot(this, this);
                return (ushort)Math.Sqrt(num);
            }

            float num2 = X * X + Y * Y + Z * Z;
            return (ushort)Math.Sqrt(num2);
        }

        //
        // Summary:
        //     Returns the length of the vector squared.
        //
        // Returns:
        //     The vector's length squared.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float LengthSquared()
        {
            if (Vector.IsHardwareAccelerated) {
                return Dot(this, this);
            }

            return X * X + Y * Y + Z * Z;
        }

        //
        // Summary:
        //     Computes the Euclidean distance between the two given poushorts.
        //
        // Parameters:
        //   value1:
        //     The first poushort.
        //
        //   value2:
        //     The second poushort.
        //
        // Returns:
        //     The distance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector3S value1, Vector3S value2)
        {
            if (Vector.IsHardwareAccelerated) {
                Vector3S vector = value1 - value2;
                float num = Dot(vector, vector);
                return (float)Math.Sqrt(num);
            }

            float num2 = value1.X - value2.X;
            float num3 = value1.Y - value2.Y;
            float num4 = value1.Z - value2.Z;
            float num5 = num2 * num2 + num3 * num3 + num4 * num4;
            return (float)Math.Sqrt(num5);
        }

        //
        // Summary:
        //     Returns the Euclidean distance squared between two specified poushorts.
        //
        // Parameters:
        //   value1:
        //     The first poushort.
        //
        //   value2:
        //     The second poushort.
        //
        // Returns:
        //     The distance squared.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vector3S value1, Vector3S value2)
        {
            if (Vector.IsHardwareAccelerated) {
                Vector3S vector = value1 - value2;
                return Dot(vector, vector);
            }

            float num = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        //
        // Summary:
        //     Restricts a vector between a minimum and a maximum value.
        //
        // Parameters:
        //   value1:
        //     The vector to restrict.
        //
        //   min:
        //     The minimum value.
        //
        //   max:
        //     The maximum value.
        //
        // Returns:
        //     The restricted vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Clamp(Vector3S value1, Vector3S min, Vector3S max)
        {
            ushort x = value1.X;
            x = ((x > max.X) ? max.X : x);
            x = ((x < min.X) ? min.X : x);
            ushort y = value1.Y;
            y = ((y > max.Y) ? max.Y : y);
            y = ((y < min.Y) ? min.Y : y);
            ushort z = value1.Z;
            z = ((z > max.Z) ? max.Z : z);
            z = ((z < min.Z) ? min.Z : z);
            return new Vector3S(x, y, z);
        }

        //
        // Summary:
        //     Transforms a vector by the specified Quaternion rotation value.
        //
        // Parameters:
        //   value:
        //     The vector to rotate.
        //
        //   rotation:
        //     The rotation to apply.
        //
        // Returns:
        //     The transformed vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Transform(Vector3S value, Quaternion rotation)
        {
            throw new NotImplementedException();
            /*ushort num = rotation.X + rotation.X;
            ushort num2 = rotation.Y + rotation.Y;
            ushort num3 = rotation.Z + rotation.Z;
            ushort num4 = rotation.W * num;
            ushort num5 = rotation.W * num2;
            ushort num6 = rotation.W * num3;
            ushort num7 = rotation.X * num;
            ushort num8 = rotation.X * num2;
            ushort num9 = rotation.X * num3;
            ushort num10 = rotation.Y * num2;
            ushort num11 = rotation.Y * num3;
            ushort num12 = rotation.Z * num3;
            return new Vector3S(value.X * (1f - num10 - num12) + value.Y * (num8 - num6) + value.Z * (num9 + num5), value.X * (num8 + num6) + value.Y * (1f - num7 - num12) + value.Z * (num11 - num4), value.X * (num9 - num5) + value.Y * (num11 + num4) + value.Z * (1f - num7 - num10));*/
        }

        //
        // Summary:
        //     Adds two vectors together.
        //
        // Parameters:
        //   left:
        //     The first vector to add.
        //
        //   right:
        //     The second vector to add.
        //
        // Returns:
        //     The summed vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Add(Vector3S left, Vector3S right)
        {
            return left + right;
        }

        //
        // Summary:
        //     Subtracts the second vector from the first.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The difference vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Subtract(Vector3S left, Vector3S right)
        {
            return left - right;
        }

        //
        // Summary:
        //     Returns a new vector whose values are the product of each pair of elements in
        //     two specified vectors.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The element-wise product vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Multiply(Vector3S left, Vector3S right)
        {
            return left * right;
        }

        //
        // Summary:
        //     Multiplies a vector by a specified scalar.
        //
        // Parameters:
        //   left:
        //     The vector to multiply.
        //
        //   right:
        //     The scalar value.
        //
        // Returns:
        //     The scaled vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Multiply(Vector3S left, ushort right)
        {
            return left * right;
        }

        //
        // Summary:
        //     Multiplies a scalar value by a specified vector.
        //
        // Parameters:
        //   left:
        //     The scaled value.
        //
        //   right:
        //     The vector.
        //
        // Returns:
        //     The scaled vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Multiply(ushort left, Vector3S right)
        {
            return left * right;
        }

        //
        // Summary:
        //     Divides the first vector by the second.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The vector resulting from the division.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Divide(Vector3S left, Vector3S right)
        {
            return left / right;
        }

        //
        // Summary:
        //     Divides the specified vector by a specified scalar value.
        //
        // Parameters:
        //   left:
        //     The vector.
        //
        //   divisor:
        //     The scalar value.
        //
        // Returns:
        //     The vector that results from the division.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Divide(Vector3S left, ushort divisor)
        {
            return left / divisor;
        }

        //
        // Summary:
        //     Negates a specified vector.
        //
        // Parameters:
        //   value:
        //     The vector to negate.
        //
        // Returns:
        //     The negated vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Negate(Vector3S value)
        {
            return -value;
        }

        //
        // Summary:
        //     Creates a new System.Numerics.Vector3S object whose three elements have the same
        //     value.
        //
        // Parameters:
        //   value:
        //     The value to assign to all three elements.
        public Vector3S(ushort value)
            : this(value, value, value)
        {
        }

        //
        // Summary:
        //     Creates a vector whose elements have the specified values.
        //
        // Parameters:
        //   x:
        //     The value to assign to the System.Numerics.Vector3S.X field.
        //
        //   y:
        //     The value to assign to the System.Numerics.Vector3S.Y field.
        //
        //   z:
        //     The value to assign to the System.Numerics.Vector3S.Z field.
        public Vector3S(ushort x, ushort y, ushort z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //
        // Summary:
        //     Copies the elements of the vector to a specified array.
        //
        // Parameters:
        //   array:
        //     The destination array.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     array is null.
        //
        //   T:System.ArgumentException:
        //     The number of elements in the current instance is greater than in the array.
        //
        //   T:System.RankException:
        //     array is multidimensional.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public void CopyTo(float[] array)
        {
            CopyTo(array, 0);
        }

        //
        // Summary:
        //     Copies the elements of the vector to a specified array starting at a specified
        //     index position.
        //
        // Parameters:
        //   array:
        //     The destination array.
        //
        //   index:
        //     The index at which to copy the first element of the vector.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     array is null.
        //
        //   T:System.ArgumentException:
        //     The number of elements in the current instance is greater than in the array.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     index is less than zero. -or- index is greater than or equal to the array length.
        //
        //   T:System.RankException:
        //     array is multidimensional.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(float[] array, ushort index)
        {
            if (array == null) {
                throw new NullReferenceException();
            }

            if (index < 0 || index >= array.Length) {
                throw new ArgumentOutOfRangeException();
            }

            if (array.Length - index < 3) {
                throw new ArgumentException();
            }

            array[index] = X;
            array[index + 1] = Y;
            array[index + 2] = Z;
        }

        //
        // Summary:
        //     Returns a value that indicates whether this instance and another vector are equal.
        //
        // Parameters:
        //   other:
        //     The other vector.
        //
        // Returns:
        //     true if the two vectors are equal; otherwise, false.
        public bool Equals(Vector3S other)
        {
            if (X == other.X && Y == other.Y) {
                return Z == other.Z;
            }

            return false;
        }

        //
        // Summary:
        //     Returns the dot product of two vectors.
        //
        // Parameters:
        //   vector1:
        //     The first vector.
        //
        //   vector2:
        //     The second vector.
        //
        // Returns:
        //     The dot product.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector3S vector1, Vector3S vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
        }

        //
        // Summary:
        //     Returns a vector whose elements are the minimum of each of the pairs of elements
        //     in two specified vectors.
        //
        // Parameters:
        //   value1:
        //     The first vector.
        //
        //   value2:
        //     The second vector.
        //
        // Returns:
        //     The minimized vector.
        public static Vector3S Min(Vector3S value1, Vector3S value2)
        {
            return new Vector3S((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y, (value1.Z < value2.Z) ? value1.Z : value2.Z);
        }

        //
        // Summary:
        //     Returns a vector whose elements are the maximum of each of the pairs of elements
        //     in two specified vectors.
        //
        // Parameters:
        //   value1:
        //     The first vector.
        //
        //   value2:
        //     The second vector.
        //
        // Returns:
        //     The maximized vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S Max(Vector3S value1, Vector3S value2)
        {
            return new Vector3S((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y, (value1.Z > value2.Z) ? value1.Z : value2.Z);
        }

        //
        // Summary:
        //     Adds two vectors together.
        //
        // Parameters:
        //   left:
        //     The first vector to add.
        //
        //   right:
        //     The second vector to add.
        //
        // Returns:
        //     The summed vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator +(Vector3S left, Vector3S right)
        {
            return new Vector3S((ushort)(left.X + right.X), (ushort)(left.Y + right.Y), (ushort)(left.Z + right.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator +(Vector3S left, Vector3I right)
        {
            return new Vector3S((ushort)(left.X + right.X), (ushort)(left.Y + right.Y), (ushort)(left.Z + right.Z));
        }

        //
        // Summary:
        //     Subtracts the second vector from the first.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The vector that results from subtracting right from left.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator -(Vector3S left, Vector3S right)
        {
            return new Vector3S((ushort)(left.X - right.X), (ushort)(left.Y - right.Y), (ushort)(left.Z - right.Z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator -(Vector3S left, Vector3I right)
        {
            return new Vector3S((ushort)(left.X - right.X), (ushort)(left.Y - right.Y), (ushort)(left.Z - right.Z));
        }

        //
        // Summary:
        //     Returns a new vector whose values are the product of each pair of elements in
        //     two specified vectors.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The element-wise product vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator *(Vector3S left, Vector3S right)
        {
            return new Vector3S((ushort)(left.X * right.X), (ushort)(left.Y * right.Y), (ushort)(left.Z * right.Z));
        }

        //
        // Summary:
        //     Multiples the specified vector by the specified scalar value.
        //
        // Parameters:
        //   left:
        //     The vector.
        //
        //   right:
        //     The scalar value.
        //
        // Returns:
        //     The scaled vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator *(Vector3S left, ushort right)
        {
            return left * new Vector3S(right);
        }

        //
        // Summary:
        //     Multiples the scalar value by the specified vector.
        //
        // Parameters:
        //   left:
        //     The vector.
        //
        //   right:
        //     The scalar value.
        //
        // Returns:
        //     The scaled vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator *(ushort left, Vector3S right)
        {
            return new Vector3S(left) * right;
        }

        //
        // Summary:
        //     Divides the first vector by the second.
        //
        // Parameters:
        //   left:
        //     The first vector.
        //
        //   right:
        //     The second vector.
        //
        // Returns:
        //     The vector that results from dividing left by right.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator /(Vector3S left, Vector3S right)
        {
            return new Vector3S((ushort)(left.X / right.X), (ushort)(left.Y / right.Y), (ushort)(left.Z / right.Z));
        }

        //
        // Summary:
        //     Divides the specified vector by a specified scalar value.
        //
        // Parameters:
        //   value1:
        //     The vector.
        //
        //   value2:
        //     The scalar value.
        //
        // Returns:
        //     The result of the division.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator /(Vector3S value1, ushort value2)
        {
            return new Vector3S((ushort)(value1.X / value2), (ushort)(value1.Y / value2), (ushort)(value1.Z / value2));
        }

        //
        // Summary:
        //     Negates the specified vector.
        //
        // Parameters:
        //   value:
        //     The vector to negate.
        //
        // Returns:
        //     The negated vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3S operator -(Vector3S value)
        {
            return Zero - value;
        }

        //
        // Summary:
        //     Returns a value that indicates whether each pair of elements in two specified
        //     vectors is equal.
        //
        // Parameters:
        //   left:
        //     The first vector to compare.
        //
        //   right:
        //     The second vector to compare.
        //
        // Returns:
        //     true if left and right are equal; otherwise, false.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3S left, Vector3S right)
        {
            if (left.X == right.X && left.Y == right.Y) {
                return left.Z == right.Z;
            }

            return false;
        }

        //
        // Summary:
        //     Returns a value that indicates whether two specified vectors are not equal.
        //
        // Parameters:
        //   left:
        //     The first vector to compare.
        //
        //   right:
        //     The second vector to compare.
        //
        // Returns:
        //     true if left and right are not equal; otherwise, false.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3S left, Vector3S right)
        {
            if (left.X == right.X && left.Y == right.Y) {
                return left.Z != right.Z;
            }

            return true;
        }

        public static implicit operator Vector3I(Vector3S value)
            => new Vector3I(value.X, value.Y, value.Z);
    }
}
