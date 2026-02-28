// Author: Deci | Project: SmartImage.Lib | Name: INamedEnumOption.cs
// Date: 2026/02/21 @ 15:02:28

namespace SmartImage.Lib.Model;

public interface INamedEnumOption<out TEnum> where TEnum : Enum
{

	public TEnum Option { get; }

	public string Name => Option.ToString();
}