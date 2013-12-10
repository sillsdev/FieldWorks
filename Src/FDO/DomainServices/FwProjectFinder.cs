// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwProjectFinder.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// In a separate thread, finds any FW projects on a host computer.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class FwProjectFinder
	{
		private readonly Thread m_projectFinderThread;
		private readonly string m_host;
		private readonly Action<Exception> m_exceptionCallback;
		private readonly Action<string> m_projectFoundCallback;
		private readonly Action m_onCompletedCallback;
		private volatile bool m_forceStop = false;
		private readonly string m_projectsDir;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwProjectFinder"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="projectFoundCallback">The project found callback.</param>
		/// <param name="onCompletedCallback">Callback to run when the search is completed.</param>
		/// <param name="exceptionCallback">The exception callback.</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		/// <param name="projectsDir">The projects directory.</param>
		/// ------------------------------------------------------------------------------------
		public FwProjectFinder(string host, Action<string> projectFoundCallback,
			Action onCompletedCallback, Action<Exception> exceptionCallback, bool showLocalProjects, string projectsDir)
		{
			if (string.IsNullOrEmpty(host))
				throw new ArgumentNullException("host");
			if (projectFoundCallback == null)
				throw new ArgumentNullException("projectFoundCallback");

			m_host = host;
			m_projectFoundCallback = projectFoundCallback;
			m_onCompletedCallback = onCompletedCallback;
			m_exceptionCallback = exceptionCallback;
			m_fShowLocalProjects = showLocalProjects;
			m_projectsDir = projectsDir;

			m_projectFinderThread = new Thread(FindProjects);
			m_projectFinderThread.Name = "Project Finder";
			m_projectFinderThread.Start();
		}

		private bool m_fShowLocalProjects;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the host for any projects.
		/// Any projects found fire the projectFoundCallback delegate.
		/// Any exception that is thrown is passed to the exceptionCallback delegate.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FindProjects()
		{
			try
			{
				// if we were asked to (typically host is our local machine) show fwdata files.
				if (m_fShowLocalProjects)
				{
					// search sub dirs
					string[] dirs = Directory.GetDirectories(m_projectsDir);
					foreach (string dir in dirs)
					{
						string file = Path.Combine(dir, FdoFileHelper.GetXmlDataFileName(Path.GetFileName(dir)));
						if (FileUtils.SimilarFileExists(file))
							m_projectFoundCallback(file);
						else
						{
							string db4oFile = Path.Combine(dir, FdoFileHelper.GetDb4oDataFileName(Path.GetFileName(dir)));
							//If the db4o file exists it will be added to the list later and therefore we do not want to
							//show the .bak file to the user in the open project dialog
							if (!FileUtils.SimilarFileExists(db4oFile))
							{
								// See if there is a .bak file
								string backupFile = Path.ChangeExtension(file, FdoFileHelper.ksFwDataFallbackFileExtension);
								//NOTE: RickM  I think this probably should be changed to TrySimilarFileExists but don't want to try this
								//on a release build.
								if (FileUtils.SimilarFileExists(backupFile))
									m_projectFoundCallback(backupFile);
							}
						}
						if (m_forceStop)
							return;
					}
					if (!ClientServerServices.Current.Local.ShareMyProjects)
						return;
				}
				// if host is not local machine OR ShareMyProjects is true show client server files.
				foreach (string fullServerPath in ClientServerServices.Current.ProjectNames(m_host, true))
				{
					// The current strategy for handling directory seperator chars in a system where
					// client and servers are different platforms is to convert seperator chars back and
					// forth when crossing the platform boundary.
					string adjustedfullServerPath = fullServerPath.Replace(MiscUtils.IsUnix ? @"\" : @"/",
						Path.DirectorySeparatorChar.ToString());
					m_projectFoundCallback(adjustedfullServerPath);
					if (m_forceStop)
						return;
				}
			}
			catch (Exception e)
			{
				if (m_exceptionCallback != null)
					m_exceptionCallback(e);
			}
			if (!m_forceStop && m_onCompletedCallback != null)
				m_onCompletedCallback();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the thread to stop (so no more projects will be found).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ForceStop()
		{
			m_forceStop = true;
			m_projectFinderThread.Join();
		}
	}
}
