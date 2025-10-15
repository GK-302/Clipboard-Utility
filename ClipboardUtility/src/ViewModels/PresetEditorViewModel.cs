using ClipboardUtility.src.Models;
using ClipboardUtility.src.Services;
using ClipboardUtility.src.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClipboardUtility.src.ViewModels
{
    internal class PresetEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public PresetEditorViewModel(ProcessingPreset preset)
        {
            // Work on a clone so callers decide whether to persist
            EditingPreset = preset.Clone();
            Steps = new ObservableCollection<ProcessingStep>(EditingPreset.Steps.OrderBy(s => s.Order));

            // Available modes for UI
            ProcessingModes = System.Enum.GetValues(typeof(ProcessingMode)).Cast<ProcessingMode>().ToList();

            // 初期選択モード
            SelectedModeToAdd = ProcessingMode.None;

            AddStepCommand = new RelayCommand(ExecuteAddStep);
            AddSelectedModeCommand = new RelayCommand(ExecuteAddSelectedMode);
            RemoveStepCommand = new RelayCommand(ExecuteRemoveStep);
            MoveUpCommand = new RelayCommand(ExecuteMoveUp);
            MoveDownCommand = new RelayCommand(ExecuteMoveDown);
        }

        public ProcessingPreset EditingPreset { get; }

        public ObservableCollection<ProcessingStep> Steps { get; }

        public System.Collections.Generic.IList<ProcessingMode> ProcessingModes { get; }

        private ProcessingStep? _selectedStep;
        public ProcessingStep? SelectedStep
        {
            get => _selectedStep;
            set { if (_selectedStep != value) { _selectedStep = value; OnPropertyChanged(); } }
        }

        // ComboBox で選択された ProcessingMode
        private ProcessingMode _selectedModeToAdd;
        public ProcessingMode SelectedModeToAdd
        {
            get => _selectedModeToAdd;
            set
            {
                if (_selectedModeToAdd != value)
                {
                    _selectedModeToAdd = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddStepCommand { get; }
        public ICommand AddSelectedModeCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        private void ExecuteAddStep(object? _) 
        {
            var order = Steps.Any() ? Steps.Max(s => s.Order) + 1 : 0;
            var step = new ProcessingStep(order, ProcessingMode.None, true);
            Steps.Add(step);
            SelectedStep = step;
            RefreshOrderIndexes();
        }

        private void ExecuteAddSelectedMode(object? _)
        {
            // ComboBox で選択されたモードを追加
            var order = Steps.Any() ? Steps.Max(s => s.Order) + 1 : 0;
            var step = new ProcessingStep(order, SelectedModeToAdd, true);
            Steps.Add(step);
            SelectedStep = step;
            RefreshOrderIndexes();
        }

        private void ExecuteRemoveStep(object? _) 
        {
            if (SelectedStep == null) return;
            Steps.Remove(SelectedStep);
            SelectedStep = Steps.FirstOrDefault();
            RefreshOrderIndexes();
        }

        private void ExecuteMoveUp(object? _) 
        {
            if (SelectedStep == null) return;
            var idx = Steps.IndexOf(SelectedStep);
            if (idx <= 0) return;
            Steps.Move(idx, idx - 1);
            RefreshOrderIndexes();
        }

        private void ExecuteMoveDown(object? _) 
        {
            if (SelectedStep == null) return;
            var idx = Steps.IndexOf(SelectedStep);
            if (idx < 0 || idx >= Steps.Count - 1) return;
            Steps.Move(idx, idx + 1);
            RefreshOrderIndexes();
        }

        private void RefreshOrderIndexes()
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].Order = i;
            }
        }

        public ProcessingPreset GetResultingPreset()
        {
            EditingPreset.Steps = Steps.ToList();
            // ensure orders
            for (int i = 0; i < EditingPreset.Steps.Count; i++) EditingPreset.Steps[i].Order = i;
            return EditingPreset;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
