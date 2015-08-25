// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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

#if RANDYTODO
			string className = XmlUtils.GetOptionalAttributeValue(
				(argument as XCore.Command).Parameters[0], "className");
			if (className == null || className != "PartOfSpeech")
				return false;
#endif

			using (var dlg = new MasterCategoryListDlg())
			{
				FdoCache cache = PropertyTable.GetValue<FdoCache>("cache");
				Debug.Assert(cache != null);
				var owningObj = PropertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
				dlg.SetDlginfo((owningObj is ICmPossibilityList) ? owningObj as ICmPossibilityList : cache.LangProject.PartsOfSpeechOA, PropertyTable, true, null);
				switch (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						// This is the equivalent functionality, but is deferred processing.
						// This is done so that the JumpToRecord can be processed last.
						Publisher.Publish("JumpToRecord", dlg.SelectedPOS.Hvo);
						break;
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}
}
