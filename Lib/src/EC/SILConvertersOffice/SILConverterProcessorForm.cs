using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal partial class SILConverterProcessorForm : SILConvertersOffice.BaseConverterForm
	{
		public SILConverterProcessorForm()
		{
			InitializeComponent();
		}

		private void buttonDebug_Click(object sender, EventArgs e)
		{
			DirectableEncConverter aEC = m_aFontPlusEC.DirectableEncConverter;
			if (aEC != null)
			{
				IEncConverter aIEC = aEC.GetEncConverter;
				if (aIEC != null)
				{
					bool bOrigValue = aIEC.Debug;
					aIEC.Debug = true;

					RefreshTextBoxes(aEC);

					aIEC.Debug = bOrigValue;
				}
			}
		}

		protected void buttonRefresh_Click(object sender, EventArgs e)
		{
			RefreshTextBoxes(m_aFontPlusEC.DirectableEncConverter);
		}
	}
}
