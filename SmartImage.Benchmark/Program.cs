using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SmartImage.Benchmark
{
	public static class Program
	{

		public static void Main(string[] args)
		{
			var cfg = DefaultConfig.Instance
				.AddExporter(new HtmlExporter())
				.AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()))
				.AddJob(Job.InProcess.WithRuntime(CoreRuntime.Core80));

			/*var cfg = DefaultConfig.Instance
				// .AddExporter(new HtmlExporter())
				.AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()) {})
				.AddJob(Job.Default.WithRuntime(CoreRuntime.Core80));*/

			BenchmarkRunner.Run<Benchmark2>(cfg);
		}

	}

	// [MemoryDiagnoser]
	// [SimpleJob()]
	// [RyuJitX64Job]
}