//-------------------------------------------------------------------------------------------------
// <copyright file="FolderFirstComparer.cs" company="Microsoft">
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
// Custom comparer for hierarchy nodes.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Globalization;

	/// <summary>
	/// Compares HierarchyNodes according to the following sorting order: the root comes
	/// first, then a library folder, then any other folder nodes, then the file nodes.
	/// Within each group, they're sorted by case-insensitive compare of the caption.
	/// </summary>
	public sealed class FolderFirstComparer : IComparer
	{
		public int Compare(object x, object y)
		{
			Node node1 = x as Node;
			Node node2 = y as Node;

			// Null checks.
			if (node1 == null && node2 == null)
			{
				return 0;
			}
			if (node1 == null && node2 != null)
			{
				return 1;
			}
			if (node1 != null && node2 == null)
			{
				return -1;
			}

			// If they're the same type, then sort by caption.
			if (node1.GetType() == node2.GetType() || (node1 is FileNode && node2 is FileNode))
			{
				return String.Compare(node1.Caption, node2.Caption, StringComparison.InvariantCultureIgnoreCase);
			}

			// Root first, then library, then folders, then file.
			if (node1 is ProjectNode)
			{
				return -1;
			}
			if (node2 is ProjectNode)
			{
				return 1;
			}
			if (node1 is ReferenceFolderNode)
			{
				return -1;
			}
			if (node2 is ReferenceFolderNode)
			{
				return 1;
			}
			if (node1 is FolderNode)
			{
				return -1;
			}
			if (node2 is FolderNode)
			{
				return 1;
			}

			Tracer.Fail("FolderFirstComparer.Compare: We should not be hitting here.");
			return 0;
		}
	}
}
