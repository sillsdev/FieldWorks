// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.Styles;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Controls.Styles
{
	/// <summary />
	[TestFixture]
	public class FwAttributesTest
	{
		/// <summary />
		[Test]
		public void IsInherited_CheckBoxUnchecked_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Unchecked;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", checkBox));
				}
			}
		}

		/// <summary />
		[Test]
		public void IsInherited_CheckBoxChecked_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Checked;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", checkBox));
				}
			}
		}

		/// <summary />
		[Test]
		public void IsInherited_CheckBoxIndeterminate_ReturnsTrue()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Indeterminate;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = true;
					Assert.IsTrue(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", checkBox));
				}
			}
		}

		/// <summary />
		[Test]
		public void IsInherited_ShowingInheritedPropertiesIsFalseWithCheckBoxIndeterminate_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Indeterminate;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = false;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", checkBox));
				}
			}
		}

		/// <summary />
		[Test]
		public void IsInherited_FwColorComboRed_ReturnsFalse()
		{
			using (var colorCombo = new FwColorCombo())
			{
				colorCombo.ColorValue = Color.Red;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", colorCombo));
				}
			}
		}

		/// <summary>
		/// This test verifies that the tie between ShowUnspecified and
		/// IsInherited is maintained. The IsInherited property used to be loosely tied to the ColorValue
		/// this is no longer the case.
		/// </summary>
		[Test]
		public void IsInherited_ShowUnspecified_ReturnsTrue()
		{
			using (var colorCombo = new FwColorCombo())
			{
				colorCombo.ShowUnspecified = true;
				using (var fontAttributes = new FwFontAttributes())
				{
					fontAttributes.ShowingInheritedProperties = true;
					Assert.IsTrue(ReflectionHelper.GetBoolResult(fontAttributes, "IsInherited", colorCombo));
				}
			}
		}
	}
}