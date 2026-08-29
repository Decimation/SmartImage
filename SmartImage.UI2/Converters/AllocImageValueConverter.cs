using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using SmartImage.Lib.Images.Alloc;

namespace SmartImage.UI2.Converters;

public class AllocImageValueConverter : IValueConverter
{

	// Keyed by instance identity so entries are collected alongside their AllocImageStream
	// rather than needing an explicit cache-invalidation/eviction policy.
	private static readonly ConditionalWeakTable<IAllocImage, Bitmap> s_cache = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not IAllocImage { HasSource: true } allocImg) {
			return default;
		}

		if (s_cache.TryGetValue(allocImg, out var cached)) {
			return cached;
		}

		using var stream = allocImg.GetSource();
		var bitmap = new Bitmap(stream);

		s_cache.AddOrUpdate(allocImg, bitmap);
		return bitmap;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return default;
	}

}