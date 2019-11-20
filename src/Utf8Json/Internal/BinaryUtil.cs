using System;
using System.Diagnostics;

#if NETSTANDARD

using System.Runtime.CompilerServices;

#endif

namespace Spreads.Serialization.Utf8Json.Internal
{
    public static class BinaryUtil
    {
        private const int ArrayMaxSize = 0x7FFFFFC7; // https://msdn.microsoft.com/en-us/library/system.array

#if NETSTANDARD

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void EnsureCapacity(ref byte[] bytes, int offset, int appendLength)
        {
            var newLength = offset + appendLength;

            Debug.Assert(bytes != null);

            // like MemoryStream.EnsureCapacity
            var current = bytes.Length;
            if (newLength > current)
            {
                EnsureCapacitySlow(ref bytes, offset, appendLength);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureCapacitySlow(ref byte[] bytes, int offset, int appendLength)
        {
            var newLength = offset + appendLength;
            var current = bytes.Length;
            int num = newLength;
            if (num < 256)
            {
                num = 256;
                FastResize(ref bytes, num);
                return;
            }

            if (current == ArrayMaxSize)
            {
                ThrowMaxCapacity();
            }

            var newSize = unchecked((current * 2));
            if (newSize < 0) // overflow
            {
                num = ArrayMaxSize;
            }
            else
            {
                if (num < newSize)
                {
                    num = newSize;
                }
            }

            FastResize(ref bytes, num);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowMaxCapacity()
        {
            throw new InvalidOperationException("byte[] size reached maximum size of array(0x7FFFFFC7), can not write to single byte[]. Details: https://msdn.microsoft.com/en-us/library/system.array");
        }

#if NETSTANDARD

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void FastResize(ref byte[] array, int newSize)
        {
            if (newSize < 0)
            {
#if SPREADS
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(newSize));
#else
                throw new ArgumentOutOfRangeException("newSize");
#endif
            }

            byte[] array2 = array;
            if (array2 == null || array2.Length == 0)
            {
#if SPREADS
                array = Buffers.BufferPool<byte>.Rent(newSize);
#else
                array = new byte[newSize];
#endif
                return;
            }

            if (array2.Length != newSize)
            {
#if SPREADS
                byte[] array3 = Buffers.BufferPool<byte>.Rent(newSize);
#else
                byte[] array3 = new byte[newSize];
#endif
                Unsafe.CopyBlockUnaligned(ref array3[0], ref array2[0], (uint)((array2.Length > newSize) ? newSize : array2.Length));
#if SPREADS
                if (array2 != JsonSerializer.MemoryPool.buffer)
                {
                    Buffers.BufferPool<byte>.Return(array2, true);
                }
#endif
                array = array3;
            }
        }

#if NETSTANDARD

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static
#if NETSTANDARD
            unsafe
#endif
            byte[] FastCloneWithResize(byte[] src, int newSize)
        {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");

            if (src == null) return new byte[newSize];

            if (src.Length < newSize) throw new ArgumentException("length < newSize");

            if (newSize == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] dst = new byte[newSize];

            Unsafe.CopyBlockUnaligned(ref dst[0], ref src[0], (uint)newSize);

            return dst;
        }
    }
}
