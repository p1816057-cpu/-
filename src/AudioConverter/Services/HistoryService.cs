using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioConverter.Data;
using AudioConverter.Models;

namespace AudioConverter.Services
{
    public sealed class HistoryService
    {
        private readonly string _historyPath;
        private readonly List<HistoryEntry> _entries;
        private readonly object _sync = new object();
        private readonly int _limit;

        public HistoryService(string storageRoot, int limit)
        {
            _historyPath = Path.Combine(storageRoot, "history.json");
            _limit = Math.Max(1, limit);
            _entries = JsonFile.Load<List<HistoryEntry>>(_historyPath) ?? new List<HistoryEntry>();
            if (_entries.Count > _limit)
            {
                _entries.RemoveRange(_limit, _entries.Count - _limit);
            }
        }

        public IReadOnlyList<HistoryEntry> Entries
        {
            get
            {
                lock (_sync)
                {
                    return _entries.ToList();
                }
            }
        }

        public void Add(HistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (_sync)
            {
                _entries.Insert(0, entry);
                while (_entries.Count > _limit)
                {
                    _entries.RemoveAt(_entries.Count - 1);
                }

                SaveCore();
            }
        }

        public void AddRange(IEnumerable<HistoryEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var entry in entries)
                {
                    _entries.Insert(0, entry);
                }

                while (_entries.Count > _limit)
                {
                    _entries.RemoveAt(_entries.Count - 1);
                }

                SaveCore();
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _entries.Clear();
                SaveCore();
            }
        }

        private void SaveCore()
        {
            try
            {
                JsonFile.Save(_historyPath, _entries);
            }
            catch
            {
                // 历史记录写入失败不影响转换主流程
            }
        }
    }
}
