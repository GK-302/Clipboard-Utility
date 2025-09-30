// NotificationViewModel.cs
using System.ComponentModel;

namespace ClipboardUtility.src.ViewModels;

/// <summary>
/// 通知用のViewModel。通知メッセージを保持し、変更をUIに通知します。
/// </summary>
///     
// 通知の種類などを定義するenum（例）
public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}
internal class NotificationViewModel : INotifyPropertyChanged
{
    private string _notificationMessage;
    private string _NotificationsimultaneousMessage;

    // UIがバインドする対象となるプロパティ
    public string NotificationMessage
    {
        get => _notificationMessage;
        set
        {
            _notificationMessage = value;
            OnPropertyChanged(nameof(NotificationMessage)); // "NotificationMessage"が変更されたことをUIに通知
        }
    }
    public string NotificationsimultaneousMessage
    {
        get => _NotificationsimultaneousMessage;
        set
        {
            _NotificationsimultaneousMessage = value;
            OnPropertyChanged(nameof(NotificationsimultaneousMessage));
        }
    }



    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}