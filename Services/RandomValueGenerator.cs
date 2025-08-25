using System;
using System.Text;

namespace reviews.Services
{
    /// <summary>
    /// Generates a random value consisting of 4 uppercase letters followed by 13 digits.
    /// </summary>
    public class RandomValueGenerator
    {
        public static string GenerateRandomValue()
        {
            var random = new Random();

            // Generate 4 random uppercase letters
            string letters = GenerateRandomLetters(random, 4);

            // Generate 13 random digits
            string digits = GenerateRandomDigits(random, 13);

            // Combine letters and digits
            return $"{letters}{digits}";
        }

        private static string GenerateRandomLetters(Random random, int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                char letter = (char)random.Next('A', 'Z' + 1); // Random uppercase letter
                builder.Append(letter);
            }
            return builder.ToString();
        }

        private static string GenerateRandomDigits(Random random, int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                char digit = (char)random.Next('0', '9' + 1); // Random digit
                builder.Append(digit);
            }
            return builder.ToString();
        }

    }
}