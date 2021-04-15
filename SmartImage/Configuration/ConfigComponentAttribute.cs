using JetBrains.Annotations;
using System;

namespace SmartImage.Configuration
{
	/// <summary>
	/// <seealso cref="UserSearchConfig"/>
	/// <seealso cref="ConfigComponents"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	internal sealed class ConfigComponentAttribute : Attribute
	{
		internal object DefaultValue { get; set; }

		/// <summary>
		/// Component name
		/// </summary>
		internal string Id { get; set; }


		internal bool SetDefaultIfNull { get; set; }


		/// <summary>
		/// Parameter name
		/// </summary>
		[CanBeNull]
		internal string ParameterName { get; set; }


		internal ConfigComponentAttribute(string id, string parameterName, object defaultValue, bool setDefaultIfNull)
		{
			Id = id;
			DefaultValue = defaultValue;
			SetDefaultIfNull = setDefaultIfNull;
			ParameterName = parameterName;
		}

		internal ConfigComponentAttribute(string id, [CanBeNull] string parameterName, object defaultValue) : this(id,
			parameterName, defaultValue, false)
		{ }


		internal ConfigComponentAttribute(string id, object defaultValue) : this(id, null, defaultValue) { }
	}
}