using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Language
{
	/// <summary>
	/// Direct port of the C++ class LgIcuCollator
	///
	/// NOTE: Not currently enabled in Windows (FWNX-296).
	/// </summary>
	[Serializable()]
	[ClassInterface(ClassInterfaceType.None)]
	[Guid("e771361c-ff54-4120-9525-98a0b7a9accf")]
	public class ManagedLgIcuCollator : ILgCollatingEngine, IDisposable
	{
		#region Member variables

		private ILgWritingSystemFactory m_qwsf;
		private string m_stuLocale;
		private IntPtr m_pCollator;

		private const int keysize = 1024;

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

		protected byte[] GetSortKey(string bstrValue, byte[] prgbKey, ref Int32 pcbKey)
		{
			byte[] pbKey;
			Int32 crgbKey = pcbKey;
			EnsureCollator();
			pcbKey = Icu.ucol_GetSortKey(m_pCollator, bstrValue, -1, ref prgbKey, prgbKey.Length);
			if (pcbKey > crgbKey)
			{
				pbKey = null;
			}

			else
			{
				pbKey = prgbKey;
			}

			return pbKey;
		}

		protected byte[] GetSortKey(string bstrValue, byte[] prgbKey, ref Int32 pcbKey, out byte[] vbKey)
		{
			vbKey = null;
			Int32 cbKey = pcbKey;
			byte[] pbKey = GetSortKey(bstrValue, prgbKey, ref cbKey);
			if (cbKey > pcbKey)
			{
				Int32 cbKey1 = cbKey + 1;
				vbKey = new byte[cbKey];
				pbKey = GetSortKey(bstrValue, vbKey, ref cbKey1);
				Debug.Assert(cbKey == cbKey1, "cbKey == cbKey1");
				// As long as it fits assume OK.
				Debug.Assert(cbKey1 + 1 <= vbKey.Length, "cbKey1 + 1 <= vbKey.Length");
				// pure paranoia -- it's supposed to be NUL-terminated.
				vbKey[cbKey] = 0;
			}
			pcbKey = cbKey;
			return pbKey;
		}

		protected void EnsureCollator()
		{
			if (m_pCollator != IntPtr.Zero)
				return;
			// we already have one.
			Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;

			if (m_stuLocale == String.Empty)
			{
				m_pCollator = Icu.ucol_Open(null, out uerr);
			}

			else
			{
				byte[] rgchLoc = new byte[128];
				Int32 cch = Icu.uloc_GetName(m_stuLocale, ref rgchLoc, rgchLoc.Length, out uerr);
				Debug.Assert(cch < rgchLoc.Length, "cch < rgchLoc.Length");
				rgchLoc[cch] = 0;
				if (uerr != Icu.UErrorCode.U_ZERO_ERROR)
					throw new ApplicationException(string.Format("uloc_GetName returned {0}", uerr));

				m_pCollator = Icu.ucol_Open(rgchLoc, out uerr);
			}

			if (!(uerr == Icu.UErrorCode.U_ZERO_ERROR || uerr == Icu.UErrorCode.U_ERROR_WARNING_START || uerr == Icu.UErrorCode.U_USING_DEFAULT_WARNING))
			{
				throw new ApplicationException(string.Format("ucol_Open returned {0}", uerr));
			}
		}

		internal void DoneCleanup()
		{
			if (m_pCollator != IntPtr.Zero)
			{
				Icu.ucol_Close(m_pCollator);
				m_pCollator = IntPtr.Zero;
			}
		}

		#region ILgCollatingEngine implementation
		public string get_SortKey(string bstrValue, LgCollatingOptions colopt)
		{
			throw new System.NotImplementedException();
		}

		public void SortKeyRgch(string _ch, int cchIn, LgCollatingOptions colopt, int cchMaxOut, ArrayPtr _chKey, out int _cchOut)
		{
			throw new System.NotImplementedException();
		}


		public int Compare(string bstrValue1, string bstrValue2, LgCollatingOptions colopt)
		{
			EnsureCollator();
			Int32 cbKey1 = keysize;
			byte[] rgbKey1 = new byte[keysize + 1];
			byte[] vbKey1 = null;
			byte[] pbKey1 = GetSortKey(bstrValue1, rgbKey1, ref cbKey1, out vbKey1);

			Int32 cbKey2 = keysize;
			byte[] rgbKey2 = new byte[keysize + 1];
			byte[] vbKey2 = null;
			byte[] pbKey2 = GetSortKey(bstrValue2, rgbKey2, ref cbKey2, out vbKey2);

			return CompareVariant(pbKey1, pbKey2, colopt);
		}


		public object get_SortKeyVariant(string bstrValue, LgCollatingOptions colopt)
		{
			EnsureCollator();

			Int32 cbKey = keysize;
			byte[] rgbKey = new byte[keysize + 1];
			byte[] vbKey = null;
			byte[] pbKey = GetSortKey(bstrValue, rgbKey, ref cbKey, out vbKey);

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
