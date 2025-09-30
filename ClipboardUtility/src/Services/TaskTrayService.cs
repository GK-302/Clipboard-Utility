using ClipboardUtility.src.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO; // 追加: NotifyIcon, MouseEventArgs, MouseButtons, ContextMenuStrip 用


namespace ClipboardUtility.src.Services;

public class TaskTrayService : ITaskTrayService, IDisposable
{
    private NotifyIcon _notifyIcon;


    public void Initialize()
    {
        System.IO.Stream iconStream;
        try
        {
            var iconURI = new Uri("pack://application:,,,/src/Assets/drawing_1.ico");
            iconStream = System.Windows.Application.GetResourceStream(iconURI).Stream;
        } catch (Exception ex)
        {
            Debug.WriteLine("failed to load icon file: " + ex.Message);
            return;
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, OnShowClicked);
        menu.Items.Add("Exit", null, OnExitClicked);

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = new Icon(iconStream),
            Text = "Clipboard Utility",
            ContextMenuStrip = menu
        };

        _notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(OnNotifyIconClicked);
    }

    // 修正: OnNotifyIconClicked のシグネチャを MouseEventHandler に一致させる
    private void OnNotifyIconClicked(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        // 左クリックされたら、ViewModelのShowWindowCommandを実行
        if (e.Button == MouseButtons.Left)
        {
            // ここにコマンド実行処理を記述
        }
    }

    private void OnShowClicked(object sender, EventArgs e)
    {
        // メニューの「表示」がクリックされたら、ViewModelのShowWindowCommandを実行
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        // メニューの「終了」がクリックされたら、ViewModelのExitApplicationCommandを実行
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}