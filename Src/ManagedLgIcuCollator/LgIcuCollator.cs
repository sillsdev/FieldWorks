// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using Icu;
using Icu.Collation;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Language
{
	/// <summary>
	/// Direct port of the C++ class LgIcuCollator
	/// </summary>
	[Serializable]
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("e771361c-ff54-4120-9525-98a0b7a9accf")]
	public class ManagedLgIcuCollator : ILgCollatingEngine, IDisposable
	{
		private string m_stuLocale;
		private Collator m_collator;

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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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

		protected void EnsureCollator()
		{
			if (m_collator != null)
			{
				return;
			}
			var icuLocale = new Locale(m_stuLocale).Name;
			m_collator = Collator.Create(icuLocale, Collator.Fallback.FallbackAllowed);
		}

		internal void DoneCleanup()
		{
			if (m_collator != null)
			{
				m_collator.Dispose();
				m_collator = null;
			}
		}

		#region ILgCollatingEngine implementation

		public string get_SortKey(string bstrValue, LgCollatingOptions colopt)
		{
			throw new NotSupportedException();
		}

		public void SortKeyRgch(string _ch, int cchIn, LgCollatingOptions colopt, int cchMaxOut, ArrayPtr chKey, out int cchOut)
		{
			throw new NotSupportedException();
		}


		public int Compare(string bstrValue1, string bstrValue2, LgCollatingOptions colopt)
		{
			EnsureCollator();
			var key1 = m_collator.GetSortKey(bstrValue1).KeyData;
			var key2 = m_collator.GetSortKey(bstrValue2).KeyData;

			return CompareVariant(key1, key2, colopt);
		}


		public object get_SortKeyVariant(string bstrValue, LgCollatingOptions colopt)
		{
			EnsureCollator();
			var sortKey = m_collator.GetSortKey(bstrValue).KeyData;

			return sortKey;
		}


		public int CompareVariant(object saValue1, object saValue2, LgCollatingOptions colopt)
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


		public void Open(string bstrLocale)
		{
			if (m_collator != null)
			{
				DoneCleanup();
			}
			m_stuLocale = bstrLocale;

			EnsureCollator();
		}


		public void Close()
		{
			if (m_collator != null)
			{
				DoneCleanup();
			}
			m_stuLocale = string.Empty;
		}

		public ILgWritingSystemFactory WritingSystemFactory { get; set; }

		#endregion
	}
}