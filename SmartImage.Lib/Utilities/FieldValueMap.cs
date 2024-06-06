using Kantan.Utilities;
using Novus.Utilities;

namespace SmartImage.Lib.Utilities;

// todo

public class FieldValueMap
{

	public string Name { get; }

	public object Value { get; }

	public string FieldName { get; }

	public static FieldValueMap Find<T>(T inst, string name)
	{
		var type = inst?.GetType() ?? typeof(T);
		return Find(inst, name, type);
	}

	private static FieldValueMap Find<T>(T inst, string name, Type type)
	{
		var fld = type.GetAnyResolvedField(name);

		if (fld is not null) {
			return new FieldValueMap(name, fld.GetValue(inst), fld.Name);
		}

		return null;
	}

	public static FieldValueMap[] Find<T>(T inst, string[] names)
	{
		var type = inst?.GetType() ?? typeof(T);

		var values = new FieldValueMap[names.Length];
		int i      = 0;

		foreach (string name in names) {
			var val = Find(inst, name, type);

			if (val is not null) {
				values[i++] = val;
			}
		}

		Array.Resize(ref values, i);

		return values;
	}

	public static FieldValueMap[] Find<T, T2>(T inst, T2 names) where T2 : struct, Enum
	{
		var names2 = names.GetSetFlags().Select(x => x.ToString()).ToArray();
		return Find(inst, names2);
	}

	public FieldValueMap(string name, object value, string fieldName)
	{
		Name      = name;
		Value     = value;
		FieldName = fieldName;
	}

	public override string ToString()
	{
		return $"{nameof(Name)}: {Name} | {nameof(FieldName)}: {FieldName} | {nameof(Value)}: {Value}";
	}

}