// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

namespace SmartImage.Lib.Model;

public interface IHashable
{

	public ulong? Hash { get; }

	[property:
		MNNW(true, nameof(Hash))]
	[MNNW(true, nameof(Hash))]
	public bool HasHash => Hash.HasValue;

}