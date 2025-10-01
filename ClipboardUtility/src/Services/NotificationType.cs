using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardUtility.src.Services;

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error,
    Copy,      // クリップボードコピー時の通知
    Operation  // 文字列操作（Processing）時の通知
}