// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	[TestFixture]
	public class FwAttributesTest
	{
		[Test]
		public void UpdateForStyle_OpenTypeFeatures_RoundTripsNormalizedTags()
		{
			var fontInfo = CreateExplicitFontInfo(" smcp = 1, kern=0 ");
			using (var t = new FwFontAttributes())
			{
				t.UpdateForStyle(fontInfo);

				bool isInherited;
				Assert.That(t.GetFontFeatures(out isInherited), Is.EqualTo("kern=0,smcp=1"));
				Assert.That(isInherited, Is.False);
			}
		}

		private static FontInfo CreateExplicitFontInfo(string features)
		{
			return new FontInfo
			{
				m_bold = { ExplicitValue = false },
				m_italic = { ExplicitValue = false },
				m_superSub = { ExplicitValue = FwSuperscriptVal.kssvOff },
				m_offset = { ExplicitValue = 0 },
				m_fontColor = { ExplicitValue = Color.Black },
				m_backColor = { ExplicitValue = Color.Empty },
				m_underline = { ExplicitValue = FwUnderlineType.kuntNone },
				m_underlineColor = { ExplicitValue = Color.Empty },
				m_features = { ExplicitValue = features }
			};
		}

		[Test]
		public void IsInherited_CheckBoxUnchecked_ReturnsFalse()
		{
			using (var checkBox = new CheckBox())
			{
				checkBox.CheckState = CheckState.Unchecked;
				using (var t = new FwFontAttributes())
				{
					t.ShowingInheritedProperties = true;
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox), Is.False);
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
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox), Is.False);
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
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox), Is.True);
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
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", checkBox), Is.False);
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
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", colorCombo), Is.False);
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
					Assert.That(ReflectionHelper.GetBoolResult(t, "IsInherited", colorCombo), Is.True);
				}
			}
		}

	}
}
