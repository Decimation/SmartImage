// Author: Deci | Project: SmartImage.Lib | Name: IHashable.cs
// Date: 2024/11/13 @ 16:11:29

namespace SmartImage.Lib.Results.Data;

public interface IHashable
{

	public ulong? Hash { get; }

	public bool TryCalculateHash();

}