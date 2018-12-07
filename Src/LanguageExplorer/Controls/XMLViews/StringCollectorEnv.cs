// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This subclass is used to accumulate a string equivalent to the result that would be
	/// produced by calling Display().
	/// </summary>
	internal class StringCollectorEnv : CollectorEnv
	{
		/// <summary>
		/// The builder to which we append the text we're collecting.
		/// </summary>
		protected StringBuilder m_builder = new StringBuilder();

		/// <summary />
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		public StringCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>The result.</value>
		public string Result => m_builder.ToString();

		/// <summary>
		/// Accumulate a string into our result. The base implementation does nothing.
		/// </summary>
		public override void AddResultString(string s)
		{
			base.AddResultString (s);
			if (s == null)
			{
				return;
			}
			m_builder.Append(s);
		}
	}
}