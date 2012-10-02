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
// File: UpdaterMain.cs
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
	/// The main class for installing updates. We split checking for updates and installing
	/// updates into two apps because this app (FwVsUpdater) needs to run with admin privileges
	/// and thus presents a UAC dialog on Vista. When we just check for updates we don't need
	/// admin privileges thus no UAC dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UpdaterMain: Main
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override string AppName
		{
			get { return "FwVsUpdater"; }
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
				updater.InstallUpdates();
			}
			finally
			{
				Properties.Settings.Default.Save();
				Trace.WriteLine(string.Format("{0}: Exiting {1}", DateTime.Now.ToString(), AppName));
			}
		}
	}
}
