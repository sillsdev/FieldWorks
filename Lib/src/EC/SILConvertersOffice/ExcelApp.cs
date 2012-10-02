using System;
using System.Collections.Generic;
using System.Text;
using Office = Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows.Forms;                     // for DialogResult
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal class ExcelApp : OfficeApp
	{
		private Office.CommandBarPopup SILConvertersPopup;
		private Office.CommandBarButton ConvertFieldMenu;
		private Office.CommandBarButton ProcessTableResetMenu;

		public ExcelApp(object app)
			: base(app)
		{
		}

		public new Excel.Application Application
		{
			get { return (Excel.Application)base.Application; }
		}

		public override void LoadMenu()
		{
			base.LoadMenu();

			try
			{
				SILConvertersPopup = (Office.CommandBarPopup)NewMenuBar.Controls.Add(Office.MsoControlType.msoControlPopup, missing, missing, missing, true);
				SILConvertersPopup.Caption = "&SIL Converters";

				AddMenu(ref ConvertFieldMenu, this.SILConvertersPopup, "&Convert selection",
					"Click this item to convert the text in the selected cells with a converter from the system repository",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(ConvertTableField_Click));

				AddMenu(ref ProcessTableResetMenu, SILConvertersPopup, "&Reset",
					"Reset the list of found items and the unfinished conversion process",
					new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(Reset_Click));
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		private OfficeDocumentProcessor m_aSelectionProcessor = null;
		void ConvertTableField_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			try
			{
				if (Application.Selection == null)
					throw new ApplicationException("First select the cells that you want to convert using a system converter");

				ExcelDocument doc = new ExcelDocument(Application.Selection);
				if (m_aSelectionProcessor == null)
				{
					string strTitle = "Choose the converter for the selected areas/cells";
					EncConverters aECs = GetEncConverters;
					if (aECs != null)
					{
						IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, strTitle);
						FontConverter aFC = new FontConverter(new DirectableEncConverter(aIEC));
						m_aSelectionProcessor = new OfficeDocumentProcessor(aFC, new SILConverterProcessorForm());
					}
				}

				if (m_aSelectionProcessor != null)
					if (doc.ProcessWordByWord(m_aSelectionProcessor))
						m_aSelectionProcessor = null;
			}
			catch (Exception ex)
			{
				DisplayException(ex);
			}
		}

		void Reset_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
		{
			m_aSelectionProcessor = null;
		}
	}
}
