// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Handle BulkEditBar's "Transduce" tab action to either make a real change, or give a preview of a change.
	/// </summary>
	internal class TransduceMethod : DoItMethod
	{
		FieldReadWriter m_srcAccessor;
		ECInterfaces.IEncConverter m_converter;
		bool m_fFailed;
		ITsString m_tssSep;
		NonEmptyTargetOptions m_options;

		internal TransduceMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor, XElement spec, FieldReadWriter srcAccessor,
			ECInterfaces.IEncConverter converter, ITsString tssSep, NonEmptyTargetOptions options)
			: base(cache, sda, dstAccessor, spec)
		{
			m_srcAccessor = srcAccessor;
			m_converter = converter;
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
			if (m_options == NonEmptyTargetOptions.DoNothing)
			{
				var tssOld = OldValue(hvo);
				if (tssOld != null && tssOld.Length != 0)
				{
					return false;
				}
			}
			var tssSrc = m_srcAccessor.CurrentValue(hvo);
			return tssSrc != null && !m_srcAccessor.CurrentValue(hvo).Equals(NewValue(hvo));
		}


		protected override ITsString NewValue(int hvo)
		{
			var tssSrc = m_srcAccessor.CurrentValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
			if (m_fFailed) // once we've had a failure don't try any more this pass.
			{
				return tssSrc;
			}
			var old = tssSrc.Text;
			var converted = string.Empty;
			if (!string.IsNullOrEmpty(old))
			{
				try
				{
					converted = m_converter.Convert(old);
				}
				catch (Exception except)
				{
					MessageBox.Show(string.Format(XMLViewsStrings.ksErrorProcessing, old, except.Message));
					m_fFailed = true;
					return tssSrc;
				}
			}
			var tssNew = TsStringUtils.MakeString(converted, m_accessor.WritingSystem);
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