//-------------------------------------------------------------------------------------------------
// <copyright file="ReferenceFileNodeCollection.cs" company="Microsoft">
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
// A strongly-typed collection class for library FileNode objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;

	public class ReferenceFileNodeCollection : NodeCollection
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public ReferenceFileNodeCollection(ReferenceFolderNode parent) :
			base(parent, CaseInsensitiveComparer.DefaultInvariant)
		{
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		public new ReferenceFileNode this[int index]
		{
			get { return (ReferenceFileNode)this.InnerList[index]; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public int Add(ReferenceFileNode node)
		{
			return base.Add(node);
		}

		public bool Contains(ReferenceFileNode node)
		{
			return base.Contains(node);
		}

		public int IndexOf(ReferenceFileNode node)
		{
			return base.IndexOf(node);
		}

		public void Remove(ReferenceFileNode node)
		{
			base.Remove(node);
		}

		protected override void ValidateType(object value)
		{
			if (!(value is ReferenceFileNode))
			{
				throw new ArgumentException("value must be of type ReferenceFileNode.", "value");
			}
		}
		#endregion
	}
}
