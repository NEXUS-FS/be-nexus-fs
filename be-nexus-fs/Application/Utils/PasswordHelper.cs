using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BCrypt.Net;


namespace Application.Utils
{
    public class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public static string GenerateSecurePassword(int length = 16)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()-_=+[]{}<>?";
            string allChars = upper + lower + digits + symbols;

            // Ensure at least one character from each category
            StringBuilder password = new StringBuilder();
            password.Append(GetRandomChar(upper));
            password.Append(GetRandomChar(lower));
            password.Append(GetRandomChar(digits));
            password.Append(GetRandomChar(symbols));

            // Fill the rest with random characters
            for (int i = password.Length; i < length; i++)
            {
                password.Append(GetRandomChar(allChars));
            }

            // Shuffle to avoid predictable pattern (first 4 chars)
            return new string(password.ToString().OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static char GetRandomChar(string charset)
        {
            byte[] buffer = new byte[1];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return charset[buffer[0] % charset.Length];
        }

        public static bool ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;

            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecial = Regex.IsMatch(password, "[!@#$%^&*()\\-_=+\\[\\]{};:'\",.<>?/|\\\\]");

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}
