// Author: Deci | Project: SmartImage.Lib | Name: UploadEngineOption.cs
// Date: 2024/12/06 @ 03:12:53

using SmartImage.Lib.Engines.Search.Other;

namespace SmartImage.Lib.Engines.Upload.Base;

public enum UploadEngineOption
{

	None = 0,

	/// <summary>
	/// <see cref="LitterboxEngine"/>
	/// </summary>
	Litterbox,

	/// <summary>
	/// <see cref="CatboxEngine"/>
	/// </summary>
	Catbox,

	/// <summary>
	/// <see cref="PomfEngine"/>
	/// </summary>
	Pomf,

	/// <summary>
	/// <see cref="ImgOpsEngine"/>
	/// </summary>
	ImgOps,

	/// <summary>
	/// <see cref="TmpFilesEngine"/>
	/// </summary>
	TmpFiles,

#region 

	Obsolete = Pomf | ImgOps

#endregion
}