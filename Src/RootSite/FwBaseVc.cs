// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
#if RANDYTODO
	// TODO: Consider moving FwBaseVc to FwCoreDlgs.
#endif
	/// <remarks>
	/// FwBaseVc is the base view constructor with low-level FW-specific methods that other
	/// view constructors can use as a base.
	/// </remarks>
	public abstract class FwBaseVc : VwBaseVc
	{
		/// <summary>The view constructor's cache.</summary>
		protected LcmCache m_cache;
		/// <summary>The hvo of the language project.</summary>
		protected int m_hvoLangProject;
		private static StringBuilder s_footnoteIconString;

		/// <summary />
		protected FwBaseVc()
		{
		}

		/// <summary />
		protected FwBaseVc(int wsDefault) : base(wsDefault)
		{

		}

		#region Public and protected properties

		/// <summary>
		/// Gets or sets the hvo of the language project.
		/// </summary>
		protected int LangProjectHvo
		{
			get { return m_hvoLangProject; }
			set { m_hvoLangProject = value; }
		}

		/// <summary>
		/// Gets or sets the LCM cache for the view constructor.
		/// </summary>
		public virtual LcmCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{

				m_cache = value;
				if (m_wsDefault <= 0)
				{
					m_wsDefault = m_cache.DefaultVernWs;
				}
				LangProjectHvo = m_cache.LangProject.Hvo;
			}
		}
		#endregion

		#region Overrides of VwBaseVc

		/// <summary>
		/// This version knows how to display a generic date as a string.
		/// </summary>
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			if (vwenv.DataAccess.MetaDataCache.GetFieldType(tag) == (int)CellarPropertyType.GenDate)
			{
				// handle generic date. Because the actual GenDate struct is not available to
				// Views code, we have to handle the display of the GenDate here
				var sda = vwenv.DataAccess as ISilDataAccessManaged;
				Debug.Assert(sda != null);
				var genDate = sda.get_GenDateProp(vwenv.CurrentObject(), tag);
				var str = string.Empty;
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
				return TsStringUtils.MakeString(str, sda.WritingSystemFactory.UserWs);
			}
			return base.DisplayVariant(vwenv, tag, frag);
		}

		/// <summary>
		/// This version knows to skip doing anything for generic dates, which are read-only.
		/// </summary>
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			var mdc = vwsel.RootBox.DataAccess.MetaDataCache;
			if (mdc is IFwMetaDataCacheManaged && ((IFwMetaDataCacheManaged)mdc).FieldExists(tag) &&
				vwsel.RootBox.DataAccess.MetaDataCache.GetFieldType(tag) == (int)CellarPropertyType.GenDate)
			{
				return tssVal;
			}
			return base.UpdateProp(vwsel, hvo, tag, frag, tssVal);
		}

		/// <summary>
		/// Display the specified object (from an ORC embedded in a string). The default
		/// here knows how to display IPictures.
		/// </summary>
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			// See if it is a CmPicture.
			var sda = vwenv.DataAccess;
			var clsid = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			if (clsid != CmPictureTags.kClassId)
			{
				// don't know how to deal with it. Maybe the base implementation does.
				base.DisplayEmbeddedObject(vwenv, hvo);
				return;
			}
			var hvoFile = sda.get_ObjectProp(hvo, CmPictureTags.kflidPictureFile);
			if (hvoFile == 0)
			{
				return;
			}
			string path;
			var fileName = sda.get_UnicodeProp(hvoFile, CmFileTags.kflidInternalPath);
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
				var linkedFilesRoot = sda.get_UnicodeProp(m_hvoLangProject, LangProjectTags.kflidLinkedFilesRootDir);
				path = Path.Combine(string.IsNullOrEmpty(linkedFilesRoot) ? FwDirectoryFinder.DataDirectory : linkedFilesRoot, fileName);
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);

			IPicture picture = new PictureWrapper(path);
			// -1 is ktagNotAnAttr. 0 width & height mean use natural width/height.
			vwenv.AddPictureWithCaption(picture, -1, CaptionProps, hvoFile, m_wsDefault, 0, 0, this);
		}

		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID.
		/// </summary>
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			if (Cache == null)
			{
				throw new InvalidOperationException("Cannot find object unless the Cache is set.");
			}

			Debug.Assert(bstrGuid.Length == 8);

			var guid = MiscUtils.GetGuidFromObjData(bstrGuid);

			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
			if (obj is IScrFootnote)
			{
				return GetFootnoteIconString(DefaultWs, guid);
			}
			if (obj is ICmPicture)
			{
				return GetPictureString();
			}

			throw new NotSupportedException("Cannot get a string for objects other than footnotes and pictures.");
		}

		private ITsString GetPictureString()
		{
			var bldr = TsStringUtils.MakeString(RootSiteStrings.ksPicture, Cache.DefaultUserWs).GetBldr();
			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			return bldr.GetString();
		}

		#endregion

		#region Private/Protected helper methods and properties

		/// <summary>
		/// Gets an ORC string whose properties contain an iconic representation of a footnote.
		/// </summary>
		protected static ITsString GetFootnoteIconString(int ws, Guid footnoteGuid)
		{
			var bldr = TsStringUtils.MakeStrBldr();
			var propsBldr = TsStringUtils.MakePropsBldr();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);

			var iconData = FootnoteIconString;
			var i = 1;
			foreach (var ch in new CharEnumeratorForByteArray(footnoteGuid.ToByteArray()))
			{
				iconData[i++] = ch;
			}
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptObjData, iconData.ToString());
			bldr.Replace(0, 0, StringUtils.kszObject, propsBldr.GetTextProps());

			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

			return bldr.GetString();
		}

		/// <summary>
		/// Make a TsString in the specified (typically UI) writing system, which is forced to be
		/// displayed in the default UIElement style.
		/// </summary>
		protected ITsString MakeUiElementString(string text, int uiWs, Action<ITsPropsBldr> SetAdditionalProps)
		{
			var bldr = TsStringUtils.MakeStrBldr();
			var propsBldr = TsStringUtils.MakePropsBldr();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, uiWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, StyleServices.UiElementStylename);
			SetAdditionalProps?.Invoke(propsBldr);
			bldr.Replace(0, 0, text, propsBldr.GetTextProps());
			return bldr.GetString();
		}

		/// <summary>
		/// Gets the footnote icon string. The returned string contains the picture type (first
		/// character) and space for the guid of the clickable object (next 8 characters).
		/// </summary>
		private static StringBuilder FootnoteIconString
		{
			get
			{
				if (s_footnoteIconString == null)
				{
					using (var reader = new BinaryReader(new MemoryStream(Properties.Resources.footnote)))
					{
						// Read the icon from the resources
						var countOfBytes = (int)reader.BaseStream.Length;
						var picData = new byte[countOfBytes];
						var bytesRead = reader.Read(picData, 0, countOfBytes);
						Debug.Assert(bytesRead == countOfBytes);

						// Create a string out of the data
						var strBldr = new StringBuilder(countOfBytes / 2 + 10);
						strBldr.Append(new char[8]); // Will be replaced with the GUID later

						foreach (var ch in new CharEnumeratorForByteArray(picData))
						{
							strBldr.Append(ch);
						}
						var odtFootnoteIconType = FwObjDataTypes.kodtPictEvenHot;
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

		/// <summary>
		/// Attempts to look up the ownership hierarchy to find the HVO of the language project
		/// to set the m_hvoLangProject member variable.
		/// </summary>
		private void TryToSetLangProjectHvo(ISilDataAccess sda, int hvo)
		{
			var hvoOwner = sda.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
			while (hvoOwner != 0)
			{
				var clsid = sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Class);
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

		/// <remarks>
		/// A wrapper for the COM implementation of IPicture to hold a weak reference to image data
		/// </remarks>
		private sealed class PictureWrapper : IPicture
		{
			#region Data members
			private readonly string m_sPath;
			private int m_width;
			private int m_height;
			private WeakReference m_internalPicture;
			#endregion

			#region Constructor
			internal PictureWrapper(string path)
			{
				m_sPath = path;
			}
			#endregion

			#region Private helpers
			private IPicture Picture => m_internalPicture?.Target as IPicture ?? LoadPicture();

			private IPicture LoadPicture()
			{
				IPicture picture;
				Image image = null;
				try
				{
					try
					{
						image = Image.FromFile(FileUtils.ActualFilePath(m_sPath));
					}
					catch
					{
						// unable to read image. set to default image that indicates an invalid image.
						image = SimpleRootSite.ImageNotFoundX;
					}
					try
					{
						picture = (IPicture)OLEConvert.ToOLE_IPictureDisp(image);
					}
					catch
					{
						// conversion to OLE format from current image format is not supported (e.g. WMF file)
						// try to convert it to a bitmap and convert it to OLE format again.
						// TODO: deal with transparency
						// We could just do the following line (creating a new bitmap) instead of going
						// through a memory stream, but then we end up with an image that is too big.
						//image = new Bitmap(image, image.Size);
						using (var imageStream = new MemoryStream())
						{
							image.Save(imageStream, ImageFormat.Png);
							image.Dispose();
							// TODO-Linux: useEmbeddedColorManagement parameter is not supported
							// on Mono
							image = Image.FromStream(imageStream, true);
						}
						picture = (IPicture)OLEConvert.ToOLE_IPictureDisp(image);
					}
					m_width = picture.Width;
					m_height = picture.Height;
					m_internalPicture = new WeakReference(picture);
				}
				finally
				{
					image?.Dispose();
				}
				return picture;
			}
			#endregion

			#region Implementation of IPicture
			public void Render(IntPtr hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, IntPtr prcWBounds)
			{
				// See http://blogs.microsoft.co.il/blogs/sasha/archive/2008/07/28/finalizer-vs-application-a-race-condition-from-hell.aspx
				var p = Picture;
				p.Render(hdc, x, y, cx, cy, xSrc, ySrc, cxSrc, cySrc, prcWBounds);
				GC.KeepAlive(p);
			}

			public void put_hPal(int val)
			{
				Picture.put_hPal(val);
			}

			public void SelectPicture(int hdcIn, out int phdcOut, out int phbmpOut)
			{
				Picture.SelectPicture(hdcIn, out phdcOut, out phbmpOut);
			}

			public void PictureChanged()
			{
				Picture.PictureChanged();
			}

			public void SaveAsFile(IntPtr pstm, bool fSaveMemCopy, out int pcbSize)
			{
				Picture.SaveAsFile(pstm, fSaveMemCopy, out pcbSize);
			}

			public void SetHdc(int hdc)
			{
				Picture.SetHdc(hdc);
			}

			public int Handle
			{
				get { throw new NotSupportedException("Not sure whether we could safely implement this simply since the underlying picture could be disposed before the caller finished using the handle."); /* return Picture.Handle; */ }
			}

			public int hPal => Picture.hPal;

			public short Type => Picture.Type;

			public int Width
			{
				get
				{
					// If this is null, the picture has never been loaded, so the width has not been set
					if (m_internalPicture == null)
					{
						LoadPicture();
					}
					return m_width;
				}
			}
			public int Height
			{
				get
				{
					// If this is null, the picture has never been loaded, so the height has not been set
					if (m_internalPicture == null)
					{
						LoadPicture();
					}
					return m_height;
				}
			}

			public int CurDC
			{
				get { throw new NotSupportedException("Not sure whether we could safely implement this simply since the underlying picture could be disposed before the caller finished using the return value."); /* return Picture.CurDC; */ }
			}

			public bool KeepOriginalFormat
			{
				get { return Picture.KeepOriginalFormat; }
				set { Picture.KeepOriginalFormat = value; }
			}

			public int Attributes => Picture.Attributes;
			#endregion
		}
	}
}