// Deci SmartImage.Rdx TypeResolver.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 1:46

using Spectre.Console.Cli;

namespace SmartImage.Rdx.Utilities;

public sealed class TypeResolver : ITypeResolver, IDisposable
{

	private readonly IServiceProvider _provider;

	public TypeResolver(IServiceProvider provider)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
	}

	public object? Resolve(Type? type)
	{
		if (type == null) {
			return null;
		}

		return _provider.GetService(type);
	}

	public void Dispose()
	{
		if (_provider is IDisposable disposable) {
			disposable.Dispose();
		}
	}

}