using System.Reflection;
using Novus.Utilities;
using Terminal.Gui;

namespace SmartImage;

public static class TerminalHelper
{
	public static View[] GetViewFields(Type type)
	{
		return ReflectionHelper.GetFieldsById(type, new Assembly[] { Assembly.GetAssembly(typeof(Application)), })
		                       .Select(f => f.GetValue(null))
		                       .Cast<View>()
		                       .ToArray();
	}
}