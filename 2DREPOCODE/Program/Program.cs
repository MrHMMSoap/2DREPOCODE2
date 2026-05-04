using _2DREPOCODE.Handlers;
using System;

namespace _2DREPOCODE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UnitTestHandler unitTestHandler = new UnitTestHandler();
            unitTestHandler.RunAllTests();

            Console.ReadLine();
        }
    }
}