using System.Collections.Generic;
using System.Globalization;

namespace ClipboardUtility.src.Services;

internal sealed class CultureProvider : ICultureProvider
{
    private static readonly List<CultureInfo> _list = new()
    {
        new CultureInfo("en-US"),
        new CultureInfo("ja-JP")
    };

    public IReadOnlyList<CultureInfo> AvailableCultures { get; } = _list.AsReadOnly();

    public CultureInfo DefaultCulture => CultureInfo.CurrentUICulture;
}