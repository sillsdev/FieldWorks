// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MasterCatDlgListener.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

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


		#endregion IDisposable & Co. implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message to insert a new PartOfSpeech.
		/// Invoked by the RecordClerk via a main menu.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true, if we handled the message, otherwise false, if there was an unsupported 'classname' parameter</returns>
		public override bool OnDialogInsertItemInVector(object argument)
		{
			CheckDisposed();

			Debug.Assert(argument != null && argument is XCore.Command);
			string className = XmlUtils.GetOptionalAttributeValue(
				(argument as XCore.Command).Parameters[0], "className");
			if (className == null || className != "PartOfSpeech")
				return false;

			using (var dlg = new MasterCategoryListDlg())
			{
				FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				Debug.Assert(cache != null);
				var owningObj = m_mediator.PropertyTable.GetValue("ActiveClerkOwningObject") as ICmObject;
				dlg.SetDlginfo((owningObj is ICmPossibilityList) ? owningObj as ICmPossibilityList : cache.LangProject.PartsOfSpeechOA, m_mediator, true, null);
				switch (dlg.ShowDialog((Form)m_mediator.PropertyTable.GetValue("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						// This is the equivalent functionality, but is deferred processing.
						// This is done so that the JumpToRecord can be processed last.
						m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedPOS.Hvo);
						break;
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}
}
