//-------------------------------------------------------------------------------------------------
// <copyright file="EventListenerCollection.cs" company="Microsoft">
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
// Abstract collection class for event listeners (IVsHierarchyEvents and IVsCfgProviderEvents, for example).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;

	/// <summary>
	/// Abstract collection class for event listeners (IVsHierarchyEvents and IVsCfgProviderEvents,
	/// for example). Stores the listener keyed by cookie.
	/// </summary>
	public abstract class EventListenerCollection : CloneableCollection
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(EventListenerCollection);
		private static uint nextCookie = 1;

		private SortedList listeners;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		protected EventListenerCollection() : base(new SortedList())
		{
			this.listeners = (SortedList)this.InnerCollection;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a collection of the value elements, which are the type of event listener
		/// (IVsHierarchyEvents, for example).
		/// </summary>
		protected ICollection Values
		{
			get { return this.listeners.Values; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public override IEnumerator GetEnumerator()
		{
			return this.listeners.Values.GetEnumerator();
		}

		public void Remove(uint cookie)
		{
			if (this.listeners.ContainsKey(cookie))
			{
				this.listeners.Remove(cookie);
				Tracer.WriteLine(classType, "Remove", Tracer.Level.Information, "Removing an event listener from the {0} collection: Cookie={1}", this.GetType().Name, cookie);
			}
			else
			{
				string message = "Attempting to unadvise an unregistered event sink. Cookie=" + cookie;
				Tracer.Fail(message);
				message = Package.Instance.Context.NativeResources.GetString(ResourceId.IDS_E_UNADVISINGUNREGISTEREDEVENTSINK, cookie);
				Package.Instance.Context.ShowErrorMessageBox(message);
			}
		}

		protected uint Add(object listener)
		{
			Tracer.VerifyNonNullArgument(listener, "listener");

			Tracer.WriteLine(classType, "Add", Tracer.Level.Information, "Adding an event listener to the {0} collection: Cookie={1}", this.GetType().Name, nextCookie);
			this.listeners.Add(nextCookie, listener);
			nextCookie++;

			return (nextCookie - 1);
		}

		protected uint CookieOf(int index)
		{
			return (uint)this.listeners.GetKey(index);
		}

		protected object GetAt(int index)
		{
			return this.listeners.GetByIndex(index);
		}
		#endregion
	}
}