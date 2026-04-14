// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Listener class for adding POSes via Insert menu.
	/// </summary>
	[XCore.MediatorDispose]
	public class MasterCatDlgListener : MasterDlgListener
	{
		#region Properties

		protected override string PersistentLabel
		{
			get { return "InsertCategory"; }
		}

		#endregion Properties

		#region Construction and Initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public MasterCatDlgListener()
		{
			Subscriber.Subscribe(EventConstants.DialogInsertItemInVector, DialogInsertItemInVector);
		}

		#endregion Construction and Initialization

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MasterCatDlgListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Subscriber.Unsubscribe(EventConstants.DialogInsertItemInVector, DialogInsertItemInVector);
			}
			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the message to insert a new PartOfSpeech.
		/// Invoked by the RecordClerk via a main menu.
		/// </summary>
		/// <param name="obj">Object that contains the xCore Command object and has a ReturnValue. The
		/// ReturnValue is true if we handled the message.</param>
		private void DialogInsertItemInVector(object obj)
		{
			CheckDisposed();

			if (!(obj is ReturnObject retObj) ||
				!(retObj.Data is Command command))
			{
				Debug.Assert(false, "Received unexpected object type.");
				return;
			}
			// Return if already handled by another Subscriber.
			if (retObj.ReturnValue)
			{
				return;
			}
			// Only handle "PartOfSpeech" class.
			string className = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "className");
			if (className == null || className != "PartOfSpeech")
			{
				return;
			}

			using (var dlg = new MasterCategoryListDlg())
			{
				LcmCache cache = m_propertyTable.GetValue<LcmCache>("cache");
				Debug.Assert(cache != null);
				var owningObj = m_propertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
				dlg.SetDlginfo((owningObj is ICmPossibilityList) ? owningObj as ICmPossibilityList : cache.LangProject.PartsOfSpeechOA, m_mediator, m_propertyTable, true, null);
				switch (dlg.ShowDialog(m_propertyTable.GetValue<Form>("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						// This is the equivalent functionality, but is deferred processing.
						// This is done so that the JumpToRecord can be processed last.
#pragma warning disable 618 // suppress obsolete warning
						m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedPOS.Hvo);
#pragma warning restore 618
						break;
				}
			}
			retObj.ReturnValue = true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}
}
