// Read Stanton SmartImage.Lib NodeHelper.cs
// 2023-01-13 @ 11:37 PM

using AngleSharp.Dom;

namespace SmartImage.Lib.Utilities;

internal static class NodeHelper
{
    internal static INode TryFindElementByClassName(this INodeList nodes, string className)
    {
        return nodes.FirstOrDefault(f => f is IElement e && e.ClassName == className);
    }
}