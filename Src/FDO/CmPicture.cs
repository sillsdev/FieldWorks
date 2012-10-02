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
using System.Collections.Generic;
using System.IO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.LangProj;
using SIL.Utils;
using System.Globalization;
using System.Text;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// CmPicture encapsulates a picture that can be embedded in a text.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public partial class CmPicture : IPictureLocationBridge
	{
		#region Construction & Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		public CmPicture(FdoCache fcCache, string sTextRepOfPicture, string sFolder)
			: this(fcCache, sTextRepOfPicture, sFolder, 0, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The picture location parser (can be null).</param>
		/// ------------------------------------------------------------------------------------
		public CmPicture(FdoCache fcCache, string sTextRepOfPicture, string sFolder,
			int anchorLoc, IPictureLocationBridge locationParser)
			: base(fcCache, fcCache.CreateObject(CmPicture.kclsidCmPicture))
		{
			string[] tokens = sTextRepOfPicture.Split(new char[] {'|'});
			if (tokens.Length < 9 || tokens[0] != "CmPicture")
				throw new ArgumentException("The clipboard format for a Picture was invalid");
			string sDescription = tokens[1];
			string srcFilename = tokens[2];
			string sLayoutPos = tokens[3];
			string sLocationRange = tokens[4];
			string sCopyright = tokens[5];
			string sCaption = tokens[6];
			string sLocationRangeType = tokens[7];
			string sScaleFactor = tokens[8];

			PictureLocationRangeType locRangeType = ParseLocationRangeType(sLocationRangeType);

			InitializeNewPicture(sFolder, anchorLoc, locationParser, sDescription,
				srcFilename, sLayoutPos, sLocationRange, sCopyright, sCaption,
				locRangeType, sScaleFactor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a CmPicture for the given file, having the given caption, and located in
		/// the given folder.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		public CmPicture(FdoCache fcCache, string srcFilename, ITsString captionTss, string sFolder)
			: base(fcCache, fcCache.CreateObject(CmPicture.kclsidCmPicture))
		{
			InitializeNewPicture(srcFilename, captionTss, null, PictureLayoutPosition.CenterInColumn,
				100, PictureLocationRangeType.AfterAnchor, 0, 0, null, sFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a USFM-style Standard Format import.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="sDescription">Illustration description in English.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="sCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		public CmPicture(FdoCache fcCache, string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, string sDescription, string srcFilename,
			string sLayoutPos, string sLocationRange, string sCopyright, string sCaption,
			PictureLocationRangeType locRangeType, string sScaleFactor)
			: base(fcCache, fcCache.CreateObject(CmPicture.kclsidCmPicture))
		{
			InitializeNewPicture(sFolder, anchorLoc, locationParser, sDescription,
				srcFilename, sLayoutPos, sLocationRange, sCopyright, sCaption,
				locRangeType, sScaleFactor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a Toolbox-style Standard Format import.
		/// </summary>
		/// <param name="fcCache">FDO cache</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that the locationParser can use if
		/// necessary (can be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="descriptions">The descriptions in 0 or more writing systems.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="tssCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		public CmPicture(FdoCache fcCache, string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, Dictionary<int, string> descriptions,
			string srcFilename, string sLayoutPos, string sLocationRange, string sCopyright,
			ITsString tssCaption, PictureLocationRangeType locRangeType, string sScaleFactor)
			: base(fcCache, fcCache.CreateObject(CmPicture.kclsidCmPicture))
		{
			InitializeNewPicture(sFolder, anchorLoc, locationParser, null,
				srcFilename, sLayoutPos, sLocationRange, sCopyright, tssCaption,
				locRangeType, sScaleFactor);
			if (descriptions != null)
			{
				foreach (int ws in descriptions.Keys)
					Description.SetAlternative(descriptions[ws], ws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// <param name="ws">The WS of the caption and copyright</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeNewPicture(string srcFilename, ITsString captionTss,
			string sFolder, int ws)
		{
			InitializeNewPicture(srcFilename, captionTss, null,
				PictureLayoutPosition.CenterInColumn, 100, PictureLocationRangeType.AfterAnchor,
				0, 0, null, sFolder, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the new picture.
		/// </summary>
		/// <param name="sFolder">The folder.</param>
		/// <param name="anchorLoc">The anchor location.</param>
		/// <param name="locationParser">The location parser (can be null).</param>
		/// <param name="sDescription">Illustration description in English.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="sCaption">The caption (in the default vernacular Writing System).</param>
		/// <param name="locRangeType">Type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeNewPicture(string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, string sDescription, string srcFilename,
			string sLayoutPos, string sLocationRange, string sCopyright, string sCaption,
			PictureLocationRangeType locRangeType, string sScaleFactor)
		{
			ITsStrFactory factory = TsStrFactoryClass.Create();
			InitializeNewPicture(sFolder, anchorLoc, locationParser, sDescription, srcFilename,
			sLayoutPos, sLocationRange, sCopyright,
			factory.MakeString(sCaption, m_cache.DefaultVernWs), locRangeType, sScaleFactor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the new picture.
		/// </summary>
		/// <param name="sFolder">The folder.</param>
		/// <param name="anchorLoc">The anchor location that the locationParser can use if
		/// necessary (can be 0).</param>
		/// <param name="locationParser">The location parser (can be null).</param>
		/// <param name="sDescription">Illustration description in English.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="tssCaption">The caption (in the default vernacular Writing System).</param>
		/// <param name="locRangeType">Type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeNewPicture(string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser, string sDescription, string srcFilename,
			string sLayoutPos, string sLocationRange, string sCopyright, ITsString tssCaption,
			PictureLocationRangeType locRangeType, string sScaleFactor)
		{
			if (locationParser == null)
				locationParser = this;
			int locationMin, locationMax;
			locationParser.ParsePictureLoc(sLocationRange, anchorLoc, ref locRangeType,
				out locationMin, out locationMax);

			InitializeNewPicture(srcFilename, tssCaption,
				sDescription, ParseLayoutPosition(sLayoutPos), ParseScaleFactor(sScaleFactor),
				locRangeType, locationMin, locationMax, sCopyright, sFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up. Put the caption in the default vernacular writing system.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption (in the default vernacular Writing System)</param>
		/// <param name="description">Illustration description in English. This is not published.</param>
		/// <param name="layoutPos">Indication of where in the column/page the picture is to be
		/// laid out.</param>
		/// <param name="scaleFactor">Integral percentage by which picture is grown or shrunk.</param>
		/// <param name="locationRangeType">Indicates the type of data contained in LocationMin
		/// and LocationMax.</param>
		/// <param name="locationMin">The minimum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="locationMax">The maximum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="copyright">Publishable information about the copyright that should
		/// appear on the copyright page of the publication.</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeNewPicture(string srcFilename, ITsString captionTss,
			string description, PictureLayoutPosition layoutPos, int scaleFactor,
			PictureLocationRangeType locationRangeType, int locationMin, int locationMax,
			string copyright, string sFolder)
		{
			InitializeNewPicture(srcFilename, captionTss, description, layoutPos, scaleFactor,
				locationRangeType, locationMin, locationMax, copyright, sFolder,
				m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a new CmPicture by creating a copy of the file in the given folder and
		/// hooking everything up.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption (in the given Writing System)</param>
		/// <param name="description">Illustration description in English. This is not
		/// published.</param>
		/// <param name="layoutPos">Indication of where in the column/page the picture is to be
		/// laid out.</param>
		/// <param name="scaleFactor">Integral percentage by which picture is grown or shrunk.</param>
		/// <param name="locationRangeType">Indicates the type of data contained in LocationMin
		/// and LocationMax.</param>
		/// <param name="locationMin">The minimum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="locationMax">The maximum Scripture reference at which this picture can
		/// be laid out.</param>
		/// <param name="copyright">Publishable information about the copyright that should
		/// appear on the copyright page of the publication.</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// <param name="ws">The WS of the caption and copyright</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeNewPicture(string srcFilename, ITsString captionTss,
			string description, PictureLayoutPosition layoutPos, int scaleFactor,
			PictureLocationRangeType locationRangeType, int locationMin, int locationMax,
			string copyright, string sFolder, int ws)
		{
			// Set the caption first since creating the CmFile will throw if srcFilename is empty.
			if (captionTss != null)
				Caption.SetAlternative(captionTss, ws);
			// Locate CmFolder with given name or create it, if neccessary
			ICmFolder folder = CmFolder.FindOrCreateFolder(m_cache, (int)LangProject.LangProjectTags.kflidPictures, sFolder);
			PictureFileRA = CmFile.FindOrCreateFile(folder, srcFilename);
			int wsEn = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			if (!String.IsNullOrEmpty(description) && wsEn > 0)
				Description.SetAlternative(description, wsEn);
			LayoutPos = layoutPos;
			ScaleFactor = scaleFactor;
			LocationRangeType = locationRangeType;
			LocationMin = locationMin;
			LocationMax = locationMax;
			if (!string.IsNullOrEmpty(copyright))
			{
				string sExistingCopyright = PictureFileRA.Copyright.GetAlternative(ws).Text;
				if (sExistingCopyright != null && sExistingCopyright != copyright)
				{
					Logger.WriteEvent("Could not update copyright for picture " +
						PictureFileRA.AbsoluteInternalPath + " to '" + copyright + "'");
					return;
				}
				PictureFileRA.Copyright.SetAlternative(copyright, ws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to parse the given token as a layout position string.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLayoutPosition.CenterInColumn if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private static PictureLayoutPosition ParseLayoutPosition(string token)
		{
			switch (token)
			{
				case "col":
					return PictureLayoutPosition.CenterInColumn;
				case "span":
					return PictureLayoutPosition.CenterOnPage;
				case "right":
					return PictureLayoutPosition.RightAlignInColumn;
				case "left":
					return PictureLayoutPosition.LeftAlignInColumn;
				case "fillcol":
					return PictureLayoutPosition.FillColumnWidth;
				case "fillspan":
					return PictureLayoutPosition.FillPageWidth;
				case "fullpage":
					return PictureLayoutPosition.FullPage;
				default:
					try
					{
						return (PictureLayoutPosition)Enum.Parse(typeof(PictureLayoutPosition), token);
					}
					catch (ArgumentException e)
					{
						Logger.WriteError(e);
						return PictureLayoutPosition.CenterInColumn;
					}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the string representing the type of the location range.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLocationRangeType.AfterAnchor if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private PictureLocationRangeType ParseLocationRangeType(string token)
		{
			try
			{
				return (PictureLocationRangeType)Enum.Parse(typeof(PictureLocationRangeType), token);
			}
			catch (ArgumentException e)
			{
				Logger.WriteError(e);
				return PictureLocationRangeType.AfterAnchor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to parse the given token as a layout position string.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <returns>The enumeration corresponding to the parsed token, or
		/// PictureLayoutPosition.CenterInColumn if unable to parse.</returns>
		/// ------------------------------------------------------------------------------------
		private static int ParseScaleFactor(string token)
		{
			if (string.IsNullOrEmpty(token))
				return 100;

			int scaleFactor = 0;
			foreach (char ch in token)
			{
				int value = CharUnicodeInfo.GetDigitValue(ch);
				if (value >= 0 && scaleFactor <= 100)
					scaleFactor = (scaleFactor == 0) ? value : scaleFactor * 10 + value;
				else if (scaleFactor > 0)
					break;
			}
			if (scaleFactor == 0)
			{
				if (!String.IsNullOrEmpty(token))
					Logger.WriteEvent("Unexpected CmPicture Scale value: " + token);
				scaleFactor = 100;
			}
			else
				scaleFactor = Math.Min(scaleFactor, 1000);
			return scaleFactor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the picture location range string.
		/// </summary>
		/// <param name="s">The string representation of the picture location range.</param>
		/// <param name="anchorLocation">The anchor location.</param>
		/// <param name="locType">The type of the location range. The incoming value tells us
		/// the assumed type for parsing. The out value can be set to a different type if we
		/// discover that the actual value is another type.</param>
		/// <param name="locationMin">The location min.</param>
		/// <param name="locationMax">The location max.</param>
		/// ------------------------------------------------------------------------------------
		public void ParsePictureLoc(string s, int anchorLocation,
			ref PictureLocationRangeType locType, out int locationMin, out int locationMax)
		{
			locationMin = locationMax = 0;
			switch (locType)
			{
				case PictureLocationRangeType.AfterAnchor:
					return;
				case PictureLocationRangeType.ReferenceRange:
					throw new NotSupportedException("CmPicture cannot parse Scripture references. Use another implementation of IPictureLocationParser.");
				case PictureLocationRangeType.ParagraphRange:
					if (String.IsNullOrEmpty(s))
					{
						locType = PictureLocationRangeType.AfterAnchor;
						return;
					}
					string[] pieces = s.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
					if (pieces.Length == 2)
					{
						if (Int32.TryParse(pieces[0], out locationMin))
							if (Int32.TryParse(pieces[1], out locationMax))
								return;
					}
					locType = PictureLocationRangeType.AfterAnchor;
					return;
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a picture to the end of the paragraph using the given writing system.
		/// </summary>
		/// <param name="ws">given writing system</param>
		/// <param name="strBldr">The string builder for the paragraph being composed</param>
		/// ------------------------------------------------------------------------------------
		public void AppendPicture(int ws, ITsStrBldr strBldr)
		{
			// Make a TsTextProps with the relevant object data and the same ws as its
			// context.
			byte[] objData = MiscUtils.GetObjData(m_cache.GetGuidFromId(this.Hvo),
				(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);

			// Insert the orc with the resulting properties.
			strBldr.Replace(strBldr.Length, strBldr.Length,
				new string(StringUtils.kchObject, 1), propsBldr.GetTextProps());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the properties of a CmPicture with the given file, caption, and folder.
		/// The caption is put into the default vernacular writing system
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// ------------------------------------------------------------------------------------
		public void UpdatePicture(string srcFilename, ITsString captionTss, string sFolder)
		{
			UpdatePicture(srcFilename, captionTss, sFolder, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the properties of a CmPicture with the given file, caption, and folder.
		/// </summary>
		/// <param name="srcFilename">The full path to the original filename (an internal copy
		/// will be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="ws">The WS for the location in the caption MultiUnicode to put the
		/// caption</param>
		/// ------------------------------------------------------------------------------------
		public void UpdatePicture(string srcFilename, ITsString captionTss, string sFolder, int ws)
		{
			// Locate CmFolder with given name or create it, if neccessary
			ICmFolder folder = CmFolder.FindOrCreateFolder(m_cache, (int)LangProject.LangProjectTags.kflidPictures, sFolder);
			ICmFile file = PictureFileRA;
			if (file == null)
			{
				file = folder.FilesOC.Add(new CmFile());
				PictureFileRA = file;
			}
			// (The case-independent comparison is valid only for Microsoft Windows.)
			string sInternalAbsPath = file.AbsoluteInternalPath;
			if (srcFilename != null &&
				!srcFilename.Equals(sInternalAbsPath, StringComparison.InvariantCultureIgnoreCase))
			{
				((CmFile)file).SetInternalPath(srcFilename);
			}
			Caption.SetAlternative(captionTss, ws);
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, Hvo,
				(int)CmPicture.CmPictureTags.kflidCaption, ws, 0 , 0);
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, file.Hvo,
				(int)CmFile.CmFileTags.kflidInternalPath, 0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an ORC pointing to this picture at the specified location.
		/// </summary>
		/// <param name="tss">String into which ORC is to be inserted</param>
		/// <param name="ich">character offset where insertion is to occur</param>
		/// <param name="hvoObj">The object that owns the given tss</param>
		/// <param name="flid">The owning flid of the tss</param>
		/// <param name="ws">The writing system alternative (used only for multi's)</param>
		/// ------------------------------------------------------------------------------------
		public void InsertORCAt(ITsString tss, int ich, int hvoObj, int flid, int ws)
		{
			// REVIEW: Why do we have InsertORCAt when InsertOwningORCIntoPara probably does much
			// the same thing? InsertORCAt is used in production code. InsertOwningORCIntoPara is
			// used in some tests.
			System.Diagnostics.Debug.Assert(tss.Length >= ich);

			// Make a TsTextProps with the relevant object data and the same ws as its
			// context.
			byte[] objData = MiscUtils.GetObjData(m_cache.GetGuidFromId(Hvo),
				(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);
			int nvar;
			int wsRun = tss.get_PropertiesAt(ich).
				GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsRun);

			// Insert the orc with the resulting properties.
			ITsStrBldr tsStrBldr = tss.GetBldr();
			tsStrBldr.Replace(ich, ich, new string(StringUtils.kchObject, 1),
				propsBldr.GetTextProps());

			if (ws == 0)
				m_cache.SetTsStringProperty(hvoObj, flid, tsStrBldr.GetString());
			else
			{
				// This branch has probably not been tested, not currently used.
				m_cache.SetMultiStringAlt(hvoObj, flid, ws, tsStrBldr.GetString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert picture (ORC with the GUID in the properties) into the builder
		/// for the paragraph
		/// </summary>
		/// <param name="tsStrBldr">A string builder for the paragraph that is to contain the
		/// picture ORC</param>
		/// <param name="ich">The 0-based character offset into the paragraph</param>
		/// <param name="ws">The writing system id</param>
		/// ------------------------------------------------------------------------------------
		public void InsertOwningORCIntoPara(ITsStrBldr tsStrBldr, int ich, int ws)
		{
			StringUtils.InsertOrcIntoPara(Guid, FwObjDataTypes.kodtGuidMoveableObjDisp,
				tsStrBldr, ich, ich, ws);
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
				int wsEn = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				return Description.GetAlternative(wsEn).Text ?? String.Empty;
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
		/// <param name="picture">The picture.</param>
		/// ------------------------------------------------------------------------------------
		public string GetPictureLocAsString(ICmPicture picture)
		{
			return picture.LocationRangeType == PictureLocationRangeType.AfterAnchor ? null :
				picture.LocationMin + "-" + picture.LocationMax;
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
			if (picLocBridge == null)
				picLocBridge = this;
			string sResult = BuildTextRepParamString(EnglishDescriptionAsString,
				(fFileNameOnly ? Path.GetFileName(PictureFileRA.InternalPath) :
				PictureFileRA.AbsoluteInternalPath), LayoutPosAsString,
				picLocBridge.GetPictureLocAsString(this),
				PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text,
				Caption.VernacularDefaultWritingSystem.Text);
			if (sReference != null)
				sResult = BuildTextRepParamString(sResult, sReference);

			// TODO (TE-7759) Include LocationRangeType and ScaleFactor
			return sResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified text represents a picture.
		/// </summary>
		/// <param name="textRepOfPicture">The text representation of a picture.</param>
		/// <returns>
		/// 	<c>true</c> if the specified text represents a picture; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsPicture(string textRepOfPicture)
		{
			string[] tokens = textRepOfPicture.Split(new char[] {'|'});
			return tokens.Length >= 7 && tokens[0] == "CmPicture";
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
		public string TextRepOfPicture
		{
			get
			{
				// TODO (TE-7759); Remove LocationRangeType and ScaleFactor parameters if/when
				// they become part of the USFM Standard.
				return BuildTextRepParamString("CmPicture", GetTextRepOfPicture(false, null, this),
					LocationRangeType, ScaleFactor);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method called to implement virtual property. Returns internal pathname of associated
		/// file.  (LT-7104 requested internal path instead of original path.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString PathNameTSS
		{
			get
			{
				if (PictureFileRA == null || String.IsNullOrEmpty(PictureFileRA.InternalPath))
					return null;
				string pathname;
				if (Path.IsPathRooted(PictureFileRA.InternalPath))
					pathname = PictureFileRA.InternalPath;
				else
					pathname = Path.Combine(m_cache.LangProject.ExternalLinkRootDir,
						PictureFileRA.InternalPath);
				return m_cache.MakeUserTss(pathname);
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to make sure any CmFiles that are only there only for this picture are
		/// deleted as well
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DeleteObjectSideEffects(Set<int> objectsToDeleteAlso, SIL.FieldWorks.Common.Controls.ProgressState state)
		{
			ICmFile cmFile = PictureFileRA;

			// Check to make sure there is a picture file associated with this picture and that the only thing that CmFile is
			// around for is to provide a file for this picture.  Checking that the backreference of the file that this picture
			// uses is 1 is a valid way to make sure of this.
			// If this CmFile serves no other purpose, we should get rid of it.
			if (cmFile != null && cmFile.BackReferences.Count == 1)
			{
				objectsToDeleteAlso.Add(cmFile.Hvo);
			}

			base.DeleteObjectSideEffects(objectsToDeleteAlso, state);
		}
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
			StringBuilder bldr = new StringBuilder();

			foreach (object p in parms)
			{
				bldr.Append(p);
				bldr.Append("|");
			}
			bldr.Remove(bldr.Length - 1, 1);
			return bldr.ToString();
		}
		#endregion
	}
}
