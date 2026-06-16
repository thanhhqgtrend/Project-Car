using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNet.Identity;

namespace LuxuryCar.Identity
{
    public class AspNetCorePasswordHasher : IPasswordHasher
    {
        private const int Pbkdf2IterCount = 10000;
        private const int Pbkdf2SubkeyLength = 256 / 8;
        private const int SaltSize = 128 / 8;

        public string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var subkey = KeyDerivation(password, salt, Pbkdf2IterCount, Pbkdf2SubkeyLength);
            var outputBytes = new byte[1 + SaltSize + Pbkdf2SubkeyLength];
            outputBytes[0] = 0x00;
            Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, Pbkdf2SubkeyLength);
            return Convert.ToBase64String(outputBytes);
        }

        public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            if (hashedPassword == null || providedPassword == null)
            {
                return PasswordVerificationResult.Failed;
            }

            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(hashedPassword);
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }

            if (decoded.Length == 1 + SaltSize + Pbkdf2SubkeyLength && decoded[0] == 0x00)
            {
                var salt = new byte[SaltSize];
                Buffer.BlockCopy(decoded, 1, salt, 0, SaltSize);
                var expected = new byte[Pbkdf2SubkeyLength];
                Buffer.BlockCopy(decoded, 1 + SaltSize, expected, 0, Pbkdf2SubkeyLength);
                var actual = KeyDerivation(providedPassword, salt, Pbkdf2IterCount, Pbkdf2SubkeyLength);
                return FixedTimeEquals(actual, expected) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }

            if (decoded.Length > 13 && decoded[0] == 0x01)
            {
                return VerifyAspNetCoreV3(decoded, providedPassword);
            }

            return PasswordVerificationResult.Failed;
        }

        private static PasswordVerificationResult VerifyAspNetCoreV3(byte[] decoded, string password)
        {
            try
            {
                var prf = ReadNetworkByteOrder(decoded, 1);
                var iterCount = (int)ReadNetworkByteOrder(decoded, 5);
                var saltLength = (int)ReadNetworkByteOrder(decoded, 9);
                if (saltLength < 128 / 8 || decoded.Length < 13 + saltLength)
                {
                    return PasswordVerificationResult.Failed;
                }

                var salt = new byte[saltLength];
                Buffer.BlockCopy(decoded, 13, salt, 0, salt.Length);
                var subkeyLength = decoded.Length - 13 - salt.Length;
                if (subkeyLength < 128 / 8)
                {
                    return PasswordVerificationResult.Failed;
                }

                var expectedSubkey = new byte[subkeyLength];
                Buffer.BlockCopy(decoded, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);
                var actualSubkey = KeyDerivation(password, salt, iterCount, subkeyLength, (int)prf);
                return FixedTimeEquals(actualSubkey, expectedSubkey) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24)
                | ((uint)buffer[offset + 1] << 16)
                | ((uint)buffer[offset + 2] << 8)
                | buffer[offset + 3];
        }

        private static byte[] KeyDerivation(string password, byte[] salt, int iterationCount, int numBytesRequested, int prf = 1)
        {
            var algorithm = prf == 2 ? HashAlgorithmName.SHA512 : prf == 1 ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA1;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterationCount, algorithm))
            {
                return pbkdf2.GetBytes(numBytesRequested);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
