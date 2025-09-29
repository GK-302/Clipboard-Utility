﻿using Clippo.Services;
using Clippo.src.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Clippo.src.Properties;
using System.Windows.Media;
using System.Diagnostics;

namespace Clippo.src.ViewModels;

/// <summary>
/// メインウィンドウのViewModel。UIのロジックと状態を管理します。
/// INotifyPropertyChangedを実装し、プロパティの変更をUIに通知します。
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ClipboardService _clipboardService;
    private string _ClipboardLabel = string.Empty;
    private TextProcessingService _textProcessingService = new();
    private NotificationsService _notificationsService = new();

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// UIに表示するためのクリップボード履歴
    /// </summary>

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
        int charCount = _textProcessingService.CountCharacters(newText);
        string formattedText;
        try 
        { 
            formattedText = string.Format(Resources.NotificationFormat_CopiedWords, charCount);
        } catch (FormatException ex) 
        { 
            // フォーマットエラーが発生した場合の処理（例: ログ出力など）
            Debug.WriteLine($"Format error: {ex.Message}");
            formattedText = $"Copied {charCount} words"; // フォールバックのメッセージ
        }
        _notificationsService.ShowNotification(formattedText);
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