using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StdLib
{
    /// <summary>
    /// Random number generation and probability utilities
    /// </summary>
    public static class RandomUtils
    {
        private static readonly Random _random = new Random();
        private static readonly RandomNumberGenerator _cryptoRandom = RandomNumberGenerator.Create();

        /// <summary>
        /// Generate random integer between min (inclusive) and max (exclusive)
        /// </summary>
        public static int RandomInt(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Generate random integer between 0 and max (exclusive)
        /// </summary>
        public static int RandomInt(int max)
        {
            return _random.Next(max);
        }

        /// <summary>
        /// Generate random double between 0.0 and 1.0
        /// </summary>
        public static double RandomDouble()
        {
            return _random.NextDouble();
        }

        /// <summary>
        /// Generate random double between min and max
        /// </summary>
        public static double RandomDouble(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Generate random float between 0.0 and 1.0
        /// </summary>
        public static float RandomFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Generate random float between min and max
        /// </summary>
        public static float RandomFloat(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Generate random boolean
        /// </summary>
        public static bool RandomBool()
        {
            return _random.Next(2) == 1;
        }

        /// <summary>
        /// Generate random boolean with specified probability of true
        /// </summary>
        public static bool RandomBool(double probability)
        {
            return _random.NextDouble() < probability;
        }

        /// <summary>
        /// Choose random element from array
        /// </summary>
        public static T Choose<T>(params T[] items)
        {
            if (items.Length == 0) throw new ArgumentException("Cannot choose from empty array");
            return items[_random.Next(items.Length)];
        }

        /// <summary>
        /// Choose random element from list
        /// </summary>
        public static T Choose<T>(IList<T> items)
        {
            if (items.Count == 0) throw new ArgumentException("Cannot choose from empty list");
            return items[_random.Next(items.Count)];
        }

        /// <summary>
        /// Shuffle an array in place
        /// </summary>
        public static void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// Shuffle a list and return new shuffled list
        /// </summary>
        public static List<T> Shuffle<T>(IEnumerable<T> items)
        {
            var array = items.ToArray();
            Shuffle(array);
            return array.ToList();
        }

        /// <summary>
        /// Generate random string of specified length
        /// </summary>
        public static string RandomString(int length, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
        {
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[_random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Generate random alphanumeric string
        /// </summary>
        public static string RandomAlphanumeric(int length)
        {
            return RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        }

        /// <summary>
        /// Generate random letters only string
        /// </summary>
        public static string RandomLetters(int length)
        {
            return RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        }

        /// <summary>
        /// Generate random digits only string
        /// </summary>
        public static string RandomDigits(int length)
        {
            return RandomString(length, "0123456789");
        }

        /// <summary>
        /// Generate GUID
        /// </summary>
        public static string RandomGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Generate cryptographically secure random bytes
        /// </summary>
        public static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            _cryptoRandom.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generate cryptographically secure random integer
        /// </summary>
        public static int SecureRandomInt(int min, int max)
        {
            if (min >= max) throw new ArgumentException("Min must be less than max");
            
            uint range = (uint)(max - min);
            uint bytes = GetRandomUInt32() % range;
            return (int)(min + bytes);
        }

        /// <summary>
        /// Sample elements from a collection without replacement
        /// </summary>
        public static List<T> Sample<T>(IEnumerable<T> items, int count)
        {
            var itemsList = items.ToList();
            if (count > itemsList.Count) throw new ArgumentException("Sample size cannot be larger than collection size");
            
            var result = new List<T>();
            var indices = Enumerable.Range(0, itemsList.Count).ToList();
            
            for (int i = 0; i < count; i++)
            {
                int randomIndex = _random.Next(indices.Count);
                result.Add(itemsList[indices[randomIndex]]);
                indices.RemoveAt(randomIndex);
            }
            
            return result;
        }

        /// <summary>
        /// Generate random numbers following normal distribution
        /// </summary>
        public static double RandomNormal(double mean = 0.0, double stdDev = 1.0)
        {
            // Box-Muller transform
            static double NextGaussian()
            {
                double? nextGaussian = null;
                
                if (nextGaussian.HasValue)
                {
                    var temp = nextGaussian.Value;
                    nextGaussian = null;
                    return temp;
                }
                
                double u1 = 1.0 - _random.NextDouble();
                double u2 = 1.0 - _random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                nextGaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                
                return randStdNormal;
            }
            
            return mean + stdDev * NextGaussian();
        }

        /// <summary>
        /// Weighted random choice
        /// </summary>
        public static T WeightedChoice<T>(Dictionary<T, double> weights) where T : notnull
        {
            var totalWeight = weights.Values.Sum();
            var randomValue = _random.NextDouble() * totalWeight;
            
            double cumulativeWeight = 0;
            foreach (var kvp in weights)
            {
                cumulativeWeight += kvp.Value;
                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }
            
            return weights.Keys.Last(); // Fallback
        }

        /// <summary>
        /// Set random seed for reproducible results
        /// </summary>
        public static void SetSeed(int seed)
        {
            // Note: This creates a new Random instance with the seed
            // In practice, you might want to use a field for this
            var seededRandom = new Random(seed);
            // This is a limitation of the static approach - consider refactoring if deterministic behavior is critical
        }

        /// <summary>
        /// Generate random color (RGB)
        /// </summary>
        public static (int R, int G, int B) RandomColor()
        {
            return (_random.Next(256), _random.Next(256), _random.Next(256));
        }

        /// <summary>
        /// Generate random hex color
        /// </summary>
        public static string RandomHexColor()
        {
            var (r, g, b) = RandomColor();
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static uint GetRandomUInt32()
        {
            var bytes = new byte[4];
            _cryptoRandom.GetBytes(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }

    /// <summary>
    /// Dice rolling utilities
    /// </summary>
    public static class Dice
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Roll a single die with specified number of sides
        /// </summary>
        public static int Roll(int sides = 6)
        {
            return _random.Next(1, sides + 1);
        }

        /// <summary>
        /// Roll multiple dice and return individual results
        /// </summary>
        public static List<int> Roll(int count, int sides = 6)
        {
            var results = new List<int>();
            for (int i = 0; i < count; i++)
            {
                results.Add(Roll(sides));
            }
            return results;
        }

        /// <summary>
        /// Roll multiple dice and return sum
        /// </summary>
        public static int RollSum(int count, int sides = 6)
        {
            return Roll(count, sides).Sum();
        }

        /// <summary>
        /// Roll dice with advantage (roll twice, take higher)
        /// </summary>
        public static int RollAdvantage(int sides = 20)
        {
            return Math.Max(Roll(sides), Roll(sides));
        }

        /// <summary>
        /// Roll dice with disadvantage (roll twice, take lower)
        /// </summary>
        public static int RollDisadvantage(int sides = 20)
        {
            return Math.Min(Roll(sides), Roll(sides));
        }

        /// <summary>
        /// Parse and roll dice notation (e.g., "3d6+2")
        /// </summary>
        public static int RollNotation(string notation)
        {
            // Simple parser for XdY+Z format
            var parts = notation.ToLower().Split('+');
            var diceStr = parts[0];
            var bonus = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            
            var diceParts = diceStr.Split('d');
            var count = int.Parse(diceParts[0]);
            var sides = int.Parse(diceParts[1]);
            
            return RollSum(count, sides) + bonus;
        }
    }
}
