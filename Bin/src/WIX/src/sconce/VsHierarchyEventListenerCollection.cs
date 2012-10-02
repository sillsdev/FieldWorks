//-------------------------------------------------------------------------------------------------
// <copyright file="VsHierarchyEventListenerCollection.cs" company="Microsoft">
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
// Collection class for IVsHierarchyEvents event listeners.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;

	using Microsoft.VisualStudio.Shell.Interop;

	public sealed class VsHierarchyEventListenerCollection : EventListenerCollection
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(VsHierarchyEventListenerCollection);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public VsHierarchyEventListenerCollection()
		{
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		public IVsHierarchyEvents this[int index]
		{
			get { return (IVsHierarchyEvents)this.GetAt(index); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public uint Add(IVsHierarchyEvents listener)
		{
			return base.Add(listener);
		}

		/// <summary>
		/// Clones this object by performing a shallow copy of the collection items.
		/// </summary>
		/// <returns>A shallow copy of this object.</returns>
		public override object Clone()
		{
			VsHierarchyEventListenerCollection clone = new VsHierarchyEventListenerCollection();
			this.CloneInto(clone);
			return clone;
		}

		/// <summary>
		/// Notifies all of the listeners that the specified node and all of its children need to be invalidated.
		/// </summary>
		/// <param name="parent">The parent node that needs to be invalidated.</param>
		public void OnInvalidateItems(Node parent)
		{
			Tracer.VerifyNonNullArgument(parent, "parent");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnInvalidateItems", Tracer.Level.Information, "Notifying all of our listeners that the hierarchy needs to be refreshed from '{0}'.", parent.Caption);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsHierarchyEvents eventItem in clone)
			{
				try
				{
					eventItem.OnInvalidateItems(parent.HierarchyId);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnInvalidateItems", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the registered listeners that a node was added to the hierarchy.
		/// </summary>
		/// <param name="node">The node that was added.</param>
		public void OnItemAdded(Node node)
		{
			Tracer.VerifyNonNullArgument(node, "node");

			// Let all of our listeners know that an item was added.
			Tracer.WriteLine(classType, "OnItemAdded", Tracer.Level.Information, "Notifying all of our listeners that '{0}' was added to the hierarchy.", node.Caption);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsHierarchyEvents eventItem in clone)
			{
				uint parentId = (node.Parent != null ? node.Parent.HierarchyId : NativeMethods.VSITEMID_NIL);
				uint previousSiblingId = (node.PreviousSibling != null ? node.PreviousSibling.HierarchyId : NativeMethods.VSITEMID_NIL);
				uint nodeId = node.HierarchyId;
				try
				{
					eventItem.OnItemAdded(parentId, previousSiblingId, nodeId);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnItemAdded", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the registered listeners that a node was removed from the hierarchy.
		/// </summary>
		/// <param name="node">The node that was removed.</param>
		public void OnItemDeleted(Node node)
		{
			Tracer.VerifyNonNullArgument(node, "node");

			// Let all of our listeners know that an item was deleted.
			Tracer.WriteLine(classType, "OnItemDeleted", Tracer.Level.Information, "Notifying all of our listeners that '{0}' was removed from the hierarchy.", node.Caption);

			// There are some cases where the collection is changed while we're iterating it,
			// for example when the project node is removed. To be safe, we'll create a copy
			// of the collection and iterate over that. We just want a shallow copy, though,
			// and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsHierarchyEvents eventItem in clone)
			{
				try
				{
					eventItem.OnItemDeleted(node.HierarchyId);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnItemDeleted", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of our event listeners that an item in the hierarchy has changed.
		/// </summary>
		/// <param name="node">The <see cref="Node"/> that has changed.</param>
		/// <param name="propertyId">The property that has changed.</param>
		public void OnPropertyChanged(Node node, __VSHPROPID propertyId)
		{
			Tracer.VerifyNonNullArgument(node, "node");

			object newValue;
			node.GetProperty(propertyId, out newValue);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			for (int i = 0; i < clone.Count; i++)
			{
				IVsHierarchyEvents eventItem = (IVsHierarchyEvents)clone[i];
				Tracer.WriteLineVerbose(classType, "OnPropertyChanged", "Notifying event listener {0} that '{1}' has changed its {2} property to '{3}'.", this.CookieOf(i), node.Caption, propertyId, newValue);
				try
				{
					eventItem.OnPropertyChanged(node.HierarchyId, (int)propertyId, 0);
				}
				catch (Exception e)
				{
					Tracer.WriteLineWarning(classType, "OnPropertyChanged", "There was an exception in the event listener {0} event handling code: {1}", this.CookieOf(i), e.ToString());
				}
			}
		}
		#endregion
	}
}