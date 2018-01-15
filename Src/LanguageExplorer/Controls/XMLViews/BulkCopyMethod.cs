// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class BulkCopyMethod : DoItMethod
	{
		FieldReadWriter m_srcAccessor;
		ITsString m_tssSep;
		NonEmptyTargetOptions m_options;

		public BulkCopyMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor, XElement spec, FieldReadWriter srcAccessor, ITsString tssSep, NonEmptyTargetOptions options)
			: base(cache, sda, dstAccessor, spec)
		{
			m_srcAccessor = srcAccessor;
			m_tssSep = tssSep;
			m_options = options;
		}

		/// <summary>
		/// The preview looks neater if things that won't change are not shown as changing.
		/// So, only 'ok to change' if there is a difference.
		/// </summary>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
			{
				return false;
			}
			var tssOld = OldValue(hvo);
			if (m_options == NonEmptyTargetOptions.DoNothing && tssOld != null && tssOld.Length != 0)
			{
				return false;		// Don't want to modify existing data.
			}
			string sOld = null;
			if (tssOld != null)
			{
				sOld = tssOld.Text;
			}
			var tssNew = NewValue(hvo);
			string sNew = null;
			if (tssNew != null)
			{
				sNew = tssNew.Text;
			}
			if (string.IsNullOrEmpty(sOld) && string.IsNullOrEmpty(sNew))
			{
				return false;		// They're really the same, regardless of properties.
			}
			if (sOld != sNew)
			{
				return true;
			}
			return !tssNew.Equals(tssOld);
		}


		protected override ITsString NewValue(int hvo)
		{
			var tssNew = m_srcAccessor.CurrentValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
			if (m_options != NonEmptyTargetOptions.Append)
			{
				return tssNew;
			}
			var tssOld = OldValue(hvo);
			if (tssOld == null || tssOld.Length == 0)
			{
				return tssNew;
			}
			var bldr = tssOld.GetBldr();
			bldr.ReplaceTsString(bldr.Length, bldr.Length, m_tssSep);
			bldr.ReplaceTsString(bldr.Length, bldr.Length, tssNew);
			return bldr.GetString();
		}
	}
}