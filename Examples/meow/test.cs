using StdLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace test
{
	public class meow
	{
		public static int ten = 0;
	}

	[TestingOnly]
	public class Program
	{
		public static void tester()
		{
			meow.ten = 11;
		}

		public static void Main(string[] args)
		{
			InlineTestRunner.RunAllTests();
		}

	}

}

