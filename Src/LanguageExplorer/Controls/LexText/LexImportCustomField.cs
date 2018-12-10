// Copyright (c) 2006-2028 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using LanguageExplorer.SfmToXml;

namespace LanguageExplorer.Controls.LexText
{
	internal class LexImportCustomField : LexImportField, ILexImportCustomField
	{
		private uint m_crc;

		// This is a TERRIBLE constructor with so many parameters ... but, so be it for now, otherwise
		//  it would require too many set accessors
		public LexImportCustomField(int fdClass, string uiClass, int flid, bool big, int wsSelector,
			string name, string uiDest, string prop, string sig, bool list, bool multi, bool unique, string mdf)
			: base(name, uiDest, prop, sig, list, multi, unique, mdf)
		{
			const int kclidLexEntry = 5002;
			const int kclidLexSense = 5016;

			Class = fdClass == kclidLexEntry ? "LexEntry" : fdClass== kclidLexSense?"LexSense":"UnknownClass";
			UIClass = uiClass;
			FLID = flid;
			Big = big;
			WsSelector = wsSelector;
		}

		public string CustomKey => $"_n:{UIName}_c:{Class}_t:{Signature}";

		public int WsSelector { get; }

		public bool Big { get; }

		public int FLID { get; }

		public string Class { get; }

		public string UIClass { get; set; }

		public uint CRC	// intent is to use this value to compare to others to see if they are the same or different
		{
			get
			{
				if (m_crc == 0)
				{
					var data = new StringBuilder();
					data.Append(CustomKey);
					data.Append('1');
					data.Append(WsSelector);
					data.Append('2');
					data.Append(Big);
					data.Append('3');
					data.Append(FLID);
					data.Append('4');
					data.Append(Class);
					data.Append('5');
					data.Append(UIClass);
					data.Append('6');
					data.Append(ListRootId);

					var asciiEncoding = new ASCIIEncoding();
					var byteData = asciiEncoding.GetBytes(data.ToString());

					var crc = new CRC();
					m_crc = crc.CalculateCRC(byteData, byteData.Length);
				}
				return m_crc;
			}
		}

		public Guid ListRootId { get; set; } = Guid.Empty;
	}
}