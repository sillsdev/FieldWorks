using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SharpViewsDemo
{
	class DemoStyleSheet : IStylesheet
	{
		public IStyle Style(string name)
		{
			IStyle result;
			if (name == null)
				name = "";
			m_styles.TryGetValue(name, out result);
			return result;
		}

		public void SetStyle(string name, IStyle style)
		{
			m_styles[name] = style;
		}

		private Dictionary<string, IStyle> m_styles = new Dictionary<string, IStyle>();
	}

	class DemoStyle : IStyle
	{
		private string m_Name;

		public ICharacterStyleInfo OverrideCharacterStyleInfo(int ws)
		{
			return null;
		}

		public string Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public bool IsParagraphStyle
		{
			get { return ParagraphStyleInfo != null; }
		}

		public IParaStyleInfo ParagraphStyleInfo { get; set; }

		public void RevertToCharStyle()
		{
			ParagraphStyleInfo = null;
		}

		public ICharacterStyleInfo DefaultCharacterStyleInfo { get; set; }
	}

	internal class DemoParaStyleInfo : IParaStyleInfo
	{
		public IStyleProp<FwTextAlign> Alignment { get; set; }
		public IStyleProp<LineHeightInfo> LineHeight { get; set; }
		public IStyleProp<int> SpaceBefore { get; set; }
		public IStyleProp<int> SpaceAfter { get; set; }
		public IStyleProp<int> FirstLineIndent { get; set; }
		public IStyleProp<int> LeadingIndent { get; set; }
		public IStyleProp<int> TrailingIndent { get; set; }
		public IStyleProp<Color> BorderColor { get; set; }
		public IStyleProp<int> BorderLeading { get; set; }
		public IStyleProp<int> BorderTrailing { get; set; }
		public IStyleProp<int> BorderTop { get; set; }
		public IStyleProp<int> BorderBottom { get; set; }
		public IStyleProp<int> MarginLeading { get; set; }
		public IStyleProp<int> MarginTrailing { get; set; }
		public IStyleProp<int> MarginTop { get; set; }
		public IStyleProp<int> MarginBottom { get; set; }
		public IStyleProp<int> PadLeading { get; set; }
		public IStyleProp<int> PadTrailing { get; set; }
		public IStyleProp<int> PadTop { get; set; }
		public IStyleProp<int> PadBottom { get; set; }
	}

	internal class DemoCharStyleInfo : ICharacterStyleInfo
	{
		public IStyleProp<string> FontName { get; set; }
		public IStyleProp<int> FontSize { get; set; }
		public IStyleProp<Color> FontColor { get; set; }
		public IStyleProp<Color> BackColor { get; set; }
		public IStyleProp<bool> Bold { get; set; }
		public IStyleProp<bool> Italic { get; set; }
		public IStyleProp<FwSuperscriptVal> SuperSub { get; set; }
		public IStyleProp<FwUnderlineType> Underline { get; set; }
		public IStyleProp<Color> UnderlineColor { get; set; }
		public IStyleProp<int> Offset { get; set; }
		public IStyleProp<string> Features { get; set; }
	}

	internal class DemoStyleProp<T> : IStyleProp<T>
	{
		public DemoStyleProp()
		{
			if (Value != null)
				ValueIsSet = true;
		}

		public T Value { get; set; }
		public bool ValueIsSet { get; set; }
	}
}
