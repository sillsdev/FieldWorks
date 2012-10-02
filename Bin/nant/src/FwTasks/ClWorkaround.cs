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
// File: ClWorkaround.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Types;
using NAnt.VisualCpp.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("clworkaround")]
	public class ClWorkaround : ClTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the <see cref="T:System.Diagnostics.ProcessStartInfo"/> of the specified
		/// <see cref="T:System.Diagnostics.Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="T:System.Diagnostics.Process"/> of which
		/// the <see cref="T:System.Diagnostics.ProcessStartInfo"/> should be updated.</param>
		/// ------------------------------------------------------------------------------------
		protected override void PrepareProcess(System.Diagnostics.Process process)
		{
			base.PrepareProcess(process);

			process.StartInfo.FileName = "RunAsUser.exe";
			process.StartInfo.Arguments = "cl.exe " + process.StartInfo.Arguments;
		}
	}
}
