using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SmartImage.Core;

namespace SmartImage.Utilities
{
	/// <summary>
	/// <seealso cref="SearchConfig"/>
	/// <seealso cref="ConfigComponents"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	internal sealed class ConfigComponentAttribute : Attribute
	{
		internal object DefaultValue { get; set; }

		internal string Id { get; set; }

		internal bool SetDefaultIfNull { get; set; }

		[CanBeNull]
		internal string ArgumentName { get; set; }


		internal ConfigComponentAttribute(string id, object defaultValue, bool setDefaultIfNull, string argumentName)
		{
			Id               = id;
			DefaultValue     = defaultValue;
			SetDefaultIfNull = setDefaultIfNull;
			ArgumentName     = argumentName;
		}

		internal ConfigComponentAttribute(string id, object defaultValue, [CanBeNull] string argumentName) : this(id,
			defaultValue, false, argumentName) { }


		internal ConfigComponentAttribute(string id, object defaultValue) : this(id, defaultValue, null) { }
	}
}