// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwBaseTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.DotNet.Tasks;
using NAnt.Core.Types;

using SIL.FieldWorks.Tools;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FwTasks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FwBaseTask: CompilerBase
	{

		#region Private Static Fields

		private static Regex s_classNameRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.\{]+|class\s+(?<class>\w+)|(?<keyword>\w+))*");
		private static Regex s_namespaceRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.\{]+|namespace\s+(?<namespace>(\w+(\.\w+)*)+)|(?<keyword>\w+))*");

		#endregion Private Static Fields

		/// <summary>
		/// Gets the class name regular expression for the language of the
		/// current compiler.
		/// </summary>
		/// <value>
		/// Class name regular expression for the language of the current
		/// compiler.
		/// </value>
		protected override Regex ClassNameRegex
		{
			get { return s_classNameRegex; }
		}
		/// <summary>
		/// Gets the namespace regular expression for the language of the current compiler.
		/// </summary>
		/// <value>
		/// Namespace regular expression for the language of the current
		/// compiler.
		/// </value>
		protected override Regex NamespaceRegex
		{
			get { return s_namespaceRegex; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this attribute. We don't use it, but it is marked as "required" in the
		/// base class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("target")]
		public new string OutputTarget
		{
			get { return base.OutputTarget; }
			set { base.OutputTarget = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check if one of the <paramref name='files'/> has changed since the last compile.
		/// </summary>
		/// <param name="files">Name of files</param>
		/// <returns><c>true</c> if one of the files has changed, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool FileUpdated(StringCollection files)
		{
			bool fRet = false;

			string updatedFile = FileSet.FindMoreRecentLastWriteTime(files,
				OutputFile.LastWriteTime);
			if (updatedFile != null)
			{
				Log(Level.Verbose, "{0} is out of date, recompiling.",
					updatedFile);
				fRet = true;
			}
			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the generation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			// ignore base class implementation
		}
	}
}
