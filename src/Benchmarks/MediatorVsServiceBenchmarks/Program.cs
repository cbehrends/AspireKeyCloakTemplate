using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Running;

namespace MediatorVsServiceBenchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

