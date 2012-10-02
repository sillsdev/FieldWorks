using System;
using System.Collections.Generic;
using System.Text;

namespace SilEncConverters40
{
	public class EcTextBox : System.Windows.Forms.TextBox
	{
		protected override void OnPreviewKeyDown(System.Windows.Forms.PreviewKeyDownEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("PreviewKeyDown: KeyValue: {0}, KeyCode: {1}, KeyData: {2}",
				e.KeyValue, e.KeyCode, e.KeyData));
			if (e.Alt && (e.KeyCode == System.Windows.Forms.Keys.X))
				ConvertNumberToChar(false);
			base.OnPreviewKeyDown(e);
		}

		protected void ConvertNumberToChar(bool bIsLegacy)
		{
			int nCharsToLook = 4;   // assume unicode
			if (bIsLegacy)
				nCharsToLook = 3;
			int nCaretLocation = this.SelectionStart;
			this.Select(Math.Max(0, nCaretLocation - nCharsToLook), nCharsToLook);
			string str = null;
			int nVal = 0;
			try
			{
				if (bIsLegacy)
				{
					if (this.SelectedText[0] == 'x') // hex format
					{
						this.Select(this.SelectionStart - 1, 4);    // grab the preceding '0' as well
						nVal = Convert.ToInt32(this.SelectedText, 16);
					}
					else
						nVal = Convert.ToInt32(this.SelectedText, 10);

					// however, the value is a byte value and for display we need Unicode (word) values
					byte[] aby = new byte[1] { (byte)nVal };
					char[] ach = Encoding.Default.GetChars(aby);
					nVal = ach[0];
				}
				else
					nVal = Convert.ToInt32(this.SelectedText, 16);
				str += (char)nVal;
				this.SelectedText = str;
			}
			catch (FormatException)
			{
				// give up and go back to the way it was
				if (nCaretLocation > 0)
				{
					this.Select(nCaretLocation - 1, 1);
					if (this.SelectedText.Length > 0)
					{
						char ch = this.SelectedText[0];
						string strHex = String.Format("{0:X4}", (int)ch);
						this.SelectedText = strHex;
						return;
					}
				}

				this.Select(nCaretLocation, 0);
			}
		}
	}
}
