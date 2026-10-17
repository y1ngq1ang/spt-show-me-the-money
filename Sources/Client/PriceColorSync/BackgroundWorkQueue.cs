using System;
using System.Collections.Generic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.PriceColorSync;

internal sealed class BackgroundWorkQueue<T>
{
    private readonly Queue<T> _pending = new();
    private readonly HashSet<T> _queued = new();

    public int Count => _pending.Count;

    public void Enqueue(T item)
    {
        if (_queued.Add(item))
            _pending.Enqueue(item);
    }

    public int Process(int maximum, Action<T> action)
    {
        return Process(maximum, action, null);
    }

    public int Process(int maximum, Action<T> action, Func<bool>? withinBudget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);

        // Only work present at entry is eligible; requeueing cannot spin in one frame.
        int count = Math.Min(maximum, _pending.Count);
        int processed = 0;

        while (processed < count)
        {
            // Always allow one item so an expensive calculation cannot starve the
            // queue. Check between items; an individual action cannot be interrupted.
            if (processed > 0 && withinBudget != null && !withinBudget())
                break;

            T item = _pending.Dequeue();
            _queued.Remove(item);
            action(item);
            processed++;
        }

        return processed;
    }

    public void Clear()
    {
        _pending.Clear();
        _queued.Clear();
    }
}
