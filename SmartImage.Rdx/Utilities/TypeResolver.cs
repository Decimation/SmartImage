// Author: Deci | Project: SmartImage.Rdx | Name: TypeResolver.cs
// Date: 2026/06/13 @ 17:06:11

#nullable disable
using SmartImage;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Utilities;

public sealed class TypeResolver : ITypeResolver
{

	private readonly IServiceProvider m_provider;

	public TypeResolver(IServiceProvider provider)
	{
		m_provider = provider;
	}

	[CBN]
	public object Resolve([CBN] Type type) => type == null ? null : m_provider.GetService(type);
}