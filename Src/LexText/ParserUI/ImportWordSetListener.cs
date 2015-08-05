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
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ImportWordSetListener.
	/// </summary>
	[XCore.MediatorDispose]
	public class ImportWordSetListener : IxCoreColleague, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// xCore Mediator.
		/// </summary>
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;

		#endregion Data members

		public ImportWordSetListener()
		{
		}

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
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize the IxCoreColleague object.
		/// </summary>
		public void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_mediator.AddColleague(this);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion IxCoreColleague implementation

		#region XCORE Message Handlers

		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to the problem
		/// on how to control we are CommandSet are handled by listeners are visible. It is difficult
		/// because some commands, like this one, may be appropriate from more than 1 area.</remarks>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				string areaChoice = m_propertyTable.GetValue<string>("areaChoice");
				return (areaChoice == "textsWords");
			}
		}

		public virtual bool OnDisplayImportWordSet(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
		/// <summary>
		/// Handles the xWorks message for Edit Parser Parameters
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnImportWordSet(object argument)
		{
			CheckDisposed();

			using (ImportWordSetDlg dlg = new ImportWordSetDlg(m_mediator, m_propertyTable))
			{
				dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window"));
			}
			return true;
		}

		#endregion XCORE Message Handlers
	}
	/// <summary>
	/// Summary description for ImportWordSetListener.
	/// </summary>
	[XCore.MediatorDispose]
	public class ParserParametersListener : IxCoreColleague, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// xCore Mediator.
		/// </summary>
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;

		#endregion Data members

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
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize the IxCoreColleague object.
		/// </summary>
		public void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_mediator.AddColleague(this);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium;  }
		}

		#endregion IxCoreColleague implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message for Edit Parser Parameters
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnEditParserParameters(object argument)
		{
			CheckDisposed();

			var cache = m_propertyTable.GetValue<FdoCache>("cache");
			if (cache == null)
				throw new ArgumentException("no cache!");

			using (var dlg = new ParserParametersDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				IMoMorphData md = cache.LangProject.MorphologicalDataOA;
				dlg.SetDlgInfo(ParserUIStrings.ksParserParameters, md.ParserParameters);
				if (dlg.ShowDialog(m_propertyTable.GetValue<XWindow>("window")) == DialogResult.OK)
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
