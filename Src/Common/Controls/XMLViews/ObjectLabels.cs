// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ObjectLabels.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class ObjectLabel : ITssValue
	{
		/// <summary>
		/// This controls which writing system will be tried for the name to display.
		/// </summary>
		protected IEnumerable<int> m_writingSystemIds;

		/// <summary>
		///
		/// </summary>
		protected ICmObject m_obj;

		/// <summary>
		///
		/// </summary>
		protected FdoCache m_cache;

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
		protected string m_bestWs;

		/// <summary>
		/// Factory method for creating an ObjectLabel,
		/// even if the class is some kind of CmPossibility,
		/// as long as its hvo is not 0.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <param name="displayWs">The display ws.</param>
		/// <returns></returns>
		public static ObjectLabel CreateObjectLabelOnly(FdoCache cache, ICmObject obj, string displayNameProperty, string displayWs)
		{
			return obj == null ? new NullObjectLabel(cache) : new ObjectLabel(cache, obj, displayNameProperty, displayWs);
		}

		/// <summary>
		/// a  factory method for creating the correct type of object label, depending on the
		/// class of the object
		/// </summary>
		public static ObjectLabel CreateObjectLabel(FdoCache cache, ICmObject obj, string displayNameProperty, string displayWs)
		{
			if (obj == null)
				return new NullObjectLabel(cache);

			var classId = obj.ClassID;
			return cache.ClassIsOrInheritsFrom(classId, CmPossibilityTags.kClassId)
					? new CmPossibilityLabel(cache, obj as ICmPossibility, displayNameProperty, displayWs)
					: (MoInflClassTags.kClassId == classId
						? new MoInflClassLabel(cache, obj as IMoInflClass, displayNameProperty, displayWs)
						: new ObjectLabel(cache, obj, displayNameProperty, displayWs));
		}

		/// <summary>
		/// a  factory method for creating the correct type of object label, depending on the
		/// class of the object
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <returns></returns>
		public static ObjectLabel CreateObjectLabel(FdoCache cache, ICmObject obj, string displayNameProperty)
		{
			return CreateObjectLabel(cache, obj, displayNameProperty, null);
		}

		/// <summary>
		/// Get a list of hvos, create a collection of labels for them.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objs">The objs.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <param name="displayWs">The display ws.</param>
		/// <param name="fIncludeNone">if set to <c>true</c> [f include none].</param>
		/// <returns>
		/// A list of ObjectLabel structs.
		/// </returns>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(FdoCache cache, IEnumerable<ICmObject> objs,
			string displayNameProperty, string displayWs, bool fIncludeNone)
		{
			foreach(var obj in objs)
			{
				yield return CreateObjectLabel(cache, obj, displayNameProperty,
					displayWs);
			}
			// You get a pretty green dialog box if this is inserted first!?
			if (fIncludeNone)
				yield return new NullObjectLabel(cache);
		}

		/// <summary>
		/// Get a list of objects, create a collection of labels for them.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objs">The objs.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <param name="displayWs">The display ws.</param>
		/// <returns>A list of ObjectLabel structs.</returns>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(FdoCache cache, IEnumerable<ICmObject> objs,
			string displayNameProperty, string displayWs)
		{
			return CreateObjectLabels(cache, objs, displayNameProperty, displayWs, false);
		}

		/// <summary>
		/// Get a list of objects, create a collection of labels for them using the best available
		/// writing system property.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objs">The objs.</param>
		/// <param name="displayNameProperty">The display name property.</param>
		/// <returns>
		/// A list of ObjectLabel structs.
		/// </returns>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(FdoCache cache, IEnumerable<ICmObject> objs,
			string displayNameProperty)
		{
			return CreateObjectLabels(cache, objs, displayNameProperty, "best analorvern");
		}

		/// <summary>
		/// Given a list of objects, create a collection of labels for them using the default
		/// display name and writing system properties.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="objs">The objs.</param>
		/// <returns>
		/// A list of ObjectLabel structs.
		/// </returns>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(FdoCache cache, IEnumerable<ICmObject> objs)
		{
			return CreateObjectLabels(cache, objs, null, null);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="displayNameProperty">the property to use to get the label.</param>
		protected ObjectLabel(FdoCache cache, ICmObject obj, string displayNameProperty)
			: this(cache, obj, displayNameProperty, "analysis")
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="displayNameProperty">the property to use to get the label.</param>
		/// <param name="sDisplayWs">the ws to use to get the label.</param>
		protected ObjectLabel(FdoCache cache, ICmObject obj, string displayNameProperty, string sDisplayWs)
		{
			m_cache = cache;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = string.IsNullOrEmpty(sDisplayWs) ? "best analorvern" : sDisplayWs;
			m_obj = obj; // This must be done before the EstablishWritingSystemsToTry call, which relies on the hvo having been set
			EstablishWritingSystemsToTry(m_displayWs);
			if (m_displayWs.StartsWith("best"))
				m_bestWs = m_displayWs;
		}

		/// <summary>
		/// the object
		/// </summary>
		public ICmObject Object
		{
			get
			{
				return m_obj;
			}
			set
			{
				m_obj = value;
			}
		}

		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// <value>The cache.</value>
		public FdoCache Cache
		{
			get
			{
				return m_cache;
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
		public virtual bool HaveSubItems
		{
			get
			{
				return false;	//only sub-classes may have children
			}
		}

		/// <summary>
		/// the labels of the sub items of this object.
		/// </summary>
		public virtual IEnumerable<ObjectLabel> SubItems
		{
			get
			{
				yield break;
			}
		}

		/// <summary>
		/// Create the ordered vector of writing sytems to try for displaying names.
		/// </summary>
		protected void EstablishWritingSystemsToTry(string sDisplayWs)
		{
			if (m_writingSystemIds != null || m_cache == null || m_obj == null)
				return;

			if (string.IsNullOrEmpty(sDisplayWs))
				sDisplayWs = "analysis vernacular";		// very general default.
			int flid = 0;
			if (!string.IsNullOrEmpty(m_displayNameProperty))
			{
#if WANTPORT //  (FWR-2786 to investigate this)(FLEx) Needs replacement for virtual property handler
				CmObject obj = m_cache.GetObject(Hvo);
				string className = m_cache.MetaDataCache.GetClassName(obj.ClassID);
				IVwVirtualHandler vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerName(className, m_displayNameProperty);
				if (vh != null)
					flid = vh.Tag;
#endif
			}
			m_writingSystemIds = WritingSystemServices.GetWritingSystemIdsFromLabel(m_cache,
				sDisplayWs,
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem,
				m_obj.Hvo,
				flid,
				null);
		}

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public virtual ITsString AsTss
		{
			get
			{
				// to do: make this use the new CmObjectUI or whatever it is called, when that
				// is available.
				if (m_displayNameProperty != null)
				{
					if (m_obj is IMoMorphSynAnalysis)
					{
						var msa = m_obj as IMoMorphSynAnalysis;
						switch (m_displayNameProperty)
						{
							case "InterlinearName": // Fall through.
							case "InterlinearNameTSS":
								return msa.InterlinearNameTSS;
							case "LongName":
							{
								return m_obj.Cache.TsStrFactory.MakeString(
									msa.LongName,
									m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
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
					else if (m_displayNameProperty == "LongNameTSS" && m_obj is ILexSense)
					{
						return (m_obj as ILexSense).LongNameTSS;
					}
					else if (m_displayNameProperty == "LongName" && m_obj is IFsFeatStruc)
					{
						return (m_obj as IFsFeatStruc).LongNameTSS;
					}
					else if (m_displayNameProperty == "ObjectIdName")
					{
						return m_obj.ObjectIdName;
					}
					else
					{
						var prop = m_obj.GetType().GetProperty(m_displayNameProperty);
						if (prop != null)
						{
							var val = prop.GetValue(m_obj, null);
							if (val is ITsString)
								return val as ITsString;
						}
					}
				}
				return m_obj.ShortNameTSS;
			}
		}

		#endregion ITssValue Implementation
	}

	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class NullObjectLabel : ObjectLabel
	{
		private string m_label = XMLViewsStrings.ksEmptyLC;

		/// <summary>
		/// Constructor.
		/// </summary>
		public NullObjectLabel()
			: base(null, null, null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public NullObjectLabel(FdoCache cache)
			: base(cache, null, null)
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

		#region ITssValue Implementation

		/// <summary>
		/// Get an ITsString representation.
		/// </summary>
		public override ITsString AsTss
		{
			get
			{
				return m_cache.TsStrFactory.MakeString(
					DisplayName,
					m_cache.WritingSystemFactory.UserWs);
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
		/// <param name="pos">The possibility.</param>
		/// <param name="displayNameProperty">property name to display</param>
		/// <param name="displayWs">writing system to display</param>
		public CmPossibilityLabel(FdoCache cache, ICmPossibility pos, string displayNameProperty,
			string displayWs)
			: base(cache, pos, displayNameProperty, displayWs)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="pos">The possibility.</param>
		/// <param name="displayNameProperty">property name to display</param>
		public CmPossibilityLabel(FdoCache cache, ICmPossibility pos, string displayNameProperty)
			: base(cache, pos, displayNameProperty)
		{
		}

		/// <summary>
		/// Gets the possibility.
		/// </summary>
		/// <value>The possibility.</value>
		public ICmPossibility Possibility
		{
			get
			{
				return m_obj as ICmPossibility;
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
				var cp = Possibility;
				var muaName = cp.Name;
				Debug.Assert(muaName != null);
				var muaAbbr = cp.Abbreviation;
				Debug.Assert(muaAbbr != null);
				var tisb = TsIncStrBldrClass.Create();
				var tsf = m_cache.TsStrFactory;
				var userWs = m_cache.WritingSystemFactory.UserWs;
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
					var analWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					string name = null;
					var nameWs = 0;
					string abbr = null;
					var abbrWs = 0;
					foreach (var ws in m_writingSystemIds)
					{
						var alt = muaAbbr.get_String(ws).Text;
						if (abbrWs == 0 && !string.IsNullOrEmpty(alt))
						{
							// Save abbr and ws
							abbrWs = ws;
							abbr = alt;
						}
						alt = muaName.get_String(ws).Text;
						if (nameWs == 0 && !string.IsNullOrEmpty(alt))
						{
							// Save name and ws
							nameWs = ws;
							name = alt;
						}
					}
					if (string.IsNullOrEmpty(name))
					{
						name = XMLViewsStrings.ksQuestionMarks;
						nameWs = userWs;
					}
					if (string.IsNullOrEmpty(abbr))
					{
						abbr = XMLViewsStrings.ksQuestionMarks;
						abbrWs = userWs;
					}
					if ((m_displayNameProperty != null)
						&& (m_displayNameProperty == "LongName"))
					{
						Debug.Assert(!string.IsNullOrEmpty(abbr));
						tisb.AppendTsString(tsf.MakeString(abbr, abbrWs));
						tisb.AppendTsString(tsf.MakeString(" - ", userWs));
					}
					Debug.Assert(!string.IsNullOrEmpty(name));
					tisb.AppendTsString(tsf.MakeString(name, nameWs));
				}

				return tisb.GetString();
			}
		}

		#endregion ITssValue Implementation

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override IEnumerable<ObjectLabel> SubItems
		{
			get
			{
				return CreateObjectLabels(m_cache, Possibility.SubPossibilitiesOS, m_displayNameProperty, m_displayWs);
			}
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		/// <returns></returns>
		public override bool HaveSubItems
		{
			get
			{
				return Possibility.SubPossibilitiesOS.Count > 0;
			}
		}
	}
	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class MoInflClassLabel : ObjectLabel
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="inflClass">The inflection class.</param>
		/// <param name="displayNameProperty">property name to display</param>
		/// <param name="displayWs">writing system to display</param>
		public MoInflClassLabel(FdoCache cache, IMoInflClass inflClass, string displayNameProperty,
			string displayWs)
			: base(cache, inflClass, displayNameProperty, displayWs)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO Cache object.</param>
		/// <param name="inflClass">The inflection class.</param>
		/// <param name="displayNameProperty">property name to display</param>
		public MoInflClassLabel(FdoCache cache, IMoInflClass inflClass, string displayNameProperty)
			: base(cache, inflClass, displayNameProperty)
		{
		}

		/// <summary>
		/// Gets the inflection class.
		/// </summary>
		/// <value>The inflection class.</value>
		public IMoInflClass InflectionClass
		{
			get
			{
				return m_obj as IMoInflClass;
			}
		}

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override IEnumerable<ObjectLabel> SubItems
		{
			get
			{
				return CreateObjectLabels(m_cache, InflectionClass.SubclassesOC, m_displayNameProperty, m_displayWs);
			}
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		/// <returns></returns>
		public override bool HaveSubItems
		{
			get
			{
				return InflectionClass.SubclassesOC.Count > 0;
			}
		}
	}
}
