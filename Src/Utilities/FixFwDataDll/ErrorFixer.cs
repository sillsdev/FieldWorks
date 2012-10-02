// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LinkFixer.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FwCoreDlgs;
using System.Windows.Forms;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.FixData
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Connect the error fixing code to the FieldWorks UtilityDlg facility.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ErrorFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility Members

		/// <summary>
		/// Set the UtilityDlg that invokes this utility.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);
				m_dlg = value;
			}
		}

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return Strings.ksFindAndFixErrors;
			}
		}

		/// <summary>
		/// Load 0 or more items in the main utility dialog's list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		/// <summary>
		/// When selected in the main utility dialog, fill in some more information there.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = Strings.ksUseThisWhen;
			m_dlg.WhatDescription = Strings.ksThisUtilityAttemptsTo;
			m_dlg.RedoDescription = Strings.ksCannotUndo;
		}

		/// <summary>
		/// Run the utility on command from the main utility dialog.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			using (var dlg = new FixErrorsDlg())
			{
				try
				{
					if (dlg.ShowDialog(m_dlg) == DialogResult.OK)
					{
						string pathname = Path.Combine(
							Path.Combine(DirectoryFinder.ProjectsDirectory, dlg.SelectedProject),
							dlg.SelectedProject + Resources.FwFileExtensions.ksFwDataXmlFileExtension);
						if (File.Exists(pathname))
						{
							using (new WaitCursor(m_dlg))
							{
								using (var progressDlg = new ProgressDialogWithTask(m_dlg, null))
								{
									string fixes = (string)progressDlg.RunTask(true, FixDataFile, pathname);
									if (fixes.Length > 0)
										MessageBox.Show(fixes, Strings.ksErrorsFoundOrFixed);
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

			FwData data = new FwData(pathname, progressDlg);
			data.FixErrorsAndSave();

			foreach (var err in data.Errors)
				bldr.AppendLine(err);
			return bldr.ToString();
		}

		#endregion
	}
}
