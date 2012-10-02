//-------------------------------------------------------------------------------------------------
// <copyright file="ReferenceFileNode.cs" company="Microsoft">
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
// A reference file node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Drawing;

	/// <summary>
	/// A reference file node within the Solution Explorer hierarchy.
	/// </summary>
	public class ReferenceFileNode : FileNode
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceFileNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent hierarchy.</param>
		/// <param name="absolutePath">The absolute path to the file.</param>
		public ReferenceFileNode(Hierarchy hierarchy, string absolutePath)
			: base(hierarchy, absolutePath)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a value indicating whether this node can be deleted from disk.
		/// </summary>
		/// <remarks>
		/// We don't want to support deletions of reference nodes. We will support removing it
		/// from the project, though.
		/// </remarks>
		public override bool CanDelete
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether this node can be removed from the project.
		/// </summary>
		public override bool CanRemoveFromProject
		{
			get { return true; }
		}

		/// <summary>
		/// Gets the image for a reference file node.
		/// </summary>
		public override Image Image
		{
			get { return HierarchyImages.ReferenceFile; }
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public override NodeProperties Properties
		{
			get { return new ReferenceFileNodeProperties(this); }
		}

		/// <summary>
		/// Gets the context menu that Visual Studio should use when right mouse clicking this node.
		/// </summary>
		public override VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_REFERENCE; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Executes a command from the standard Visual Studio 97 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public override bool ExecuteStandard97Command(VsCommand command)
		{
			bool supported = true;

			switch (command)
			{
				case VsCommand.Remove:
				case VsCommand.Delete:
					this.RemoveFromProject();
					break;

				default:
					supported = base.ExecuteStandard97Command(command);
					break;
			}

			return supported;
		}

		/// <summary>
		/// Queries the state of a command from the standard Visual Studio 97 command set on this node.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns>One of the <see cref="CommandStatus"/> values if the node handles the command;
		/// otherwise <see cref="CommandStatus.Unhandled"/>.</returns>
		public override CommandStatus QueryStandard97CommandStatus(VsCommand command)
		{
			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand.Open:
				case VsCommand.OpenWith:
					status = CommandStatus.NotSupportedOrEnabled;
					break;

				case VsCommand.Rename:
					status = CommandStatus.NotSupportedOrEnabled;
					break;
			}

			return status;
		}
		#endregion
	}
}
