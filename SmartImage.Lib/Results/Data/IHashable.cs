// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface IHashable
{

	public Lazy<ulong> Hash { get; }

	public ulong TryCalculateHash();

	public const ulong INVALID_HASH = UInt64.MinValue;

	public bool HasHash => Hash.IsValueCreated && Hash.Value != INVALID_HASH;

}