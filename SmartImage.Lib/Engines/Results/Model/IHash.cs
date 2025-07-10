// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

namespace SmartImage.Lib.Engines.Results.Model;

public interface IHash
{
	public Lazy<ulong?> Hash { get; }

	[MNNW(true, nameof(Hash))]
	public bool HasHash => Hash is { IsValueCreated: true, Value: not null };

}