using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Utilities
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	internal class ConfigFieldAttribute : Attribute
	{
		public object DefaultValue { get; set; }

		public string Id { get; set; }
		
		public bool SetDefaultIfNull { get; set; }


		public ConfigFieldAttribute(string id, object defaultValue, bool setDefaultIfNull)
		{
			Id               = id;
			DefaultValue     = defaultValue;
			SetDefaultIfNull = setDefaultIfNull;
		}

		public ConfigFieldAttribute(string id, object defaultValue) : this(id, defaultValue, true)
		{
			
		}
	}
}
