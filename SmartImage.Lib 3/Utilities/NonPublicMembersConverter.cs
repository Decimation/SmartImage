// Read S SmartImage.Lib NonPublicMembersConverter.cs
// 2023-09-26 @ 10:51 PM

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartImage.Lib.Utilities;

#pragma warning disable CS0649
public class NonPublicMembersConverter<T> : JsonConverter<T> where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        T instance = (T)Activator.CreateInstance(typeToConvert, nonPublic: true);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString();

            PropertyInfo propertyInfo =
                typeToConvert.GetProperty(propertyName,
                                          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                reader.Read(); // Move to the property value
                object value = JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options);
                propertyInfo.SetValue(instance, value);
            }
            else
            {
                reader.Skip();
            }
        }

        return instance;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}