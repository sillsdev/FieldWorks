// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ObjectLabels.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This interface should be implemented by items in the list, unless they ARE ITsStrings.
	/// Eventually we may also allow items that merely have a property that returns an ITsString.
	/// </summary>
	public interface ITssValue
	{
		/// <summary>
		/// Get a TsString representation of the object.
		/// </summary>
		ITsString AsTss {get;}
	}

	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class ObjectLabel : ITssValue
	{
		/// <summary>
		/// This controls which writing system will be tried for the name to display.
		/// </summary>
		protected Set<int> m_writingSystemIds;

		/// <summary>
		///
		/// </summary>
		protected FdoCache m_cache;

		/// <summary>
		///
		/// </summary>
		protected int m_hvo;

		/// <summary>
		/// controls which property of the object will be used for the name to display.
		/// </summary>
		protected string m_displayNameProperty;
		/// <summary>
		///
		/// </summary>
		protected string m_displayWs;
		/// <summary>
		///
		/// </summary>
		protected string m_bestWs = null;

		/// <summary>
		/// Factory method for creating an ObjectLabel,
		/// even if the class is some kind of CmPossibility,
		/// as long as its hvo is not 0.
		/// </summary>
		static public ObjectLabel CreateObjectLabelOnly(FdoCache cache, int hvo,
			string displayNameProperty, string displayWs)
		{
			if (hvo == 0)
				return new NullObjectLabel(cache);
			else
				return new ObjectLabel(cache, hvo, displayNameProperty, displayWs);
		}

		/// <summary>
		/// a  factory method for creating the correct type of object label, depending on the
		/// class of the object
		/// </summary>
		static public ObjectLabel CreateObjectLabel(FdoCache cache, int hvo,
			string displayNameProperty, string displayWs)
		{
			if (hvo == 0)
			{
				return new NullObjectLabel(cache);
			}
			else
			{
				//enhance: this is very expensive currently, it loads the entire object.
				// does it? it's not obvious to the casual observer...
				uint classId = (uint)cache.GetClassOfObject(hvo);
				uint baseClassId = 0;
				if (classId == CmPossibility.kClassId)
					baseClassId = classId;	// not exactly true, but simplifies logic below.
				else
					baseClassId = cache.MetaDataCacheAccessor.GetBaseClsId(classId);

				if (CmPossibility.kClassId == baseClassId)
					return new CmPossibilityLabel(cache, hvo, displayNameProperty, displayWs);
				else if (MoInflClass.kClassId == classId)
					return new MoInflClassLabel(cache, hvo, displayNameProperty, displayWs);
				else
					return new ObjectLabel(cache, hvo, displayNameProperty, displayWs);
			}
		}

		/// <summary>
		/// a  factory method for creating the correct type of object label, depending on the
		/// class of the object
		/// </summary>
		static public ObjectLabel CreateObjectLabel(FdoCache cache, int hvo,
			string displayNameProperty)
		{
			return CreateObjectLabel(cache, hvo, displayNameProperty, null);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">the property to use to get the label.</param>
		protected ObjectLabel(FdoCache cache, int hvo, string displayNameProperty)
		{
			InitializeNew(displayNameProperty, cache, "analysis", hvo);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">the property to use to get the label.</param>
		/// <param name="sDisplayWs">the ws to use to get the label.</param>
		protected ObjectLabel(FdoCache cache, int hvo, string displayNameProperty,
			string sDisplayWs)
		{
			InitializeNew(displayNameProperty, cache, sDisplayWs, hvo);
		}

		private void InitializeNew(string displayNameProperty, FdoCache cache, string sDisplayWs, int hvo)
		{
			m_displayNameProperty = displayNameProperty;
			m_cache = cache;
			m_displayWs = (sDisplayWs == null || sDisplayWs == String.Empty) ? "best analorvern" : sDisplayWs;
			Hvo = hvo; // This must be done before the EstablishWritingSystemsToTry call, which relies on the hvo having been set
			EstablishWritingSystemsToTry(m_displayWs);
			if (m_displayWs.StartsWith("best"))
				m_bestWs = m_displayWs;
		}

		/// <summary>
		/// the id of the object
		/// </summary>
		public int Hvo
		{
			get
			{
				return m_hvo;
			}
			set
			{
				m_hvo = value;
			}
		}
		/// <summary>
		/// the FDO Cache of the object
		/// </summary>
		public virtual FdoCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				//just for subclasses
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// What would be shown, say, in a combobox
		/// </summary>
		public virtual string DisplayName
		{
			set
			{
				//just for subclasses
				throw new NotImplementedException();
			}

			get
			{
				return AsTss.Text;
			}
		}

		/// <summary>
		/// Override the method to return the right string.
		/// </summary>
		/// <returns>A display string</returns>
		public override string ToString()
		{
			return DisplayName;
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		/// <returns></returns>
		public virtual bool GetHaveSubItems()
		{
			return false;	//only sub-classes may have children
		}

		/// <summary>
		/// the labels of the sub items of this object.
		/// </summary>
		public virtual ObjectLabelCollection SubItems
		{
			get
			{
				return new ObjectLabelCollection(m_cache, new List<int>(0), m_displayNameProperty, m_displayWs);
			}
		}

		/// <summary>
		/// Create the ordered vector of writing sytems to try for displaying names.
		/// </summary>
		protected void EstablishWritingSystemsToTry(string sDisplayWs)
		{
			if (m_cache == null || m_writingSystemIds != null)
				return;

			if (sDisplayWs == null || sDisplayWs == String.Empty)
				sDisplayWs = "analysis vernacular";		// very general default.
			int flid = 0;
			if (!string.IsNullOrEmpty(m_displayNameProperty))
			{
				string className = m_cache.GetClassName((uint)m_cache.GetClassOfObject(Hvo));
				IVwVirtualHandler vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerName(className, m_displayNameProperty);
				if (vh != null)
					flid = vh.Tag;
			}
			m_writingSystemIds = LangProject.GetWritingSystemIdsFromLabel(m_cache, sDisplayWs, m_cache.DefaultUserWs, m_hvo, flid, null);
		}

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public virtual ITsString AsTss
		{
			get
			{
				CmObject obj = (CmObject)CmObject.CreateFromDBObject(m_cache, Hvo);
				// to do: make this use the new CmObjectUI or whatever it is called, when that
				// is available.
				//enhance: get or construct a name w/out loading the object
				if (m_displayNameProperty != null)
				{
					if (obj is MoMorphSynAnalysis)
					{
						MoMorphSynAnalysis msa = obj as MoMorphSynAnalysis;
						switch (m_displayNameProperty)
						{
							case "InterlinearName": // Fall through.
							case "InterlinearNameTSS":
								return msa.InterlinearNameTSS;
							case "LongName":
							{
								ITsStrFactory tsf = TsStrFactoryClass.Create();
								return tsf.MakeString(msa.LongName, m_cache.LangProject.DefaultAnalysisWritingSystem);
							}
							case "LongNameTs":
							{
								return msa.LongNameTs;
							}
							case "ChooserNameTS":
							{
								return msa.ChooserNameTS;
							}
						}
					}
					else if (m_displayNameProperty == "LongNameTSS" &&
						obj is LexSense)
					{
						return ((LexSense)obj).LongNameTSS;
					}
					else if (m_displayNameProperty == "LongName" &&
						obj is FsFeatStruc)
					{
						return ((FsFeatStruc)obj).LongNameTSS;
					}
				}
				return obj.ShortNameTSS;
			}
		}

		#endregion ITssValue Implementation
	}


	/// <summary>
	/// Strongly typed collection for ObjectLabel structs
	/// </summary>
	public class ObjectLabelCollection : System.Collections.CollectionBase
	{
		/// <summary>
		/// Default Contructor.
		/// </summary>
		/// <returns>An ObjectLabelCollection with no Objectlabels.</returns>
		/// <remarks>
		/// Client must add the ObjectLabels manually.
		/// </remarks>
		public ObjectLabelCollection()
		{
		}

		/// <summary>
		/// Get a list of hvos, create a collection of labels for them.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, List<int> hvos,
			string displayNameProperty, string displayWs, bool fIncludeNone)
		{
			Init(hvos, cache, displayNameProperty, displayWs, fIncludeNone);
		}

		/// <summary>
		/// Get a set of hvos, create a collection of labels for them.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, Set<int> hvos,
			string displayNameProperty, string displayWs, bool fIncludeNone)
		{
			Init(new List<int>(hvos.ToArray()), cache, displayNameProperty, displayWs, fIncludeNone);
		}

		/// <summary>
		/// Get a list of hvos, create a collection of labels for them.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, List<int> hvos,
			string displayNameProperty, string displayWs)
		{
			Init(hvos, cache, displayNameProperty, displayWs, false);
		}

		/// <summary>
		/// Get a set of hvos, create a collection of labels for them.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, Set<int> hvos,
			string displayNameProperty, string displayWs)
		{
			Init(new List<int>(hvos.ToArray()), cache, displayNameProperty, displayWs, false);
		}

		/// <summary>
		/// Get a list of hvos, create a collection of labels for them using the best available
		/// writing system property.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, List<int> hvos,
			string displayNameProperty)
		{
			Init(hvos, cache, displayNameProperty, "best analorvern", false);
		}

		/// <summary>
		/// Get a set of hvos, create a collection of labels for them using the best available
		/// writing system property.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, Set<int> hvos,
			string displayNameProperty)
		{
			Init(new List<int>(hvos.ToArray()), cache, displayNameProperty, "best analorvern", false);
		}

		/// <summary>
		/// Given a list of hvos, create a collection of labels for them using the default
		/// display name and writing system properties.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, List<int> hvos)
		{
			Init(hvos, cache, null, null, false);
		}

		/// <summary>
		/// Given a set of hvos, create a collection of labels for them using the default
		/// display name and writing system properties.
		/// </summary>
		/// <returns>An ObjectLabelCollection of ObjectLabel structs.</returns>
		public ObjectLabelCollection(FdoCache cache, Set<int> hvos)
		{
			Init(new List<int>(hvos.ToArray()), cache, null, null, false);
		}

		private void Init(List<int> hvos, FdoCache cache, string displayNameProperty,
			string displayWs, bool fIncludeNone)
		{
			foreach (int hvo in hvos)
			{
				this.Add(ObjectLabel.CreateObjectLabel(cache, hvo, displayNameProperty,
					displayWs));
			}
			// You get a pretty green dialog box if this is inserted first!?
			if (fIncludeNone)
				this.Add(ObjectLabel.CreateObjectLabel(cache, 0, displayNameProperty,
					displayWs));
		}
		/// <summary>
		/// Add an ObjectLabel struct to the collection.
		/// </summary>
		/// <param name="ol">The ObjectLabel to add.</param>
		public ObjectLabel Add(ObjectLabel ol)
		{
			List.Add(ol);
			return ol;
		}


		/// <summary>
		/// Remove the ObjectLabel at the specified index.
		/// </summary>
		/// <param name="index">Index of object to remove.</param>
		public void Remove(int index)
		{
			List.RemoveAt(index);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the ObjectLabel at the given index.
		/// </summary>
		/// <param name="index">Index of object to return.</param>
		/// <returns>The ObjectLabel at the specified index.</returns>
		/// -----------------------------------------------------------------------------------
		public ObjectLabel this[int index]
		{
			get
			{
				return (ObjectLabel)List[index];
			}
		}

		/// <summary>
		/// Return whether the list in this object collection is flat.
		/// </summary>
		public bool IsFlatList()
		{
			foreach (ObjectLabel label in this.List)
			{
				if (label.GetHaveSubItems())
					return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class NullObjectLabel : ObjectLabel
	{
		private string m_label = Strings.ksEmpty;

		/// <summary>
		/// Constructor.
		/// </summary>
		public NullObjectLabel()
			: base(null, 0, null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public NullObjectLabel(FdoCache cache)
			: base(cache, 0, null)
		{
		}

		/// <summary>
		/// What would be shown, say, in a combobox
		/// </summary>
		public override string DisplayName
		{
			get
			{
				return m_label;
			}
			set
			{
				m_label = value;
			}
		}

		/// <summary>
		/// Access the cache.
		/// </summary>
		public override FdoCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
			}
		}

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public override ITsString AsTss
		{
			get
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(DisplayName, m_cache.LangProject.DefaultUserWritingSystem);
			}
		}

		#endregion ITssValue Implementation
	}

	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class CmPossibilityLabel : ObjectLabel
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">property name to display</param>
		/// <param name="displayWs">writing system to display</param>
		public CmPossibilityLabel(FdoCache cache, int hvo, string displayNameProperty,
			string displayWs)
			: base(cache, hvo, displayNameProperty, displayWs)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">property name to display</param>
		public CmPossibilityLabel(FdoCache cache, int hvo, string displayNameProperty)
			: base(cache, hvo, displayNameProperty)
		{
		}

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public override ITsString AsTss
		{
			get
			{
				ICmPossibility cp = CmPossibility.CreateFromDBObject(m_cache, m_hvo);
				MultiUnicodeAccessor muaName = cp.Name;
				Debug.Assert(muaName != null);
				MultiUnicodeAccessor muaAbbr = cp.Abbreviation;
				Debug.Assert(muaAbbr != null);
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				int userWs = m_cache.LangProject.DefaultUserWritingSystem;
				if (m_bestWs != null)
				{
					ITsString tssAbbr = null;
					switch (m_bestWs)
					{
						case "best analysis":
							tssAbbr = muaAbbr.BestAnalysisAlternative;
							break;
						case "best vernacular":
							tssAbbr = muaAbbr.BestVernacularAlternative;
							break;
						case "best analorvern":
							tssAbbr = muaAbbr.BestAnalysisVernacularAlternative;
							break;
						case "best vernoranal":
							tssAbbr = muaAbbr.BestVernacularAnalysisAlternative;
							break;
					}
					if ((m_displayNameProperty != null)
						&& ((m_displayNameProperty == "LongName") || (m_displayNameProperty == "AbbrAndNameTSS")))
					{
						tisb.AppendTsString(tssAbbr);
						tisb.AppendTsString(tsf.MakeString(" - ", userWs));
					}
					switch (m_bestWs)
					{
						case "best analysis":
							tisb.AppendTsString(muaName.BestAnalysisAlternative);
							break;
						case "best vernacular":
							tisb.AppendTsString(muaName.BestVernacularAlternative);
							break;
						case "best analorvern":
							tisb.AppendTsString(muaName.BestAnalysisVernacularAlternative);
							break;
						case "best vernoranal":
							tisb.AppendTsString(muaName.BestVernacularAnalysisAlternative);
							break;
					}
				}
				else
				{
					int analWs = m_cache.LangProject.DefaultAnalysisWritingSystem;
					string name = null;
					int nameWs = 0;
					string abbr = null;
					int abbrWs = 0;
					foreach (int ws in m_writingSystemIds)
					{
						string alt = muaAbbr.GetAlternative(ws);
						if (abbrWs == 0 && alt != null && alt != String.Empty)
						{
							// Save abbr and ws
							abbrWs = ws;
							abbr = alt;
						}
						alt = muaName.GetAlternative(ws);
						if (nameWs == 0 && alt != null && alt != String.Empty)
						{
							// Save name and ws
							nameWs = ws;
							name = alt;
						}
					}
					if (name == null || name == String.Empty)
					{
						name = Strings.ksQuestions;
						nameWs = userWs;
					}
					if (abbr == null || abbr == String.Empty)
					{
						abbr = Strings.ksQuestions;
						abbrWs = userWs;
					}
					if ((m_displayNameProperty != null)
						&& (m_displayNameProperty == "LongName"))
					{
						Debug.Assert(abbr != null && abbr != String.Empty);
						tisb.AppendTsString(tsf.MakeString(abbr, abbrWs));
						tisb.AppendTsString(tsf.MakeString(" - ", userWs));
					}
					Debug.Assert(name != null && name != String.Empty);
					tisb.AppendTsString(tsf.MakeString(name, nameWs));
				}

				return tisb.GetString();
			}
		}

		#endregion ITssValue Implementation

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override ObjectLabelCollection SubItems
		{
			get
			{
				ICmPossibility possibility =
					(ICmPossibility)CmObject.CreateFromDBObject(m_cache, m_hvo);
				return new ObjectLabelCollection(m_cache,
					new List<int>(possibility.SubPossibilitiesOS.HvoArray),
					m_displayNameProperty, m_displayWs);
			}
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		/// <returns></returns>
		public override bool GetHaveSubItems()
		{
			// enhance: this is *extremely* inefficient. should store this possibility as a
			// member variable.
			ICmPossibility possibility = (ICmPossibility)CmObject.CreateFromDBObject(m_cache, m_hvo);
			return possibility.SubPossibilitiesOS.Count > 0;
		}
	}
	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class MoInflClassLabel : ObjectLabel
	{
		private IMoInflClass m_ic;
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">property name to display</param>
		/// <param name="displayWs">writing system to display</param>
		public MoInflClassLabel(FdoCache cache, int hvo, string displayNameProperty,
			string displayWs)
			: base(cache, hvo, displayNameProperty, displayWs)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="hvo">ID of the database object.</param>
		/// <param name="displayNameProperty">property name to display</param>
		public MoInflClassLabel(FdoCache cache, int hvo, string displayNameProperty)
			: base(cache, hvo, displayNameProperty)
		{
		}

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override ObjectLabelCollection SubItems
		{
			get
			{
//                m_ic = (IMoInflClass) CmObject.CreateFromDBObject(m_cache, m_hvo);
				return new ObjectLabelCollection(m_cache,
					new List<int>(m_ic.SubclassesOC.HvoArray),
					m_displayNameProperty, m_displayWs);
			}
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		/// <returns></returns>
		public override bool GetHaveSubItems()
		{
			m_ic = (IMoInflClass)CmObject.CreateFromDBObject(m_cache, m_hvo);
			if (m_ic == null)
				return false;
			return m_ic.SubclassesOC.Count > 0;
		}
	}
}
