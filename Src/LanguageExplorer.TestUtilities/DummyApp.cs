// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.TestUtilities
{
	public class DummyApp : IApp
	{
		public DummyApp()
		{
			PictureHolder = new PictureHolder();
		}
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		public string GetHelpString(string ksPropName)
		{
			throw new NotSupportedException();
		}

		public string HelpFile
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public string SupportEmailAddress
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public string FeedbackEmailAddress
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public bool PreFilterMessage(ref Message m)
		{
			throw new NotSupportedException();
		}

		public string ResourceString(string stid)
		{
			throw new NotSupportedException();
		}

		public MsrSysType MeasurementSystem { get; set; }
		public Form ActiveMainWindow
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public string ApplicationName => "DummyApp";

		public LcmCache Cache
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public PictureHolder PictureHolder { get; private set; }

		public void RefreshAllViews()
		{
			throw new NotSupportedException();
		}

		public void RestartSpellChecking()
		{
			throw new NotSupportedException();
		}

		public RegistryKey SettingsKey
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotSupportedException();
		}

		public void EnableMainWindows(bool fEnable)
		{
			throw new NotSupportedException();
		}

		public void RemoveFindReplaceDialog()
		{
			throw new NotSupportedException();
		}

		public void HandleIncomingLink(FwLinkArgs link)
		{
			throw new NotSupportedException();
		}

		public void HandleOutgoingLink(FwAppArgs link)
		{
			throw new NotSupportedException();
		}

		public bool UpdateExternalLinks(string oldLinkedFilesRootDir)
		{
			throw new NotSupportedException();
		}

		public bool ShowFindReplaceDialog(bool fReplace, IVwRootSite rootsite, LcmCache cache, Form mainForm)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DummyApp()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

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
				PictureHolder.Dispose();
			}
			PictureHolder = null;

			IsDisposed = true;
		}
	}
}
