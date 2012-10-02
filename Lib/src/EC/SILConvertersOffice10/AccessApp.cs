using System;
using Microsoft.Office.Interop.Access.Dao;
using SILConvertersOffice;
using Office = Microsoft.Office.Core;
using Access = Microsoft.Office.Interop.Access;
using System.Windows.Forms;                     // for DialogResult
using ECInterfaces;
using SilEncConverters40;

namespace SILConvertersOffice10
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

		public override string GetCustomUI()
		{
			return Properties.Resources.RibbonAccess;
		}

		public void ConvertTableFieldDialog_Click(Office.IRibbonControl control)
		{
#if DEBUG
			MessageBox.Show("ConvertTableFieldDialog_Click");
#endif

			Database aDb = Application.CurrentDb();
			if (aDb == null)
				return;

			TableDefs aTableDefs = null;
			TableDef aTableDef = null;
			Recordset aRecordSet = null;
			try
			{
				aTableDefs = aDb.TableDefs;
				DbFieldSelect dlg = new DbFieldSelect(aTableDefs);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					aTableDef = aTableDefs[dlg.TableName];
					aRecordSet = aTableDef.OpenRecordset(RecordsetTypeEnum.dbOpenTable, 0);
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
						aDb.CommitTrans((int)CommitTransOptionsEnum.dbForceOSFlush);
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
						aDb.CommitTrans((int)CommitTransOptionsEnum.dbForceOSFlush);
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
