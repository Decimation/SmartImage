// Read Stanton SmartImage.Lib NodeHelper.cs
// 2023-01-13 @ 11:37 PM

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AngleSharp.Dom;
using Flurl.Http;
using Flurl.Http.Configuration;
using JetBrains.Annotations;
using SmartImage.Lib.Results;

// ReSharper disable AnnotateNotNullParameter

namespace SmartImage.Lib.Utilities;

public static class NodeUtil
{

	public static readonly JsonSerializerOptions Options = new(JsonSerializerOptions.Default)
	{
		IncludeFields          = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Converters =
		{
			new UrlTypeConverter()
		}
	};

	public static JsonNode TryGetKeyValue(this JsonObject v, string k)
		=> v.ContainsKey(k) ? v[k] : null;

	[CBN]
	[LinqTunnel]
	internal static T2 ApplyFunctorInnerPredicate<T, T2>(Func<Func<T2, bool>, T2> functor,
	                                                     Func<T, bool> predicate)
		=> functor(f => f is T e && predicate(e));

	[CBN]
	internal static INode FirstOrDefaultElement(this INodeList nodes, Func<IElement, bool> predicate)
		=> ApplyFunctorInnerPredicate<IElement, INode>(nodes.FirstOrDefault, predicate);

	/*[CBN]
	internal static INode ElemFunctor(Func<Func<INode, bool>, INode> functor, Predicate<IElement> elemPredicate)
		=> ApplyFunctorInnerPredicate(functor, elemPredicate);*/


	[CBN]
	internal static INode FirstOrDefaultElementByClassName(this INodeList nodes, string className)
		=> ApplyFunctorInnerPredicate<IElement, INode>(nodes.FirstOrDefault, e => e.ClassName == className);


	/*
	 * IEnumerable<T>	bound
	 *
	 */


	// Why am I recreating LINQ


	[return: NN]
	internal static IEnumerable<T> TryFindElementsByClassName<T>(Func<Func<T, bool>, IEnumerable<T>> where,
	                                                             Func<T, bool> predicate)
		=> where(predicate);


	public static IEnumerable<string> QueryAllAttribute(this IParentNode doc, string sel, string attr)
		=> doc.QuerySelectorAll(sel)
			.Select(e => e.GetAttribute(attr));

	public static INode RecurseChildren(this INode node, int childNodeIndex, int level)
	{
		/*if (level <= 0) {
			return n;
		}

		return RecurseChildren(n.ChildNodes[childNodeIndex], childNodeIndex, --level);*/
		// return level <= 0 ? n : RecurseChildren(n.ChildNodes[childNodeIndex], childNodeIndex, --level);

		while (true) {
			if (level <= 0)
				return node;

			node  = node.ChildNodes[childNodeIndex];
			level = --level;
		}
	}

}