// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// This is a custom implementation of the ISilDataAccess interface for use by custom
	/// Views-enabled text boxes.
	/// </summary>
	internal class TextBoxDataAccess : SilDataAccessManagedBase
	{
		private readonly Dictionary<HvoFlidWSKey, ITsString> m_strings = new Dictionary<HvoFlidWSKey, ITsString>();
		private ILgWritingSystemFactory m_wsf;

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object. If the writing system factory is set, all strings will be removed.
		/// All rootboxes that use this data access should be reconstructed to remove any references
		/// to the old factory.
		///
		/// </summary>
		/// <value></value>
		/// <returns>A ILgWritingSystemFactory</returns>
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_wsf;
			}
			set
			{
				m_strings.Clear();
				m_wsf = value;
			}
		}

		public override void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
		}

		public override void BeginNonUndoableTask()
		{
		}

		public override void EndUndoTask()
		{
		}

		public override void EndNonUndoableTask()
		{
		}

		public override ITsString get_StringProp(int hvo, int tag)
		{
			return get_MultiStringAlt(hvo, tag, 0);
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			SetMultiStringAlt(hvo, tag, 0, tss);
		}

		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			Debug.Assert(ws > WritingSystemServices.kwsFirstAnal); // FWNX-260: kwsFirstAnal not handled in C++ yet (March/2010).

			var key = new HvoFlidWSKey(hvo, tag, ws);
			ITsString tss;
			if (!m_strings.TryGetValue(key, out tss))
			{
				tss = TsStringUtils.EmptyString(ws == 0 ? m_wsf.UserWs : ws);
				m_strings[key] = tss;
			}
			return tss;
		}

		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			var key = new HvoFlidWSKey(hvo, tag, ws);
			if (_tss == null)
			{
				m_strings.Remove(key);
			}
			else
			{
				m_strings[key] = _tss;
			}
			SendPropChanged(hvo, tag, ws, 0, 0);
		}

		public override bool get_IsValidObject(int hvo)
		{
			return true;
		}

		private TextBoxMetaDataCache m_mdc = new TextBoxMetaDataCache();

		public override IFwMetaDataCache MetaDataCache
		{
			get
			{
				return m_mdc;
			}
			set
			{
				base.MetaDataCache = value;
			}
		}

		/// <summary>
		/// A very trivial MDC, just enough to allow styles to be applied to text in the text box.
		/// </summary>
		private sealed class TextBoxMetaDataCache : IFwMetaDataCache
		{
			public void InitXml(string bstrPathname, bool fClearPrevCache)
			{
				throw new NotSupportedException();
			}

			public void GetFieldIds(int cflid, ArrayPtr rgflid)
			{
				throw new NotSupportedException();
			}

			public string GetOwnClsName(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetDstClsName(int luFlid)
			{
				throw new NotSupportedException();
			}

			public int GetOwnClsId(int luFlid)
			{
				throw new NotSupportedException();
			}

			public int GetDstClsId(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetFieldName(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetFieldLabel(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetFieldHelp(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetFieldXml(int luFlid)
			{
				throw new NotSupportedException();
			}

			public int GetFieldWs(int luFlid)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// The only field that gets asked about in a text box is the main string, and we need a type that DOES
			/// allow styles to be applied.
			/// </summary>
			public int GetFieldType(int luFlid)
			{
				return (int)CellarPropertyType.String;
			}

			public bool get_IsValidClass(int luFlid, int luClid)
			{
				throw new NotSupportedException();
			}

			public void GetClassIds(int cclid, ArrayPtr rgclid)
			{
				throw new NotSupportedException();
			}

			public string GetClassName(int luClid)
			{
				throw new NotSupportedException();
			}

			public bool GetAbstract(int luClid)
			{
				throw new NotSupportedException();
			}

			public int GetBaseClsId(int luClid)
			{
				throw new NotSupportedException();
			}

			public string GetBaseClsName(int luClid)
			{
				throw new NotSupportedException();
			}

			public int GetFields(int luClid, bool fIncludeSuperclasses, int grfcpt, int cflidMax, ArrayPtr rgflid)
			{
				throw new NotSupportedException();
			}

			public int GetClassId(string bstrClassName)
			{
				throw new NotSupportedException();
			}

			public int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
			{
				throw new NotSupportedException();
			}

			public int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
			{
				throw new NotSupportedException();
			}

			public void GetDirectSubclasses(int luClid, int cluMax, out int cluOut, ArrayPtr rgluSubclasses)
			{
				throw new NotSupportedException();
			}

			public void GetAllSubclasses(int luClid, int cluMax, out int cluOut, ArrayPtr rgluSubclasses)
			{
				throw new NotSupportedException();
			}

			public void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotSupportedException();
			}

			public bool get_IsVirtual(int luFlid)
			{
				throw new NotSupportedException();
			}

			public string GetFieldNameOrNull(int luFlid)
			{
				throw new NotSupportedException();
			}

			public int FieldCount
			{
				get { throw new NotSupportedException(); }
			}

			public int ClassCount
			{
				get { throw new NotSupportedException(); }
			}
		}
	}
}