// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is a base class for several ways of faking and actually making a change.
	/// </summary>
	internal abstract class DoItMethod : IGetReplacedObjects
	{
		protected FieldReadWriter m_accessor; // typically the destination accessor, sometimes also the source.
		ISilDataAccess m_sda;
		LcmCache m_cache;
		XElement m_nodeSpec; // specification node for the column

		string m_sEditIf = null;
		bool m_fEditIfNot = false;
		MethodInfo m_miEditIf = null;
		int m_wsEditIf = 0;

		protected DoItMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec)
		{
			m_cache = cache;
			m_accessor = accessor;
			m_sda = sda;
			m_nodeSpec = spec;

			m_sEditIf = XmlUtils.GetOptionalAttributeValue(spec, "editif");
			if (string.IsNullOrEmpty(m_sEditIf))
			{
				return;
			}
			if (m_sEditIf[0] == '!')
			{
				m_fEditIfNot = true;
				m_sEditIf = m_sEditIf.Substring(1);
			}
			var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(spec, "ws"));
			if (sWs != null)
			{
				m_wsEditIf = XmlViewsUtils.GetWsFromString(sWs, m_cache);
			}
		}

		internal bool IsMultilingual(int flid)
		{
			return IsMultilingual(flid, m_cache.DomainDataByFlid.MetaDataCache);
		}

		internal static bool IsMultilingual(int flid, IFwMetaDataCache mdc)
		{
			switch ((CellarPropertyType)(mdc.GetFieldType(flid) & (int)CellarPropertyTypeFilter.VirtualMask))
			{
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Fake doing the change by setting the specified property to the appropriate value
		/// for each item in the set. Disable items that can't be set.
		/// </summary>
		/// <param name="itemsToChange">The items to change.</param>
		/// <param name="tagMadeUpFieldIdentifier">The tag made up field identifier.</param>
		/// <param name="tagEnable">The tag enable.</param>
		/// <param name="state">The state.</param>
		public void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnable, ProgressState state)
		{
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (var hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				var fEnable = OkToChange(hvo);
				if (fEnable)
				{
					m_sda.SetString(hvo, tagMadeUpFieldIdentifier, NewValue(hvo));
				}
				m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		public void Doit(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_sda.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			var commitChanges = XmlUtils.GetOptionalAttributeValue(m_nodeSpec, "commitChanges");
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (var hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				Doit(hvo);
				BulkEditBar.CommitChanges(hvo, commitChanges, m_cache, m_accessor.WritingSystem);
			}
			m_sda.EndUndoTask();
		}

		/// <summary>
		/// Make the change. Usually you can just override NewValue and/or OkToChange.
		/// </summary>
		/// <param name="hvo"></param>
		public virtual void Doit(int hvo)
		{
			if (OkToChange(hvo))
			{
				SetNewValue(hvo, NewValue(hvo));
			}
		}

		/// <summary>
		/// Get the old value, assuming it is some kind of string property
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected ITsString OldValue(int hvo)
		{
			return m_accessor.CurrentValue(hvo);
		}

		protected void SetNewValue(int hvoItem, ITsString tss)
		{
			m_accessor.SetNewValue(hvoItem, tss);
		}

		protected virtual bool OkToChange(int hvo)
		{
			if (string.IsNullOrEmpty(m_sEditIf) || m_wsEditIf == 0)
			{
				return true;
			}
			var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (m_miEditIf == null)
			{
				m_miEditIf = co.GetType().GetMethod(m_sEditIf);
			}
			if (m_miEditIf == null)
			{
				return true;
			}
			var o = m_miEditIf.Invoke(co, new object[] { m_wsEditIf });
			if (o.GetType() != typeof(bool))
			{
				return true;
			}
			return m_fEditIfNot ? !(bool)o : (bool)o;
		}

		protected abstract ITsString NewValue(int hvo);

		#region IGetReplacedObjects Members

		public Dictionary<int, int> ReplacedObjects { get; } = new Dictionary<int, int>();

		#endregion
	}
}