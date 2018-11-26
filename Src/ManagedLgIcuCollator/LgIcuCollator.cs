// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using Icu;
using Icu.Collation;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.ViewsInterfaces;
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
		#region Member variables

		private ILgWritingSystemFactory m_qwsf;
		private string m_stuLocale;
		private Collator m_collator;

		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ManagedLgIcuCollator()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				DoneCleanup();
			}
			IsDisposed = true;
		}
		#endregion

		protected void EnsureCollator()
		{
			if (m_collator != null)
				return;

			string icuLocale = new Locale(m_stuLocale).Name;
			m_collator = Collator.Create(icuLocale);
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
			throw new NotImplementedException();
		}

		public void SortKeyRgch(string _ch, int cchIn, LgCollatingOptions colopt, int cchMaxOut, ArrayPtr _chKey, out int _cchOut)
		{
			throw new NotImplementedException();
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
			byte[] key1 = saValue1 as byte[];
			byte[] key2 = saValue2 as byte[];

			EnsureCollator();

			if (key1 == null)
			{
				return 1;
			}
			if (key2 == null)
			{
				return -1;
			}

			int maxlen = key1.Length > key2.Length ? key1.Length : key2.Length;
			for (int i = 0; i < maxlen; ++i)
			{
				if (key1[i] > key2[i])
					return 1;
				if (key2[i] > key1[i])
					return -1;
			}

			if (key1.Length > key2.Length)
				return 1;
			if (key2.Length > key1.Length)
				return -1;

			return 0;
		}


		public void Open(string bstrLocale)
		{
			if (m_collator != null)
				DoneCleanup();

			m_stuLocale = bstrLocale;

			EnsureCollator();
		}


		public void Close()
		{
			if (m_collator != null)
			{
				DoneCleanup();
			}
			m_stuLocale = String.Empty;
		}


		public ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_qwsf; }
			set { m_qwsf = value; }
		}

		#endregion
	}
}
