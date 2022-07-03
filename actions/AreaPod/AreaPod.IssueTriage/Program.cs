using System;

namespace AreaPod.IssueTriage
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Triaging Issue for Pod: {string.Join(' ', args)}");

            string? GITHUB_TOKEN = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            if (GITHUB_TOKEN is null)
            {
                Console.WriteLine("No GITHUB_TOKEN available");
            }
            else
            {
                Console.WriteLine("GITHUB_TOKEN available");
            }
        }
    }
}