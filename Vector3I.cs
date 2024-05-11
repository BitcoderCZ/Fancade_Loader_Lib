using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FancadeLoaderLib
{
    public struct Vector3I
    {
        //
        // Summary:
        //     The X component of the vector.
        public int X;

        //
        // Summary:
        //     The Y component of the vector.
        public int Y;

        //
        // Summary:
        //     The Z component of the vector.
        public int Z;

        //
        // Summary:
        //     Gets a vector whose 3 elements are equal to zero.
        //
        // Returns:
        //     A vector whose three elements are equal to zero (that is, it returns the vector
        //     (0,0,0).
        public static Vector3I Zero
        {
            get
            {
                return default(Vector3I);
            }
        }

        //
        // Summary:
        //     Gets a vector whose 3 elements are equal to one.
        //
        // Returns:
        //     A vector whose three elements are equal to one (that is, it returns the vector
        //     (1,1,1).
        public static Vector3I One
        {
            get
            {
                return new Vector3I(1, 1, 1);
            }
        }

        //
        // Summary:
        //     Gets the vector (1,0,0).
        //
        // Returns:
        //     The vector (1,0,0).
        public static Vector3I UnitX
        {
            get
            {
                return new Vector3I(1, 0, 0);
            }
        }

        //
        // Summary:
        //     Gets the vector (0,1,0).
        //
        // Returns:
        //     The vector (0,1,0).
        public static Vector3I UnitY
        {
            get
            {
                return new Vector3I(0, 1, 0);
            }
        }

        //
        // Summary:
        //     Gets the vector (0,0,1).
        //
        // Returns:
        //     The vector (0,0,1).
        public static Vector3I UnitZ
        {
            get
            {
                return new Vector3I(0, 0, 1);
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
            if (!(obj is Vector3I))
            {
                return false;
            }

            return Equals((Vector3I)obj);
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
        public int Length()
        {
            if (Vector.IsHardwareAccelerated)
            {
                float num = Dot(this, this);
                return (int)Math.Sqrt(num);
            }

            float num2 = X * X + Y * Y + Z * Z;
            return (int)Math.Sqrt(num2);
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
            if (Vector.IsHardwareAccelerated)
            {
                return Dot(this, this);
            }

            return X * X + Y * Y + Z * Z;
        }

        //
        // Summary:
        //     Computes the Euclidean distance between the two given points.
        //
        // Parameters:
        //   value1:
        //     The first point.
        //
        //   value2:
        //     The second point.
        //
        // Returns:
        //     The distance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector3I value1, Vector3I value2)
        {
            if (Vector.IsHardwareAccelerated)
            {
                Vector3I vector = value1 - value2;
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
        //     Returns the Euclidean distance squared between two specified points.
        //
        // Parameters:
        //   value1:
        //     The first point.
        //
        //   value2:
        //     The second point.
        //
        // Returns:
        //     The distance squared.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vector3I value1, Vector3I value2)
        {
            if (Vector.IsHardwareAccelerated)
            {
                Vector3I vector = value1 - value2;
                return Dot(vector, vector);
            }

            float num = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        //
        // Summary:
        //     Returns a vector with the same direction as the specified vector, but with a
        //     length of one.
        //
        // Parameters:
        //   value:
        //     The vector to normalize.
        //
        // Returns:
        //     The normalized vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I Normalize(Vector3I value)
        {
            if (Vector.IsHardwareAccelerated)
            {
                int num = value.Length();
                return value / num;
            }

            int num2 = value.X * value.X + value.Y * value.Y + value.Z * value.Z;
            int num3 = (int)Math.Sqrt(num2);
            return new Vector3I(value.X / num3, value.Y / num3, value.Z / num3);
        }

        //
        // Summary:
        //     Computes the cross product of two vectors.
        //
        // Parameters:
        //   vector1:
        //     The first vector.
        //
        //   vector2:
        //     The second vector.
        //
        // Returns:
        //     The cross product.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I Cross(Vector3I vector1, Vector3I vector2)
        {
            return new Vector3I(vector1.Y * vector2.Z - vector1.Z * vector2.Y, vector1.Z * vector2.X - vector1.X * vector2.Z, vector1.X * vector2.Y - vector1.Y * vector2.X);
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
        public static Vector3I Clamp(Vector3I value1, Vector3I min, Vector3I max)
        {
            int x = value1.X;
            x = ((x > max.X) ? max.X : x);
            x = ((x < min.X) ? min.X : x);
            int y = value1.Y;
            y = ((y > max.Y) ? max.Y : y);
            y = ((y < min.Y) ? min.Y : y);
            int z = value1.Z;
            z = ((z > max.Z) ? max.Z : z);
            z = ((z < min.Z) ? min.Z : z);
            return new Vector3I(x, y, z);
        }

        //
        // Summary:
        //     Transforms a vector by a specified 4x4 matrix.
        //
        // Parameters:
        //   position:
        //     The vector to transform.
        //
        //   matrix:
        //     The transformation matrix.
        //
        // Returns:
        //     The transformed vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I Transform(Vector3I position, Matrix4x4 matrix)
        {
            throw new NotImplementedException();
            //return new Vector3I(position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42, position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43);
        }

        //
        // Summary:
        //     Transforms a vector normal by the given 4x4 matrix.
        //
        // Parameters:
        //   normal:
        //     The source vector.
        //
        //   matrix:
        //     The matrix.
        //
        // Returns:
        //     The transformed vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I TransformNormal(Vector3I normal, Matrix4x4 matrix)
        {
            throw new NotImplementedException();
            //return new Vector3I(normal.X * matrix.M11 + normal.Y * matrix.M21 + normal.Z * matrix.M31, normal.X * matrix.M12 + normal.Y * matrix.M22 + normal.Z * matrix.M32, normal.X * matrix.M13 + normal.Y * matrix.M23 + normal.Z * matrix.M33);
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
        public static Vector3I Transform(Vector3I value, Quaternion rotation)
        {
            throw new NotImplementedException();
            /*int num = rotation.X + rotation.X;
            int num2 = rotation.Y + rotation.Y;
            int num3 = rotation.Z + rotation.Z;
            int num4 = rotation.W * num;
            int num5 = rotation.W * num2;
            int num6 = rotation.W * num3;
            int num7 = rotation.X * num;
            int num8 = rotation.X * num2;
            int num9 = rotation.X * num3;
            int num10 = rotation.Y * num2;
            int num11 = rotation.Y * num3;
            int num12 = rotation.Z * num3;
            return new Vector3I(value.X * (1f - num10 - num12) + value.Y * (num8 - num6) + value.Z * (num9 + num5), value.X * (num8 + num6) + value.Y * (1f - num7 - num12) + value.Z * (num11 - num4), value.X * (num9 - num5) + value.Y * (num11 + num4) + value.Z * (1f - num7 - num10));*/
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
        public static Vector3I Add(Vector3I left, Vector3I right)
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
        public static Vector3I Subtract(Vector3I left, Vector3I right)
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
        public static Vector3I Multiply(Vector3I left, Vector3I right)
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
        public static Vector3I Multiply(Vector3I left, int right)
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
        public static Vector3I Multiply(int left, Vector3I right)
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
        public static Vector3I Divide(Vector3I left, Vector3I right)
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
        public static Vector3I Divide(Vector3I left, int divisor)
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
        public static Vector3I Negate(Vector3I value)
        {
            return -value;
        }

        //
        // Summary:
        //     Creates a new System.Numerics.Vector3I object whose three elements have the same
        //     value.
        //
        // Parameters:
        //   value:
        //     The value to assign to all three elements.
        public Vector3I(int value)
            : this(value, value, value)
        {
        }

        //
        // Summary:
        //     Creates a new System.Numerics.Vector3I object from the specified System.Numerics.Vector2
        //     object and the specified value.
        //
        // Parameters:
        //   value:
        //     The vector with two elements.
        //
        //   z:
        //     The additional value to assign to the System.Numerics.Vector3I.Z field.

        public Vector3I(Vector2 value, int z)
            : this((int)value.X, (int)value.Y, z)
        {
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Creates a vector whose elements have the specified values.
        //
        // Parameters:
        //   x:
        //     The value to assign to the System.Numerics.Vector3I.X field.
        //
        //   y:
        //     The value to assign to the System.Numerics.Vector3I.Y field.
        //
        //   z:
        //     The value to assign to the System.Numerics.Vector3I.Z field.
        public Vector3I(int x, int y, int z)
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
        public void CopyTo(float[] array, int index)
        {
            if (array == null)
            {
                throw new NullReferenceException();
            }

            if (index < 0 || index >= array.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (array.Length - index < 3)
            {
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
        public bool Equals(Vector3I other)
        {
            if (X == other.X && Y == other.Y)
            {
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
        public static float Dot(Vector3I vector1, Vector3I vector2)
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
        public static Vector3I Min(Vector3I value1, Vector3I value2)
        {
            return new Vector3I((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y, (value1.Z < value2.Z) ? value1.Z : value2.Z);
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
        public static Vector3I Max(Vector3I value1, Vector3I value2)
        {
            return new Vector3I((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y, (value1.Z > value2.Z) ? value1.Z : value2.Z);
        }

        //
        // Summary:
        //     Returns a vector whose elements are the absolute values of each of the specified
        //     vector's elements.
        //
        // Parameters:
        //   value:
        //     A vector.
        //
        // Returns:
        //     The absolute value vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I Abs(Vector3I value)
        {
            return new Vector3I(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
        }

        //
        // Summary:
        //     Returns a vector whose elements are the square root of each of a specified vector's
        //     elements.
        //
        // Parameters:
        //   value:
        //     A vector.
        //
        // Returns:
        //     The square root vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3I SquareRoot(Vector3I value)
        {
            return new Vector3I((int)Math.Sqrt(value.X), (int)Math.Sqrt(value.Y), (int)Math.Sqrt(value.Z));
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
        public static Vector3I operator +(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
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
        public static Vector3I operator -(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
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
        public static Vector3I operator *(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
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
        public static Vector3I operator *(Vector3I left, int right)
        {
            return left * new Vector3I(right);
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
        public static Vector3I operator *(int left, Vector3I right)
        {
            return new Vector3I(left) * right;
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
        public static Vector3I operator /(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
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
        public static Vector3I operator /(Vector3I value1, int value2)
        {
            int num = 1 / value2;
            return new Vector3I(value1.X * num, value1.Y * num, value1.Z * num);
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
        public static Vector3I operator -(Vector3I value)
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
        public static bool operator ==(Vector3I left, Vector3I right)
        {
            if (left.X == right.X && left.Y == right.Y)
            {
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
        public static bool operator !=(Vector3I left, Vector3I right)
        {
            if (left.X == right.X && left.Y == right.Y)
            {
                return left.Z != right.Z;
            }

            return true;
        }
    }
}
