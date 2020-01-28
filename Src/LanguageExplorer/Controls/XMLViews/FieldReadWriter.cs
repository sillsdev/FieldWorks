// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// These classes are command objects representing the task of getting a string value
	/// from a browse column in a way specified by its node and writing a new string value
	/// back to the object. This is orthogonal to the several different transformations
	/// that can be performed on the strings so obtained. This abstract class also has the
	/// responsibility for deciding whether a field read/writer can be created for the column,
	/// and if so, for creating the appropriate subclass.
	/// </summary>
	internal abstract class FieldReadWriter : IGhostable
	{
		public abstract ITsString CurrentValue(int hvo);
		public abstract void SetNewValue(int hvo, ITsString tss);
		public abstract int WritingSystem { get; }

		protected GhostParentHelper m_ghostParentHelper;

		protected FieldReadWriter(ISilDataAccess sda)
		{
			DataAccess = sda;
		}

		public ISilDataAccess DataAccess { get; set; }

		// If ws is zero, determine a ws for the specified string field.
		internal static int GetWsFromMetaData(int wsIn, int flid, LcmCache cache)
		{
			if (wsIn != 0)
			{
				return wsIn;
			}
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			// ws not specified in the file; better be in metadata
			var ws = mdc.GetFieldWs(flid);
			switch (ws)
			{
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsAnalVerns:
					return cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
				case WritingSystemServices.kwsVernAnals:
					return cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
				default:
					return 0;
			}
		}

		public static FieldReadWriter Create(XElement node, LcmCache cache)
		{
			return Create(node, cache, 0);
		}

		public static FieldReadWriter Create(XElement node, LcmCache cache, int hvoRootObj)
		{
			var transduceField = XmlUtils.GetOptionalAttributeValue(node, "transduce");
			if (string.IsNullOrEmpty(transduceField))
			{
				return null;
			}
			var parts = transduceField.Split('.');
			if (parts.Length != 2 && parts.Length != 3)
			{
				return null;
			}
			var className = parts[0];
			var fieldName = parts[1];
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			var clid = mdc.GetClassId(className);
			if (clid == 0)
			{
				return null;
			}
			var flid = mdc.GetFieldId2(clid, fieldName, true);
			var ws = WritingSystemServices.GetWritingSystem(cache, node.ConvertElement(), null, hvoRootObj, flid, 0).Handle;
			if (parts.Length == 2)
			{
				// parts are divided into class.propname
				FieldReadWriter frw = mdc.IsMultilingual(flid) ? new OwnMultilingualPropReadWriter(cache, flid, ws) : new OwnStringPropReadWriter(cache, flid, GetWsFromMetaData(ws, flid, cache));
				frw.InitForGhostItems(cache, node);
				return frw;
			}
			// parts.Length is 3. We have class.objectpropname.propname
			var clidDst = mdc.GetDstClsId(flid);
			var fieldType = mdc.GetFieldType(flid);
			var flid2 = mdc.GetFieldId2(clidDst, parts[2], true);
			var clidCreate = clidDst;   // default
			var createClassName = XmlUtils.GetOptionalAttributeValue(node, "transduceCreateClass");
			if (createClassName != null)
			{
				clidCreate = mdc.GetClassId(createClassName);
			}
			if (mdc.IsMultilingual(flid2))
			{
				Debug.Assert(ws != 0);
				// If it's a multilingual field and we didn't get a writing system, we can't transduce this field.
				if (ws == 0)
				{
					return null;
				}
				switch (fieldType)
				{
					case (int)CellarPropertyType.OwningAtomic:
						return new OwnAtomicMultilingualPropReadWriter(cache, flid2, ws, flid, clidCreate);
					case (int)CellarPropertyType.OwningSequence:
						return new OwnSeqMultilingualPropReadWriter(cache, flid2, ws, flid, clidCreate);
					default:
						return null; // can't handle yet
				}
			}
			switch (fieldType)
			{
				case (int)CellarPropertyType.OwningAtomic:
					return new OwnAtomicStringPropReadWriter(cache, flid2, ws, flid, clidCreate);
				case (int)CellarPropertyType.OwningSequence:
					return new OwnSeqStringPropReadWriter(cache, flid2, ws, flid, clidCreate);
				default:
					return null; // can't handle yet
			}
		}
		internal virtual List<int> FieldPath => null;

		#region IGhostable Members

		public void InitForGhostItems(LcmCache cache, XElement colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		#endregion

		/// <summary>
		/// FieldReadWriter for strings stored in (non-multilingual) props of the object itself.
		/// </summary>
		private class OwnStringPropReadWriter : FieldReadWriter
		{
			protected int m_flid;
			private int m_flidType;
			protected int m_ws;
			private LcmCache m_cache;

			public OwnStringPropReadWriter(LcmCache cache, int flid, int ws)
				: base(cache.MainCacheAccessor)
			{
				m_cache = cache;
				m_flid = flid;
				m_flidType = GetFlidType();
				m_ws = ws;
			}

			private int GetFlidType()
			{
				return m_cache.MetaDataCacheAccessor.GetFieldType(m_flid);
			}

			internal override List<int> FieldPath => new List<int>(new[] { m_flid });

			public override ITsString CurrentValue(int hvo)
			{
				var hvoStringOwner = hvo;
				if (m_ghostParentHelper != null)
				{
					hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
				}
				if (hvoStringOwner == 0)
				{
					return null; // hasn't been created yet.
				}
				if (m_flidType != (int)CellarPropertyType.Unicode)
				{
					return DataAccess.get_StringProp(hvoStringOwner, m_flid);
				}
				var ustring = DataAccess.get_UnicodeProp(hvoStringOwner, m_flid);
				// Enhance: For the time being Default Analysis Ws is sufficient. If there is ever
				// a Unicode vernacular field that is made Bulk Editable, we will need to rethink this code.
				return TsStringUtils.MakeString(ustring ?? string.Empty, m_cache.DefaultAnalWs);
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				var hvoStringOwner = hvo;
				if (m_ghostParentHelper != null)
				{
					hvoStringOwner = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvo, m_flid);
				}
				if (m_flidType == (int)CellarPropertyType.Unicode)
				{
					SetUnicodeStringValue(hvoStringOwner, tss);
				}
				else
				{
					SetStringValue(hvoStringOwner, tss);
				}
			}

			private void SetUnicodeStringValue(int hvoStringOwner, ITsString tss)
			{
				var strValue = (tss == null) ? string.Empty : tss.Text;
				DataAccess.set_UnicodeProp(hvoStringOwner, m_flid, strValue);
			}

			protected virtual void SetStringValue(int hvoStringOwner, ITsString tss)
			{
				DataAccess.SetString(hvoStringOwner, m_flid, tss);
			}

			public override int WritingSystem => m_ws;
		}

		/// <summary>
		/// FieldReadWriter for strings stored in a non-multilingual string prop of an object
		/// owned in an atomic property of the base object.
		/// </summary>
		private sealed class OwnAtomicStringPropReadWriter : OwnStringPropReadWriter
		{
			private int m_flidObj;
			private int m_clid; // to create if missing

			public OwnAtomicStringPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
				: base(cache, flidString, ws)
			{
				m_flidObj = flidObj;
				m_clid = clid;
			}

			internal override List<int> FieldPath
			{
				get
				{
					var fieldPath = base.FieldPath;
					fieldPath.Insert(0, m_flidObj);
					return fieldPath;
				}
			}

			public override ITsString CurrentValue(int hvo)
			{
				return base.CurrentValue(DataAccess.get_ObjectProp(hvo, m_flidObj));
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				var ownedAtomicObj = DataAccess.get_ObjectProp(hvo, m_flidObj);
				var fHadObject = ownedAtomicObj != 0;
				if (!fHadObject)
				{
					if (m_clid == 0)
					{
						return;
					}
					ownedAtomicObj = DataAccess.MakeNewObject(m_clid, hvo, m_flidObj, -2);
				}
				base.SetNewValue(ownedAtomicObj, tss);
			}
		}

		/// <summary>
		/// FieldReadWriter for strings stored in a non-multilingual string prop of the FIRST object
		/// owned in an sequence property of the base object.
		/// </summary>
		private sealed class OwnSeqStringPropReadWriter : OwnStringPropReadWriter
		{
			private int m_flidObj;
			private int m_clid; // to create if missing

			public OwnSeqStringPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
				: base(cache, flidString, ws)
			{
				m_flidObj = flidObj;
				m_clid = clid;
			}

			internal override List<int> FieldPath
			{
				get
				{
					var fieldPath = base.FieldPath;
					fieldPath.Insert(0, m_flidObj);
					return fieldPath;
				}
			}

			public override ITsString CurrentValue(int hvo)
			{
				return DataAccess.get_VecSize(hvo, m_flidObj) > 0 ? base.CurrentValue(DataAccess.get_VecItem(hvo, m_flidObj, 0)) : null;
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				int firstSeqObj;
				var fHadOwningItem = DataAccess.get_VecSize(hvo, m_flidObj) > 0;
				if (fHadOwningItem)
				{
					firstSeqObj = DataAccess.get_VecItem(hvo, m_flidObj, 0);
				}
				else
				{
					// make first vector item if we know the class to base it on.
					if (m_clid == 0)
					{
						return;
					}
					firstSeqObj = DataAccess.MakeNewObject(m_clid, hvo, m_flidObj, 0);
				}
				base.SetNewValue(firstSeqObj, tss);
			}
		}

		/// <summary>
		/// FieldReadWriter for strings stored in multilingual props of an object.
		/// </summary>
		private class OwnMultilingualPropReadWriter : OwnStringPropReadWriter
		{
			private bool m_fFieldAllowsMultipleRuns;

			public OwnMultilingualPropReadWriter(LcmCache cache, int flid, int ws)
				: base(cache, flid, ws)
			{

				try
				{
					var fieldType = DataAccess.MetaDataCache.GetFieldType(flid);
					m_fFieldAllowsMultipleRuns = fieldType == (int)CellarPropertyType.MultiString;
				}
				catch (KeyNotFoundException)
				{
					m_fFieldAllowsMultipleRuns = true; // Possibly a decorator field??
				}
			}

			public override ITsString CurrentValue(int hvo)
			{
				var hvoStringOwner = hvo;
				if (m_ghostParentHelper != null)
				{
					hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
				}
				return hvoStringOwner == 0 ? null : DataAccess.get_MultiStringAlt(hvoStringOwner, m_flid, m_ws);
			}

			// In this subclass we're setting a multistring.
			protected override void SetStringValue(int hvoStringOwner, ITsString tss)
			{
				if (!m_fFieldAllowsMultipleRuns && tss.RunCount > 1)
				{
					// Illegally trying to store a multi-run TSS in a single-run field. This will fail.
					// Typically it's just that we tried to insert an English comma or similar.
					// Patch it up by making the whole string take on the properties of the first run.
					var bldr = tss.GetBldr();
					bldr.SetProperties(0, bldr.Length, tss.get_Properties(0));
					tss = bldr.GetString();
				}
				DataAccess.SetMultiStringAlt(hvoStringOwner, m_flid, m_ws, tss);
			}
		}

		/// <summary>
		/// FieldReadWriter for strings stored in a multilingual prop of an object
		/// owned in an atomic property of the base object.
		/// </summary>
		private sealed class OwnAtomicMultilingualPropReadWriter : OwnMultilingualPropReadWriter
		{
			private int m_flidObj;
			private int m_clid; // to create if missing

			public OwnAtomicMultilingualPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
				: base(cache, flidString, ws)
			{
				m_flidObj = flidObj;
				m_clid = clid;
			}

			public override ITsString CurrentValue(int hvo)
			{
				return base.CurrentValue(DataAccess.get_ObjectProp(hvo, m_flidObj));
			}

			internal override List<int> FieldPath
			{
				get
				{
					var fieldPath = base.FieldPath;
					fieldPath.Insert(0, m_flidObj);
					return fieldPath;
				}
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				var ownedAtomicObj = DataAccess.get_ObjectProp(hvo, m_flidObj);
				var fHadObject = ownedAtomicObj != 0;
				if (!fHadObject)
				{
					if (m_clid == 0)
					{
						return;
					}
					ownedAtomicObj = DataAccess.MakeNewObject(m_clid, hvo, m_flidObj, -2);
				}
				base.SetNewValue(ownedAtomicObj, tss);
			}
		}

		/// <summary>
		/// FieldReadWriter for strings stored in a multilingual prop of the FIRST object
		/// owned in an sequence property of the base object.
		/// </summary>
		private sealed class OwnSeqMultilingualPropReadWriter : OwnMultilingualPropReadWriter
		{
			private int m_flidObj;
			private int m_clid; // to create if missing

			public OwnSeqMultilingualPropReadWriter(LcmCache cache, int flidString, int ws, int flidObj, int clid)
				: base(cache, flidString, ws)
			{
				m_flidObj = flidObj;
				m_clid = clid;
			}

			internal override List<int> FieldPath
			{
				get
				{
					var fieldPath = base.FieldPath;
					fieldPath.Insert(0, m_flidObj);
					return fieldPath;
				}
			}

			public override ITsString CurrentValue(int hvo)
			{
				return DataAccess.get_VecSize(hvo, m_flidObj) > 0 ? base.CurrentValue(DataAccess.get_VecItem(hvo, m_flidObj, 0)) : null;
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				int firstSeqObj;
				var fHadOwningItem = DataAccess.get_VecSize(hvo, m_flidObj) > 0;
				if (fHadOwningItem)
				{
					firstSeqObj = DataAccess.get_VecItem(hvo, m_flidObj, 0);
				}
				else
				{
					// make first vector item if we know the class to base it on.
					if (m_clid == 0)
					{
						return;
					}
					firstSeqObj = DataAccess.MakeNewObject(m_clid, hvo, m_flidObj, 0);
				}
				base.SetNewValue(firstSeqObj, tss);
			}
		}
	}
}