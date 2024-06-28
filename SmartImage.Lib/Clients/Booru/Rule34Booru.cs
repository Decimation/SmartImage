// Author: Deci | Project: SmartImage.Lib | Name: Rule34Booru.cs
// Date: 2024/06/18 @ 14:06:04

namespace SmartImage.Lib.Clients.Booru;

public class Rule34Booru : BaseGelbooruClient
{

	public override string Name => "Rule34";

	public Rule34Booru() : base("https://rule34.xxx/") { }

}