using System;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;

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
		private IntPtr m_pCollator;

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
			if (m_pCollator != IntPtr.Zero)
				return;

			string icuLocale = Icu.GetName(m_stuLocale);
			m_pCollator = Icu.OpenCollator(icuLocale);
		}

		internal void DoneCleanup()
		{
			if (m_pCollator != IntPtr.Zero)
			{
				Icu.CloseCollator(m_pCollator);
				m_pCollator = IntPtr.Zero;
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
			byte[] pbKey1 = Icu.GetSortKey(m_pCollator, bstrValue1);
			byte[] pbKey2 = Icu.GetSortKey(m_pCollator, bstrValue2);

			return CompareVariant(pbKey1, pbKey2, colopt);
		}


		public object get_SortKeyVariant(string bstrValue, LgCollatingOptions colopt)
		{
			EnsureCollator();
			byte[] pbKey = Icu.GetSortKey(m_pCollator, bstrValue);

			return pbKey;
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
			if (m_pCollator != IntPtr.Zero)
				DoneCleanup();

			m_stuLocale = bstrLocale;

			EnsureCollator();
		}


		public void Close()
		{
			if (m_pCollator != IntPtr.Zero)
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
