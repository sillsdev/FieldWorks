using System;

namespace TECkit_Mapping_Editor
{
	partial class DisplayUnicodeNamesForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisplayUnicodeNamesForm));
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.radioButtonDecimal = new System.Windows.Forms.RadioButton();
			this.radioButtonHexadecimal = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeNames = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeValues = new System.Windows.Forms.RadioButton();
			this.radioButtonQuotedChars = new System.Windows.Forms.RadioButton();
			this.radioButtonByCodePoint = new System.Windows.Forms.RadioButton();
			this.radioButtonUnicodeSubsets = new System.Windows.Forms.RadioButton();
			this.comboBoxCodePointRange = new System.Windows.Forms.ComboBox();
			this.dataGridViewCharacters = new TECkit_Mapping_Editor.MyDataGridView();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.helpProviderCP = new System.Windows.Forms.HelpProvider();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.groupBoxSendToEditor = new System.Windows.Forms.GroupBox();
			this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.labelFontName = new System.Windows.Forms.Label();
			this.flowLayoutPanelChooseByRange = new System.Windows.Forms.FlowLayoutPanel();
			this.groupBoxUnicodeRanges = new System.Windows.Forms.GroupBox();
			this.flowLayoutPanelRecentRanges = new System.Windows.Forms.FlowLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewCharacters)).BeginInit();
			this.statusStrip.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			this.groupBoxSendToEditor.SuspendLayout();
			this.flowLayoutPanel.SuspendLayout();
			this.flowLayoutPanelChooseByRange.SuspendLayout();
			this.groupBoxUnicodeRanges.SuspendLayout();
			this.SuspendLayout();
			//
			// toolTip
			//
			this.toolTip.AutoPopDelay = 20000;
			this.toolTip.InitialDelay = 500;
			this.toolTip.ReshowDelay = 100;
			this.toolTip.ShowAlways = true;
			//
			// radioButtonDecimal
			//
			this.radioButtonDecimal.AutoSize = true;
			this.radioButtonDecimal.Location = new System.Drawing.Point(3, 3);
			this.radioButtonDecimal.Name = "radioButtonDecimal";
			this.radioButtonDecimal.Size = new System.Drawing.Size(63, 17);
			this.radioButtonDecimal.TabIndex = 0;
			this.radioButtonDecimal.TabStop = true;
			this.radioButtonDecimal.Text = "&Decimal";
			this.toolTip.SetToolTip(this.radioButtonDecimal, "When you click on a cell, the decimal value of the character will be inserted at " +
					"the current cursor location of the map file");
			this.radioButtonDecimal.UseVisualStyleBackColor = true;
			//
			// radioButtonHexadecimal
			//
			this.radioButtonHexadecimal.AutoSize = true;
			this.radioButtonHexadecimal.Location = new System.Drawing.Point(3, 26);
			this.radioButtonHexadecimal.Name = "radioButtonHexadecimal";
			this.radioButtonHexadecimal.Size = new System.Drawing.Size(86, 17);
			this.radioButtonHexadecimal.TabIndex = 1;
			this.radioButtonHexadecimal.TabStop = true;
			this.radioButtonHexadecimal.Text = "&Hexadecimal";
			this.toolTip.SetToolTip(this.radioButtonHexadecimal, "When you click on a cell, the hexadecimal value of the character will be inserted" +
					" at the current cursor location of the map file");
			this.radioButtonHexadecimal.UseVisualStyleBackColor = true;
			//
			// radioButtonUnicodeNames
			//
			this.radioButtonUnicodeNames.AutoSize = true;
			this.radioButtonUnicodeNames.Location = new System.Drawing.Point(3, 49);
			this.radioButtonUnicodeNames.Name = "radioButtonUnicodeNames";
			this.radioButtonUnicodeNames.Size = new System.Drawing.Size(101, 17);
			this.radioButtonUnicodeNames.TabIndex = 2;
			this.radioButtonUnicodeNames.TabStop = true;
			this.radioButtonUnicodeNames.Text = "Unicode &Names";
			this.toolTip.SetToolTip(this.radioButtonUnicodeNames, "When you click on a cell, the Unicode name of the character will be inserted at t" +
					"he current cursor location of the map file");
			this.radioButtonUnicodeNames.UseVisualStyleBackColor = true;
			//
			// radioButtonUnicodeValues
			//
			this.radioButtonUnicodeValues.AutoSize = true;
			this.radioButtonUnicodeValues.Location = new System.Drawing.Point(3, 72);
			this.radioButtonUnicodeValues.Name = "radioButtonUnicodeValues";
			this.radioButtonUnicodeValues.Size = new System.Drawing.Size(100, 17);
			this.radioButtonUnicodeValues.TabIndex = 3;
			this.radioButtonUnicodeValues.TabStop = true;
			this.radioButtonUnicodeValues.Text = "Unicode &Values";
			this.toolTip.SetToolTip(this.radioButtonUnicodeValues, "When you click on a cell, the Unicode value of the character will be inserted at " +
					"the current cursor location of the map file");
			this.radioButtonUnicodeValues.UseVisualStyleBackColor = true;
			//
			// radioButtonQuotedChars
			//
			this.radioButtonQuotedChars.AutoSize = true;
			this.radioButtonQuotedChars.Location = new System.Drawing.Point(3, 95);
			this.radioButtonQuotedChars.Name = "radioButtonQuotedChars";
			this.radioButtonQuotedChars.Size = new System.Drawing.Size(90, 17);
			this.radioButtonQuotedChars.TabIndex = 4;
			this.radioButtonQuotedChars.TabStop = true;
			this.radioButtonQuotedChars.Text = "&Quoted Chars";
			this.toolTip.SetToolTip(this.radioButtonQuotedChars, "When you click on a cell, the character in quotes will be inserted at the current" +
					" cursor location of the map file");
			this.radioButtonQuotedChars.UseVisualStyleBackColor = true;
			//
			// radioButtonByCodePoint
			//
			this.radioButtonByCodePoint.AutoSize = true;
			this.radioButtonByCodePoint.Location = new System.Drawing.Point(3, 3);
			this.radioButtonByCodePoint.Name = "radioButtonByCodePoint";
			this.radioButtonByCodePoint.Size = new System.Drawing.Size(82, 17);
			this.radioButtonByCodePoint.TabIndex = 0;
			this.radioButtonByCodePoint.TabStop = true;
			this.radioButtonByCodePoint.Text = "&Code Points";
			this.toolTip.SetToolTip(this.radioButtonByCodePoint, "Show Characters based on Code Point Range");
			this.radioButtonByCodePoint.UseVisualStyleBackColor = true;
			this.radioButtonByCodePoint.CheckedChanged += new System.EventHandler(this.radioButtonByCodePoint_CheckedChanged);
			//
			// radioButtonUnicodeSubsets
			//
			this.radioButtonUnicodeSubsets.AutoSize = true;
			this.radioButtonUnicodeSubsets.Location = new System.Drawing.Point(91, 3);
			this.radioButtonUnicodeSubsets.Name = "radioButtonUnicodeSubsets";
			this.radioButtonUnicodeSubsets.Size = new System.Drawing.Size(63, 17);
			this.radioButtonUnicodeSubsets.TabIndex = 1;
			this.radioButtonUnicodeSubsets.TabStop = true;
			this.radioButtonUnicodeSubsets.Text = "&Subsets";
			this.toolTip.SetToolTip(this.radioButtonUnicodeSubsets, "Show characters based on Unicode Range");
			this.radioButtonUnicodeSubsets.UseVisualStyleBackColor = true;
			this.radioButtonUnicodeSubsets.CheckedChanged += new System.EventHandler(this.radioButtonUnicodeSubsets_CheckedChanged);
			//
			// comboBoxCodePointRange
			//
			this.tableLayoutPanel.SetColumnSpan(this.comboBoxCodePointRange, 2);
			this.comboBoxCodePointRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxCodePointRange.FormattingEnabled = true;
			this.comboBoxCodePointRange.Items.AddRange(new object[] {
			"0000-007F",
			"0080-00FF",
			"0100-017F",
			"0180-01FF",
			"0200-027F",
			"0280-02FF",
			"0300-037F",
			"0380-03FF",
			"0400-047F",
			"0480-04FF",
			"0500-057F",
			"0580-05FF",
			"0600-067F",
			"0680-06FF",
			"0700-077F",
			"0780-07FF",
			"0800-087F",
			"0880-08FF",
			"0900-097F",
			"0980-09FF",
			"0A00-0A7F",
			"0A80-0AFF",
			"0B00-0B7F",
			"0B80-0BFF",
			"0C00-0C7F",
			"0C80-0CFF",
			"0D00-0D7F",
			"0D80-0DFF",
			"0E00-0E7F",
			"0E80-0EFF",
			"0F00-0F7F",
			"0F80-0FFF",
			"1000-107F",
			"1080-10FF",
			"1100-117F",
			"1180-11FF",
			"1200-127F",
			"1280-12FF",
			"1300-137F",
			"1380-13FF",
			"1400-147F",
			"1480-14FF",
			"1500-157F",
			"1580-15FF",
			"1600-167F",
			"1680-16FF",
			"1700-177F",
			"1780-17FF",
			"1800-187F",
			"1880-18FF",
			"1900-197F",
			"1980-19FF",
			"1A00-1A7F",
			"1A80-1AFF",
			"1B00-1B7F",
			"1B80-1BFF",
			"1C00-1C7F",
			"1C80-1CFF",
			"1D00-1D7F",
			"1D80-1DFF",
			"1E00-1E7F",
			"1E80-1EFF",
			"1F00-1F7F",
			"1F80-1FFF",
			"2000-207F",
			"2080-20FF",
			"2100-217F",
			"2180-21FF",
			"2200-227F",
			"2280-22FF",
			"2300-237F",
			"2380-23FF",
			"2400-247F",
			"2480-24FF",
			"2500-257F",
			"2580-25FF",
			"2600-267F",
			"2680-26FF",
			"2700-277F",
			"2780-27FF",
			"2800-287F",
			"2880-28FF",
			"2900-297F",
			"2980-29FF",
			"2A00-2A7F",
			"2A80-2AFF",
			"2B00-2B7F",
			"2B80-2BFF",
			"2C00-2C7F",
			"2C80-2CFF",
			"2D00-2D7F",
			"2D80-2DFF",
			"2E00-2E7F",
			"2E80-2EFF",
			"2F00-2F7F",
			"2F80-2FFF",
			"3000-307F",
			"3080-30FF",
			"3100-317F",
			"3180-31FF",
			"3200-327F",
			"3280-32FF",
			"3300-337F",
			"3380-33FF",
			"3400-347F",
			"3480-34FF",
			"3500-357F",
			"3580-35FF",
			"3600-367F",
			"3680-36FF",
			"3700-377F",
			"3780-37FF",
			"3800-387F",
			"3880-38FF",
			"3900-397F",
			"3980-39FF",
			"3A00-3A7F",
			"3A80-3AFF",
			"3B00-3B7F",
			"3B80-3BFF",
			"3C00-3C7F",
			"3C80-3CFF",
			"3D00-3D7F",
			"3D80-3DFF",
			"3E00-3E7F",
			"3E80-3EFF",
			"3F00-3F7F",
			"3F80-3FFF",
			"4000-407F",
			"4080-40FF",
			"4100-417F",
			"4180-41FF",
			"4200-427F",
			"4280-42FF",
			"4300-437F",
			"4380-43FF",
			"4400-447F",
			"4480-44FF",
			"4500-457F",
			"4580-45FF",
			"4600-467F",
			"4680-46FF",
			"4700-477F",
			"4780-47FF",
			"4800-487F",
			"4880-48FF",
			"4900-497F",
			"4980-49FF",
			"4A00-4A7F",
			"4A80-4AFF",
			"4B00-4B7F",
			"4B80-4BFF",
			"4C00-4C7F",
			"4C80-4CFF",
			"4D00-4D7F",
			"4D80-4DFF",
			"4E00-4E7F",
			"4E80-4EFF",
			"4F00-4F7F",
			"4F80-4FFF",
			"5000-507F",
			"5080-50FF",
			"5100-517F",
			"5180-51FF",
			"5200-527F",
			"5280-52FF",
			"5300-537F",
			"5380-53FF",
			"5400-547F",
			"5480-54FF",
			"5500-557F",
			"5580-55FF",
			"5600-567F",
			"5680-56FF",
			"5700-577F",
			"5780-57FF",
			"5800-587F",
			"5880-58FF",
			"5900-597F",
			"5980-59FF",
			"5A00-5A7F",
			"5A80-5AFF",
			"5B00-5B7F",
			"5B80-5BFF",
			"5C00-5C7F",
			"5C80-5CFF",
			"5D00-5D7F",
			"5D80-5DFF",
			"5E00-5E7F",
			"5E80-5EFF",
			"5F00-5F7F",
			"5F80-5FFF",
			"6000-607F",
			"6080-60FF",
			"6100-617F",
			"6180-61FF",
			"6200-627F",
			"6280-62FF",
			"6300-637F",
			"6380-63FF",
			"6400-647F",
			"6480-64FF",
			"6500-657F",
			"6580-65FF",
			"6600-667F",
			"6680-66FF",
			"6700-677F",
			"6780-67FF",
			"6800-687F",
			"6880-68FF",
			"6900-697F",
			"6980-69FF",
			"6A00-6A7F",
			"6A80-6AFF",
			"6B00-6B7F",
			"6B80-6BFF",
			"6C00-6C7F",
			"6C80-6CFF",
			"6D00-6D7F",
			"6D80-6DFF",
			"6E00-6E7F",
			"6E80-6EFF",
			"6F00-6F7F",
			"6F80-6FFF",
			"7000-707F",
			"7080-70FF",
			"7100-717F",
			"7180-71FF",
			"7200-727F",
			"7280-72FF",
			"7300-737F",
			"7380-73FF",
			"7400-747F",
			"7480-74FF",
			"7500-757F",
			"7580-75FF",
			"7600-767F",
			"7680-76FF",
			"7700-777F",
			"7780-77FF",
			"7800-787F",
			"7880-78FF",
			"7900-797F",
			"7980-79FF",
			"7A00-7A7F",
			"7A80-7AFF",
			"7B00-7B7F",
			"7B80-7BFF",
			"7C00-7C7F",
			"7C80-7CFF",
			"7D00-7D7F",
			"7D80-7DFF",
			"7E00-7E7F",
			"7E80-7EFF",
			"7F00-7F7F",
			"7F80-7FFF",
			"8000-807F",
			"8080-80FF",
			"8100-817F",
			"8180-81FF",
			"8200-827F",
			"8280-82FF",
			"8300-837F",
			"8380-83FF",
			"8400-847F",
			"8480-84FF",
			"8500-857F",
			"8580-85FF",
			"8600-867F",
			"8680-86FF",
			"8700-877F",
			"8780-87FF",
			"8800-887F",
			"8880-88FF",
			"8900-897F",
			"8980-89FF",
			"8A00-8A7F",
			"8A80-8AFF",
			"8B00-8B7F",
			"8B80-8BFF",
			"8C00-8C7F",
			"8C80-8CFF",
			"8D00-8D7F",
			"8D80-8DFF",
			"8E00-8E7F",
			"8E80-8EFF",
			"8F00-8F7F",
			"8F80-8FFF",
			"9000-907F",
			"9080-90FF",
			"9100-917F",
			"9180-91FF",
			"9200-927F",
			"9280-92FF",
			"9300-937F",
			"9380-93FF",
			"9400-947F",
			"9480-94FF",
			"9500-957F",
			"9580-95FF",
			"9600-967F",
			"9680-96FF",
			"9700-977F",
			"9780-97FF",
			"9800-987F",
			"9880-98FF",
			"9900-997F",
			"9980-99FF",
			"9A00-9A7F",
			"9A80-9AFF",
			"9B00-9B7F",
			"9B80-9BFF",
			"9C00-9C7F",
			"9C80-9CFF",
			"9D00-9D7F",
			"9D80-9DFF",
			"9E00-9E7F",
			"9E80-9EFF",
			"9F00-9F7F",
			"9F80-9FFF",
			"A000-A07F",
			"A080-A0FF",
			"A100-A17F",
			"A180-A1FF",
			"A200-A27F",
			"A280-A2FF",
			"A300-A37F",
			"A380-A3FF",
			"A400-A47F",
			"A480-A4FF",
			"A500-A57F",
			"A580-A5FF",
			"A600-A67F",
			"A680-A6FF",
			"A700-A77F",
			"A780-A7FF",
			"A800-A87F",
			"A880-A8FF",
			"A900-A97F",
			"A980-A9FF",
			"AA00-AA7F",
			"AA80-AAFF",
			"AB00-AB7F",
			"AB80-ABFF",
			"AC00-AC7F",
			"AC80-ACFF",
			"AD00-AD7F",
			"AD80-ADFF",
			"AE00-AE7F",
			"AE80-AEFF",
			"AF00-AF7F",
			"AF80-AFFF",
			"B000-B07F",
			"B080-B0FF",
			"B100-B17F",
			"B180-B1FF",
			"B200-B27F",
			"B280-B2FF",
			"B300-B37F",
			"B380-B3FF",
			"B400-B47F",
			"B480-B4FF",
			"B500-B57F",
			"B580-B5FF",
			"B600-B67F",
			"B680-B6FF",
			"B700-B77F",
			"B780-B7FF",
			"B800-B87F",
			"B880-B8FF",
			"B900-B97F",
			"B980-B9FF",
			"BA00-BA7F",
			"BA80-BAFF",
			"BB00-BB7F",
			"BB80-BBFF",
			"BC00-BC7F",
			"BC80-BCFF",
			"BD00-BD7F",
			"BD80-BDFF",
			"BE00-BE7F",
			"BE80-BEFF",
			"BF00-BF7F",
			"BF80-BFFF",
			"C000-C07F",
			"C080-C0FF",
			"C100-C17F",
			"C180-C1FF",
			"C200-C27F",
			"C280-C2FF",
			"C300-C37F",
			"C380-C3FF",
			"C400-C47F",
			"C480-C4FF",
			"C500-C57F",
			"C580-C5FF",
			"C600-C67F",
			"C680-C6FF",
			"C700-C77F",
			"C780-C7FF",
			"C800-C87F",
			"C880-C8FF",
			"C900-C97F",
			"C980-C9FF",
			"CA00-CA7F",
			"CA80-CAFF",
			"CB00-CB7F",
			"CB80-CBFF",
			"CC00-CC7F",
			"CC80-CCFF",
			"CD00-CD7F",
			"CD80-CDFF",
			"CE00-CE7F",
			"CE80-CEFF",
			"CF00-CF7F",
			"CF80-CFFF",
			"D000-D07F",
			"D080-D0FF",
			"D100-D17F",
			"D180-D1FF",
			"D200-D27F",
			"D280-D2FF",
			"D300-D37F",
			"D380-D3FF",
			"D400-D47F",
			"D480-D4FF",
			"D500-D57F",
			"D580-D5FF",
			"D600-D67F",
			"D680-D6FF",
			"D700-D77F",
			"D780-D7FF",
			"D800-D87F",
			"D880-D8FF",
			"D900-D97F",
			"D980-D9FF",
			"DA00-DA7F",
			"DA80-DAFF",
			"DB00-DB7F",
			"DB80-DBFF",
			"DC00-DC7F",
			"DC80-DCFF",
			"DD00-DD7F",
			"DD80-DDFF",
			"DE00-DE7F",
			"DE80-DEFF",
			"DF00-DF7F",
			"DF80-DFFF",
			"E000-E07F",
			"E080-E0FF",
			"E100-E17F",
			"E180-E1FF",
			"E200-E27F",
			"E280-E2FF",
			"E300-E37F",
			"E380-E3FF",
			"E400-E47F",
			"E480-E4FF",
			"E500-E57F",
			"E580-E5FF",
			"E600-E67F",
			"E680-E6FF",
			"E700-E77F",
			"E780-E7FF",
			"E800-E87F",
			"E880-E8FF",
			"E900-E97F",
			"E980-E9FF",
			"EA00-EA7F",
			"EA80-EAFF",
			"EB00-EB7F",
			"EB80-EBFF",
			"EC00-EC7F",
			"EC80-ECFF",
			"ED00-ED7F",
			"ED80-EDFF",
			"EE00-EE7F",
			"EE80-EEFF",
			"EF00-EF7F",
			"EF80-EFFF",
			"F000-F07F",
			"F080-F0FF",
			"F100-F17F",
			"F180-F1FF",
			"F200-F27F",
			"F280-F2FF",
			"F300-F37F",
			"F380-F3FF",
			"F400-F47F",
			"F480-F4FF",
			"F500-F57F",
			"F580-F5FF",
			"F600-F67F",
			"F680-F6FF",
			"F700-F77F",
			"F780-F7FF",
			"F800-F87F",
			"F880-F8FF",
			"F900-F97F",
			"F980-F9FF",
			"FA00-FA7F",
			"FA80-FAFF",
			"FB00-FB7F",
			"FB80-FBFF",
			"FC00-FC7F",
			"FC80-FCFF",
			"FD00-FD7F",
			"FD80-FDFF",
			"FE00-FE7F",
			"FE80-FEFF",
			"FF00-FF7F",
			"FF80-FFFF"});
			this.comboBoxCodePointRange.Location = new System.Drawing.Point(169, 362);
			this.comboBoxCodePointRange.MaxDropDownItems = 18;
			this.comboBoxCodePointRange.Name = "comboBoxCodePointRange";
			this.comboBoxCodePointRange.Size = new System.Drawing.Size(212, 21);
			this.comboBoxCodePointRange.TabIndex = 3;
			this.toolTip.SetToolTip(this.comboBoxCodePointRange, "This is a list of code point or Unicode subset ranges to display in the table abo" +
					"ve");
			this.comboBoxCodePointRange.Visible = false;
			this.comboBoxCodePointRange.SelectedIndexChanged += new System.EventHandler(this.comboBoxCodePointRange_SelectedIndexChanged);
			//
			// dataGridViewCharacters
			//
			this.dataGridViewCharacters.AllowUserToAddRows = false;
			this.dataGridViewCharacters.AllowUserToDeleteRows = false;
			this.dataGridViewCharacters.AllowUserToResizeRows = false;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCharacters.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
			this.dataGridViewCharacters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.dataGridViewCharacters.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.dataGridViewCharacters.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.dataGridViewCharacters.CausesValidation = false;
			this.dataGridViewCharacters.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.dataGridViewCharacters.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCharacters.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
			this.dataGridViewCharacters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.dataGridViewTextBoxColumn1,
			this.dataGridViewTextBoxColumn2,
			this.dataGridViewTextBoxColumn3,
			this.dataGridViewTextBoxColumn4,
			this.dataGridViewTextBoxColumn5,
			this.dataGridViewTextBoxColumn6,
			this.dataGridViewTextBoxColumn7,
			this.dataGridViewTextBoxColumn8,
			this.dataGridViewTextBoxColumn9,
			this.dataGridViewTextBoxColumn10,
			this.dataGridViewTextBoxColumn11,
			this.dataGridViewTextBoxColumn12,
			this.dataGridViewTextBoxColumn13,
			this.dataGridViewTextBoxColumn14,
			this.dataGridViewTextBoxColumn15,
			this.dataGridViewTextBoxColumn16});
			this.tableLayoutPanel.SetColumnSpan(this.dataGridViewCharacters, 4);
			this.dataGridViewCharacters.Cursor = System.Windows.Forms.Cursors.Hand;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCharacters.DefaultCellStyle = dataGridViewCellStyle3;
			this.dataGridViewCharacters.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.dataGridViewCharacters.EnableHeadersVisualStyles = false;
			this.dataGridViewCharacters.Location = new System.Drawing.Point(3, 114);
			this.dataGridViewCharacters.MultiSelect = false;
			this.dataGridViewCharacters.Name = "dataGridViewCharacters";
			this.dataGridViewCharacters.ReadOnly = true;
			this.dataGridViewCharacters.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			this.dataGridViewCharacters.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCharacters.RowsDefaultCellStyle = dataGridViewCellStyle4;
			this.dataGridViewCharacters.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.dataGridViewCharacters.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCharacters.RowTemplate.ReadOnly = true;
			this.dataGridViewCharacters.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.dataGridViewCharacters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.dataGridViewCharacters.ShowEditingIcon = false;
			this.dataGridViewCharacters.Size = new System.Drawing.Size(159, 242);
			this.dataGridViewCharacters.TabIndex = 0;
			this.toolTip.SetToolTip(this.dataGridViewCharacters, "Click \"View\", \"Configure * font\" to see a character map from which you can choose" +
					" character values");
			this.dataGridViewCharacters.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewCharacters_CellMouseLeave);
			this.dataGridViewCharacters.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewCharacters_CellMouseDown);
			this.dataGridViewCharacters.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewCharacters_CellMouseUp);
			this.dataGridViewCharacters.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewCharacters_CellMouseEnter);
			this.dataGridViewCharacters.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewCharacters_CellMouseDoubleClick);
			this.dataGridViewCharacters.SelectionChanged += new System.EventHandler(this.dataGridViewCharacters_SelectionChanged);
			//
			// dataGridViewTextBoxColumn1
			//
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn1.HeaderText = "0";
			this.dataGridViewTextBoxColumn1.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn1.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn1.Width = 18;
			//
			// dataGridViewTextBoxColumn2
			//
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn2.HeaderText = "1";
			this.dataGridViewTextBoxColumn2.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn2.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn2.Width = 18;
			//
			// dataGridViewTextBoxColumn3
			//
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn3.HeaderText = "2";
			this.dataGridViewTextBoxColumn3.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn3.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			this.dataGridViewTextBoxColumn3.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn3.Width = 18;
			//
			// dataGridViewTextBoxColumn4
			//
			this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn4.HeaderText = "3";
			this.dataGridViewTextBoxColumn4.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn4.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn4.Width = 18;
			//
			// dataGridViewTextBoxColumn5
			//
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn5.HeaderText = "4";
			this.dataGridViewTextBoxColumn5.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn5.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
			this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn5.Width = 18;
			//
			// dataGridViewTextBoxColumn6
			//
			this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn6.HeaderText = "5";
			this.dataGridViewTextBoxColumn6.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn6.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
			this.dataGridViewTextBoxColumn6.ReadOnly = true;
			this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn6.Width = 18;
			//
			// dataGridViewTextBoxColumn7
			//
			this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn7.HeaderText = "6";
			this.dataGridViewTextBoxColumn7.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn7.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
			this.dataGridViewTextBoxColumn7.ReadOnly = true;
			this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn7.Width = 18;
			//
			// dataGridViewTextBoxColumn8
			//
			this.dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn8.HeaderText = "7";
			this.dataGridViewTextBoxColumn8.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn8.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
			this.dataGridViewTextBoxColumn8.ReadOnly = true;
			this.dataGridViewTextBoxColumn8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn8.Width = 18;
			//
			// dataGridViewTextBoxColumn9
			//
			this.dataGridViewTextBoxColumn9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn9.HeaderText = "8";
			this.dataGridViewTextBoxColumn9.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn9.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
			this.dataGridViewTextBoxColumn9.ReadOnly = true;
			this.dataGridViewTextBoxColumn9.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn9.Width = 18;
			//
			// dataGridViewTextBoxColumn10
			//
			this.dataGridViewTextBoxColumn10.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn10.HeaderText = "9";
			this.dataGridViewTextBoxColumn10.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn10.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
			this.dataGridViewTextBoxColumn10.ReadOnly = true;
			this.dataGridViewTextBoxColumn10.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn10.Width = 18;
			//
			// dataGridViewTextBoxColumn11
			//
			this.dataGridViewTextBoxColumn11.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn11.HeaderText = "A";
			this.dataGridViewTextBoxColumn11.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn11.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
			this.dataGridViewTextBoxColumn11.ReadOnly = true;
			this.dataGridViewTextBoxColumn11.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn11.Width = 19;
			//
			// dataGridViewTextBoxColumn12
			//
			this.dataGridViewTextBoxColumn12.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn12.HeaderText = "B";
			this.dataGridViewTextBoxColumn12.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn12.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
			this.dataGridViewTextBoxColumn12.ReadOnly = true;
			this.dataGridViewTextBoxColumn12.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn12.Width = 19;
			//
			// dataGridViewTextBoxColumn13
			//
			this.dataGridViewTextBoxColumn13.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn13.HeaderText = "C";
			this.dataGridViewTextBoxColumn13.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn13.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
			this.dataGridViewTextBoxColumn13.ReadOnly = true;
			this.dataGridViewTextBoxColumn13.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn13.Width = 19;
			//
			// dataGridViewTextBoxColumn14
			//
			this.dataGridViewTextBoxColumn14.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn14.HeaderText = "D";
			this.dataGridViewTextBoxColumn14.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn14.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
			this.dataGridViewTextBoxColumn14.ReadOnly = true;
			this.dataGridViewTextBoxColumn14.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn14.Width = 20;
			//
			// dataGridViewTextBoxColumn15
			//
			this.dataGridViewTextBoxColumn15.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn15.HeaderText = "E";
			this.dataGridViewTextBoxColumn15.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn15.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
			this.dataGridViewTextBoxColumn15.ReadOnly = true;
			this.dataGridViewTextBoxColumn15.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn15.Width = 19;
			//
			// dataGridViewTextBoxColumn16
			//
			this.dataGridViewTextBoxColumn16.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn16.HeaderText = "F";
			this.dataGridViewTextBoxColumn16.MaxInputLength = 1;
			this.dataGridViewTextBoxColumn16.MinimumWidth = 2;
			this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
			this.dataGridViewTextBoxColumn16.ReadOnly = true;
			this.dataGridViewTextBoxColumn16.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.dataGridViewTextBoxColumn16.Width = 18;
			//
			// statusStrip
			//
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripStatusLabel});
			this.statusStrip.Location = new System.Drawing.Point(0, 386);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(441, 22);
			this.statusStrip.TabIndex = 5;
			this.statusStrip.Text = "statusStrip1";
			//
			// toolStripStatusLabel
			//
			this.toolStripStatusLabel.Name = "toolStripStatusLabel";
			this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 4;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.Controls.Add(this.dataGridViewCharacters, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.groupBoxSendToEditor, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.labelFontName, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.flowLayoutPanelChooseByRange, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.groupBoxUnicodeRanges, 1, 0);
			this.tableLayoutPanel.Controls.Add(this.comboBoxCodePointRange, 2, 3);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 4;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(441, 386);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// groupBoxSendToEditor
			//
			this.groupBoxSendToEditor.Controls.Add(this.flowLayoutPanel);
			this.groupBoxSendToEditor.Location = new System.Drawing.Point(3, 3);
			this.groupBoxSendToEditor.MinimumSize = new System.Drawing.Size(140, 92);
			this.groupBoxSendToEditor.Name = "groupBoxSendToEditor";
			this.groupBoxSendToEditor.Size = new System.Drawing.Size(140, 92);
			this.groupBoxSendToEditor.TabIndex = 4;
			this.groupBoxSendToEditor.TabStop = false;
			this.groupBoxSendToEditor.Text = "Send to Editor";
			//
			// flowLayoutPanel
			//
			this.flowLayoutPanel.Controls.Add(this.radioButtonDecimal);
			this.flowLayoutPanel.Controls.Add(this.radioButtonHexadecimal);
			this.flowLayoutPanel.Controls.Add(this.radioButtonUnicodeNames);
			this.flowLayoutPanel.Controls.Add(this.radioButtonUnicodeValues);
			this.flowLayoutPanel.Controls.Add(this.radioButtonQuotedChars);
			this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel.Location = new System.Drawing.Point(3, 16);
			this.flowLayoutPanel.Name = "flowLayoutPanel";
			this.flowLayoutPanel.Size = new System.Drawing.Size(134, 73);
			this.flowLayoutPanel.TabIndex = 0;
			//
			// labelFontName
			//
			this.labelFontName.Location = new System.Drawing.Point(3, 98);
			this.labelFontName.Name = "labelFontName";
			this.labelFontName.Size = new System.Drawing.Size(140, 13);
			this.labelFontName.TabIndex = 6;
			this.labelFontName.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			//
			// flowLayoutPanelChooseByRange
			//
			this.tableLayoutPanel.SetColumnSpan(this.flowLayoutPanelChooseByRange, 2);
			this.flowLayoutPanelChooseByRange.Controls.Add(this.radioButtonByCodePoint);
			this.flowLayoutPanelChooseByRange.Controls.Add(this.radioButtonUnicodeSubsets);
			this.flowLayoutPanelChooseByRange.Location = new System.Drawing.Point(3, 362);
			this.flowLayoutPanelChooseByRange.Name = "flowLayoutPanelChooseByRange";
			this.flowLayoutPanelChooseByRange.Size = new System.Drawing.Size(159, 21);
			this.flowLayoutPanelChooseByRange.TabIndex = 7;
			this.flowLayoutPanelChooseByRange.Visible = false;
			//
			// groupBoxUnicodeRanges
			//
			this.tableLayoutPanel.SetColumnSpan(this.groupBoxUnicodeRanges, 2);
			this.groupBoxUnicodeRanges.Controls.Add(this.flowLayoutPanelRecentRanges);
			this.groupBoxUnicodeRanges.Location = new System.Drawing.Point(149, 3);
			this.groupBoxUnicodeRanges.MinimumSize = new System.Drawing.Size(205, 92);
			this.groupBoxUnicodeRanges.Name = "groupBoxUnicodeRanges";
			this.groupBoxUnicodeRanges.Size = new System.Drawing.Size(205, 92);
			this.groupBoxUnicodeRanges.TabIndex = 5;
			this.groupBoxUnicodeRanges.TabStop = false;
			this.groupBoxUnicodeRanges.Text = "Recent Ranges";
			this.groupBoxUnicodeRanges.Visible = false;
			//
			// flowLayoutPanelRecentRanges
			//
			this.flowLayoutPanelRecentRanges.AutoScroll = true;
			this.flowLayoutPanelRecentRanges.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanelRecentRanges.Location = new System.Drawing.Point(3, 16);
			this.flowLayoutPanelRecentRanges.Name = "flowLayoutPanelRecentRanges";
			this.flowLayoutPanelRecentRanges.Size = new System.Drawing.Size(199, 73);
			this.flowLayoutPanelRecentRanges.TabIndex = 0;
			this.flowLayoutPanelRecentRanges.Visible = false;
			//
			// DisplayUnicodeNamesForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(441, 408);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.statusStrip);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DisplayUnicodeNamesForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Character Maps";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DisplayUnicodeNamesForm_FormClosing);
			this.ResizeEnd += new System.EventHandler(this.DisplayUnicodeNamesForm_ResizeEnd);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewCharacters)).EndInit();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.tableLayoutPanel.ResumeLayout(false);
			this.groupBoxSendToEditor.ResumeLayout(false);
			this.flowLayoutPanel.ResumeLayout(false);
			this.flowLayoutPanel.PerformLayout();
			this.flowLayoutPanelChooseByRange.ResumeLayout(false);
			this.flowLayoutPanelChooseByRange.PerformLayout();
			this.groupBoxUnicodeRanges.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolTip toolTip;
		private MyDataGridView dataGridViewCharacters;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
		private System.Windows.Forms.HelpProvider helpProviderCP;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.ComboBox comboBoxCodePointRange;
		private System.Windows.Forms.GroupBox groupBoxSendToEditor;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
		private System.Windows.Forms.RadioButton radioButtonDecimal;
		private System.Windows.Forms.RadioButton radioButtonHexadecimal;
		private System.Windows.Forms.RadioButton radioButtonUnicodeNames;
		private System.Windows.Forms.RadioButton radioButtonUnicodeValues;
		private System.Windows.Forms.RadioButton radioButtonQuotedChars;
		private System.Windows.Forms.GroupBox groupBoxUnicodeRanges;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelRecentRanges;
		private System.Windows.Forms.Label labelFontName;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelChooseByRange;
		private System.Windows.Forms.RadioButton radioButtonByCodePoint;
		private System.Windows.Forms.RadioButton radioButtonUnicodeSubsets;

		private void radioButtonByCodePoint_CheckedChanged(object sender, EventArgs e)
		{
			comboBoxCodePointRange.Items.Clear();
			comboBoxCodePointRange.Items.AddRange(new object[] {
			"0000-007F",
			"0080-00FF",
			"0100-017F",
			"0180-01FF",
			"0200-027F",
			"0280-02FF",
			"0300-037F",
			"0380-03FF",
			"0400-047F",
			"0480-04FF",
			"0500-057F",
			"0580-05FF",
			"0600-067F",
			"0680-06FF",
			"0700-077F",
			"0780-07FF",
			"0800-087F",
			"0880-08FF",
			"0900-097F",
			"0980-09FF",
			"0A00-0A7F",
			"0A80-0AFF",
			"0B00-0B7F",
			"0B80-0BFF",
			"0C00-0C7F",
			"0C80-0CFF",
			"0D00-0D7F",
			"0D80-0DFF",
			"0E00-0E7F",
			"0E80-0EFF",
			"0F00-0F7F",
			"0F80-0FFF",
			"1000-107F",
			"1080-10FF",
			"1100-117F",
			"1180-11FF",
			"1200-127F",
			"1280-12FF",
			"1300-137F",
			"1380-13FF",
			"1400-147F",
			"1480-14FF",
			"1500-157F",
			"1580-15FF",
			"1600-167F",
			"1680-16FF",
			"1700-177F",
			"1780-17FF",
			"1800-187F",
			"1880-18FF",
			"1900-197F",
			"1980-19FF",
			"1A00-1A7F",
			"1A80-1AFF",
			"1B00-1B7F",
			"1B80-1BFF",
			"1C00-1C7F",
			"1C80-1CFF",
			"1D00-1D7F",
			"1D80-1DFF",
			"1E00-1E7F",
			"1E80-1EFF",
			"1F00-1F7F",
			"1F80-1FFF",
			"2000-207F",
			"2080-20FF",
			"2100-217F",
			"2180-21FF",
			"2200-227F",
			"2280-22FF",
			"2300-237F",
			"2380-23FF",
			"2400-247F",
			"2480-24FF",
			"2500-257F",
			"2580-25FF",
			"2600-267F",
			"2680-26FF",
			"2700-277F",
			"2780-27FF",
			"2800-287F",
			"2880-28FF",
			"2900-297F",
			"2980-29FF",
			"2A00-2A7F",
			"2A80-2AFF",
			"2B00-2B7F",
			"2B80-2BFF",
			"2C00-2C7F",
			"2C80-2CFF",
			"2D00-2D7F",
			"2D80-2DFF",
			"2E00-2E7F",
			"2E80-2EFF",
			"2F00-2F7F",
			"2F80-2FFF",
			"3000-307F",
			"3080-30FF",
			"3100-317F",
			"3180-31FF",
			"3200-327F",
			"3280-32FF",
			"3300-337F",
			"3380-33FF",
			"3400-347F",
			"3480-34FF",
			"3500-357F",
			"3580-35FF",
			"3600-367F",
			"3680-36FF",
			"3700-377F",
			"3780-37FF",
			"3800-387F",
			"3880-38FF",
			"3900-397F",
			"3980-39FF",
			"3A00-3A7F",
			"3A80-3AFF",
			"3B00-3B7F",
			"3B80-3BFF",
			"3C00-3C7F",
			"3C80-3CFF",
			"3D00-3D7F",
			"3D80-3DFF",
			"3E00-3E7F",
			"3E80-3EFF",
			"3F00-3F7F",
			"3F80-3FFF",
			"4000-407F",
			"4080-40FF",
			"4100-417F",
			"4180-41FF",
			"4200-427F",
			"4280-42FF",
			"4300-437F",
			"4380-43FF",
			"4400-447F",
			"4480-44FF",
			"4500-457F",
			"4580-45FF",
			"4600-467F",
			"4680-46FF",
			"4700-477F",
			"4780-47FF",
			"4800-487F",
			"4880-48FF",
			"4900-497F",
			"4980-49FF",
			"4A00-4A7F",
			"4A80-4AFF",
			"4B00-4B7F",
			"4B80-4BFF",
			"4C00-4C7F",
			"4C80-4CFF",
			"4D00-4D7F",
			"4D80-4DFF",
			"4E00-4E7F",
			"4E80-4EFF",
			"4F00-4F7F",
			"4F80-4FFF",
			"5000-507F",
			"5080-50FF",
			"5100-517F",
			"5180-51FF",
			"5200-527F",
			"5280-52FF",
			"5300-537F",
			"5380-53FF",
			"5400-547F",
			"5480-54FF",
			"5500-557F",
			"5580-55FF",
			"5600-567F",
			"5680-56FF",
			"5700-577F",
			"5780-57FF",
			"5800-587F",
			"5880-58FF",
			"5900-597F",
			"5980-59FF",
			"5A00-5A7F",
			"5A80-5AFF",
			"5B00-5B7F",
			"5B80-5BFF",
			"5C00-5C7F",
			"5C80-5CFF",
			"5D00-5D7F",
			"5D80-5DFF",
			"5E00-5E7F",
			"5E80-5EFF",
			"5F00-5F7F",
			"5F80-5FFF",
			"6000-607F",
			"6080-60FF",
			"6100-617F",
			"6180-61FF",
			"6200-627F",
			"6280-62FF",
			"6300-637F",
			"6380-63FF",
			"6400-647F",
			"6480-64FF",
			"6500-657F",
			"6580-65FF",
			"6600-667F",
			"6680-66FF",
			"6700-677F",
			"6780-67FF",
			"6800-687F",
			"6880-68FF",
			"6900-697F",
			"6980-69FF",
			"6A00-6A7F",
			"6A80-6AFF",
			"6B00-6B7F",
			"6B80-6BFF",
			"6C00-6C7F",
			"6C80-6CFF",
			"6D00-6D7F",
			"6D80-6DFF",
			"6E00-6E7F",
			"6E80-6EFF",
			"6F00-6F7F",
			"6F80-6FFF",
			"7000-707F",
			"7080-70FF",
			"7100-717F",
			"7180-71FF",
			"7200-727F",
			"7280-72FF",
			"7300-737F",
			"7380-73FF",
			"7400-747F",
			"7480-74FF",
			"7500-757F",
			"7580-75FF",
			"7600-767F",
			"7680-76FF",
			"7700-777F",
			"7780-77FF",
			"7800-787F",
			"7880-78FF",
			"7900-797F",
			"7980-79FF",
			"7A00-7A7F",
			"7A80-7AFF",
			"7B00-7B7F",
			"7B80-7BFF",
			"7C00-7C7F",
			"7C80-7CFF",
			"7D00-7D7F",
			"7D80-7DFF",
			"7E00-7E7F",
			"7E80-7EFF",
			"7F00-7F7F",
			"7F80-7FFF",
			"8000-807F",
			"8080-80FF",
			"8100-817F",
			"8180-81FF",
			"8200-827F",
			"8280-82FF",
			"8300-837F",
			"8380-83FF",
			"8400-847F",
			"8480-84FF",
			"8500-857F",
			"8580-85FF",
			"8600-867F",
			"8680-86FF",
			"8700-877F",
			"8780-87FF",
			"8800-887F",
			"8880-88FF",
			"8900-897F",
			"8980-89FF",
			"8A00-8A7F",
			"8A80-8AFF",
			"8B00-8B7F",
			"8B80-8BFF",
			"8C00-8C7F",
			"8C80-8CFF",
			"8D00-8D7F",
			"8D80-8DFF",
			"8E00-8E7F",
			"8E80-8EFF",
			"8F00-8F7F",
			"8F80-8FFF",
			"9000-907F",
			"9080-90FF",
			"9100-917F",
			"9180-91FF",
			"9200-927F",
			"9280-92FF",
			"9300-937F",
			"9380-93FF",
			"9400-947F",
			"9480-94FF",
			"9500-957F",
			"9580-95FF",
			"9600-967F",
			"9680-96FF",
			"9700-977F",
			"9780-97FF",
			"9800-987F",
			"9880-98FF",
			"9900-997F",
			"9980-99FF",
			"9A00-9A7F",
			"9A80-9AFF",
			"9B00-9B7F",
			"9B80-9BFF",
			"9C00-9C7F",
			"9C80-9CFF",
			"9D00-9D7F",
			"9D80-9DFF",
			"9E00-9E7F",
			"9E80-9EFF",
			"9F00-9F7F",
			"9F80-9FFF",
			"A000-A07F",
			"A080-A0FF",
			"A100-A17F",
			"A180-A1FF",
			"A200-A27F",
			"A280-A2FF",
			"A300-A37F",
			"A380-A3FF",
			"A400-A47F",
			"A480-A4FF",
			"A500-A57F",
			"A580-A5FF",
			"A600-A67F",
			"A680-A6FF",
			"A700-A77F",
			"A780-A7FF",
			"A800-A87F",
			"A880-A8FF",
			"A900-A97F",
			"A980-A9FF",
			"AA00-AA7F",
			"AA80-AAFF",
			"AB00-AB7F",
			"AB80-ABFF",
			"AC00-AC7F",
			"AC80-ACFF",
			"AD00-AD7F",
			"AD80-ADFF",
			"AE00-AE7F",
			"AE80-AEFF",
			"AF00-AF7F",
			"AF80-AFFF",
			"B000-B07F",
			"B080-B0FF",
			"B100-B17F",
			"B180-B1FF",
			"B200-B27F",
			"B280-B2FF",
			"B300-B37F",
			"B380-B3FF",
			"B400-B47F",
			"B480-B4FF",
			"B500-B57F",
			"B580-B5FF",
			"B600-B67F",
			"B680-B6FF",
			"B700-B77F",
			"B780-B7FF",
			"B800-B87F",
			"B880-B8FF",
			"B900-B97F",
			"B980-B9FF",
			"BA00-BA7F",
			"BA80-BAFF",
			"BB00-BB7F",
			"BB80-BBFF",
			"BC00-BC7F",
			"BC80-BCFF",
			"BD00-BD7F",
			"BD80-BDFF",
			"BE00-BE7F",
			"BE80-BEFF",
			"BF00-BF7F",
			"BF80-BFFF",
			"C000-C07F",
			"C080-C0FF",
			"C100-C17F",
			"C180-C1FF",
			"C200-C27F",
			"C280-C2FF",
			"C300-C37F",
			"C380-C3FF",
			"C400-C47F",
			"C480-C4FF",
			"C500-C57F",
			"C580-C5FF",
			"C600-C67F",
			"C680-C6FF",
			"C700-C77F",
			"C780-C7FF",
			"C800-C87F",
			"C880-C8FF",
			"C900-C97F",
			"C980-C9FF",
			"CA00-CA7F",
			"CA80-CAFF",
			"CB00-CB7F",
			"CB80-CBFF",
			"CC00-CC7F",
			"CC80-CCFF",
			"CD00-CD7F",
			"CD80-CDFF",
			"CE00-CE7F",
			"CE80-CEFF",
			"CF00-CF7F",
			"CF80-CFFF",
			"D000-D07F",
			"D080-D0FF",
			"D100-D17F",
			"D180-D1FF",
			"D200-D27F",
			"D280-D2FF",
			"D300-D37F",
			"D380-D3FF",
			"D400-D47F",
			"D480-D4FF",
			"D500-D57F",
			"D580-D5FF",
			"D600-D67F",
			"D680-D6FF",
			"D700-D77F",
			"D780-D7FF",
			"D800-D87F",
			"D880-D8FF",
			"D900-D97F",
			"D980-D9FF",
			"DA00-DA7F",
			"DA80-DAFF",
			"DB00-DB7F",
			"DB80-DBFF",
			"DC00-DC7F",
			"DC80-DCFF",
			"DD00-DD7F",
			"DD80-DDFF",
			"DE00-DE7F",
			"DE80-DEFF",
			"DF00-DF7F",
			"DF80-DFFF",
			"E000-E07F",
			"E080-E0FF",
			"E100-E17F",
			"E180-E1FF",
			"E200-E27F",
			"E280-E2FF",
			"E300-E37F",
			"E380-E3FF",
			"E400-E47F",
			"E480-E4FF",
			"E500-E57F",
			"E580-E5FF",
			"E600-E67F",
			"E680-E6FF",
			"E700-E77F",
			"E780-E7FF",
			"E800-E87F",
			"E880-E8FF",
			"E900-E97F",
			"E980-E9FF",
			"EA00-EA7F",
			"EA80-EAFF",
			"EB00-EB7F",
			"EB80-EBFF",
			"EC00-EC7F",
			"EC80-ECFF",
			"ED00-ED7F",
			"ED80-EDFF",
			"EE00-EE7F",
			"EE80-EEFF",
			"EF00-EF7F",
			"EF80-EFFF",
			"F000-F07F",
			"F080-F0FF",
			"F100-F17F",
			"F180-F1FF",
			"F200-F27F",
			"F280-F2FF",
			"F300-F37F",
			"F380-F3FF",
			"F400-F47F",
			"F480-F4FF",
			"F500-F57F",
			"F580-F5FF",
			"F600-F67F",
			"F680-F6FF",
			"F700-F77F",
			"F780-F7FF",
			"F800-F87F",
			"F880-F8FF",
			"F900-F97F",
			"F980-F9FF",
			"FA00-FA7F",
			"FA80-FAFF",
			"FB00-FB7F",
			"FB80-FBFF",
			"FC00-FC7F",
			"FC80-FCFF",
			"FD00-FD7F",
			"FD80-FDFF",
			"FE00-FE7F",
			"FE80-FEFF",
			"FF00-FF7F",
			"FF80-FFFF"});
		}

		protected UnicodeSubsetMap m_aUSM = null;

		private void radioButtonUnicodeSubsets_CheckedChanged(object sender, EventArgs e)
		{
			comboBoxCodePointRange.Items.Clear();
			if (m_aUSM == null)
				m_aUSM = new UnicodeSubsetMap();

			foreach (UnicodeSubset aUS in m_aUSM.Values)
				comboBoxCodePointRange.Items.Add(aUS.Name);

			comboBoxCodePointRange.SelectedItem = UnicodeSubsetMap.cstrDefSubsetName;
		}
	}

	internal class MyDataGridView : System.Windows.Forms.DataGridView
	{
		public bool IsVerticalScrollBarVisible
		{
			get
			{
				return VerticalScrollBar.Visible;
			}
		}
	}
}