using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Listener class for the InsertEntryDlg class.
	/// </summary>
	public class InsertRecordDlgListener : DlgListenerBase
	{
		#region Properties

		protected override string PersistentLabel
		{
			get { return "InsertRecord"; }
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

			var command = (Command) argument;
			string className = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "className");
			if (className == null || className != "RnGenericRec")
				return false;

			bool subrecord = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subrecord", false);
			bool subsubrecord = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subsubrecord", false);

			using (var dlg = new InsertRecordDlg())
			{
				var cache = m_propertyTable.GetValue<FdoCache>("cache");
				ICmObject obj = null;
				ICmObject objSelected = m_propertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject");
				ICmObject objOwning = m_propertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
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
				dlg.SetDlgInfo(cache, m_mediator, m_propertyTable, obj);
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					IRnGenericRec newRec = dlg.NewRecord;
					m_mediator.SendMessage("JumpToRecord", newRec.Hvo);
				}
			}
			return true; // We "handled" the message, regardless of what happened.
		}

		#endregion XCORE Message Handlers
	}

	public class GoLinkRecordDlgListener : DlgListenerBase
	{
		#region Overrides of DlgListenerBase

		protected override string PersistentLabel
		{
			get { return "GoLinkRecord"; }
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
				var cache = m_propertyTable.GetValue<FdoCache>("cache");
				Debug.Assert(cache != null);
				dlg.SetDlgInfo(cache, null, m_mediator, m_propertyTable);
				if (dlg.ShowDialog() == DialogResult.OK)
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedObject.Hvo);
			}
			return true;
		}

		public virtual bool OnDisplayGotoRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = m_propertyTable.GetStringProperty("areaChoice", null);
				var areas = new[] { "notebook" };
				foreach (string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						var obj = m_propertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == RnResearchNbkTags.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}
	}
}
