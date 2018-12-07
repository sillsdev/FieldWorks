// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Collect the results as a TsString.
	/// </summary>
	internal class TsStringCollectorEnv : CollectorEnv
	{
		private ITsIncStrBldr m_builder;
		private bool m_fNewParagraph = false;
		private int m_cParaOpened = 0;

		/// <summary />
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		public TsStringCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot):
			base(baseEnv, sda, hvoRoot)
		{
			m_builder = TsStringUtils.MakeIncStrBldr();
			// In case we add some raw strings, typically numbers, satisfy the constraints of string
			// builders by giving it SOME writing system.
			m_builder.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, sda.WritingSystemFactory.UserWs);
		}

		/// <summary>
		/// Accumulate a TsString into our result. The base implementation does nothing.
		/// </summary>
		public override void AddTsString(ITsString tss)
		{
			AppendSpaceForFirstWordInNewParagraph(tss.Text);
			m_builder.AppendTsString(tss);
		}

		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		public override void AddResultString(string s)
		{
			AppendSpaceForFirstWordInNewParagraph(s);
			m_builder.Append(s);
		}

		/// <summary>
		/// keep track of the opened paragraphs, so we can add spaces before strings in new paragraphs.
		/// </summary>
		public override void OpenParagraph()
		{
			base.OpenParagraph();
			m_cParaOpened++;
			if (m_cParaOpened > 1)
			{
				m_fNewParagraph = true;
			}
		}

		/// <summary>
		/// Indicates whether this collector will append space for first word in new paragraph.
		/// True, by default.
		/// </summary>
		public bool RequestAppendSpaceForFirstWordInNewParagraph { get; set; } = true;

		/// <summary>
		/// We want to append a space if its the first (non-zero-lengthed) word in new paragraph
		/// (after the first paragraph).
		/// </summary>
		private void AppendSpaceForFirstWordInNewParagraph(string s)
		{
			if (m_fNewParagraph && !string.IsNullOrEmpty(s))
			{
				if (RequestAppendSpaceForFirstWordInNewParagraph)
				{
					m_builder.Append(" ");
				}
				m_fNewParagraph = false;
			}
		}

		/// <summary>
		/// Accumulate a string into our result, with known writing system.
		/// This base implementation ignores the writing system.
		/// </summary>
		public override void AddResultString(string s, int ws)
		{
			// we want to prepend a space to our string before appending more text.
			AppendSpaceForFirstWordInNewParagraph(s);
			m_builder.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			m_builder.Append(s);
		}

		/// <summary>
		/// Gets the result.
		/// </summary>
		public ITsString Result => m_builder.GetString();
	}
}