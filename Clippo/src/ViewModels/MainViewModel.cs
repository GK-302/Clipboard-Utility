using Clippo.src.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Clippo.src.ViewModels;

/// <summary>
/// メインウィンドウのViewModel。UIのロジックと状態を管理します。
/// INotifyPropertyChangedを実装し、プロパティの変更をUIに通知します。
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private string _clipboardHistory = "クリップボードの監視を開始しました...何かコピーしてみてください。\n";

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// UIに表示するためのクリップボード履歴
    /// </summary>
    public string ClipboardHistory
    {
        get => _clipboardHistory;
        set
        {
            _clipboardHistory = value;
            OnPropertyChanged(); // UIに変更を通知
        }
    }

    public MainViewModel()
    {
        _clipboardService = new ClipboardService();
    }

    /// <summary>
    /// ClipboardServiceの監視を開始するメソッド。
    /// </summary>
    public void Initialize(System.Windows.Window window)
    {
        _clipboardService.StartMonitoring(window);
        // ClipboardUpdatedイベントを購読し、イベント発生時に実行するメソッドを登録
        _clipboardService.ClipboardUpdated += OnClipboardUpdated;
    }

    /// <summary>
    /// ClipboardServiceから通知を受け取ったときの処理
    /// </summary>
    private void OnClipboardUpdated(object sender, string newText)
    {
        // ここで改行削除などのメインロジックを実装する
        // string processedText = newText.Replace("\r\n", " ").Replace("\n", " ");
        // Clipboard.SetText(processedText);

        // UI表示用の履歴を更新
        string log = $"[{DateTime.Now:HH:mm:ss}] コピーされました: \"{newText}\"\n";
        ClipboardHistory += log;
    }

    /// <summary>
    /// アプリケーション終了時にリソースを解放する
    /// </summary>
    public void Cleanup()
    {
        _clipboardService?.Dispose();
    }

    // INotifyPropertyChangedの実装
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}