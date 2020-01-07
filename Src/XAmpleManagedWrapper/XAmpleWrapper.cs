// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;

namespace XAmpleManagedWrapper
{
	public class XAmpleWrapper : IXAmpleWrapper, IDisposable
	{
		private XAmpleDLLWrapper m_xample;
		private static readonly object m_lockObject = new object();

		#region Disposable stuff

		/// <summary />
		~XAmpleWrapper()
		{
			Dispose(false);
		}

		private bool IsDisposed
		{
			get;
			set;
		}

		/// <summary />
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
				// dispose managed and unmanaged objects
				m_xample.Dispose();
			}
			m_xample = null;
			IsDisposed = true;
		}
		#endregion

		#region IXAmpleWrapper implementation
		public void Init()
		{
			m_xample = new XAmpleDLLWrapper();
			m_xample.Init();
		}


		public string ParseWord (string wordform)
		{
			lock (m_lockObject)
			{
				return m_xample.ParseString(wordform);
			}
		}


		public string TraceWord (string wordform, string selectedMorphs)
		{
			return m_xample.TraceString(wordform, selectedMorphs);
		}


		public void LoadFiles (string fixedFilesDir, string dynamicFilesDir, string databaseName)
		{
			lock (m_lockObject)
			{
				m_xample.LoadFiles(fixedFilesDir, dynamicFilesDir, databaseName);
			}
		}


		public void SetParameter (string name, string value)
		{
			m_xample.SetParameter (name, value);
		}


		public int AmpleThreadId => m_xample.GetAmpleThreadId ();
		#endregion
	}
}
