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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.Utils.ComTypes;
using XCore;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This provides a base class for implementing IVwViewConstructor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class VwBaseVc : IVwViewConstructor
	{
		/// <summary>
		/// The GenDate long format frag
		/// </summary>
		public const int kfragGenDateShort = 783459845;
		/// <summary>
		/// The GenDate short format frag
		/// </summary>
		public const int kfragGenDateLong = 783459846;
		/// <summary>
		/// The GenDate sort format frag
		/// </summary>
		public const int kfragGenDateSort = 783459847;

		private string m_sName = "Unknown VC";
		/// <summary>Default writing system used in this view.</summary>
		protected int m_wsDefault = 0;

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ITsTextProps to apply to the caption of pictures
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ITsTextProps CaptionProps
		{
			get
			{
				throw new NotImplementedException("Derived classes that support pictures should implement this.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets a name for this VC for debugging purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return m_sName; }
			set { m_sName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default writing system id for the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int DefaultWs
		{
			get { return m_wsDefault; }
			set { m_wsDefault = value; }
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
		/// Display (usually basic) properties as a string in custom ways. Often not used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is used for displaying vectors in complex ways. Often not used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
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
		public virtual IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
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
			throw new NotImplementedException();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID. No reasonable base implementation exists.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString GetStrForGuid(string bstrGuid)
		{
			throw new NotImplementedException();
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
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Not implemented.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag,
			ITsString tssVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Mediator is typically supplied during first real layout by root site.
		/// </summary>
		internal Mediator Mediator { get; set; }

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Perform whatever action is appropriate when the user clicks on a hot link
		/// See the class comment on FwLinkArgs for details on how all the parts of (internal)
		/// hyperlinking work.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void DoHotLinkAction(string strData, ISilDataAccess sda)
		{
			if (strData.Length > 0 && (int)strData[0] == (int)FwObjDataTypes.kodtExternalPathName)
			{
				string url = strData.Substring(1).Trim(); // may also be just a file name, launches default app.

				// If the file path is saved as a non rooted path, then it is relative to the project's
				// LinkedFilesRootDir.  In this case get the full path.
				// We also need to deal with url's like the following ( http:// silfw:// http:\ silfw:\ which are considered Not rooted).
				// thus the check for FileUtils.IsFilePathValid(url)
				// Added check IsFileUriOrPath since paths starting with silfw: are valid paths on Linux.
				if (FileUtils.IsFileUriOrPath(url) && FileUtils.IsFilePathValid(url) && !Path.IsPathRooted(url))
				{
					string linkedFilesFolder = GetLinkedFilesRootDir(sda);
					url = Path.Combine(linkedFilesFolder, url);
				}

				// See if we can handle it (via our own LinkListener) without starting a process.
				var args = new LocalLinkArgs() {Link = url};
				if (Mediator != null)
				{
					Mediator.SendMessage("HandleLocalHotlink", args);
					if (args.LinkHandledLocally)
						return;
				}

				// Review JohnT: do we need to do some validation here?
				// C++ version uses URLIs, and takes another approach to launching files if not.
				// But no such function in .NET that I can find.

				try
				{
					Process.Start(url);
				}
				catch (Exception e)
				{
					MessageBox.Show(null,
						String.Format(Properties.Resources.ksMissingLinkTarget, url, e.Message),
						Properties.Resources.ksCannotFollowLink);
				}
			}
		}

		/// <summary>
		/// Get the Linked Files Root Directory value from the language project represented by the
		/// ISilDataAccess argument.
		/// </summary>
		protected string GetLinkedFilesRootDir(ISilDataAccess sda)
		{
			var linkedFilesRootDirID = sda.MetaDataCache.GetFieldId("LangProject", "LinkedFilesRootDir", false);
			int hvo = GetHvoOfProject(sda);
			return sda.get_UnicodeProp(hvo, linkedFilesRootDirID);
		}

		/// <summary>
		/// Get the Hvo of the LangProject by tracing up the ownership chain from a known
		/// CmPossibility with a fixed guid that exists in all FieldWorks projects.
		/// </summary>
		private int GetHvoOfProject(ISilDataAccess sda)
		{
			// fixed guid of a possibility eventually owned by LangProject
			Guid guid = new Guid("d7f713e4-e8cf-11d3-9764-00c04f186933");	// MoMorphType: bound root
			int hvoPoss = GetIdFromGuid(sda, ref guid);
			int flidOwner = sda.MetaDataCache.GetFieldId("CmObject", "Owner", false);
			int hvoNewOwner = sda.get_ObjectProp(hvoPoss, flidOwner);
			int hvoOwner = 0;
			while (hvoNewOwner != 0)
			{
				hvoOwner = hvoNewOwner;
				hvoNewOwner = sda.get_ObjectProp(hvoOwner, flidOwner);
			}
			return hvoOwner;
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
			return sda.get_ObjFromGuid(guid);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display the specified object (from an ORC embedded in a string). The default
		/// implementation doesn't know how to do this.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			return;
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
