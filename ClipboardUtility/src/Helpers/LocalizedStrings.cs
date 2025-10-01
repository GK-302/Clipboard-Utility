using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ClipboardUtility.src.Properties;

namespace ClipboardUtility.src.Helpers;

// リソースの公開ラッパー（動的切替対応）
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    public static LocalizedStrings Instance { get; } = new LocalizedStrings();

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizedStrings() { }

    public string SettingTitle => Resources.SettingTitle;
    public string WindowTitle => Resources.WindowTitle;
    public string NotificationFormat_CopiedWords => Resources.NotificationFormat_CopiedWords;
    public string NotificationFormat_LineBreakRemoved => Resources.NotificationFormat_LineBreakRemoved;
    public string NotificationFormat_WhitespaceNormalized => Resources.NotificationFormat_WhitespaceNormalized;
    public string NotificationFormat_NormalizeUnicode => Resources.NotificationFormat_NormalizeUnicode;
    public string NotificationFormat_RemoveDiacritics => Resources.NotificationFormat_RemoveDiacritics;
    public string NotificationFormat_RemovePunctuation => Resources.NotificationFormat_RemovePunctuation;
    public string NotificationFormat_RemoveControlChars => Resources.NotificationFormat_RemoveControlChars;
    public string NotificationFormat_RemoveUrls => Resources.NotificationFormat_RemoveUrls;
    public string NotificationFormat_RemoveEmails => Resources.NotificationFormat_RemoveEmails;
    public string NotificationFormat_RemoveHtmlTags => Resources.NotificationFormat_RemoveHtmlTags;
    public string NotificationFormat_StripMarkdownLinks => Resources.NotificationFormat_StripMarkdownLinks;
    public string NotificationFormat_ConvertTabsToSpaces => Resources.NotificationFormat_ConvertTabsToSpaces;
    public string NotificationFormat_Trim => Resources.NotificationFormat_Trim;
    public string NotificationFormat_ToUpper => Resources.NotificationFormat_ToUpper;
    public string NotificationFormat_ToLower => Resources.NotificationFormat_ToLower;
    public string NotificationFormat_ToTitleCase => Resources.NotificationFormat_ToTitleCase;
    public string NotificationFormat_ToPascalCase => Resources.NotificationFormat_ToPascalCase;
    public string NotificationFormat_ToCamelCase => Resources.NotificationFormat_ToCamelCase;
    public string NotificationFormat_Truncate => Resources.NotificationFormat_Truncate;
    public string NotificationFormat_JoinLinesWithSpace => Resources.NotificationFormat_JoinLinesWithSpace;
    public string NotificationFormat_RemoveDuplicateLines => Resources.NotificationFormat_RemoveDuplicateLines;
    public string NotificationFormat_CollapseWhitespace => Resources.NotificationFormat_CollapseWhitespace;
    public string SaveText => Resources.SaveText;
    public string CancelText => Resources.CancelText;
    public string ShowCopyNotificationTitle => Resources.ShowCopyNotification;
    public string ShowOperationNotificationTitle => Resources.ShowOperationNotification;
    public string ProcessingModeTitle => Resources.ProcessingMode;
    public string NotificationOffsetXTitle => Resources.NotificationOffsetX;
    public string NotificationOffsetYTitle => Resources.NotificationOffsetY;
    public string NotificationMarginTitle => Resources.NotificationMargin;
    public string NotificationMinWidthTitle => Resources.NotificationMinWidth;
    public string NotificationMaxWidthTitle => Resources.NotificationMaxWidth;
    public string NotificationMinHeightTitle => Resources.NotificationMinHeight;
    public string NotificationMaxHeightTitle => Resources.NotificationMaxHeight;
    public string ShowCopyNotification => Resources.ShowCopyNotification;
    public string ShowOperationNotification => Resources.ShowOperationNotification;
    public string ProcessingMode => Resources.ProcessingMode;
    public string OpenSettingText => Resources.OpenSetting;
    public string ExitText => Resources.Exit;

    public void ChangeCulture(CultureInfo culture)
    {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        CultureInfo.CurrentUICulture = culture;
        // 各プロパティ名で通知（簡易）
        OnPropertyChanged(nameof(SettingTitle));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(NotificationFormat_CopiedWords));
        OnPropertyChanged(nameof(NotificationFormat_LineBreakRemoved));
        OnPropertyChanged(nameof(NotificationFormat_WhitespaceNormalized));
        OnPropertyChanged(nameof(NotificationFormat_NormalizeUnicode));
        OnPropertyChanged(nameof(NotificationFormat_RemoveDiacritics));
        OnPropertyChanged(nameof(NotificationFormat_RemovePunctuation));
        OnPropertyChanged(nameof(NotificationFormat_RemoveControlChars));
        OnPropertyChanged(nameof(NotificationFormat_RemoveUrls));
        OnPropertyChanged(nameof(NotificationFormat_RemoveEmails));
        OnPropertyChanged(nameof(NotificationFormat_RemoveHtmlTags));
        OnPropertyChanged(nameof(NotificationFormat_StripMarkdownLinks));
        OnPropertyChanged(nameof(NotificationFormat_ConvertTabsToSpaces));
        OnPropertyChanged(nameof(NotificationFormat_Trim));
        OnPropertyChanged(nameof(NotificationFormat_ToUpper));
        OnPropertyChanged(nameof(NotificationFormat_ToLower));
        OnPropertyChanged(nameof(NotificationFormat_ToTitleCase));
        OnPropertyChanged(nameof(NotificationFormat_ToPascalCase));
        OnPropertyChanged(nameof(NotificationFormat_ToCamelCase));
        OnPropertyChanged(nameof(NotificationFormat_Truncate));
        OnPropertyChanged(nameof(NotificationFormat_JoinLinesWithSpace));
        OnPropertyChanged(nameof(NotificationFormat_RemoveDuplicateLines));
        OnPropertyChanged(nameof(NotificationFormat_CollapseWhitespace));
        OnPropertyChanged(nameof(SaveText));
        OnPropertyChanged(nameof(CancelText));
        OnPropertyChanged(nameof(ShowCopyNotificationTitle));
        OnPropertyChanged(nameof(ShowOperationNotificationTitle));
        OnPropertyChanged(nameof(ProcessingModeTitle));
        OnPropertyChanged(nameof(NotificationOffsetXTitle));
        OnPropertyChanged(nameof(NotificationOffsetYTitle));
        OnPropertyChanged(nameof(NotificationMarginTitle));
        OnPropertyChanged(nameof(NotificationMinWidthTitle));
        OnPropertyChanged(nameof(NotificationMaxWidthTitle));
        OnPropertyChanged(nameof(NotificationMinHeightTitle));
        OnPropertyChanged(nameof(NotificationMaxHeightTitle));
        OnPropertyChanged(nameof(ShowCopyNotification));
        OnPropertyChanged(nameof(ShowOperationNotification));
        OnPropertyChanged(nameof(ProcessingMode));
        OnPropertyChanged(nameof(OpenSettingText));
        OnPropertyChanged(nameof(ExitText));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}