// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using System.Windows.Forms;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FixData;
using SIL.LCModel.Utils;

namespace LanguageExplorer.UtilityTools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Connect the error fixing code to the FieldWorks UtilityDlg facility.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ErrorFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal ErrorFixer(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility Members

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label => LanguageExplorerResources.ksFindAndFixErrors;

		/// <summary>
		/// When selected in the main utility dialog, fill in some more information there.
		/// </summary>
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksUseErrorFixerWhen;
			m_dlg.WhatDescription = LanguageExplorerResources.ksErrorFixerUtilityAttemptsTo;
			m_dlg.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
		}

		/// <summary>
		/// Run the utility on command from the main utility dialog.
		/// </summary>
		public void Process()
		{
			using (var dlg = new FixErrorsDlg())
			{
				try
				{
					if (dlg.ShowDialog(m_dlg) == DialogResult.OK)
					{
						string pathname = Path.Combine(
							Path.Combine(FwDirectoryFinder.ProjectsDirectory, dlg.SelectedProject),
							dlg.SelectedProject + LcmFileHelper.ksFwDataXmlFileExtension);
						if (File.Exists(pathname))
						{
							using (new WaitCursor(m_dlg))
							{
								using (var progressDlg = new ProgressDialogWithTask(m_dlg))
								{
									string fixes = (string)progressDlg.RunTask(true, FixDataFile, pathname);
									if (fixes.Length > 0)
									{
										MessageBox.Show(fixes, LanguageExplorerResources.ksErrorsFoundOrFixed);
										File.WriteAllText(pathname.Replace(LcmFileHelper.ksFwDataXmlFileExtension, "fixes"), fixes);
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		private object FixDataFile(IProgress progressDlg, params object[] parameters)
		{
			string pathname = parameters[0] as string;
			StringBuilder bldr = new StringBuilder();

			FwDataFixer data = new FwDataFixer(pathname, progressDlg, LogErrors, ErrorCount);
			_errorsFixed = 0;
			_errors.Clear();
			data.FixErrorsAndSave();

			foreach (var err in _errors)
				bldr.AppendLine(err);
			return bldr.ToString();
		}

		private List<string> _errors = new List<string>();
		private int _errorsFixed = 0;
		private void LogErrors(string message, bool errorFixed)
		{
			_errors.Add(message);
			if (errorFixed)
				++_errorsFixed;
		}

		private int ErrorCount()
		{
			return _errorsFixed;
		}
		#endregion
	}
}
