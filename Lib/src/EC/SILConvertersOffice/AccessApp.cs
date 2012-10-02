#define UseDialogBoxToGetTableField

using System;
using System.Collections.Generic;
using System.Text;
using Office = Microsoft.Office.Core;
using Access = Microsoft.Office.Interop.Access;
using System.Windows.Forms;                     // for DialogResult
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal class AccessApp : OfficeApp
	{
		private Office.CommandBarPopup SILConvertersPopup;
#if UseDialogBoxToGetTableField
		private Office.CommandBarButton ConvertFieldMenu;
#else
		public Dictionary<Office.CommandBarPopup, List<Office.CommandBarButton>> m_listTablePopups = new Dictionary<Microsoft.Office.Core.CommandBarPopup,List<Microsoft.Office.Core.CommandBarButton>>();
#endif

		public AccessApp(object app)
			: base(app)
		{
		}

		public new Access.Application Application
		{
			get { return (Access.Application)base.Application; }
		}

		public override void UnloadMenu()
		{
#if UseDialogBoxToGetTableField
			ReleaseComObject(ConvertFieldMenu);
#else
			foreach (Office.CommandBarPopup aTablePopup in m_listTablePopups.Keys)
			{
				List<Office.CommandBarButton> aListOfButtons = m_listTablePopups[aTablePopup];
				foreach (Office.CommandBarButton aButton in aListOfButtons)
					ReleaseComObject(aButton);
				aListOfButtons.Clear();
				ReleaseComObject(aTablePopup);
			}
			m_listTablePopups.Clear();
#endif
			base.UnloadMenu();
		}

		public override void LoadMenu()
		{
			dao.Database aDb = (dao.Database)Application.CurrentDb();
			if (aDb == null)
			{
				// Application.NewCurrentDatabase
			}
			else
			{
				base.LoadMenu();

				try
				{
					SILConvertersPopup = (Office.CommandBarPopup)NewMenuBar.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
					SILConvertersPopup.Caption = "&SIL Converters";

#if !UseDialogBoxToGetTableField
					foreach (dao.TableDef aTable in aDb.TableDefs)
					{
						if (aTable.Attributes == 0) // don't want system tables
						{
							Office.CommandBarPopup aPopup = (Office.CommandBarPopup)SILConvertersPopup.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
							aPopup.Caption = aTable.Name;

							List<Office.CommandBarButton> aListOfCommandButtons = new List<Microsoft.Office.Core.CommandBarButton>();
							foreach (dao.Field aField in aTable.Fields)
							{
								Office.CommandBarButton aFieldButton = null;
								AddMenu(ref aFieldButton, aPopup, aField.Name, aTable.Name,
								new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ConvertTableField_Click));
								aListOfCommandButtons.Add(aFieldButton);
							}

							m_listTablePopups.Add(aPopup, aListOfCommandButtons);
						}
					}
#else
					AddMenu(ref ConvertFieldMenu, this.SILConvertersPopup, "&Convert a field in a table",
						"Click this item to convert a field in a table with a converter from the system repository",
						new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ConvertTableFieldDialog_Click));
#endif
				}
				catch (Exception ex)
				{
					DisplayException(ex);
				}
			}

			ReleaseComObject(aDb);
		}

#if !UseDialogBoxToGetTableField
		void ConvertTableField_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			string strFieldName = Ctrl.Caption;
			string strTableName = Ctrl.TooltipText;
			MessageBox.Show(String.Format("Converting '{0}.{1}'", strTableName, strFieldName));
		}
#else
		void ConvertTableFieldDialog_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			dao.Database aDb = (dao.Database)Application.CurrentDb();
			if (aDb == null)
				return;

			dao.TableDefs aTableDefs = null;
			dao.TableDef aTableDef = null;
			dao.Recordset aRecordSet = null;
			try
			{
				aTableDefs = aDb.TableDefs;
				DbFieldSelect dlg = new DbFieldSelect(aTableDefs);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					aTableDef = aTableDefs[dlg.TableName];
					aRecordSet = aTableDef.OpenRecordset(dao.RecordsetTypeEnum.dbOpenTable, 0);
					// dao.RecordsetOptionEnum.);

					if (!aRecordSet.Updatable)
						throw new ApplicationException("Can't edit this table? Is it opened? If so, then close it and try again.");

					string strTitle = String.Format("Select the Converter for the {0}.{1} field", dlg.TableName, dlg.FieldName);
					EncConverters aECs = GetEncConverters;
					if (aECs != null)
					{
						IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, strTitle);
						FontConverter aFC = new FontConverter(new DirectableEncConverter(aIEC));
						OfficeDocumentProcessor aTableProcessor = new OfficeDocumentProcessor(aFC, new SILConverterProcessorForm());
						AccessDocument rsDoc = new AccessDocument(aRecordSet, dlg.FieldName);

						// do a transaction just in case we throw an exception trying to update and the user
						//  wants to rollback.
						aDb.BeginTrans();
						rsDoc.ProcessWordByWord(aTableProcessor);
						aDb.CommitTrans((int)dao.CommitTransOptionsEnum.dbForceOSFlush);
					}
				}
			}
			catch (Exception ex)
			{
				DisplayException(ex);
				if (ex.Message != Connect.cstrAbortMessage)
				{
					if (MessageBox.Show("Would you like to rollback the transaction?", Connect.cstrCaption, MessageBoxButtons.YesNo) == DialogResult.Yes)
						aDb.Rollback();
					else
						aDb.CommitTrans((int)dao.CommitTransOptionsEnum.dbForceOSFlush);
				}
			}
			finally
			{
				if( aRecordSet != null )
					aRecordSet.Close();
				ReleaseComObject(aRecordSet);
				ReleaseComObject(aTableDef);
				ReleaseComObject(aTableDefs);
				ReleaseComObject(aDb);
			}
		}
#endif
	}
}
