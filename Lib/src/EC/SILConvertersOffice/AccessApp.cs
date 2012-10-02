using System;
using Office = Microsoft.Office.Core;
using Access = Microsoft.Office.Interop.Access;
using System.Windows.Forms;                     // for DialogResult
using ECInterfaces;
using SilEncConverters40;

namespace SILConvertersOffice
{
	internal class AccessApp : OfficeApp
	{
		public AccessApp(object app)
			: base(app)
		{
		}

		public new Access.Application Application
		{
			get { return (Access.Application)base.Application; }
		}

#if BUILD_FOR_OFF12
		public override string GetCustomUI()
		{
			return SILConvertersOffice07.Properties.Resources.RibbonAccess;
		}
#else
		public override void UnloadMenu()
		{
			ReleaseComObject(ConvertFieldMenu);
			base.UnloadMenu();
		}

		private Office.CommandBarPopup SILConvertersPopup;
		private Office.CommandBarButton ConvertFieldMenu;

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

					AddMenu(ref ConvertFieldMenu, this.SILConvertersPopup, "&Convert a field in a table",
						"Click this item to convert a field in a table with a converter from the system repository",
						new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ConvertTableFieldDialog_Click));
				}
				catch (Exception ex)
				{
					DisplayException(ex);
				}
			}

			ReleaseComObject(aDb);
		}
#endif

#if BUILD_FOR_OFF12
		public void ConvertTableFieldDialog_Click(Office.IRibbonControl control)
#else
		void ConvertTableFieldDialog_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
#endif
		{
#if DEBUG
			MessageBox.Show("ConvertTableFieldDialog_Click");
#endif
			dao.Database aDb = Application.CurrentDb();
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
				if (ex.Message != cstrAbortMessage)
				{
					if (MessageBox.Show("Would you like to rollback the transaction?", OfficeApp.cstrCaption, MessageBoxButtons.YesNo) == DialogResult.Yes)
						aDb.Rollback();
					else
						aDb.CommitTrans((int)dao.CommitTransOptionsEnum.dbForceOSFlush);
				}
			}
			finally
			{
				if (aRecordSet != null)
					aRecordSet.Close();
				ReleaseComObject(aRecordSet);
				ReleaseComObject(aTableDef);
				ReleaseComObject(aTableDefs);
				ReleaseComObject(aDb);
			}
		}
	}
}
