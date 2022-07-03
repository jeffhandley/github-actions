using System;

namespace AreaPod.IssueTriage
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Triaging Issue for Pod: {string.Join(' ', args)}");
        }
    }
}