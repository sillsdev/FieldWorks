// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MasterPhonFeatDlgListener.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Listener class for adding POSes via Insert menu.
	/// </summary>
	[XCore.MediatorDispose]
	public class MasterPhonFeatDlgListener : MasterDlgListener
	{

		#region Properties

		protected override string PersistentLabel
		{
			get { return "InsertPhonologicalFeature"; }
		}

		#endregion Properties

		#region Construction and Initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public MasterPhonFeatDlgListener()
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
		~MasterPhonFeatDlgListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		#endregion IDisposable & Co. implementation

		#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message to insert a new FsFeatDefn.
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
			if ((className == null) || (className != "FsClosedFeature"))
				return false;
			if (className == "FsClosedFeature" && (argument as XCore.Command).Id != "CmdInsertPhonologicalClosedFeature")
				return false;

			using (MasterPhonologicalFeatureListDlg dlg = new MasterPhonologicalFeatureListDlg(className))
			{
				FdoCache cache = m_propertyTable.GetValue<FdoCache>("cache");
				Debug.Assert(cache != null);
				string sXmlFile = Path.Combine(FwDirectoryFinder.CodeDirectory, String.Format("Language Explorer{0}MGA{0}GlossLists{0}PhonFeatsEticGlossList.xml", Path.DirectorySeparatorChar));
				if (cache.LanguageProject.PhFeatureSystemOA == null)
				{
					// Mainly for Memory-only backend, makes system more self-repairing.
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
					   () =>
					   {
						   cache.LanguageProject.PhFeatureSystemOA =
							   cache.ServiceLocator.GetInstance<IFsFeatureSystemFactory>().Create();
					   });
				}
				dlg.SetDlginfo(cache.LangProject.PhFeatureSystemOA, m_mediator, m_propertyTable, true, "masterPhonFeatListDlg", sXmlFile);
				switch (dlg.ShowDialog(m_propertyTable.GetValue<Form>("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						//m_mediator.SendMessage("JumpToRecord", dlg.SelectedFeatDefn.Hvo);
						// This is the equivalent functionality, but is deferred processing.
						// This is done so that the JumpToRecord can be processed last.
						if (dlg.SelectedFeatDefn != null)
							m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedFeatDefn.Hvo);
						// LT-6412: this call will now cause the Mediator to be disposed while it is busy processing
						// this call, so there is code in the Mediator to handle in the middle of a msg the case
						// where the object is nolonger valid.  This has happend before and was being handled, this
						// call "SendMessageToAllNow" has not had the code to handle the exception, so it was added.
						m_mediator.SendMessageToAllNow("MasterRefresh", cache.LangProject.PhFeatureSystemOA);
						break;
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}
}
