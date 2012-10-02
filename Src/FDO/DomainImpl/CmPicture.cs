// --------------------------------------------------------------------------------------------
// Copyright (C) 2005 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: CmPicture.cs
// Responsibility: TE Team
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		CmPicture
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Text;
using SIL.FieldWorks.FDO.DomainServices;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// CmPicture encapsulates a picture that can be embedded in a text.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	internal partial class CmPicture
		{
		#region Public methods
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Append a picture to the end of the paragraph using the given writing system.
		///// </summary>
		///// <param name="ws">given writing system</param>
		///// <param name="strBldr">The string builder for the paragraph being composed</param>
		///// ------------------------------------------------------------------------------------
		//public void AppendPicture(int ws, ITsStrBldr strBldr)
		//{
		//    // Make a TsTextProps with the relevant object data and the same ws as its
		//    // context.
		//    byte[] objData = MiscUtils.GetObjData(this.Guid,
		//        (byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
		//    ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
		//    propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
		//        objData, objData.Length);
		//    propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);

		//    // Insert the orc with the resulting properties.
		//    strBldr.Replace(strBldr.Length, strBldr.Length,
		//        new string(TsStringUtils.kChObject, 1), propsBldr.GetTextProps());
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the properties of a CmPicture with the given file, caption, and folder.
		/// </summary>
		/// <param name="srcFilename">The full path to the filename (this might be an "internal"
		/// copy of the original the user chose)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="ws">The WS for the location in the caption MultiUnicode to put the
		/// caption</param>
		/// ------------------------------------------------------------------------------------
		public void UpdatePicture(string srcFilename, ITsString captionTss, string sFolder, int ws)
		{
			// Set the caption first since creating the CmFile will throw if srcFilename is empty.
			if (ws != 0)
				Caption.set_String(ws, captionTss);

			ICmFile file = PictureFileRA;
			if (file == null)
			{
				ICmFolder folder = DomainObjectServices.FindOrCreateFolder(m_cache, LangProjectTags.kflidPictures, sFolder);
				PictureFileRA = DomainObjectServices.FindOrCreateFile(folder, srcFilename);
			}
			else
			{
				Debug.Assert(sFolder == CmFolderTags.LocalPictures,
					"TODO: If we ever actually support use of different folders, we need to handle folder changes.");
				if (srcFilename != null && !FileUtils.PathsAreEqual(srcFilename, file.AbsoluteInternalPath))
					file.InternalPath = srcFilename;
			}
			// We shouldn't need to this in the new FDO.
			//m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, Hvo,
			//    (int)CmPicture.CmPictureTags.kflidCaption, ws, 0 , 0);
			//m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, file.Hvo,
			//    (int)CmFile.CmFileTags.kflidInternalPath, 0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an ORC pointing to this picture at the specified location.
		/// </summary>
		/// <param name="tss">String into which ORC is to be inserted</param>
		/// <param name="ich">character offset where insertion is to occur</param>
		/// <returns>a new TsString with the ORC</returns>
		/// ------------------------------------------------------------------------------------
		public ITsString InsertORCAt(ITsString tss, int ich)
		{
			// Insert the orc with the resulting properties.
			ITsStrBldr tsStrBldr = tss.GetBldr();
			InsertORCAt(tsStrBldr, ich);

			return tsStrBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an ORC pointing to this picture at the specified location.
		/// </summary>
		/// <param name="tsStrBldr">String into which ORC is to be inserted</param>
		/// <param name="ich">character offset where insertion is to occur</param>
		/// ------------------------------------------------------------------------------------
		public void InsertORCAt(ITsStrBldr tsStrBldr, int ich)
		{
			Debug.Assert(tsStrBldr.Length >= ich);

			// Make a TsTextProps with the relevant object data and the same ws as its
			// context.
			int nvar;
			int wsRun = tsStrBldr.get_PropertiesAt(ich).GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);

			// Insert the orc with the resulting properties.
			TsStringUtils.InsertOrcIntoPara(Guid, FwObjDataTypes.kodtGuidMoveableObjDisp,
				tsStrBldr, ich, ich, wsRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description in the English writing system. Returns empty strng if this
		/// is not set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EnglishDescriptionAsString
		{
			get
			{
				int wsEn = Services.WritingSystemManager.GetWsFromStr("en");
				return Description.get_String(wsEn).Text ?? String.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the layout position as a string. Rather that just taking the natural string
		/// representation of each of the <see cref="PictureLayoutPosition"/> values, we convert
		/// a couple of them to use USFM-compatible variations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LayoutPosAsString
		{
			get
			{
				switch (LayoutPos)
				{
					case PictureLayoutPosition.CenterInColumn: return "col";
					case PictureLayoutPosition.CenterOnPage: return "span";
					default: return LayoutPos.ToString();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string PictureLocAsString
		{
			get
			{
				return LocationRangeType == PictureLocationRangeType.AfterAnchor ? null :
					LocationMin + "-" + LocationMax;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture suitable for exporting or for building
		/// a clipboard representation of the object.
		/// </summary>
		/// <param name="fFileNameOnly">If set to <c>true</c> the picture filename does not
		/// contain the full path specification. Use <c>false</c> for the full path (e.g., for
		/// use on the clipboard).
		/// </param>
		/// <param name="sReference">A string containing the picture's reference (can be null or
		/// empty).</param>
		/// <param name="picLocBridge">A picture location bridge object (can be null).</param>
		/// <returns>String representation of the picture</returns>
		/// ------------------------------------------------------------------------------------
		public string GetTextRepOfPicture(bool fFileNameOnly, string sReference,
			IPictureLocationBridge picLocBridge)
		{
			string sPicLoc = (picLocBridge == null) ? PictureLocAsString :
				picLocBridge.GetPictureLocAsString(this);
			string sResult = BuildTextRepParamString(EnglishDescriptionAsString,
				(fFileNameOnly ? Path.GetFileName(PictureFileRA.InternalPath) : PictureFileRA.AbsoluteInternalPath),
				LayoutPosAsString, sPicLoc, PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text,
				Caption.VernacularDefaultWritingSystem.Text);
			if (sReference != null)
				sResult = BuildTextRepParamString(sResult, sReference);

			// TODO (TE-7759) Include LocationRangeType and ScaleFactor
			return sResult;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scale factor, the integral percentage of the available width that
		/// this picture is to occupy. Picture should be shrunk or grown to fill this width.
		/// For example, if there are two columns of text on a page and the picture spans the
		/// columns, then the available width for the picture is the page-width minus the
		/// margins. So if the ScaleFactor is 80, then the picture would be sized so that its
		/// width occupies 80% of that space. Valid values are from 1 to 100.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ModelProperty(CellarPropertyType.Integer, 48006, "int")]
		public int ScaleFactor
		{
			get { return ScaleFactor_Generated == 0 ? 100 : ScaleFactor_Generated; }
			set { ScaleFactor_Generated = (value <= 0 || value > 100 ? 100 : value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the picture suitable to put on the clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TextRepresentation
		{
			get
			{
				// TODO (TE-7759); Remove LocationRangeType and ScaleFactor parameters if/when
				// they become part of the USFM Standard.
				return BuildTextRepParamString("CmPicture", GetTextRepOfPicture(false, null, null),
					LocationRangeType, ScaleFactor);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method called to implement virtual property. Returns internal pathname of associated
		/// file.  (LT-7104 requested internal path instead of original path.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString PathNameTSS
		{
			get
			{
				if (PictureFileRA == null || String.IsNullOrEmpty(PictureFileRA.InternalPath))
					return null;
				string pathname = PictureFileRA.AbsoluteInternalPath;
				return m_cache.MakeUserTss(pathname);
			}
		}

		/// <summary>
		/// Get the sense number of the owning LexSense.
		/// ENHANCE DamienD: register this property as modified when its dependencies change
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString SenseNumberTSS
		{
			get
			{
				string sNumber;
				if (Owner == null || Owner.ClassID != LexSenseTags.kClassId)
				{
					sNumber = Strings.ksZero;
				}
				else
				{
					sNumber = (Owner as LexSense).SenseNumber;
				}
				return m_cache.MakeUserTss(sNumber);
			}
		}
		#endregion

		#region Overridden methods
		// JohnT: this was once overridden to delete the associated CmFile also, if nothing else uses it.
		// However we can no longer be sure of this without searching all strings in the system to see whether
		// any contain embedded links to the file. For now we are just accepting that some CmFiles will leak.
		//internal override void OnBeforeObjectDeleted()

		#endregion

		#region Private Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the given parameters to build a text-representation, with parameters
		/// delimited  by a vertical bar (|).
		/// </summary>
		/// <param name="parms">The parameters</param>
		/// ------------------------------------------------------------------------------------
		private string BuildTextRepParamString(params object[] parms)
		{
			return parms.ToString(false, "|");
		}
		#endregion
	}
}
