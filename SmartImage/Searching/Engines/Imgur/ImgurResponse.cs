#region

using JetBrains.Annotations;

#endregion

namespace SmartImage.Searching.Engines.Imgur
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class ImgurResponse<T>
	{
		public T Data { get; set; }

		public bool Success { get; set; }

		public int Status { get; set; }
	}
}