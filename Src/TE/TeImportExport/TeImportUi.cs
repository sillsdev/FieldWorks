// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeImportUi.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.TE
{
	#region TeImportUi class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UI elements of import. Gets called from TeImport.
	/// </summary>
	/// <remarks>This class needs to deal with multi threading issues since it gets called
	/// from TeImport (which runs in the background), but every UI element it creates needs
	/// to run on the main thread.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class TeImportUi: IFWDisposable
	{
		#region Data members
		/// <summary></summary>
		protected TeImporter m_importer;

		private IHelpTopicProvider m_helpTopicProvider;
		private ProgressDialogWithTask m_progressDialog;
		private bool m_fDisposed;

		/// <summary>Added for multi threading purposes</summary>
		private Control m_ctrl;
		#endregion

		#region Delegates
		private delegate void ExceptionErrorMessageInvoker(EncodingConverterException e);
		private delegate void StringErrorMessageInvoker(string s);
		private delegate void SetTeImporterInvoker(TeImporter importer);
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportUi"/> class.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public TeImportUi(ProgressDialogWithTask progressDialog, IHelpTopicProvider helpTopicProvider)
		{
			m_progressDialog = progressDialog;
			if (m_progressDialog != null)
				m_progressDialog.Canceling += OnCancelPressed;
			m_helpTopicProvider = helpTopicProvider;
			m_ctrl = new Control();
			m_ctrl.CreateControl();
		}

		#region Disposed stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <remarks>This property is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// TeImportUi is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~TeImportUi()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_ctrl != null)
					m_ctrl.Dispose();
			}

			m_ctrl = null;
			m_progressDialog = null;
			m_importer = null;

			m_fDisposed = true;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the importer.
		/// </summary>
		/// <value>The importer.</value>
		/// ------------------------------------------------------------------------------------
		public TeImporter Importer
		{
			set
			{
				CheckDisposed();
				if (m_ctrl.InvokeRequired)
				{
					SetTeImporterInvoker method = delegate(TeImporter imp) { m_importer = imp; };
					m_ctrl.Invoke(method, value);
				}
				else
					m_importer = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to cancel the import.
		/// </summary>
		/// <value><c>true</c> if user pressed cancel button; otherwise, <c>false</c>.</value>
		/// <remarks>Thread safe</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool CancelImport
		{
			get
			{
				if (m_progressDialog != null)
					return m_progressDialog.Canceled;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a message indicating progress status.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string StatusMessage
		{
			get
			{
				CheckDisposed();
				return m_progressDialog.Message;
			}
			set
			{
				CheckDisposed();
				m_progressDialog.Message = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int Maximum
		{
			get
			{
				CheckDisposed();
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();
				m_progressDialog.Maximum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int Position
		{
			set
			{
				CheckDisposed();
				m_progressDialog.Position = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is displaying a UI.
		/// </summary>
		/// <value>always <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDisplayingUi
		{
			get { return true; }
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="cSteps"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Step(int cSteps)
		{
			CheckDisposed();
			m_progressDialog.Step(cSteps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an import error message.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public virtual void ErrorMessage(EncodingConverterException e)
		{
			CheckDisposed();

			if (m_ctrl.InvokeRequired)
			{
				m_ctrl.Invoke(new ExceptionErrorMessageInvoker(ErrorMessage), e);
			}
			else
			{
				// TODO-Linux: Help is not implemented in Mono
				MessageBox.Show(e.Message,
					ScriptureUtilsException.GetResourceString("kstidImportErrorCaption"),
					MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0,
					m_helpTopicProvider.HelpFile, HelpNavigator.Topic, e.HelpTopic);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an import error message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ErrorMessage(string message)
		{
			CheckDisposed();

			if (m_ctrl.InvokeRequired)
			{
				m_ctrl.Invoke(new StringErrorMessageInvoker(ErrorMessage), message);
			}
			else
			{
				MessageBox.Show(message,
					ScriptureUtilsException.GetResourceString("kstidImportErrorCaption"));
			}
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user pressed the cancel button on the progress dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnCancelPressed(object sender, CancelEventArgs e)
		{
			Debug.Assert(!m_ctrl.InvokeRequired);

			try
			{
				// pause the import thread, while we ask if they really want to cancel
				m_importer.Pause();
				if (m_importer.PrevBook != null)
				{
					string sMsg = string.Format(TeResourceHelper.GetResourceString("kstidConfirmStopImport"),
												m_importer.PrevBook);

					if (MessageBox.Show(sMsg, FwUtils.ksTeAppName,
										MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
					{
						// the user does not wish to cancel the import, so cancel the event
						e.Cancel = true;
						return;
					}
				}
				//Display "stopping" message in progress bar
				m_progressDialog.Message = TeResourceHelper.GetResourceString("kstidStoppingImport");
				m_progressDialog.Position = m_progressDialog.Maximum;
			}
			finally
			{
				// resume the import thread
				m_importer.Resume();
			}
		}
		#endregion
	}
	#endregion
}
