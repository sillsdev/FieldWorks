using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;

public sealed class StagedEntryState
{
	public StagedEntryState(string rootClass)
	{
		RootClass = rootClass ?? throw new ArgumentNullException(nameof(rootClass));
		Root = new StagedObjectState(rootClass, "$root");
	}

	public string RootClass { get; }
	public StagedObjectState Root { get; }
}

public sealed class StagedObjectState
{
	public StagedObjectState(string className, string debugId)
	{
		ClassName = className;
		DebugId = debugId;
	}

	public string ClassName { get; }
	public string DebugId { get; }

	public Dictionary<string, string?> Fields { get; }
		= new(StringComparer.Ordinal);

	public Dictionary<string, StagedObjectState> Objects { get; }
		= new(StringComparer.Ordinal);

	public Dictionary<string, StagedSequenceState> Sequences { get; }
		= new(StringComparer.Ordinal);

	public StagedObjectState GetOrCreateObject(string field, string className)
	{
		if (!Objects.TryGetValue(field, out var obj))
		{
			obj = new StagedObjectState(className, $"{DebugId}.{field}");
			Objects[field] = obj;
		}

		return obj;
	}

	public StagedSequenceState GetOrCreateSequence(string field, string itemClass)
	{
		if (!Sequences.TryGetValue(field, out var seq))
		{
			seq = new StagedSequenceState(itemClass, $"{DebugId}.{field}");
			Sequences[field] = seq;
		}

		return seq;
	}
}

public sealed class StagedSequenceState
{
	public StagedSequenceState(string itemClass, string debugId)
	{
		ItemClass = itemClass;
		DebugId = debugId;
	}

	public string ItemClass { get; }
	public string DebugId { get; }
	private readonly Dictionary<int, StagedObjectState> _items = new();

	/// <summary>
	/// Total number of items in the sequence. Items may be materialized lazily.
	/// </summary>
	public int Count { get; private set; }

	public bool TryGetItem(int index, out StagedObjectState? item)
	{
		if (index < 0 || index >= Count)
		{
			item = null;
			return false;
		}

		return _items.TryGetValue(index, out item);
	}

	public void SetCount(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		Count = count;
	}

	public StagedObjectState EnsureItem(int index)
	{
		if (index < 0)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (index >= Count)
			Count = index + 1;

		if (_items.TryGetValue(index, out var existing))
			return existing;

		var created = new StagedObjectState(ItemClass, $"{DebugId}[{index}]");
		_items[index] = created;
		return created;
	}
}