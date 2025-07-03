using System;
using System.Collections.Generic;
using System.Linq;

namespace StdLib
{
    /// <summary>
    /// Advanced mathematical functions and utilities
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Mathematical constants
        /// </summary>
        public static class Constants
        {
            public const double PI = Math.PI;
            public const double E = Math.E;
            public const double TAU = 2 * Math.PI;
            public const double GOLDEN_RATIO = 1.618033988749895;
            public const double SQRT_2 = 1.4142135623730951;
            public const double SQRT_3 = 1.7320508075688772;
            public const double LN_2 = 0.6931471805599453;
            public const double LN_10 = 2.302585092994046;
        }

        /// <summary>
        /// Calculate factorial of a number
        /// </summary>
        public static long Factorial(int n)
        {
            if (n < 0) throw new ArgumentException("Factorial is not defined for negative numbers");
            if (n == 0 || n == 1) return 1;
            
            long result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }

        /// <summary>
        /// Calculate greatest common divisor
        /// </summary>
        public static int GCD(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Calculate least common multiple
        /// </summary>
        public static int LCM(int a, int b)
        {
            return Math.Abs(a * b) / GCD(a, b);
        }

        /// <summary>
        /// Check if a number is prime
        /// </summary>
        public static bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;
            
            for (int i = 3; i * i <= n; i += 2)
            {
                if (n % i == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Generate Fibonacci sequence up to n terms
        /// </summary>
        public static List<long> Fibonacci(int count)
        {
            var result = new List<long>();
            if (count <= 0) return result;
            
            if (count >= 1) result.Add(0);
            if (count >= 2) result.Add(1);
            
            for (int i = 2; i < count; i++)
            {
                result.Add(result[i - 1] + result[i - 2]);
            }
            
            return result;
        }

        /// <summary>
        /// Calculate combinations (n choose r)
        /// </summary>
        public static long Combinations(int n, int r)
        {
            if (r > n || r < 0) return 0;
            if (r == 0 || r == n) return 1;
            
            r = Math.Min(r, n - r); // Take advantage of symmetry
            
            long result = 1;
            for (int i = 0; i < r; i++)
            {
                result = result * (n - i) / (i + 1);
            }
            return result;
        }

        /// <summary>
        /// Calculate permutations (n permute r)
        /// </summary>
        public static long Permutations(int n, int r)
        {
            if (r > n || r < 0) return 0;
            
            long result = 1;
            for (int i = n; i > n - r; i--)
            {
                result *= i;
            }
            return result;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        public static double ToRadians(double degrees)
        {
            return degrees * Constants.PI / 180.0;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        public static double ToDegrees(double radians)
        {
            return radians * 180.0 / Constants.PI;
        }

        /// <summary>
        /// Calculate distance between two points
        /// </summary>
        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        /// <summary>
        /// Calculate area of a circle
        /// </summary>
        public static double CircleArea(double radius)
        {
            return Constants.PI * radius * radius;
        }

        /// <summary>
        /// Calculate area of a triangle using base and height
        /// </summary>
        public static double TriangleArea(double baseLength, double height)
        {
            return 0.5 * baseLength * height;
        }

        /// <summary>
        /// Calculate area of a triangle using Heron's formula
        /// </summary>
        public static double TriangleAreaHeron(double a, double b, double c)
        {
            double s = (a + b + c) / 2;
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        /// <summary>
        /// Clamp a value between min and max
        /// </summary>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        public static double Lerp(double start, double end, double t)
        {
            return start + t * (end - start);
        }

        /// <summary>
        /// Map a value from one range to another
        /// </summary>
        public static double Map(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Calculate nth root of a number
        /// </summary>
        public static double NthRoot(double number, double n)
        {
            return Math.Pow(number, 1.0 / n);
        }

        /// <summary>
        /// Calculate logarithm with custom base
        /// </summary>
        public static double LogBase(double number, double baseValue)
        {
            return Math.Log(number) / Math.Log(baseValue);
        }

        /// <summary>
        /// Round to nearest multiple
        /// </summary>
        public static double RoundToNearest(double value, double multiple)
        {
            return Math.Round(value / multiple) * multiple;
        }

        /// <summary>
        /// Calculate compound interest
        /// </summary>
        public static double CompoundInterest(double principal, double rate, double time, int compoundingFrequency = 1)
        {
            return principal * Math.Pow(1 + rate / compoundingFrequency, compoundingFrequency * time);
        }

        /// <summary>
        /// Calculate simple interest
        /// </summary>
        public static double SimpleInterest(double principal, double rate, double time)
        {
            return principal * rate * time;
        }

        /// <summary>
        /// Calculate percentage change
        /// </summary>
        public static double PercentageChange(double oldValue, double newValue)
        {
            if (oldValue == 0) return newValue == 0 ? 0 : double.PositiveInfinity;
            return ((newValue - oldValue) / oldValue) * 100;
        }

        /// <summary>
        /// Calculate area of rectangle
        /// </summary>
        public static double RectangleArea(double width, double height)
        {
            return width * height;
        }

        /// <summary>
        /// Calculate area of trapezoid
        /// </summary>
        public static double TrapezoidArea(double base1, double base2, double height)
        {
            return ((base1 + base2) / 2) * height;
        }

        /// <summary>
        /// Calculate volume of sphere
        /// </summary>
        public static double SphereVolume(double radius)
        {
            return (4.0 / 3.0) * Constants.PI * Math.Pow(radius, 3);
        }

        /// <summary>
        /// Calculate volume of cylinder
        /// </summary>
        public static double CylinderVolume(double radius, double height)
        {
            return Constants.PI * radius * radius * height;
        }

        /// <summary>
        /// Calculate hypotenuse of right triangle
        /// </summary>
        public static double Hypotenuse(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }
    }

    /// <summary>
    /// Statistical functions
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Calculate mean (average) of a collection
        /// </summary>
        public static double Mean(IEnumerable<double> values)
        {
            return values.Average();
        }

        /// <summary>
        /// Calculate median of a collection
        /// </summary>
        public static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToList();
            int count = sorted.Count;
            
            if (count == 0) throw new ArgumentException("Cannot calculate median of empty collection");
            
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                return sorted[count / 2];
            }
        }

        /// <summary>
        /// Calculate mode (most frequent value) of a collection
        /// </summary>
        public static double Mode(IEnumerable<double> values)
        {
            var groups = values.GroupBy(x => x);
            var maxCount = groups.Max(g => g.Count());
            return groups.First(g => g.Count() == maxCount).Key;
        }

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        public static double StandardDeviation(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            var mean = Mean(valuesList);
            var sumOfSquares = valuesList.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumOfSquares / valuesList.Count);
        }

        /// <summary>
        /// Calculate variance
        /// </summary>
        public static double Variance(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            var mean = Mean(valuesList);
            return valuesList.Sum(x => Math.Pow(x - mean, 2)) / valuesList.Count;
        }

        /// <summary>
        /// Calculate correlation coefficient between two datasets
        /// </summary>
        public static double Correlation(IEnumerable<double> x, IEnumerable<double> y)
        {
            var xList = x.ToList();
            var yList = y.ToList();
            
            if (xList.Count != yList.Count)
                throw new ArgumentException("X and Y must have the same number of elements");
            
            var xMean = Mean(xList);
            var yMean = Mean(yList);
            
            var numerator = xList.Zip(yList, (xi, yi) => (xi - xMean) * (yi - yMean)).Sum();
            var xSumSquares = xList.Sum(xi => Math.Pow(xi - xMean, 2));
            var ySumSquares = yList.Sum(yi => Math.Pow(yi - yMean, 2));
            
            return numerator / Math.Sqrt(xSumSquares * ySumSquares);
        }

        /// <summary>
        /// Calculate percentile of a value in a dataset
        /// </summary>
        public static double Percentile(IEnumerable<double> values, double percentile)
        {
            var sorted = values.OrderBy(x => x).ToList();
            if (sorted.Count == 0) throw new ArgumentException("Cannot calculate percentile of empty collection");
            
            double index = (percentile / 100.0) * (sorted.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            
            if (lowerIndex == upperIndex)
            {
                return sorted[lowerIndex];
            }
            
            return MathUtils.Lerp(sorted[lowerIndex], sorted[upperIndex], index - lowerIndex);
        }
    }
}
