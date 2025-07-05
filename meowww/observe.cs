using StdLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace testing
{
	public class Program
	{
		static Observable<string> cats = new ();
		public static void Main(string[] args)
		{
			cats.Subscribe(cat => 
			{
				Console.WriteLine("Cat sound: " + cat);
			});
			cats.Add("purr");
			cats.Add("meow");
		}

	}

}

