using System.Security.Cryptography;

namespace POVISAPI.Common
{
    public static class PasswordGenerator
    {
        // Ambiguous characters (0/O, 1/l/I) are left out so the password is easy to read out or copy by hand.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%&*?";

        public static string GenerateTemporary(int length = 12)
        {
            var all = Upper + Lower + Digits + Symbols;
            var chars = new List<char>
            {
                Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols)
            };
            while (chars.Count < length) chars.Add(Pick(all));

            // Fisher-Yates shuffle so the guaranteed characters are not always first.
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars.ToArray());
        }

        private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
