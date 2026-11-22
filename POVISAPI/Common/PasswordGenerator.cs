using System.Security.Cryptography;

namespace POVISAPI.Common
{
    public static class PasswordGenerator
    {
        public const int TemporaryLength = 8;

        /// <summary>A temporary password of 8 random digits (0-9), easy to read from an SMS and type in.
        /// It is single-use in practice: the user is forced to replace it on their next sign-in.</summary>
        public static string GenerateTemporary(int length = TemporaryLength)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            return new string(chars);
        }
    }
}
