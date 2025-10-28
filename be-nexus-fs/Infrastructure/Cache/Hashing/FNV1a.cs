using System;
using System.Text;

namespace Infrastructure.Cache.Hashing
{
    public static class FNV1a
    {
        private const uint FNV_OFFSET_BASIS_32 = 2166136261;
        private const uint FNV_PRIME_32 = 16777619;

        private const ulong FNV_OFFSET_BASIS_64 = 14695981039346656037;
        private const ulong FNV_PRIME_64 = 1099511628211;

        public static uint Hash32(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            uint hash = FNV_OFFSET_BASIS_32;

            foreach (byte b in data)
            {
                hash ^= b;
                hash *= FNV_PRIME_32;
            }

            return hash;
        }

        public static ulong Hash64(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ulong hash = FNV_OFFSET_BASIS_64;

            foreach (byte b in data)
            {
                hash ^= b;
                hash *= FNV_PRIME_64;
            }

            return hash;
        }

        public static uint Hash32(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Hash32(Encoding.UTF8.GetBytes(input));
        }

        public static ulong Hash64(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Hash64(Encoding.UTF8.GetBytes(input));
        }
    }

    /// <summary>
    /// Instance-based FNV-1a 32-bit hash implementation that allows incremental hashing
    /// </summary>
    public class FNV1a32
    {
        private const uint FNV_OFFSET_BASIS_32 = 2166136261;
        private const uint FNV_PRIME_32 = 16777619;
        
        private uint _hash;

        public FNV1a32()
        {
            Reset();
        }

        public void Reset()
        {
            _hash = FNV_OFFSET_BASIS_32;
        }

        public void Update(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            foreach (byte b in data)
            {
                _hash ^= b;
                _hash *= FNV_PRIME_32;
            }
        }

        public void Update(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Update(Encoding.UTF8.GetBytes(input));
        }

        public void Update(byte value)
        {
            _hash ^= value;
            _hash *= FNV_PRIME_32;
        }

        public uint Digest()
        {
            return _hash;
        }
    }

    /// <summary>
    /// Instance-based FNV-1a 64-bit hash implementation that allows incremental hashing
    /// </summary>
    public class FNV1a64
    {
        private const ulong FNV_OFFSET_BASIS_64 = 14695981039346656037;
        private const ulong FNV_PRIME_64 = 1099511628211;
        
        private ulong _hash;

        public FNV1a64()
        {
            Reset();
        }

        public void Reset()
        {
            _hash = FNV_OFFSET_BASIS_64;
        }

        public void Update(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            foreach (byte b in data)
            {
                _hash ^= b;
                _hash *= FNV_PRIME_64;
            }
        }

        public void Update(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Update(Encoding.UTF8.GetBytes(input));
        }

        public void Update(byte value)
        {
            _hash ^= value;
            _hash *= FNV_PRIME_64;
        }

        public ulong Digest()
        {
            return _hash;
        }
    }
}