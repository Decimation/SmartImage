// Author: Deci | Project: SmartImage.Lib | Name: Rule34Booru.cs
// Date: 2024/06/18 @ 14:06:04

using System.Diagnostics.CodeAnalysis;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Clients.Booru;
// TODO

[Experimental(AppSupport.DIAG_SMRTIMG_EXP001)]
public class Rule34Booru : BaseGelbooruClient
{

	public override string Name => "Rule34";

	public Rule34Booru() : base("https://rule34.xxx/") { }

}