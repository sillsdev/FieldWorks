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
// File: Main.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.DevTools.FwVsUpdater
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class Main
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mains the method.
		/// </summary>
		/// <param name="args">The args.</param>
		/// ------------------------------------------------------------------------------------
		public void MainMethod(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Trace.WriteLine(DateTime.Now.ToString());
			StringBuilder bldr = new StringBuilder();
			bldr.Append(DateTime.Now.ToString());
			bldr.AppendFormat(": Starting {0} (", AppName);
			int iArg = 0;
			foreach (string s in args)
			{
				if (iArg > 0)
					bldr.Append(", ");
				bldr.Append(s);
				iArg++;
			}
			bldr.Append(")");
			Trace.WriteLine(bldr.ToString());

			bool fFirstTime = false;
			bool fForce = false;
			for (int iArgs = 0; iArgs < args.Length; iArgs++)
			{
				string arg = args[iArgs];
				if (arg == "/first" || arg == "-first")
					fFirstTime = true;
				else if (arg == "/f" || arg == "-f")
					fForce = true;
				else if (arg.Length > 0 && arg[0] != '/' && arg[0] != '-')
				{
					if (arg != Properties.Settings.Default.FwRoot)
						Properties.Settings.Default.FwRoot = arg;
				}
				else
				{
					System.Windows.Forms.MessageBox.Show(
						string.Format("Updates VS addins and templates for use with FieldWorks build process\n" +
						"\nUsage:\n" +
						"{0} [options] [FwPath]\n" +
						"\nOptions:\n" +
						"-f\tForce check for updates, even if check already happened today",
						"{0}. (C) 2008 by SIL International", AppName));
					Trace.WriteLine(DateTime.Now.ToString() + ": Displayed usage. Exiting");
					return;
				}
			}

			if (Properties.Settings.Default.FwRoot.Length == 0)
			{
				using (FwPath fwPath = new FwPath())
				{
					if (fwPath.ShowDialog() == DialogResult.OK && fwPath.Path.Length > 0)
						Properties.Settings.Default.FwRoot = fwPath.Path;
					else
					{
						Trace.WriteLine(DateTime.Now.ToString() + ": Can't continue without a FW path");
						return;
					}
				}
			}

			Update(fFirstTime, fForce);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string AppName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the update task
		/// </summary>
		/// <param name="fFirstTime">first time</param>
		/// <param name="fForce">force</param>
		/// ------------------------------------------------------------------------------------
		protected abstract void Update(bool fFirstTime, bool fForce);
	}
}
