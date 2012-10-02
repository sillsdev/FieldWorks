using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	[TestFixture]
	public class FwAttributesTest
	{
		[Test]
		public void IsInherited_CheckBoxUnchecked_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Unchecked;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox));
				}
			}
		}

		[Test]
		public void IsInherited_CheckBoxChecked_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Checked;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox));
				}
			}
		}

		[Test]
		public void IsInherited_CheckBoxIndeterminate_ReturnsTrue()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Indeterminate;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.IsTrue(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox));
				}
			}
		}

		[Test]
		public void IsInherited_ShowingInheritedPropertiesIsFalseWithCheckBoxIndeterminate_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Indeterminate;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = false;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox));
				}
			}
		}

		[Test]
		public void IsInherited_FwColorComboRed_ReturnsFalse()
		{
			using (var colorCombo = new FwColorCombo())
			{
				colorCombo.ColorValue = Color.Red;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.IsFalse(ReflectionHelper.GetBoolResult(t, "IsInherited", colorCombo));
				}
			}
		}

		/// <summary>
		/// This test verifies that the tie between ShowUnspecified and
		/// IsInherited is maintained. The IsInherited property used to be loosley tied to the ColorValue
		/// this is no longer the case.
		/// </summary>
		[Test]
		public void IsInherited_ShowUnspecified_ReturnsTrue()
		{
			using (var colorCombo = new FwColorCombo())
			{
				colorCombo.ShowUnspecified = true;
				//colorCombo.ColorValue = Color.Empty;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.IsTrue(ReflectionHelper.GetBoolResult(t, "IsInherited", colorCombo));
				}
			}
		}

	}
}
