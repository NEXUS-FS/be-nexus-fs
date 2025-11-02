using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Utils;

namespace NexusFS.Tests
{
    public class PasswordHelperTests
    {
        [Fact]
        public void HashPassword_ShouldReturnDifferentHashesForSameInput()
        {
            string password = "Admin@2025";

            string hash1 = PasswordHelper.HashPassword(password);
            string hash2 = PasswordHelper.HashPassword(password);

            Assert.NotEqual(hash1, hash2); // Each hash should have a unique salt
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            string password = "Admin@2025";
            string hash = PasswordHelper.HashPassword(password);

            Assert.True(PasswordHelper.VerifyPassword(password, hash));
        }

        [Fact]
        public void ValidatePasswordComplexity_ShouldAcceptStrongPassword()
        {
            string password = "StrongPass1@";
            Assert.True(PasswordHelper.ValidatePasswordComplexity(password));
        }

        [Theory]
        [InlineData("short")]
        [InlineData("NoNumber!")]
        [InlineData("nouppercase1!")]
        [InlineData("NOLOWERCASE1!")]
        [InlineData("NoSpecial123")]
        public void ValidatePasswordComplexity_ShouldRejectWeakPasswords(string weak)
        {
            Assert.False(PasswordHelper.ValidatePasswordComplexity(weak));
        }
    }
}
