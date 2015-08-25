// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AreaManager.cs
// Responsibility:
// Last retooled:
//
// <remarks>
// This is a "Listener" which watches the 'areaChoice' property.
//
//	"Areas" are the major divisions of the application;these are represented as the big buttons in the navigation bar.
//		Each area has one or more "tools".
//
// </remarks>
// <example>
//	<code>
//		<listeners>
//			<listener assemblyPath="XCore.dll" class="XCore.AreaManager"/>
//		</listeners>
//	</code>
// </example>
using System;
using SIL.CoreImpl;
using SIL.Utils;

namespace XCore
{
#if RANDYTODO
	// Q? Do we need this listener base class? It's subclasses are:
	// MorphologyEditorDll::RespellerDlgListener
	// LexTextContorls::InsertEntryDlgListener
	// LexTextContorls::MergeEntryDlgListener
	// LexTextContorls::InsertRecordDlgListener
	// LexTextContorls::GoLinkRecordDlgListener
	//
	// If it is needed, then replace IxCoreColleague with IFlexComponent.
#endif
	/// <summary>
	/// Abstract class that implements common XCore code for several dlg classes.
	/// These classes are lighter-weight than the full dlgs that are getting created,
	/// whether they are used or not, when LexText launches.
	/// With this listener class, the dlg only gets created, when the user acts.
	/// Having this listener saves a lot of worry about resetting stuff on the dlg between openings,
	/// as the dlg gets created from scratch for each call.
	/// </summary>
	public abstract class DlgListenerBase : IFlexComponent, IFWDisposable
	{
		#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider;

		#endregion Data members

		#region Properties

		protected abstract string PersistentLabel
		{
			get;
		}

		#endregion Properties

		#region Construction and Initialization

		public DlgListenerBase()
		{
		}

		#endregion Construction and Initialization

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DlgListenerBase()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_persistProvider is IDisposable)
					((IDisposable)m_persistProvider).Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_persistProvider = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion
	}

}
