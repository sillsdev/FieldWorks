using System;
using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FwTextBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	class FwTextBoxTests : BaseTest
	{
		#region Data Members
		TestFwStylesheet m_stylesheet;
		WritingSystemManager m_wsManager;
		int m_hvoEnglishWs;
		#endregion

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_wsManager = new WritingSystemManager();

			// setup English ws.
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
		}

		[Test]
		public void TestFwTextBoxSize()
		{
			using (var textBox = new FwTextBox())
			{
				textBox.AdjustStringHeight = true;
				textBox.controlID = null;
				textBox.Enabled = false;
				textBox.TabStop = false;
				textBox.BackColor = System.Drawing.SystemColors.Control;
				textBox.HasBorder = false;
				textBox.Location = new System.Drawing.Point(5, 5);
				textBox.Size = new System.Drawing.Size(100, 30);
				textBox.Dock = System.Windows.Forms.DockStyle.Fill;
				textBox.Visible = true;
				textBox.WordWrap = false;

				textBox.Tss = TsStringHelper.MakeTSS("Test", m_hvoEnglishWs);
				Assert.LessOrEqual(textBox.PreferredHeight, textBox.Height, "The simple string should fit within the default height.");
				Assert.LessOrEqual(textBox.PreferredWidth, textBox.Width, "The simple string should fit within the default width.");

				textBox.Tss = TsStringHelper.MakeTSS("This is a very long string that should be larger than the default box size in some way or other.", m_hvoEnglishWs);
				Console.WriteLine("PreferredHeight 2 = {0}", textBox.PreferredHeight);
				Console.WriteLine("PreferredWidth 2 = {0}", textBox.PreferredWidth);
				Assert.LessOrEqual(textBox.PreferredHeight, textBox.Height, "The longer string should still fit within the default height (for no wordwrapping).");
				Assert.Greater(textBox.PreferredWidth, textBox.Width, "The longer string should not fit within the default width (for no wordwrapping)");

				textBox.WordWrap = true;
				textBox.Tss = TsStringHelper.MakeTSS("This is a very long string that should be even larger than the default box size in some way or other.", m_hvoEnglishWs);
				Console.WriteLine("PreferredHeight 3 = {0}", textBox.PreferredHeight);
				Console.WriteLine("PreferredWidth 3 = {0}", textBox.PreferredWidth);
				Assert.Greater(textBox.PreferredHeight, textBox.Height, "The longest string should not fit within the default height (for wordwrapping).");
				Assert.LessOrEqual(textBox.PreferredWidth, textBox.Width, "The longest string should fit with the default width (for wordwrapping).");
			}
		}
	}
}
