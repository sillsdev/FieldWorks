//--------------------------------------------------------------------------------------------------
// <copyright file="LitTask.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// NAnt task for the lit linker.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.NAntTasks
{
	using System;
	using System.IO;

	using NAnt.Core;
	using NAnt.Core.Attributes;

	/// <summary>
	/// Represents the NAnt task for the &lt;lit&gt; element in a NAnt script.
	/// </summary>
	[TaskName("lit")]
	public class LitTask : SingleOutputWixTask
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="LitTask"/> class.
		/// </summary>
		public LitTask() : base("lit.exe")
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Writes all of the command-line parameters for the tool to a response file, one parameter per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected override void WriteOptions(TextWriter writer)
		{
			base.WriteOptions(writer);
		}
		#endregion
	}
}