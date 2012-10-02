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
// File: DbVersionTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Creates the DbVersion.cs file
	/// </summary>
	[TaskName("dbversion")]
	public class DbVersionTask: FwBaseTask
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DbVersionTask"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DbVersionTask()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the include file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string Include
		{
			get
			{
				string include = Sources.FileNames[0];
				if (include == null)
					throw new BuildException("Missing include file.");
				return include;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the file Extension required by the current compiler
		/// </summary>
		/// <returns>Returns the file Extension required by the current compiler</returns>
		/// ------------------------------------------------------------------------------------
		public override string Extension
		{
			get {return "cs"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if we have to compile
		/// </summary>
		/// <returns><c>true</c> if compilation is necessary, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool NeedsCompiling()
		{
			bool fRet = base.NeedsCompiling();

			if (!fRet)
			{
				// Include file changed?
				StringCollection files = new StringCollection();
				files.Add(Include);
				fRet = FileUpdated(files);
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
			base.ExecuteTask();

			if (NeedsCompiling())
			{
				Log(Level.Info, "Generating {0} from {1}", OutputFile.Name,
					Path.GetFileName(Include));

				StreamReader reader = null;
				StreamWriter outStream = null;
				try
				{
					StringBuilder fileContents = new StringBuilder();
					fileContents.AppendFormat(
						"// This file is generated from {0}. Do NOT modify!\n",
						Path.GetFileName(Include));

					reader = new StreamReader(Include);
					fileContents.Append("namespace SIL.FieldWorks.Common.Framework\n{\n/// <summary></summary>\npublic ");
					fileContents.Append(reader.ReadToEnd());
					fileContents.Append("\n}\n");

					outStream = new StreamWriter(OutputFile.FullName);
					outStream.Write(fileContents.ToString());
				}
				catch(Exception e)
				{
					throw new BuildException("Generation failed.", Location, e);
				}
				finally
				{
					if (reader != null)
						reader.Close();
					if (outStream != null)
						outStream.Close();
				}
			}
		}
	}
}
