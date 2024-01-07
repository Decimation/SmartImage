using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SmartImage.Benchmark
{
	public static class Program
	{

	public	static void Main(string[] args)
		{
			var cfg = ManualConfig.CreateMinimumViable()
				.AddExporter(new HtmlExporter())
				.AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()))
				.AddJob(Job.InProcess);
			BenchmarkRunner.Run<Benchmark1>(cfg);
		}

	}

	// [MemoryDiagnoser]
	// [SimpleJob()]
	// [RyuJitX64Job]
}