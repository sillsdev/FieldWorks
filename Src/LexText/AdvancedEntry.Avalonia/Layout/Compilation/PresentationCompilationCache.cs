using System;
using System.Collections.Concurrent;
using System.Threading;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

public static class PresentationCompilationCache
{
	private static readonly ConcurrentDictionary<PresentationCompilationCacheKey, Lazy<PresentationLayout>> s_cache = new();

	public static bool TryGet(PresentationCompilationCacheKey key, out PresentationLayout? layout)
	{
		if (s_cache.TryGetValue(key, out var lazy) && lazy.IsValueCreated)
		{
			try
			{
				layout = lazy.Value;
				return true;
			}
			catch (OperationCanceledException)
			{
				// Never treat cancellation as a stable cached result.
				s_cache.TryRemove(key, out _);
			}
		}

		layout = null;
		return false;
	}

	public static PresentationLayout GetOrAdd(PresentationCompilationCacheKey key, Func<PresentationLayout> compile)
	{
		var lazy = s_cache.GetOrAdd(
			key,
			_ => new Lazy<PresentationLayout>(compile, LazyThreadSafetyMode.ExecutionAndPublication));

		try
		{
			return lazy.Value;
		}
		catch (OperationCanceledException)
		{
			// Avoid poisoning the cache with a canceled Lazy that would throw forever.
			s_cache.TryRemove(key, out _);
			throw;
		}
	}

	public static bool Invalidate(PresentationCompilationCacheKey key)
		=> s_cache.TryRemove(key, out _);

	public static void Clear()
		=> s_cache.Clear();
}
