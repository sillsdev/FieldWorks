// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary />
	public class DummyEditingHelper : EditingHelper
	{
		private WritingSystemManager m_privateWsFactory;

		/// <summary>
		/// Gets the text from clipboard.
		/// </summary>
		public ITsString CallGetTextFromClipboard()
		{
			return GetTextFromClipboard(null, false, TsStringUtils.MakeProps("bla", 1));
		}

		protected override bool IsParagraphLevelTag(int tag)
		{
			throw new NotSupportedException();
		}

		protected override int ParagraphContentsTag
		{
			get { throw new NotSupportedException(); }
		}

		protected override int ParagraphPropertiesTag
		{
			get { throw new NotSupportedException(); }
		}

		protected override ILgWritingSystemFactory WritingSystemFactory => base.WritingSystemFactory == null ? m_privateWsFactory ?? (m_privateWsFactory = new WritingSystemManager()) : base.WritingSystemFactory;
	}
}