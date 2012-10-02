using System;
using Microsoft.Office.Interop.Access.Dao;
using SILConvertersOffice;

namespace SILConvertersOffice10
{
	internal class AccessRange : OfficeRange
	{
		protected AccessDocument m_doc = null;
		protected Field m_field = null;
		protected int m_nRecordIndex = 0;

		protected Field RangeBasedOn
		{
			get { return m_field; }
		}

		public AccessRange(Field basedOnRange, AccessDocument rsDoc)
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

		public AccessDocument(Recordset doc, string strFieldName)
			: base(doc)
		{
			m_strFieldName = strFieldName;
		}

		public Recordset Document
		{
			get { return (Recordset)m_baseDocument; }
		}

		public override int WordCount
		{
			get
			{
				return Document.RecordCount;
			}
		}

		public void UpdateValue(Field aField, string strValue)
		{
			Recordset rs = Document;
			rs.Edit();
			aField.Value = strValue;
			rs.Update((int)UpdateTypeEnum.dbUpdateRegular, false);
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			Document.MoveFirst();   // to avoid an error on the first 'Edit'
			int nDontCare = 0; // don't care
			while (!Document.EOF)
			{
				Fields aFields = Document.Fields;
				Field aField = aFields[m_strFieldName];
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
