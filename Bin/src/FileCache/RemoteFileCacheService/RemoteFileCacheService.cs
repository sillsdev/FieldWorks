// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileCacheService.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Text;

namespace SIL.FieldWorks.Tools.FileCache
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class RemoteFileCacheService : ServiceBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Main method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Main()
		{
			System.ServiceProcess.ServiceBase[] servicesToRun;
			servicesToRun = new System.ServiceProcess.ServiceBase[] { new RemoteFileCacheService() };
			System.ServiceProcess.ServiceBase.Run(servicesToRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileCacheService"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RemoteFileCacheService()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the start for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallStartForTesting()
		{
			OnStart(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When implemented in a derived class, executes when a Start command is sent to the
		/// service by the Service Control Manager (SCM) or when the operating system starts
		/// (for a service that starts automatically). Specifies actions to take when the
		/// service starts.
		/// </summary>
		/// <param name="args">Data passed by the start command.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnStart(string[] args)
		{
			RemotingConfiguration.Configure(Assembly.GetExecutingAssembly().Location + ".config",
				true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When implemented in a derived class, executes when a Stop command is sent to the
		/// service by the Service Control Manager (SCM). Specifies actions to take when a
		/// service stops running.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnStop()
		{
		}
	}
}
