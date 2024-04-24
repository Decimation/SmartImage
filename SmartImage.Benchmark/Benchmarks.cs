// Deci SmartImage.Benchmark Benchmark1.cs
// $File.CreatedYear-$File.CreatedMonth-6 @ 13:53

using BenchmarkDotNet.Attributes;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
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

public class Benchmark2
{
/*
 *
 * // * Summary *
   
   BenchmarkDotNet v0.13.12, Windows 10 (10.0.19043.2364/21H1/May2021Update)
   13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
   .NET SDK 8.0.202
	 [Host] : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
   
   Job=InProcess  Toolchain=InProcessEmitToolchain
   
   | Method | Mean        | Error       | StdDev      | Gen0   | Gen1   | Allocated |
   |------- |------------:|------------:|------------:|-------:|-------:|----------:|
   | Test1  |    183.8 ns |     3.60 ns |     3.69 ns | 0.0277 |      - |     440 B |
   | Test2  | 62,358.5 ns | 1,231.50 ns | 2,057.56 ns | 0.3662 | 0.2441 |    6533 B |
   | Test3  | 60,623.4 ns | 1,177.80 ns | 1,402.09 ns | 0.3662 | 0.2441 |    6364 B |
 *
 */
	public string     s;
	public FileStream fs;

	[GlobalSetup]
	public void GlobalSetup()
	{
		s =
			@"C:\Users\Deci\Pictures\MPV Screenshots\mpv_[SubsPlease] Jujutsu Kaisen - 30 (1080p) [3DAACE2D]_00_04_56.630_00h04m56s630ms_ns].png";
		fs = File.OpenRead(s);
	}

	[Benchmark]
	public async Task<IImageFormat> Test1()
	{
		return await Image.DetectFormatAsync(fs);
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
public class Benchmark3
{
	public string     s;

	[GlobalSetup]
	public void GlobalSetup()
	{
		s =
			@"C:\Users\Deci\Pictures\MPV Screenshots\mpv_[SubsPlease] Jujutsu Kaisen - 30 (1080p) [3DAACE2D]_00_04_56.630_00h04m56s630ms_ns].png";
		
	}

	[Benchmark]
	public async Task<SearchQuery> Test1()
	{
		return await SearchQuery.TryCreateAsync(s);
	}

	[Benchmark]
	public async Task<SearchQuery2> Test2()
	{
		return await SearchQuery2.Decode(s);
	}

}