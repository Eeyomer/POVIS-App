using System.Security.Cryptography;
using POVISAPI.Models.Entity;

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

        /// <summary>A temporary password that is not the user's current password.</summary>
        public static string GenerateTemporaryFor(User user)
        {
            string password;
            do password = GenerateTemporary();
            while (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
            return password;
        }

        /// <summary>Replaces the user's password with a temporary one they must change at next sign-in.
        /// Their real password is remembered so they cannot choose it again (a second reset before they change
        /// it keeps the original, not the earlier temporary one).</summary>
        public static void ApplyTemporary(User user, string temporaryPassword)
        {
            if (!user.MustChangePassword) user.PreviousPasswordHash = user.PasswordHash;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            user.MustChangePassword = true;
        }
    }
}
