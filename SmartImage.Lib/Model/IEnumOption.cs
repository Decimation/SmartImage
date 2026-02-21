// Author: Deci | Project: SmartImage.Lib | Name: IEnumOption.cs
// Date: 2026/02/21 @ 15:02:28

namespace SmartImage.Lib.Model;

public interface IEnumOption<out TEnum> where TEnum : Enum
{

	TEnum Option { get; }

	string Name => Option.ToString();

}