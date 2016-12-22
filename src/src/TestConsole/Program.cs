using MessageWire.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tests = new RouterDealerTests();
            tests.TestSomething();

            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}
