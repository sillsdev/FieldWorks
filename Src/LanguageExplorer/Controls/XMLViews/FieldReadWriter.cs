// Copyright (c) 20105-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
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

		protected ISilDataAccess m_sda;
		protected GhostParentHelper m_ghostParentHelper;

		protected FieldReadWriter(ISilDataAccess sda)
		{
			m_sda = sda;
		}

		public ISilDataAccess DataAccess
		{
			get { return m_sda; }
			set { m_sda = value; }
		}

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
			var ws = WritingSystemServices.GetWritingSystem(cache, FwUtils.ConvertElement(node), null, hvoRootObj, flid, 0).Handle;
			if (parts.Length == 2)
			{
				// parts are divided into class.propname
				FieldReadWriter frw = DoItMethod.IsMultilingual(flid, mdc) ? new OwnMultilingualPropReadWriter(cache, flid, ws) : new OwnStringPropReadWriter(cache, flid, GetWsFromMetaData(ws, flid, cache));
				frw.InitForGhostItems(cache, node);
				return frw;
			}

			// parts.Length is 3. We have class.objectpropname.propname
			var clidDst = mdc.GetDstClsId(flid);
			var fieldType = mdc.GetFieldType(flid);
			var flid2 = mdc.GetFieldId2(clidDst, parts[2], true);
			var clidCreate = clidDst;	// default
			var createClassName = XmlUtils.GetOptionalAttributeValue(node, "transduceCreateClass");
			if (createClassName != null)
			{
				clidCreate = mdc.GetClassId(createClassName);
			}
			if (DoItMethod.IsMultilingual(flid2, mdc))
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
	}
}