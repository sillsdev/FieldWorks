// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using Icu;
using Icu.Collation;

namespace LanguageExplorer
{
	/// <summary />
	internal sealed class ManagedLgIcuCollator : IDisposable
	{
		private string _locale;
		private Collator _collator;

		/// <summary />
		~ManagedLgIcuCollator()
		{
			Dispose(false);
		}

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}
			if (disposing)
			{
				// dispose managed objects
				DoneCleanup();
			}
			IsDisposed = true;
		}

		private void EnsureCollator()
		{
			if (_collator != null)
			{
				return;
			}
			var icuLocale = new Locale(_locale).Name;
			_collator = Collator.Create(icuLocale, Collator.Fallback.FallbackAllowed);
		}

		private void DoneCleanup()
		{
			if (_collator != null)
			{
				_collator.Dispose();
				_collator = null;
			}
		}

		internal int Compare(string value1, string value2)
		{
			EnsureCollator();
			return CompareVariant(_collator.GetSortKey(value1).KeyData, _collator.GetSortKey(value2).KeyData);
		}

		internal int CompareVariant(object saValue1, object saValue2)
		{
			EnsureCollator();

			if (!(saValue1 is byte[] key1))
			{
				return 1;
			}
			if (!(saValue2 is byte[] key2))
			{
				return -1;
			}
			var maxlen = key1.Length > key2.Length ? key1.Length : key2.Length;
			for (var i = 0; i < maxlen; ++i)
			{
				if (key1[i] > key2[i])
				{
					return 1;
				}
				if (key2[i] > key1[i])
				{
					return -1;
				}
			}
			return key1.Length > key2.Length ? 1 : key2.Length > key1.Length ? -1 : 0;
		}

		internal object SortKeyVariant(string value)
		{
			EnsureCollator();
			var sortKey = _collator.GetSortKey(value).KeyData;

			return sortKey;
		}

		internal void Open(string locale)
		{
			if (_collator != null)
			{
				DoneCleanup();
			}
			_locale = locale;

			EnsureCollator();
		}

		internal void Close()
		{
			if (_collator != null)
			{
				DoneCleanup();
			}
			_locale = string.Empty;
		}
	}
}