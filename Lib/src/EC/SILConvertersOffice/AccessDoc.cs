using System;
using System.Collections.Generic;
using System.Text;
using Access = Microsoft.Office.Interop.Access;
using System.Windows.Forms;                     // for DialogResult
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal class AccessRange : OfficeRange
	{
		protected AccessDocument m_doc = null;
		protected dao.Field m_field = null;
		protected int m_nRecordIndex = 0;

		protected dao.Field RangeBasedOn
		{
			get { return m_field; }
		}

		public AccessRange(dao.Field basedOnRange, AccessDocument rsDoc)
		{
			m_doc = rsDoc;
			m_field = basedOnRange;
		}

		public override int End
		{
			get { return m_nRecordIndex; }
			set { m_nRecordIndex = value; }
		}

		public override string FontName
		{
			get { return ""; }
		}

		public override string Text
		{
			get { return (string)RangeBasedOn.Value; }
			set { m_doc.UpdateValue(RangeBasedOn, value); }
		}

		public override void Select()
		{
		}
	}

	internal class AccessDocument : OfficeDocument
	{
		protected string m_strFieldName = null;

		public AccessDocument(dao.Recordset doc, string strFieldName)
			: base(doc)
		{
			m_strFieldName = strFieldName;
		}

		public dao.Recordset Document
		{
			get { return (dao.Recordset)m_baseDocument; }
		}

		public override int WordCount
		{
			get
			{
				return Document.RecordCount;
			}
		}

		public void UpdateValue(dao.Field aField, string strValue)
		{
			dao.Recordset rs = Document;
			rs.Edit();
			aField.Value = strValue;
			rs.Update((int)dao.UpdateTypeEnum.dbUpdateRegular, false);
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			Document.MoveFirst();   // to avoid an error on the first 'Edit'
			int nDontCare = 0; // don't care
			while (!Document.EOF)
			{
				dao.Fields aFields = Document.Fields;
				dao.Field aField = aFields[m_strFieldName];
				if (aField.Value != System.DBNull.Value)
				{
					AccessRange aWordRange = new AccessRange(aField, this);

					try
					{
						if (!aWordProcessor.Process(aWordRange, ref nDontCare))
							return false;
					}
					catch (Exception)
					{
						throw;
					}
					finally
					{
						// this gets called whether we successfully process the word or not,
						//  whether we're returning 'false' (in the try) or not, and whether we
						//  have an exception or not... just exactly what we want, or MSAccess
						//  process won't release when we exit.
						OfficeApp.ReleaseComObject(aField);
						OfficeApp.ReleaseComObject(aFields);
					}
				}

				Document.MoveNext();
			}

			return true;
		}
	}
}
