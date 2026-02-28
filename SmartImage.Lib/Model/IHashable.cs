// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

namespace SmartImage.Lib.Model;

public interface IHashable
{
	public ulong? Hash { get; }

	[MNNW(true, nameof(Hash), nameof(Hash.Value))]
	public bool HasHash => Hash.HasValue;

}