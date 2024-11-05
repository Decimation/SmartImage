// Read Stanton SmartImage.Lib NodeHelper.cs
// 2023-01-13 @ 11:37 PM

using System.Diagnostics;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Flurl.Http;
using Flurl.Http.Configuration;
using JetBrains.Annotations;

// ReSharper disable AnnotateNotNullParameter

namespace SmartImage.Lib.Utilities;

internal static class NodeUtil
{

	public static JsonNode TryGetKeyValue(this JsonObject v, string k)
	{
		return v.ContainsKey(k) ? v[k] : null;
	}

	[CBN]
	[LinqTunnel]
	internal static T2 ApplyFunctorInnerPredicate<T, T2>(Func<Func<T2, bool>, T2> functor,
	                                                     Func<T, bool> predicate)
	{
		return functor(f => f is T e && predicate(e));
	}

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
	{
		return doc.QuerySelectorAll(sel)
			.Select(e => e.GetAttribute(attr));
	}

	public static INode RecurseChildren(this INode n, int idx, int c)
	{
		if (c <= 0) {
			return n;
		}

		return RecurseChildren(n.ChildNodes[idx], idx, --c);
	}

}