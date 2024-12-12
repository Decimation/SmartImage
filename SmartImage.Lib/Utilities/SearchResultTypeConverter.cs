// Author: Deci | Project: SmartImage.Lib | Name: SearchResultTypeConverter.cs
// Date: 2024/11/21 @ 13:11:05

using System.Text.Json;
using System.Text.Json.Serialization;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public sealed class SearchResultTypeConverter : JsonConverter<SearchResult>
{

	#region Overrides of JsonConverter<SearchResult>

	public override SearchResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}

	public override void Write(Utf8JsonWriter writer, SearchResult value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(SearchResult.Engine.Name), value.Engine.EngineOption.ToString());
		writer.WriteString(nameof(SearchResult.Status), $"{value.Status}");
		writer.WriteStartArray(nameof(SearchResult.Results));

		foreach (SearchResultItem result in value.Results) {
			writer.WriteStartObject();
			writer.WriteRawValue(JsonSerializer.Serialize(result, options));
			writer.WriteEndObject();
		}

		writer.WriteEndArray();
		writer.WriteEndObject();
	}

	#endregion

}