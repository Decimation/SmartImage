// Author: Deci | Project: SmartImage.Lib | Name: BaseSearchEngineTypeConverter.cs
// Date: 2024/12/11 @ 23:12:21

using System.Text.Json;
using System.Text.Json.Serialization;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Utilities;

public sealed class BaseSearchEngineTypeConverter : JsonConverter<BaseSearchEngine>
{

	#region Overrides of JsonConverter<SearchResult>

	public override BaseSearchEngine Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}

	public override void Write(Utf8JsonWriter writer, BaseSearchEngine value, JsonSerializerOptions options)
	{
		writer.WriteString(nameof(BaseSearchEngine.Name), value.Name);
	}

	#endregion

}