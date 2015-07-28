// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	internal class MockStylesheet : IStylesheet
	{
		private Dictionary<string, IStyle> Styles = new Dictionary<string, IStyle>();
		public IStyle Style(string name)
		{
			IStyle result;
			Styles.TryGetValue(name, out result);
			return result;
		}

		public MockStyle AddStyle(string name, bool isParagraph)
		{
			var result = new MockStyle() { Name = name, IsParagraphStyle = isParagraph };
			Styles[name] = result;
			return result;
		}
	}

	internal class MockStyle : IStyle
	{
		public string Name { get; set; }

		public bool IsParagraphStyle { get; set; }

		public IParaStyleInfo ParagraphStyleInfo
		{
			get { throw new NotImplementedException(); }
		}

		public ICharacterStyleInfo DefaultCharacterStyleInfo { get; set; }

		public Dictionary<int, MockCharStyleInfo> Overrides = new Dictionary<int, MockCharStyleInfo>();

		public ICharacterStyleInfo OverrideCharacterStyleInfo(int ws)
		{
			MockCharStyleInfo result;
			Overrides.TryGetValue(ws, out result);
			return result;
		}

		public FwTextAlign Alignment { get; set; }
	}

	internal class MockCharStyleInfo : ICharacterStyleInfo
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

	internal class MockStyleProp<T> : IStyleProp<T>
	{
		public T Value { get; set; }
		public bool ValueIsSet { get; set; }
	}
}