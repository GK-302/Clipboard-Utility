using ClipboardUtility.src.Helpers;
using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ClipboardUtility.src.ViewModels
{
    internal class WelcomeWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _runAtStartup;

        public bool RunAtStartup
        {
            get => _runAtStartup;
            set
            {
                if (_runAtStartup != value)
                {
                    _runAtStartup = value;
                    try
                    {
                        var settings = SettingsService.Instance.Current ?? new AppSettings();
                        settings.RunAtStartup = value;
                        SettingsService.Instance.Save(settings);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogException(ex, "WelcomeWindowViewModel: Save RunAtStartup failed");
                    }
                    OnPropertyChanged();
                }
            }
        }

        public WelcomeWindowViewModel()
        {
            // ���p�\�ȃJ���`���ꗗ
            AvailableCultures = new List<CultureInfo> { new("en-US"), new("ja-JP") };

            // ���݂̐ݒ肩��J���`�����擾
            var settings = SettingsService.Instance.Current;
            var cultureName = settings?.CultureName ?? CultureInfo.CurrentUICulture.Name;
            
            // �����I��
            _selectedCulture = AvailableCultures.FirstOrDefault(c => c.Name == cultureName)
                              ?? CultureInfo.CurrentUICulture;

            // RunAtStartup ������
            _runAtStartup = settings?.RunAtStartup ?? false;

            // �ݒ�ύX�̊Ď�
            SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, AppSettings newSettings)
        {
            if (newSettings == null) return;
            _runAtStartup = newSettings.RunAtStartup;
            OnPropertyChanged(nameof(RunAtStartup));
        }

        public IList<CultureInfo> AvailableCultures { get; }

        // �I�𒆂̃J���`���iUI��ComboBox�Ƀo�C���h�j
        private CultureInfo _selectedCulture;
        public CultureInfo SelectedCulture
        {
            get => _selectedCulture;
            set
            {
                if (value == null) return;
                if (_selectedCulture?.Name != value.Name)
                {
                    _selectedCulture = value;
                    // �����ɃJ���`����؂�ւ���iUI�X�V�p�j
                    ApplyCulture(value);
                    // �ݒ�ɕۑ�
                    SaveCultureSetting(value);
                    OnPropertyChanged();
                }
            }
        }

        private void ApplyCulture(CultureInfo ci)
        {
            if (ci == null) return;

            // �v���Z�X/�X���b�h�S�̂̊���J���`����ݒ�
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            // LocalizedStrings �ɒʒm���ăo�C���h�ς݂̃��x�����X�V
            LocalizedStrings.Instance.ChangeCulture(ci);
        }

        private void SaveCultureSetting(CultureInfo ci)
        {
            try
            {
                // ���݂̐ݒ���擾
                var settings = SettingsService.Instance.Current ?? new AppSettings();
                
                // �J���`�������X�V
                settings.CultureName = ci.Name;
                
                // �ݒ��ۑ�
                SettingsService.Instance.Save(settings);
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "WelcomeWindowViewModel.SaveCultureSetting");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}