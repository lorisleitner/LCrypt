﻿using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LCrypt.Utility.Extensions
{
    public static class SecureStringExtensions
    {
        public static async Task<byte[]> DeriveKeyAsync(this SecureString secureString, byte[] salt, int iterations,
            int keyLengthInBytes, CancellationToken cancellationToken)
        {
            if (secureString == null)
                throw new ArgumentNullException(nameof(secureString));
            if (salt == null)
                throw new ArgumentNullException(nameof(salt));
            if (salt.Length < 8)
                throw new ArgumentException("The specified salt size is smaller than 8 bytes.", nameof(salt));
            if (iterations < 1)
                throw new ArgumentException("Iteration count cannot be smaller than 1.", nameof(iterations));
            if (keyLengthInBytes < 1)
                throw new ArgumentException("The specified key size is smaller than 1 byte.", nameof(keyLengthInBytes));

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ptr = Marshal.SecureStringToBSTR(secureString);
                var length = Marshal.ReadInt32(ptr, -4);
                var passwordByteArray = new byte[length];
                var gcHandle = GCHandle.Alloc(passwordByteArray, GCHandleType.Pinned);

                try
                {
                    for (var index = 0; index < length; ++index)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        passwordByteArray[index] = Marshal.ReadByte(ptr, index);
                    }

                    using (var rfc = new Rfc2898DeriveBytes(passwordByteArray, salt, iterations))
                        return rfc.GetBytes(keyLengthInBytes);
                }
                finally
                {
                    Array.Clear(passwordByteArray, 0, length);
                    gcHandle.Free();
                    Marshal.ZeroFreeBSTR(ptr);
                }
            }, cancellationToken);
        }
    }
}