namespace ClipboardUtility.src.Services;

internal interface ITaskTrayService
{
    /// <summary>
    /// タスクトレイアイコンを初期化して表示します。
    /// </summary>
    void Initialize();

    /// <summary>
    /// クリップボードのテキスト情報を更新します。
    /// </summary>
    void UpdateClipboardInfo(string clipboardText);
}