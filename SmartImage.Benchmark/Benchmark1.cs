// Deci SmartImage.Benchmark Benchmark1.cs
// $File.CreatedYear-$File.CreatedMonth-6 @ 13:53

using BenchmarkDotNet.Attributes;
using Novus.FileTypes;
using SixLabors.ImageSharp;
using SmartImage.Lib;

namespace SmartImage.Benchmark;
#pragma warning disable CS8618
public class Benchmark1
{

	public string s;

	[GlobalSetup]
	public void GlobalSetup()
	{
		s =
			@"C:\Users\Deci\Pictures\MPV Screenshots\mpv_[SubsPlease] Jujutsu Kaisen - 30 (1080p) [3DAACE2D]_00_04_56.630_00h04m56s630ms_ns].png";

	}

	[Benchmark]
	public async Task<ImageInfo> Test1()
	{
		return await Image.IdentifyAsync(s);
	}

	[Benchmark]
	public async Task<SearchQuery> Test2()
	{
		return await SearchQuery.TryCreateAsync(s);
	}

	[Benchmark]
	public async Task<UniSource> Test3()
	{
		return await UniSource.TryGetAsync(s);
	}

}