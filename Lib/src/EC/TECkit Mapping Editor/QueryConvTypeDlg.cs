using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters40;
using System.Diagnostics;

namespace TECkit_Mapping_Editor
{
	public partial class QueryConvTypeDlg : Form
	{
		public QueryConvTypeDlg()
		{
			InitializeComponent();
		}

		public ConvType ConversionType
		{
			get
			{
				ConvType eConvType = ConvType.Legacy_to_from_Unicode;

				if (this.radioButtonLegacyLegacy.Checked)
				{
					if (this.checkBoxBiDirectional.Checked)
						eConvType = ConvType.Legacy_to_from_Legacy;
					else
						eConvType = ConvType.Legacy_to_Legacy;
				}
				else if (this.radioButtonLegacyUnicode.Checked)
				{
					if (this.checkBoxBiDirectional.Checked)
						eConvType = ConvType.Legacy_to_from_Unicode;
					else
						eConvType = ConvType.Legacy_to_Unicode;
				}
				else if (this.radioButtonUnicodeLegacy.Checked)
				{
					if (this.checkBoxBiDirectional.Checked)
						eConvType = ConvType.Unicode_to_from_Legacy;
					else
						eConvType = ConvType.Unicode_to_Legacy;
				}
				else if (this.radioButtonUnicodeUnicode.Checked)
				{
					if (this.checkBoxBiDirectional.Checked)
						eConvType = ConvType.Unicode_to_from_Unicode;
					else
						eConvType = ConvType.Unicode_to_Unicode;
				}
				else
					Debug.Assert(false);

				return eConvType;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}