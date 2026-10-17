using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

// Factory/network hooks can run off-thread. Only enqueue weak references there;
// the caller runs preparation on Unity's main thread under its frame budget.
internal sealed class ItemPreparationQueue<T> where T : class
{
    private sealed class Entry
    {
        public WeakReference? Item;
        public bool Pending;
        public long Revision;
    }

    private readonly object _gate = new();
    private ConditionalWeakTable<T, Entry> _entries = new();
    private readonly List<Entry> _known = new();
    private readonly Queue<Entry> _pending = new();

    public int Count
    {
        get
        {
            lock (_gate)
                return _pending.Count;
        }
    }

    public void Schedule(T item, bool changed)
    {
        if (item == null || InventorySimulationScope.Active)
            return;

        lock (_gate)
        {
            if (!_entries.TryGetValue(item, out Entry? entry))
            {
                entry = new Entry { Item = new WeakReference(item) };
                _entries.Add(item, entry);
                _known.Add(entry);
            }

            if (changed)
                entry.Revision++;

            if (!entry.Pending)
            {
                entry.Pending = true;
                _pending.Enqueue(entry);
            }
        }
    }

    public long Revision(T item)
    {
        lock (_gate)
            return _entries.TryGetValue(item, out Entry? entry) ? entry.Revision : 0;
    }

    public void ScheduleAll()
    {
        lock (_gate)
        {
            int live = 0;

            for (int i = 0; i < _known.Count; i++)
            {
                Entry entry = _known[i];
                if (entry.Item == null || !entry.Item.IsAlive)
                    continue;

                _known[live++] = entry;

                if (!entry.Pending)
                {
                    entry.Pending = true;
                    _pending.Enqueue(entry);
                }
            }

            if (live < _known.Count)
                _known.RemoveRange(live, _known.Count - live);
        }
    }

    public int Process(int maximum, Action<T> prepare, Func<bool>? withinBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);

        int count;
        lock (_gate)
            count = Math.Min(maximum, _pending.Count);

        int processed = 0;

        while (processed < count)
        {
            if (processed > 0 && withinBudget != null && !withinBudget())
                break;

            T? item;
            lock (_gate)
            {
                Entry entry = _pending.Dequeue();
                entry.Pending = false;
                item = entry.Item?.Target as T;
            }

            if (item != null)
                prepare(item);

            processed++;
        }

        return processed;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _pending.Clear();
            _known.Clear();
            _entries = new ConditionalWeakTable<T, Entry>();
        }
    }
}
