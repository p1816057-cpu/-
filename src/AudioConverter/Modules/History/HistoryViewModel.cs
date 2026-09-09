using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Models;
using AudioConverter.Services;

namespace AudioConverter.Modules.History
{
    public sealed class HistoryViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private List<HistoryEntry> _allEntries;
        private string _searchText = "";
        private string _selectedInputFormat = "全部";
        private string _selectedOutputFormat = "全部";
        private string _sortColumn;
        private bool _sortAscending = true;

        public HistoryViewModel(AppServices services)
        {
            _services = services;
            Title = "历史";
            Entries = new ObservableCollection<HistoryEntry>();
            InputFormatOptions = new ObservableCollection<string>();
            OutputFormatOptions = new ObservableCollection<string>();

            ClearCommand = new RelayCommand(_ => ClearHistory(), _ => !IsEmpty);
            OpenLocationCommand = new RelayCommand(
                p => OpenOutputLocation(p as string),
                p => !string.IsNullOrWhiteSpace(p as string));
            DeleteEntryCommand = new RelayCommand(p => DeleteEntry(p as HistoryEntry));

            ReloadFromStore();
        }

        public ObservableCollection<HistoryEntry> Entries { get; }

        public ObservableCollection<string> InputFormatOptions { get; }

        public ObservableCollection<string> OutputFormatOptions { get; }

        public ICommand ClearCommand { get; }

        public ICommand OpenLocationCommand { get; }

        public ICommand DeleteEntryCommand { get; }

        public bool IsEmpty
        {
            get { return Entries.Count == 0; }
        }

        public string SummaryText
        {
            get { return string.Format("共 {0} 条记录（最多保留 {1} 条）", Entries.Count, _services.Settings.Current.HistoryLimit); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyView();
                }
            }
        }

        public string SelectedInputFormat
        {
            get { return _selectedInputFormat; }
            set
            {
                if (SetProperty(ref _selectedInputFormat, value))
                {
                    ApplyView();
                }
            }
        }

        public string SelectedOutputFormat
        {
            get { return _selectedOutputFormat; }
            set
            {
                if (SetProperty(ref _selectedOutputFormat, value))
                {
                    ApplyView();
                }
            }
        }

        public string SortColumn
        {
            get { return _sortColumn; }
            private set { SetProperty(ref _sortColumn, value); }
        }

        public bool IsSortAscending
        {
            get { return _sortAscending; }
            private set { SetProperty(ref _sortAscending, value); }
        }

        public void ReloadFromStore()
        {
            _allEntries = _services.History.Entries.ToList();
            RefreshFormatOptions();
            if (SortColumn == null)
            {
                SortColumn = "时间";
                IsSortAscending = true;
            }

            ApplyView();
        }

        public void SortBy(string columnHeader)
        {
            if (string.IsNullOrEmpty(columnHeader))
            {
                return;
            }

            if (string.Equals(SortColumn, columnHeader, StringComparison.Ordinal))
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SortColumn = columnHeader;
                IsSortAscending = true;
            }

            ApplyView();
        }

        private void ApplyView()
        {
            IEnumerable<HistoryEntry> query = _allEntries;

            string keyword = (SearchText ?? "").Trim();
            if (keyword.Length > 0)
            {
                query = query.Where(e =>
                    (e.FileName ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.OutputPath ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.ErrorCode ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrEmpty(SelectedInputFormat) && SelectedInputFormat != "全部")
            {
                query = query.Where(e => string.Equals(e.InputFormat, SelectedInputFormat, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(SelectedOutputFormat) && SelectedOutputFormat != "全部")
            {
                query = query.Where(e => string.Equals(e.OutputFormat, SelectedOutputFormat, StringComparison.OrdinalIgnoreCase));
            }

            query = SortQuery(query);

            Entries.Clear();
            foreach (var entry in query)
            {
                Entries.Add(entry);
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SummaryText));
            CommandManager.InvalidateRequerySuggested();
        }

        private IEnumerable<HistoryEntry> SortQuery(IEnumerable<HistoryEntry> query)
        {
            IEnumerable<HistoryEntry> sorted;
            switch (SortColumn)
            {
                case "文件":
                    sorted = query.OrderBy(e => e.FileName);
                    break;
                case "原格式":
                    sorted = query.OrderBy(e => e.InputFormat);
                    break;
                case "转换后格式":
                    sorted = query.OrderBy(e => e.OutputFormat);
                    break;
                case "结果":
                    sorted = query.OrderBy(e => e.Result.ToString());
                    break;
                case "输出大小":
                    sorted = query.OrderBy(e => e.SizeBytes);
                    break;
                case "耗时":
                    sorted = query.OrderBy(e => e.DurationSeconds);
                    break;
                case "错误码/信息":
                    sorted = query.OrderBy(e => e.ErrorCode);
                    break;
                default:
                    sorted = query.OrderBy(e => e.Timestamp);
                    break;
            }

            return IsSortAscending ? sorted : sorted.Reverse();
        }

        private void RefreshFormatOptions()
        {
            InputFormatOptions.Clear();
            OutputFormatOptions.Clear();
            InputFormatOptions.Add("全部");
            OutputFormatOptions.Add("全部");

            foreach (var input in _allEntries
                .Select(e => e.InputFormat)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                InputFormatOptions.Add(input);
            }

            foreach (var output in _allEntries
                .Select(e => e.OutputFormat)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                OutputFormatOptions.Add(output);
            }
        }

        private void ClearHistory()
        {
            if (!ToastService.Confirm("确定要清空全部历史记录吗？此操作不可撤销。", "清空历史"))
            {
                return;
            }

            _services.History.Clear();
            ReloadFromStore();
            ToastService.Instance.Show("历史记录已清空", ToastKind.Success);
        }

        private void OpenOutputLocation(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
            {
                ToastService.Instance.Show("输出文件不存在或已被移动", ToastKind.Warning);
                return;
            }

            try
            {
                ExplorerHelper.OpenInExplorer(outputPath);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("无法打开位置：" + ex.Message, ToastKind.Error);
            }
        }

        private void DeleteEntry(HistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (!ToastService.Confirm(
                    string.Format("确定删除这条历史记录吗？\n{0}", entry.FileName),
                    "删除历史记录"))
            {
                return;
            }

            _services.History.Delete(entry);
            ReloadFromStore();
            ToastService.Instance.Show("该条历史记录已删除", ToastKind.Success);
        }
    }
}
