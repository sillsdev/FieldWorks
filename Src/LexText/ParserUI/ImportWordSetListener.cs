// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportWordSetListener.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ImportWordSetListener - XCore listener that fires up an ImportWordSetDlg, if needed.
// </remarks>
using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ImportWordSetListener.
	/// </summary>
	public class ImportWordSetListener : IFlexComponent, IFWDisposable
	{
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
		~ImportWordSetListener()
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region XCORE Message Handlers

		/// <summary>
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to the problem
		/// on how to control we are CommandSet are handled by listeners are visible. It is difficult
		/// because some commands, like this one, may be appropriate from more than 1 area.</remarks>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				return (areaChoice == "textsWords");
			}
		}

#if RANDYTODO
		public virtual bool OnDisplayImportWordSet(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Handles the xWorks message for Edit Parser Parameters
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnImportWordSet(object argument)
		{
			CheckDisposed();

			using (ImportWordSetDlg dlg = new ImportWordSetDlg(PropertyTable, Publisher))
			{
				dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window"));
			}
			return true;
		}

		#endregion XCORE Message Handlers
	}
	/// <summary>
	/// Summary description for ImportWordSetListener.
	/// </summary>
	public class ParserParametersListener : IFlexComponent, IFWDisposable
	{
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
		~ParserParametersListener()
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message for Edit Parser Parameters
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnEditParserParameters(object argument)
		{
			CheckDisposed();

			var cache = PropertyTable.GetValue<FdoCache>("cache");
			if (cache == null)
				throw new ArgumentException("no cache!");

			using (var dlg = new ParserParametersDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				IMoMorphData md = cache.LangProject.MorphologicalDataOA;
				dlg.SetDlgInfo(ParserUIStrings.ksParserParameters, md.ParserParameters);
				if (dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window")) == DialogResult.OK)
				{
					using (var helper = new UndoableUnitOfWorkHelper(
						cache.ActionHandlerAccessor,
						ParserUIStrings.ksUndoEditingParserParameters,
						ParserUIStrings.ksRedoEditingParserParameters))
					{
						md.ParserParameters = dlg.XmlRep;
						helper.RollBack = false;
					}
				}
			}
			return true;
		}

		#endregion XCORE Message Handlers
	}
}
