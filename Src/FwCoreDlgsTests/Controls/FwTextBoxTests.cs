// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Tests for FwTextBox.
	/// </summary>
	[TestFixture]
	class FwTextBoxTests
	{
		#region Data Members
		private TestFwStylesheet m_stylesheet;
		private WritingSystemManager m_wsManager;
		private int m_hvoEnglishWs;
		#endregion

		/// <summary />
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_wsManager = new WritingSystemManager();

			// setup English ws.
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
		}

		/// <summary />
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

				textBox.Tss = TsStringUtils.MakeString("Test", m_hvoEnglishWs);
				Assert.LessOrEqual(textBox.PreferredHeight, textBox.Height, "The simple string should fit within the default height.");
				Assert.LessOrEqual(textBox.PreferredWidth, textBox.Width, "The simple string should fit within the default width.");

				textBox.Tss = TsStringUtils.MakeString("This is a very long string that should be larger than the default box size in some way or other.", m_hvoEnglishWs);
				Console.WriteLine("PreferredHeight 2 = {0}", textBox.PreferredHeight);
				Console.WriteLine("PreferredWidth 2 = {0}", textBox.PreferredWidth);
				Assert.LessOrEqual(textBox.PreferredHeight, textBox.Height, "The longer string should still fit within the default height (for no wordwrapping).");
				Assert.Greater(textBox.PreferredWidth, textBox.Width, "The longer string should not fit within the default width (for no wordwrapping)");

				textBox.WordWrap = true;
				textBox.Tss = TsStringUtils.MakeString("This is a very long string that should be even larger than the default box size in some way or other.", m_hvoEnglishWs);
				Console.WriteLine("PreferredHeight 3 = {0}", textBox.PreferredHeight);
				Console.WriteLine("PreferredWidth 3 = {0}", textBox.PreferredWidth);
				Assert.Greater(textBox.PreferredHeight, textBox.Height, "The longest string should not fit within the default height (for wordwrapping).");
				Assert.LessOrEqual(textBox.PreferredWidth, textBox.Width, "The longest string should fit with the default width (for wordwrapping).");
			}
		}
	}
}