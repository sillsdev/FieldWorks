using System;

namespace XAmpleManagedWrapper
{
	public class XAmpleWrapper : IXAmpleWrapper, IDisposable
	{
		protected XAmpleDLLWrapper m_xample;

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~XAmpleWrapper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

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
				m_xample.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		#region IXAmpleWrapper implementation
		public void Init (string folderContainingXampleDll)
		{
			m_xample = new XAmpleDLLWrapper ();
			m_xample.Init (folderContainingXampleDll);
		}


		public string ParseWord (string wordform)
		{
			return m_xample.ParseString (wordform);
		}


		public string TraceWord (string wordform, string selectedMorphs)
		{
			return m_xample.TraceString(wordform, selectedMorphs);
		}


		public void LoadFiles (string fixedFilesDir, string dynamicFilesDir, string databaseName)
		{
			m_xample.LoadFiles (fixedFilesDir, dynamicFilesDir, databaseName);
		}


		public void SetParameter (string name, string value)
		{
			m_xample.SetParameter (name, value);
		}


		public int AmpleThreadId {
			get { return m_xample.GetAmpleThreadId (); }
		}

		#endregion

	}
}
