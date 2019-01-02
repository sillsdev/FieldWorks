// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.FixData;
using SIL.LCModel.Utils;

namespace LanguageExplorer.UtilityTools
{
	/// <summary>
	/// Connect the error fixing code to the FieldWorks UtilityDlg facility.
	/// </summary>
	internal sealed class ErrorFixer : IUtility
	{
		private UtilityDlg _dlg;
		private List<string> _errors = new List<string>();
		private int _errorsFixed;

		/// <summary />
		internal ErrorFixer(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			_dlg = utilityDlg;
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
			_dlg.WhenDescription = LanguageExplorerResources.ksUseErrorFixerWhen;
			_dlg.WhatDescription = LanguageExplorerResources.ksErrorFixerUtilityAttemptsTo;
			_dlg.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
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
					if (dlg.ShowDialog(_dlg) != DialogResult.OK)
					{
						return;
					}
					var pathname = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, dlg.SelectedProject), dlg.SelectedProject + LcmFileHelper.ksFwDataXmlFileExtension);
					if (!File.Exists(pathname))
					{
						return;
					}
					using (new WaitCursor(_dlg))
					{
						using (var progressDlg = new ProgressDialogWithTask(_dlg))
						{
							var fixes = (string)progressDlg.RunTask(true, FixDataFile, pathname);
							if (fixes.Length <= 0)
							{
								return;
							}
							MessageBox.Show(fixes, LanguageExplorerResources.ksErrorsFoundOrFixed);
							File.WriteAllText(pathname.Replace(LcmFileHelper.ksFwDataXmlFileExtension, "fixes"), fixes);
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
			var pathname = parameters[0] as string;
			var bldr = new StringBuilder();
			var data = new FwDataFixer(pathname, progressDlg, LogErrors, ErrorCount);
			_errorsFixed = 0;
			_errors.Clear();
			data.FixErrorsAndSave();
			foreach (var err in _errors)
			{
				bldr.AppendLine(err);
			}
			return bldr.ToString();
		}

		private void LogErrors(string message, bool errorFixed)
		{
			_errors.Add(message);
			if (errorFixed)
			{
				++_errorsFixed;
			}
		}

		private int ErrorCount()
		{
			return _errorsFixed;
		}
		#endregion
	}
}