// Author: Deci | Project: SmartImage.Lib | Name: UrlTypeConverter.cs
// Date: 2024/11/21 @ 11:11:12

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartImage.Lib.Utilities;

public class UrlTypeConverter : JsonConverter<Url>
{

	public override Url Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.GetString();
	}

	public override void Write(Utf8JsonWriter writer, Url value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}

}