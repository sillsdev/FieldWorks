// --------------------------------------------------------------------------------------------
#region Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VwBaseVc.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// This provides a base class for implementing IVwViewConstructor.
// It raises a NotImplementedException for all methods (which returns E_NOTIMPL to the COM
// object).
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.Common.RootSites
{
	#region Default style name
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains the default style name
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct StyleNames
	{
		/// <summary>The default style name ("Normal")</summary>
		public static string ksNormal = "Normal";
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This provides a base class for implementing IVwViewConstructor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class VwBaseVc : IVwViewConstructor, IFWDisposable
	{
		private string m_sName = "Unknown VC";
		/// <summary>Default writing system used in this view.</summary>
		protected int m_wsDefault = 0;
		/// <summary>The hvo of the language project.</summary>
		protected int m_hvoLangProject = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new VwBaseVc
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public VwBaseVc()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:VwBaseVc"/> class.
		/// </summary>
		/// <param name="wsDefault">The default ws.</param>
		/// ------------------------------------------------------------------------------------
		public VwBaseVc(int wsDefault)
		{
			m_wsDefault = wsDefault;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~VwBaseVc()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("VwBaseVc", "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ITsTextProps to apply to the caption of pictures
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ITsTextProps CaptionProps
		{
			get
			{
				CheckDisposed();
				throw new NotImplementedException("Derived classes that support CmPictures should implement this.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets a name for this VC for debugging purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { CheckDisposed(); return m_sName; }
			set { CheckDisposed(); m_sName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default writing system id for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int DefaultWs
		{
			get { CheckDisposed(); return m_wsDefault; }
			set { CheckDisposed(); m_wsDefault = value; }
		}

		/// <summary>
		/// Gets or sets the hvo of the language project.
		/// </summary>
		protected int LangProjectHvo
		{
			get { CheckDisposed(); return m_hvoLangProject; }
			set { CheckDisposed(); m_hvoLangProject = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them. Most
		/// subclasses should override.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// -----------------------------------------------------------------------------------
		public abstract void Display(IVwEnv vwenv, int hvo, int frag);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This pair of methods work together to allow (usually basic) properties to be displayed
		/// as strings in custom ways. Often not used.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="tag"></param>
		/// <param name="v"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is used for displaying vectors in complex ways. Often not used.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		/// <summary>
		/// This returns a picture representing the value of property tag of hvo (which is val).
		/// Needed if a call to AddIntPropPic is made.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		public virtual stdole.IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// codedoes NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available
		/// width.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public virtual int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID.
		/// </summary>
		/// <param name="bstrGuid"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString GetStrForGuid(string bstrGuid)
		{
			CheckDisposed();

			// This is a default, initially useful when a spell-check browse view is displaying
			// TE data. It might be a decent default for other views, but think about it.
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Use the restore window icon character from Marlett as the
			// footnote marker.
			ITsPropsBldr propsBldr = bldr.get_Properties(0).GetBldr();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
				"Marlett");
			bldr.Replace(0, 0, "\u0032", propsBldr.GetTextProps());

			bldr.SetIntPropValues(0, bldr.Length,
				(int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);

			return bldr.GetString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// displayusing AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object. If you are not using AddLazyItems this method may be left
		/// unimplemented.
		/// If you pre-load all the data, it should trivially succeed (i.e., without doing
		/// anything). This is the default behavior.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="hvoParent"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="ihvoMin"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
			CheckDisposed();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Not implemented.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="tssVal"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag,
			ITsString tssVal)
		{
			CheckDisposed();

			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Perform whatever action is appropriate when the user clicks on a hot link
		/// </summary>
		/// <param name="strData"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="tss"></param>
		/// <param name="ichObj"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void DoHotLinkAction(string strData, int hvoOwner, int tag,
			ITsString tss, int ichObj)
		{
			CheckDisposed();
			if (strData.Length > 0 && (int)strData[0] == (int)FwObjDataTypes.kodtExternalPathName)
			{
				string url = strData.Substring(1); // may also be just a file name, launches default app.

				// Review JohnT: do we need to do some validation here?
				// C++ version uses URLIs, and takes another approach to launching files if not.
				// But no such function in .NET that I can find.

				try
				{
					System.Diagnostics.Process.Start(url);
				}
				catch (Exception e)
				{
					MessageBox.Show(null,
						String.Format(SimpleRootSiteStrings.ksMissingLinkTarget, url, e.Message),
						SimpleRootSiteStrings.ksCannotFollowLink);
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the object ID (HVO) that corresponds to the specified GUID.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="sda"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public int GetIdFromGuid(ISilDataAccess sda, ref System.Guid guid)
		{
			CheckDisposed();
			return sda.get_ObjFromGuid(guid);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display the specified object (from an ORC embedded in a string). The default
		/// here knows how to display IPictures.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			CheckDisposed();
			// See if it is a CmPicture.
			ISilDataAccess sda = vwenv.DataAccess;
			int clsid = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			if (clsid != CmPicture.kclsidCmPicture)
				return; // don't know how to deal with it.
			int hvoFile = sda.get_ObjectProp(hvo, (int)CmPicture.CmPictureTags.kflidPictureFile);
			if (hvoFile == 0)
				return;
			string path;
			string fileName = sda.get_UnicodeProp(hvoFile, (int)CmFile.CmFileTags.kflidInternalPath);
			if (Path.IsPathRooted(fileName))
			{
				path = fileName;
			}
			else
			{
				if (m_hvoLangProject == 0)
					TryToSetLangProjectHvo(sda, hvo);
				string externalRoot = sda.get_UnicodeProp(m_hvoLangProject, (int)LangProject.LangProjectTags.kflidExtLinkRootDir);
				if (String.IsNullOrEmpty(externalRoot))
					path = Path.Combine(DirectoryFinder.FWDataDirectory, fileName);
				else
					path = Path.Combine(externalRoot, fileName);
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
			Image image;
			try
			{
				image = Image.FromFile(FileUtils.ActualFilePath(path));
			}
			catch
			{
				// unable to read image. set to default image that indicates an invalid image.
				image = ResourceHelper.ImageNotFoundX;
			}
			stdole.IPicture picture;
			try
			{
				picture = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(image);
			}
			catch
			{
				// conversion to OLE format from current image format is not supported (e.g. WMF file)
				// try to convert it to a bitmap and convert it to OLE format again.
				// TODO: deal with transparency
				// We could just do the following line (creating a new bitmap) instead of going
				// through a memory stream, but then we end up with an image that is too big.
				//image = new Bitmap(image, image.Size);
				using (MemoryStream imageStream = new MemoryStream())
				{
					image.Save(imageStream, ImageFormat.Png);
					image = Image.FromStream(imageStream, true);
				}
				picture = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(image);
			}
			// -1 is ktagNotAnAttr. 0 width & height mean use natural width/height.
			vwenv.AddPictureWithCaption(picture, -1, CaptionProps, hvoFile, m_wsDefault, 0, 0, this);
			image.Dispose();
		}

		private void TryToSetLangProjectHvo(ISilDataAccess sda, int hvo)
		{
			int hvoOwner = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
			while (hvoOwner != 0)
			{
				int clsid = sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Class);
				if (clsid == (int)LangProject.kclsidLangProject)
				{
					m_hvoLangProject = hvoOwner;
					return;
				}
				hvoOwner = sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Owner);
			}
			m_hvoLangProject = 1;	// true 99.999% of the time as of 11/24/2008
		}

		/// <summary>
		/// Default implementation. Makes no changes to the root box text properties.
		/// </summary>
		/// <param name="ttp">The text props.</param>
		/// <returns>The updated text props.</returns>
		public virtual ITsTextProps UpdateRootBoxTextProps(ITsTextProps ttp)
		{
			return null;
		}
	}
}
