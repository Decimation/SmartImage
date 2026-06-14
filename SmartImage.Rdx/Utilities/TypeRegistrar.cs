// Author: Deci | Project: SmartImage.Rdx | Name: TypeRegistrar.cs
// Date: 2026/06/13 @ 17:06:16

#nullable disable
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace SmartImage.Rdx.Utilities;

public sealed class TypeRegistrar : ITypeRegistrar
{

	private readonly IServiceCollection m_services;

	public TypeRegistrar(IServiceCollection services)
	{
		m_services = services;
	}

	public ITypeResolver Build() => new TypeResolver(m_services.BuildServiceProvider());

	public void Register(Type service, Type implementation) => m_services.AddSingleton(service, implementation);

	public void RegisterInstance(Type service, object implementation) => m_services.AddSingleton(service, implementation);

	public void RegisterLazy(Type service, Func<object> factory) => m_services.AddSingleton(service, _ => factory());

}