// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.XMLViews
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
		/// controls which property of the object will be used for the name to display.
		/// </summary>
		protected string m_displayNameProperty;

		/// <summary />
		protected string m_displayWs;

		/// <summary />
		protected string m_bestWs;

		/// <summary>
		/// Factory method for creating an ObjectLabel,
		/// even if the class is some kind of CmPossibility,
		/// as long as its hvo is not 0.
		/// </summary>
		public static ObjectLabel CreateObjectLabelOnly(LcmCache cache, ICmObject obj, string displayNameProperty, string displayWs)
		{
			return obj == null ? new NullObjectLabel(cache) : new ObjectLabel(cache, obj, displayNameProperty, displayWs);
		}

		/// <summary>
		/// a  factory method for creating the correct type of object label, depending on the
		/// class of the object
		/// </summary>
		public static ObjectLabel CreateObjectLabel(LcmCache cache, ICmObject obj, string displayNameProperty, string displayWs)
		{
			if (obj == null)
			{
				return new NullObjectLabel(cache);
			}

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
		public static ObjectLabel CreateObjectLabel(LcmCache cache, ICmObject obj, string displayNameProperty)
		{
			return CreateObjectLabel(cache, obj, displayNameProperty, null);
		}

		/// <summary>
		/// Get a list of hvos, create a collection of labels for them.
		/// </summary>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(LcmCache cache, IEnumerable<ICmObject> objs, string displayNameProperty, string displayWs, bool fIncludeNone)
		{
			foreach(var obj in objs)
			{
				yield return CreateObjectLabel(cache, obj, displayNameProperty, displayWs);
			}
			// You get a pretty green dialog box if this is inserted first!?
			if (fIncludeNone)
			{
				yield return new NullObjectLabel(cache);
			}
		}

		/// <summary>
		/// Get a list of objects, create a collection of labels for them.
		/// </summary>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(LcmCache cache, IEnumerable<ICmObject> objs, string displayNameProperty, string displayWs)
		{
			return CreateObjectLabels(cache, objs, displayNameProperty, displayWs, false);
		}

		/// <summary>
		/// Get a list of objects, create a collection of labels for them using the best available
		/// writing system property.
		/// </summary>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(LcmCache cache, IEnumerable<ICmObject> objs, string displayNameProperty)
		{
			return CreateObjectLabels(cache, objs, displayNameProperty, "best analorvern");
		}

		/// <summary>
		/// Given a list of objects, create a collection of labels for them using the default
		/// display name and writing system properties.
		/// </summary>
		public static IEnumerable<ObjectLabel> CreateObjectLabels(LcmCache cache, IEnumerable<ICmObject> objs)
		{
			return CreateObjectLabels(cache, objs, null, null);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		protected ObjectLabel(LcmCache cache, ICmObject obj, string displayNameProperty)
			: this(cache, obj, displayNameProperty, "analysis")
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		protected ObjectLabel(LcmCache cache, ICmObject obj, string displayNameProperty, string sDisplayWs)
		{
			Cache = cache;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = string.IsNullOrEmpty(sDisplayWs) ? "best analorvern" : sDisplayWs;
			Object = obj; // This must be done before the EstablishWritingSystemsToTry call, which relies on the hvo having been set
			EstablishWritingSystemsToTry(m_displayWs);
			if (m_displayWs.StartsWith("best"))
			{
				m_bestWs = m_displayWs;
			}
		}

		/// <summary>
		/// the object
		/// </summary>
		public ICmObject Object { get; set; }

		/// <summary>
		/// Gets the cache.
		/// </summary>
		public LcmCache Cache { get; }

		/// <summary>
		/// What would be shown, say, in a combobox
		/// </summary>
		public virtual string DisplayName
		{
			set
			{
				//just for subclasses
				throw new NotSupportedException();
			}

			get
			{
				return AsTss.Text;
			}
		}

		/// <summary>
		/// Override the method to return the right string.
		/// </summary>
		public override string ToString()
		{
			return DisplayName;
		}

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		public virtual bool HaveSubItems => false;

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
			if (m_writingSystemIds != null || Cache == null || Object == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(sDisplayWs))
			{
				sDisplayWs = "analysis vernacular";		// very general default.
			}
			const int flid = 0;
			m_writingSystemIds = WritingSystemServices.GetWritingSystemIdsFromLabel(Cache,
				sDisplayWs,
				Cache.ServiceLocator.WritingSystemManager.UserWritingSystem,
				Object.Hvo,
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
					if (Object is IMoMorphSynAnalysis)
					{
						var msa = Object as IMoMorphSynAnalysis;
						switch (m_displayNameProperty)
						{
							case "InterlinearName": // Fall through.
							case "InterlinearNameTSS":
								return msa.InterlinearNameTSS;
							case "LongName":
							{
								return TsStringUtils.MakeString(msa.LongName, Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
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
					else if (m_displayNameProperty == "LongNameTSS" && Object is ILexSense)
					{
						return ((ILexSense)Object).LongNameTSS;
					}
					else if (m_displayNameProperty == "LongName" && Object is IFsFeatStruc)
					{
						return ((IFsFeatStruc)Object).LongNameTSS;
					}
					else if (m_displayNameProperty == "ObjectIdName")
					{
						return Object.ObjectIdName;
					}
					else
					{
						var prop = Object.GetType().GetProperty(m_displayNameProperty);
						if (prop != null)
						{
							var val = prop.GetValue(Object, null);
							if (val is ITsString)
							{
								return val as ITsString;
							}
						}
					}
				}
				return Object.ShortNameTSS;
			}
		}

		#endregion ITssValue Implementation
	}
}
