using System.Collections.Generic;
using System.Globalization;

namespace ClipboardUtility.src.Services;

public interface ICultureProvider
{
    IReadOnlyList<CultureInfo> AvailableCultures { get; }
    CultureInfo DefaultCulture { get; }
}