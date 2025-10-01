using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardUtility.src.Services;

/// <summary>
/// アプリ設定の簡易ハードコード実装（将来的にファイル／ユーザー設定から読み込む）
/// 今はここで想定される設定値をハードコードして切り替えられるようにする。
/// </summary>
internal class AppSettings
{
    // とりあえずの想定設定値（将来 UI やファイルで変更可能にする）
    public ProcessingMode ClipboardProcessingMode { get; set; } = ProcessingMode.RemoveLineBreaks;
}
