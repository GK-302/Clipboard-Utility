using System;

namespace ClipboardUtility.src.Services;

public interface IAppRestartService
{
    /// <summary>
    /// アプリを再起動します。引数は必要に応じて渡せます。
    /// </summary>
    void Restart(string? args = null);
}