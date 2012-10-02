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
// File: AssemblyLinkerTaskEx.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the AssemblyLinkerTask. It adds the name of the resource file as namespace.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("alex")]
	public class AssemblyLinkerTaskEx : AssemblyLinkerTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Name
		{
			get { return "al"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates an assembly manifest.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			EmbeddedResourceCollection embeddedResources = base.EmbeddedResources;
			foreach (string fileName in Resources.FileNames)
			{
				if (File.Exists(fileName))
				{
					embeddedResources.Add(new EmbeddedResource(fileName, Path.GetFileName(fileName)));
				}
			}
			Resources.FileNames.Clear();

			base.ExecuteTask();
		}
	}
}
