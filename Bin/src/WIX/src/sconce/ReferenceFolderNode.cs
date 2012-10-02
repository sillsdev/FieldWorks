//--------------------------------------------------------------------------------------------------
// <copyright file="ReferenceFolderNode.cs" company="Microsoft">
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
// A reference folder node within a Solution Explorer hierarchy.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Drawing;
	using System.IO;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Represents the References folder in the Solution Explorer hierarchy.
	/// </summary>
	public class ReferenceFolderNode : VirtualFolderNode
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ReferenceFolderNode);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceFolderNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <param name="rootDirectory">The absolute path to the folder.</param>
		public ReferenceFolderNode(Hierarchy hierarchy, string rootDirectory)
			: this(hierarchy, rootDirectory, SconceStrings.ReferenceFolderCaption)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceFolderNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <param name="rootDirectory">The absolute path to the folder.</param>
		/// <param name="caption">The folder's caption.</param>
		protected ReferenceFolderNode(Hierarchy hierarchy, string rootDirectory, string caption)
			: base(hierarchy, Path.Combine(rootDirectory, caption))
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a value indicating whether the caption is editable.
		/// </summary>
		public override bool CaptionEditable
		{
			get { return false; }
		}

		/// <summary>
		/// Gets the image for the closed reference folder.
		/// </summary>
		public override Image Image
		{
			get { return HierarchyImages.ClosedReferenceFolder; }
		}

		/// <summary>
		/// Gets the image for the open reference folder.
		/// </summary>
		public override Image OpenImage
		{
			get { return HierarchyImages.OpenReferenceFolder; }
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public override NodeProperties Properties
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the context menu that Visual Studio should use when right mouse clicking this node.
		/// </summary>
		public override VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_REFERENCEROOT; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Queries the state of a command from the standard Visual Studio 2000 command set on this node.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns>One of the <see cref="CommandStatus"/> values if the node handles the command;
		/// otherwise <see cref="CommandStatus.Unhandled"/>.</returns>
		public override CommandStatus QueryStandard2KCommandStatus(VsCommand2K command)
		{
			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand2K.ADDREFERENCE:
					status = CommandStatus.SupportedAndEnabled;
					break;

				default:
					status = base.QueryStandard2KCommandStatus(command);
					break;
			}

			return status;
		}

		/// <summary>
		/// Executes a command from the standard Visual Studio 2000 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public override bool ExecuteStandard2KCommand(VsCommand2K command)
		{
			bool supported = true;

			switch (command)
			{
				case VsCommand2K.ADDREFERENCE:
					this.Hierarchy.ShowAddReferenceDialog();
					break;

				default:
					supported = base.ExecuteStandard2KCommand(command);
					break;
			}

			return supported;
		}
		#endregion
	}
}
