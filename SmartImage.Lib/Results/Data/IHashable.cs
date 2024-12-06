// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface IHashable
{
	public Lazy<ulong> Hash { get; }

	public const ulong HASH_ERROR = UInt64.MaxValue;

	public bool HasHash => Hash is { IsValueCreated: true, Value: not HASH_ERROR };

}