// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CheckerMain.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SIL.FieldWorks.DevTools.FwVsUpdater
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The main class for checking for updates. This class checks for available updates. If the
	/// user agrees to install available updates we call with FwVsUpdater.exe which needs to
	/// be run with admin privileges. On Vista this will present a UAC dialog. However, by
	/// splitting this into two apps we present the UAC dialog only when there are updates
	/// available.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckerMain: Main
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override string AppName
		{
			get { return "FwVsUpdateChecker"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the update task
		/// </summary>
		/// <param name="fFirstTime">first time</param>
		/// <param name="fForce">force</param>
		/// ------------------------------------------------------------------------------------
		protected override void Update(bool fFirstTime, bool fForce)
		{
			try
			{
				UpdateManager updater = new UpdateManager(fFirstTime);
				updater.CheckForUpdateFiles(fForce);
			}
			finally
			{
				Properties.Settings.Default.Save();
				Trace.WriteLine(string.Format("{0}: Exiting {1}", DateTime.Now.ToString(), AppName));
			}
		}
	}
}
