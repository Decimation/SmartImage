// Read Stanton SmartImage.Lib LogUtil.cs
// 2023-02-14 @ 12:31 AM

using Microsoft.Extensions.Logging;

namespace SmartImage.Lib.Utilities;

internal static class LogUtil
{
    internal static readonly ILoggerFactory Factory =
        LoggerFactory.Create(builder => builder.AddDebug());
}