using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Novus.OS;
using Novus.Utilities;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Impl.Upload;

public abstract class BaseUploadEngine : IEndpoint
{
	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract long MaxSize { get; }

	public abstract string Name { get; }

	public string EndpointUrl { get; }

	protected BaseUploadEngine(string s)
	{
		EndpointUrl = s;
		Timeout     = TimeSpan.FromSeconds(10);
	}

	// public static BaseUploadEngine Default { get; } = new LitterboxEngine();

	public abstract Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	protected bool Paranoid { get; set; }

	public TimeSpan Timeout { get; set; }

	protected virtual async Task<UploadResult> ProcessResultAsync(
		IFlurlResponse response, CancellationToken ct = default)
	{
		string url = null;
		bool   ok;

		if (response == null) {
			ok = false;

			goto ret;
		}

		var responseMessage = response.ResponseMessage;

		switch (responseMessage.StatusCode) {
			case HttpStatusCode.BadGateway:
			case HttpStatusCode.GatewayTimeout:
				url = null;
				ok  = false;
				goto ret;
		}

		url = await responseMessage.Content.ReadAsStringAsync(ct);

		ok = true;

		if (Paranoid) {
			var r2 = await url.ConfigureRequest(r =>
			{
				r.OnError = rx =>
				{
					// Debugger.Break();
					rx.ExceptionHandled = true;
				};
			}).GetAsync(cancellationToken: ct);

			if (r2 == null || r2.GetContentLength() == 0) {
				ok = false;
			}

		}

		ret:

		return new()
		{
			Url     = url,
			Size    = response.GetContentLength(),
			IsValid = ok,
			Response = response
		};
	}

	protected void Verify(string file)
	{
		if (string.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}

		if ((FileSystem.GetFileSize(file) > MaxSize)) {
			throw new ArgumentException($"File {file} is too large (max {MaxSize}) for {Name}");
		}
	}

	public static readonly BaseUploadEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseUploadEngine>(InheritanceProperties.Subclass).ToArray();

	/*public async Task<bool> IsAlive()
	{
		using var res = await ((IHttpClient) this).GetEndpointResponseAsync(Timeout);

		return !res.ResponseMessage.IsSuccessStatusCode;
	}*/

	public static BaseUploadEngine Default { get; set; } = CatboxEngine.Instance;

	public void Dispose() { }
}

public abstract class BaseCatboxEngine : BaseUploadEngine
{
	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		Verify(file);

		var response = await EndpointUrl
			               .ConfigureRequest(r =>
			               {
				               // r.Timeout = TimeSpan.FromSeconds(10);

				               r.OnError = rx =>
				               {
					               rx.ExceptionHandled = true;
				               };
			               })
			               .WithHeaders(new
			               {
				               User_Agent = HttpUtilities.UserAgent
			               })
			               .PostMultipartAsync(mp =>
			               {
				               mp.AddFile("fileToUpload", file)
					               .AddString("reqtype", "fileupload")
					               .AddString("time", "1h")
					               .AddString("userhash", string.Empty);
			               }, cancellationToken: ct, completionOption: HttpCompletionOption.ResponseHeadersRead);

		return await ProcessResultAsync(response, ct).ConfigureAwait(false);
	}

	protected BaseCatboxEngine(string s) : base(s) { }
}