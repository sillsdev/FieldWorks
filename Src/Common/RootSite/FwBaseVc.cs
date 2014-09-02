// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwBaseVc.cs
// Responsibility: FW Team
//
// <remarks>
// A base view constructor for displaying FieldWorks data
// </remarks>

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <remarks>
	/// FwBaseVc is the base view constructor with low-level FW-specific methods that other
	/// view constructors can use as a base.
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	public abstract class FwBaseVc : VwBaseVc
	{
		/// <summary>The view construtor's cache.</summary>
		protected FdoCache m_cache = null;
		/// <summary>The hvo of the language project.</summary>
		protected int m_hvoLangProject;
		/// <summary>TS String factory</summary>
		protected ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		private static StringBuilder s_footnoteIconString;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwBaseVc"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FwBaseVc()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwBaseVc"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FwBaseVc(int wsDefault) : base(wsDefault)
		{

		}

		#region Public and protected properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the hvo of the language project.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected int LangProjectHvo
		{
			get { return m_hvoLangProject; }
			set { m_hvoLangProject = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FdoCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{

				m_cache = value;
				if (m_wsDefault <= 0)
					m_wsDefault = m_cache.DefaultVernWs;
				LangProjectHvo = m_cache.LangProject.Hvo;
			}
		}
		#endregion

		#region Overrides of VwBaseVc
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This version knows how to display a generic date as a string.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			if (vwenv.DataAccess.MetaDataCache.GetFieldType(tag) == (int)CellarPropertyType.GenDate)
			{
				// handle generic date. Because the actual GenDate struct is not available to
				// Views code, we have to handle the display of the GenDate here
				var sda = vwenv.DataAccess as ISilDataAccessManaged;
				Debug.Assert(sda != null);
				var genDate = sda.get_GenDateProp(vwenv.CurrentObject(), tag);
				var tsf = TsStrFactoryClass.Create();
				string str = "";
				switch (frag)
				{
					case kfragGenDateLong:
						str = genDate.ToLongString();
						break;
					case kfragGenDateShort:
						str = genDate.ToShortString();
						break;
					case kfragGenDateSort:
						str = genDate.ToSortString();
						break;
				}
				return tsf.MakeString(str, sda.WritingSystemFactory.UserWs);
			}
			else
				return base.DisplayVariant(vwenv, tag, frag);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This version knows to skip doing anything for generic dates, which are read-only.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag,
			ITsString tssVal)
		{
			var mdc = vwsel.RootBox.DataAccess.MetaDataCache;
			if (mdc is IFwMetaDataCacheManaged && ((IFwMetaDataCacheManaged)mdc).FieldExists(tag) &&
				vwsel.RootBox.DataAccess.MetaDataCache.GetFieldType(tag) == (int)CellarPropertyType.GenDate)
			{
				return tssVal;
			}
			return base.UpdateProp(vwsel, hvo, tag, frag, tssVal);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display the specified object (from an ORC embedded in a string). The default
		/// here knows how to display IPictures.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// <param name="hvo">The ID of the embedded object</param>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Added TODO-Linux comment")]
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			// See if it is a CmPicture.
			ISilDataAccess sda = vwenv.DataAccess;
			int clsid = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			if (clsid != CmPictureTags.kClassId)
			{
				// don't know how to deal with it. Maybe the base implementation does.
				base.DisplayEmbeddedObject(vwenv, hvo);
				return;
			}
			int hvoFile = sda.get_ObjectProp(hvo, CmPictureTags.kflidPictureFile);
			if (hvoFile == 0)
				return;
			string path;
			string fileName = sda.get_UnicodeProp(hvoFile, CmFileTags.kflidInternalPath);
			if (Path.IsPathRooted(fileName))
			{
				path = fileName;
			}
			else
			{
				if (m_hvoLangProject == 0)
				{
					// REVIEW (TimS/TomB): Hvo is for a CmPicture which means it might not be owned.
					// If it's not owned, there will be no way to walk up the owner tree to find the
					// Language Project. Review all clients to see if they have embedded objects.
					// If so, they need to set the cache.
					TryToSetLangProjectHvo(sda, hvo);
				}
				string linkedFilesRoot = sda.get_UnicodeProp(m_hvoLangProject, LangProjectTags.kflidLinkedFilesRootDir);
				if (String.IsNullOrEmpty(linkedFilesRoot))
					path = Path.Combine(FwDirectoryFinder.DataDirectory, fileName);
				else
					path = Path.Combine(linkedFilesRoot, fileName);
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);

			IPicture picture = new PictureWrapper(path);
			// -1 is ktagNotAnAttr. 0 width & height mean use natural width/height.
			vwenv.AddPictureWithCaption(picture, -1, CaptionProps, hvoFile, m_wsDefault, 0, 0, this);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID.
		/// </summary>
		/// <param name="bstrGuid"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			if (Cache == null)
				throw new InvalidOperationException("Cannot find object unless the Cache is set.");
			Debug.Assert(bstrGuid.Length == 8);

			Guid guid = MiscUtils.GetGuidFromObjData(bstrGuid);

			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
			if (obj is IScrFootnote)
				return GetFootnoteIconString(DefaultWs, guid);
			if (obj is ICmPicture)
				return GetPictureString();

			throw new NotImplementedException("Cannot get a string for objects other than footnotes and pictures.");
		}

		private ITsString GetPictureString()
		{
			var bldr = Cache.TsStrFactory.MakeString(RootSiteStrings.ksPicture, Cache.DefaultUserWs).GetBldr();
			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			return bldr.GetString();
		}

		#endregion

		#region Private/Protected helper methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an ORC string whose properties contain an iconic representation of a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected static ITsString GetFootnoteIconString(int ws, Guid footnoteGuid)
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);

			StringBuilder iconData = FootnoteIconString;
			int i = 1;
			foreach(char ch in new CharEnumeratorForByteArray(footnoteGuid.ToByteArray()))
				iconData[i++] = ch;

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptObjData, iconData.ToString());
			bldr.Replace(0, 0, StringUtils.kszObject, propsBldr.GetTextProps());

			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

			return bldr.GetString();
		}

		/// <summary>
		/// Make a TsString in the specified (typicaly UI) writing system, which is forced to be
		/// displayed in the default UIElement style.
		/// </summary>
		protected ITsString MakeUiElementString(string text, int uiWs, Action<ITsPropsBldr> SetAdditionalProps)
		{
			ITsStrBldr bldr = m_tsf.GetBldr();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, uiWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, StyleServices.UiElementStylename);
			if (SetAdditionalProps != null)
				SetAdditionalProps(propsBldr);
			bldr.Replace(0, 0, text, propsBldr.GetTextProps());
			return bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote icon string. The returned string contains the picture type (first
		/// character) and space for the guid of the clickable object (next 8 characters).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static StringBuilder FootnoteIconString
		{
			get
			{
				if (s_footnoteIconString == null)
				{
					using (BinaryReader reader = new BinaryReader(new MemoryStream(Properties.Resources.footnote)))
					{
						// Read the icon from the resources
						int countOfBytes = (int)reader.BaseStream.Length;
						byte[] picData = new byte[countOfBytes];
						int bytesRead = reader.Read(picData, 0, countOfBytes);
						Debug.Assert(bytesRead == countOfBytes);

						// Create a string out of the data
						StringBuilder strBldr = new StringBuilder(countOfBytes / 2 + 10);
						strBldr.Append(new char[8]); // Will be replaced with the GUID later

						foreach(char ch in new CharEnumeratorForByteArray(picData))
							strBldr.Append(ch);

						FwObjDataTypes odtFootnoteIconType = FwObjDataTypes.kodtPictEvenHot;
						if ((countOfBytes & 1) != 0)
						{
							// We had an odd number of bytes, so add the last byte to the builder
							odtFootnoteIconType = FwObjDataTypes.kodtPictOddHot;
						}
						strBldr.Insert(0, (char)odtFootnoteIconType);
						s_footnoteIconString = strBldr;
					}
				}
				return s_footnoteIconString;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to look up the ownership hierarchy to find the HVO of the language project
		/// to set the m_hvoLangProject member variable.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void TryToSetLangProjectHvo(ISilDataAccess sda, int hvo)
		{
			int hvoOwner = sda.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
			while (hvoOwner != 0)
			{
				int clsid = sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Class);
				if (clsid == LangProjectTags.kClassId)
				{
					m_hvoLangProject = hvoOwner;
					return;
				}
				hvoOwner = sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Owner);
			}
			// Not particularly true in the new architecture, since the WSes are loaded first, so get HVO 1.
			// m_hvoLangProject = 1;	// true 99.999% of the time as of 11/24/2008
			throw new ArgumentException("Probably an ownerless object", "hvo");
		}
		#endregion
	}
}
