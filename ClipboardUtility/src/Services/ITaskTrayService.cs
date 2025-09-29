using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardUtility.src.Services;

internal interface ITaskTrayService
{
    /// <summary>
    /// タスクトレイアイコンを初期化して表示します。
    /// </summary>
    void Initialize();
}