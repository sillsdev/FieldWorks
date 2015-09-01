using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls
{

#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: InsertRecordDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Notebook\
	// TODO: when the time comes to re-enable the Notebook area tools that use it.
	//
	// TODO: Likely disposition: Dump InsertRecordDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the insertion.
	//
	/// <summary>
	/// Listener class for the InsertEntryDlg class.
	/// </summary>
	public class InsertRecordDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "InsertRecord"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region XCORE Message Handlers

		/// <summary>
		/// Handles the xWorks message to insert a new Data Notebook record.
		/// Invoked by the RecordClerk
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true, if we handled the message, otherwise false, if there was an unsupported 'classname' parameter</returns>
		public bool OnDialogInsertItemInVector(object argument)
		{
			CheckDisposed();

#if RANDYTODO
			var command = (Command) argument;
			string className = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "className");
			if (className == null || className != "RnGenericRec")
				return false;

			bool subrecord = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subrecord", false);
			bool subsubrecord = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subsubrecord", false);

			using (var dlg = new InsertRecordDlg())
			{
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				ICmObject obj = null;
				ICmObject objSelected = PropertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject");
				ICmObject objOwning = PropertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
				if (subsubrecord)
				{
					obj = objSelected;
				}
				else if (subrecord && objSelected != null)
				{
					obj = objSelected;
					while (obj.Owner is IRnGenericRec)
						obj = obj.Owner;
				}
				else
				{
					obj = objOwning;
				}
				dlg.SetDlgInfo(cache, PropertyTable, obj);
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					Publisher.Publish("JumpToRecord", dlg.NewRecord.Hvo);
				}
			}
#endif
			return true; // We "handled" the message, regardless of what happened.
		}

	#endregion XCORE Message Handlers
	}
#endif

#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: GoLinkRecordDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Notebook\
	// TODO: when the time comes to re-enable the Notebook area tools that use it.
	// TODO: Check "InFriendlyArea" property, as not all tools use this listener.
	//
	// TODO: Likely disposition: Dump GoLinkRecordDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the insertion.
	//
	public class GoLinkRecordDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Overrides of DlgListenerBase

		protected string PersistentLabel
		{
			get { return "GoLinkRecord"; } // Was on DlgListenerBase base class
		}

	#endregion

		/// <summary>
		/// Handles the xCore message to go to or link to a lexical entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoRecord(object argument)
		{
			CheckDisposed();

			using (var dlg = new RecordGoDlg())
			{
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, null, PropertyTable, Publisher);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					Publisher.Publish("JumpToRecord", dlg.SelectedObject.Hvo);
				}
			}
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayGotoRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				var areas = new[] { "notebook" };
				foreach (string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						var obj = PropertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == RnResearchNbkTags.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}
	}
#endif
}
