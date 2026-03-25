// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XAmpleWithToneParse
{
	public class XAmpleWrapperForTonePars : XAmpleManagedWrapper.XAmpleWrapper
	{
		protected XAmpleDLLWrapperForTonePars m_xampleTP;
		private static object m_lockObject = new object();

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~XAmpleWrapperForTonePars()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public new bool IsDisposed { get; private set; }

		/// <summary/>
		public new void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected new virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(
				!fDisposing,
				"****** Missing Dispose() call for " + GetType().Name + ". ****** "
			);
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_xampleTP.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		public void InitForTonePars()
		{
			m_xampleTP = new XAmpleDLLWrapperForTonePars();
			m_xampleTP.InitForTonePars();
		}

		public string ParseFileForTonePars(string inputFile, string outputFile)
		{
			lock (m_lockObject)
			{
				return m_xampleTP.ParseFileForTonePars(inputFile, outputFile);
			}
		}

		public void LoadFilesForTonePars(
			string fixedFilesDir,
			string dynamicFilesDir,
			string databaseName,
			string intxControlFile,
			int maxToReturn
		)
		{
			lock (m_lockObject)
			{
				m_xampleTP.LoadFilesForTonePars(
					fixedFilesDir,
					dynamicFilesDir,
					databaseName,
					intxControlFile,
					maxToReturn
				);
			}
		}
	}
}
