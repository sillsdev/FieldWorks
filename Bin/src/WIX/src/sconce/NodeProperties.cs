//-------------------------------------------------------------------------------------------------
// <copyright file="NodeProperties.cs" company="Microsoft">
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
// Properties for a node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Abstract base class representing properties for a node in a Solution Explorer hierarchy
	/// that will be shown in the Properties window.
	/// </summary>
	public class NodeProperties : PropertyGridTypeDescriptor, ISpecifyPropertyPages
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private Node node;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeProperties"/> class.
		/// </summary>
		protected NodeProperties(Node node)
		{
			Tracer.VerifyNonNullArgument(node, "node");
			this.node = node;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the <see cref="Node"/> that this properties object is attached to.
		/// </summary>
		[Browsable(false)]
		public Node Node
		{
			get { return this.node; }
		}

		/// <summary>
		/// Gets a value indicating whether selecting "Properties" from the context menu should
		/// trigger the property pages to be shown. Normally only the project node should return true.
		/// </summary>
		[Browsable(false)]
		protected virtual bool ShouldTriggerPropertyPages
		{
			get { return false; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#region ISpecifyPropertyPages Implementation
		//==========================================================================================
		// ISpecifyPropertyPages Implementation
		//==========================================================================================

		void ISpecifyPropertyPages.GetPages(CAUUID[] pages)
		{
			Tracer.VerifyNonEmptyArrayArgument(pages, "pages");

			pages[0] = new CAUUID();
			pages[0].cElems = 0;

			// Get the property pages that the project supports
			if (this.ShouldTriggerPropertyPages)
			{
#if !VS_2003 // TODO: On VS 2003, we get an error when showing a property page, saying something like "There is not enough storage space to complete the current operation."
				Guid[] propertyPageGuids = this.Node.Hierarchy.AttachedProject.CommonPropertyPageGuids;
				pages[0] = PackageUtility.CreateCAUUIDFromGuidArray(propertyPageGuids);
#endif
			}
		}
		#endregion

		/// <summary>
		/// Returns the name that is displayed in the left hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The name of the object, or null if the class does not have a name.</returns>
		public override string GetComponentName()
		{
			return this.Node.Caption;
		}
		#endregion
	}
}
