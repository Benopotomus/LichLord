using UnityEngine;
using Fusion;

namespace LichLord
{
    public static class Vector3CompressedExtensions
    {
        /// <summary>
        /// Subtracts one Vector3Compressed from another, returning a new Vector3Compressed with the difference.
        /// </summary>
        /// <param name="a">The first Vector3Compressed instance (minuend).</param>
        /// <param name="b">The second Vector3Compressed instance (subtrahend).</param>
        /// <returns>A new Vector3Compressed representing the difference (a - b).</returns>
        public static Vector3Compressed Subtract(this Vector3Compressed a, Vector3Compressed b)
        {
            Vector3Compressed result = default;
            result.X = a.X - b.X; // Decompresses, subtracts, and compresses back
            result.Y = a.Y - b.Y;
            result.Z = a.Z - b.Z;
            return result;
        }

        /// <summary>
        /// Subtracts one Vector3Compressed from another, normalizes the result, and returns it as a Vector3.
        /// </summary>
        /// <param name="a">The first Vector3Compressed instance (minuend).</param>
        /// <param name="b">The second Vector3Compressed instance (subtrahend).</param>
        /// <returns>A normalized Vector3 representing the direction of (a - b), or Vector3.zero if the result is zero.</returns>
        public static Vector3 SubtractAndNormalize(this Vector3Compressed a, Vector3Compressed b)
        {
            Vector3 difference = Subtract(a, b); // Implicit conversion to Vector3 and subtraction
            if (difference.sqrMagnitude > 0f)
            {
                return difference.normalized;
            }
            return Vector3.zero; // Return zero if the difference is zero to avoid normalizing a zero vector
        }
    }
}