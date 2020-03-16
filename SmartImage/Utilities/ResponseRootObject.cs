using JetBrains.Annotations;

namespace SmartImage.Utilities
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal sealed class ResponseRootObject<T>
	{
		public T Data { get; set; }

		public bool Success { get; set; }

		public int Status { get; set; }
	}
}