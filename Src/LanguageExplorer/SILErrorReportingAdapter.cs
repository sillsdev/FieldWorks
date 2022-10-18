<<<<<<< HEAD:Src/LanguageExplorer/SILErrorReportingAdapter.cs
// Copyright (c) 2013-2020 SIL International
||||||| f013144d5:Src/xWorks/SilErrorReportingAdapter.cs
﻿// Copyright (c) 2013-2017 SIL International
=======
// Copyright (c) 2013-2022 SIL International
>>>>>>> develop:Src/xWorks/SilErrorReportingAdapter.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Reporting;

namespace LanguageExplorer
{
	/// <summary>
	/// This class allows for exceptions in Palaso dlls to be reported in FieldWorks
	/// using the normal FieldWorks error handling system and reporting dialogs. When you
	/// create a Palaso dialog, create an instance of this class, passing it the dialog.
	/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/SILErrorReportingAdapter.cs
	internal sealed class SILErrorReportingAdapter : IErrorReporter, IDisposable
||||||| f013144d5:Src/xWorks/SilErrorReportingAdapter.cs
	/// ------------------------------------------------------------------------------------
	class SilErrorReportingAdapter : IErrorReporter, IDisposable
=======
	/// ------------------------------------------------------------------------------------
	internal class SilErrorReportingAdapter : IErrorReporter, IDisposable
>>>>>>> develop:Src/xWorks/SilErrorReportingAdapter.cs
	{
		private Form m_parentForm;
		private RegistryKey m_registryKey;
		private string m_supportEmailAddress;

		/// <summary />
		internal SILErrorReportingAdapter(Form parentForm, IPropertyTable propertyTable)
		{
			m_parentForm = parentForm;
			var app = propertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
			m_registryKey = app.SettingsKey;
			m_supportEmailAddress = app.SupportEmailAddress;
		}

		#region IErrorReporter

		/// <summary />
		public void ReportFatalException(Exception e)
		{
			throw e; // I think this will ultimately show the green-screen (unless something catches it)
		}

<<<<<<< HEAD:Src/LanguageExplorer/SILErrorReportingAdapter.cs
		/// <summary />
		public ErrorResult NotifyUserOfProblem(IRepeatNoticePolicy policy, string alternateButton1Label, ErrorResult resultIfAlternateButtonPressed, string message)
||||||| f013144d5:Src/xWorks/SilErrorReportingAdapter.cs
		public ErrorResult NotifyUserOfProblem(IRepeatNoticePolicy policy, string alternateButton1Label,
			ErrorResult resultIfAlternateButtonPressed, string message)
=======
		public void NotifyUserOfProblem(IRepeatNoticePolicy policy, Exception exception, string message)
		{
			if (policy.ShouldShowMessage(message))
				ErrorReporter.ReportException(new Exception(message, exception), m_registryKey,
					m_supportEmailAddress, m_parentForm, false);
		}

		public ErrorResult NotifyUserOfProblem(IRepeatNoticePolicy policy, string alternateButton1Label,
			ErrorResult resultIfAlternateButtonPressed, string message)
>>>>>>> develop:Src/xWorks/SilErrorReportingAdapter.cs
		{
			return policy.ShouldShowMessage(message) && ErrorReporter.ReportException(new Exception(message), m_registryKey, m_supportEmailAddress, m_parentForm, false)
				? ErrorResult.Abort : ErrorResult.Ignore;
		}

<<<<<<< HEAD:Src/LanguageExplorer/SILErrorReportingAdapter.cs
		/// <summary />
		public void ReportNonFatalException(Exception exception, IRepeatNoticePolicy policy)
||||||| f013144d5:Src/xWorks/SilErrorReportingAdapter.cs
		public void ReportNonFatalException(Exception exception, IRepeatNoticePolicy policy)
=======
		public void ReportNonFatalException(Exception exception, IRepeatNoticePolicy policy = null)
>>>>>>> develop:Src/xWorks/SilErrorReportingAdapter.cs
		{
<<<<<<< HEAD:Src/LanguageExplorer/SILErrorReportingAdapter.cs
			if (policy.ShouldShowErrorReportDialog(exception))
			{
||||||| f013144d5:Src/xWorks/SilErrorReportingAdapter.cs
			if (policy.ShouldShowErrorReportDialog(exception))
=======
			if (policy == null || policy.ShouldShowErrorReportDialog(exception))
>>>>>>> develop:Src/xWorks/SilErrorReportingAdapter.cs
				ErrorReporter.ReportException(exception, m_registryKey, m_supportEmailAddress, m_parentForm, false);
			}
		}

		/// <summary />
		public void ReportNonFatalExceptionWithMessage(Exception error, string message, params object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args), error), m_registryKey, m_supportEmailAddress, m_parentForm, false);
		}

		/// <summary />
		public void ReportNonFatalMessageWithStackTrace(string message, params object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args)), m_registryKey, m_supportEmailAddress, m_parentForm, false);
		}

		/// <summary />
		public void ReportFatalMessageWithStackTrace(string message, object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args)), m_registryKey, m_supportEmailAddress, m_parentForm, true);
		}
		#endregion

		#region IDisposable

		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~SILErrorReportingAdapter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
		/// <remarks>Must not be virtual.</remarks>
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
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
				m_registryKey.Dispose();
			}

			m_parentForm = null;
			m_registryKey = null;
			m_supportEmailAddress = null;

			IsDisposed = true;
		}
		#endregion
	}
}