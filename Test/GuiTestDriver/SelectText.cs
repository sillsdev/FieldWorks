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
// File: SelectText.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;

//using FwKernelLib;
//using FwViews;


namespace GuiTestDriver
{
	/// <summary>
	/// Selects text from the GUI control / view specified by path. Loc contains a coded path that
	/// navigates to the text of interest. "view:Draft/b:2/s/p:3" indicates 'the third book, first section
	/// and content, third paragraph of the Draft view'. These location symbols are part of the GUI
	/// model <view><level/></view> elements and so change from view to view. When codes are not specified, they are assigned 0.
	/// Each view defines its own valid codes. At tells which character to start the selection at.
	/// If not specified, at is set to 0, before the first character. Run specifies how many
	/// characters are selected. If abscent, the selection degenerates to an Insertion Point (IP)
	/// placement before 'at'. When an "id" is specified, the text and its properties are placed
	/// in an XML string called "Text". Variables are used to pass data to flow and check elements.
	/// If no text is found to be selected, Text contains a <notFound/> element.
	/// </summary>
	public class SelectText : ActionBase
	{
		string m_loc;
		int    m_at  = 0;
		int    m_run = 0;
		string m_text;

		public SelectText(): base()
		{
			m_loc  = null;
			m_at   = 0;
			m_run  = 0;
			m_text = null;
			m_tag  = "select-text";
		}

		public string Loc
		{
			get {return m_loc;}
			set {m_loc = value;}
		}

		public int At
		{
			get {return m_at;}
			set {m_at = value;}
		}

		public int Run
		{
			get {return m_run;}
			set {m_run = value;}
		}

		public override void Execute()
		{
			base.Execute();
			bool exists = false;
			isNotNull(m_path,"attribute 'path' must be set");
			m_path = Utilities.evalExpr(m_path);
			GuiPath gpath = new GuiPath(m_path);
			isNotNull(gpath,"attribute path='"+m_path+"' not parsed");
			GuiPath lpath = null;
			if (m_loc != null) // if it's null, get the whole string content
			{
				lpath = new GuiPath(m_loc);
				isNotNull(lpath,"attribute path='"+m_loc+"' not parsed");
			}
			Context con = (Context)Ancestor(typeof(Context));
			isNotNull(con,"Select-text must occur in some context");
			AccessibilityHelper ah = con.Accessibility;
			isNotNull(ah,"Select-text context not accessible");
			// The model names are needed in selectText.
			IPathVisitor visitor = null;
			ah = gpath.FindInGui(ah, visitor);
			isNotNull(ah,"context not accessible from path");
			if (ah != null)
			{
				if (m_loc == null) exists = getStrValue(ah);
				else               exists = selectText(lpath, ah);
			}
			Logger.getOnly().result(this);
			Finished = true; // tell do-once it's done
		}

		public override string GetDataImage (string name)
		{
			if (name == null) name = "text";
			switch (name)
			{
				case "text":	return m_text;
				case "loc":	    if (m_loc != null) return m_loc; else return "";
				case "at":		return m_at.ToString();
				case "run":		return m_run.ToString();
				default:		return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Given the GUI control or view ah, get its entire string value.
		/// </summary>
		/// <param name="ah">Accessibility helper from the GUI control or view</param>
		/// <returns>true if the string value was retrieved.</returns>
		private bool getStrValue(AccessibilityHelper ah)
		{
			m_text = ah.Value;
			return m_text != null;
		}

		/// <summary>
		/// Given the GUI control or view ah, find and highlight the text indicated
		/// via the location (level) path.
		/// </summary>
		/// <param name="lpath">Level path array.</param>
		/// <param name="ah">Accessibility helper from the GUI control or view</param>
		/// <returns>true if the selected string was retrieved.</returns>
		private bool selectText(GuiPath lpath, AccessibilityHelper ah)
		{
			IVwRootBox rbox = ah.RootBox();
			isNotNull(rbox,"view rootbox not found");

			IVwSelection sel = null; // returned selection

			// create a SelLevInfo[] array to the content using the model view levels.
			int clevels = 0;
			SelLevInfo[] rgvsli;
			if (DrillToContent(lpath, out rgvsli, out clevels))
			{
				int ihvoRoot = 0; // first rootbox
				int tagTextProp = 16002; // kflidStTxtPara_Contents
				int cpropPrevious = 0;
				int ichAnchor = m_at;  // starting character number
				int ichEnd    = m_at + m_run;  // ending character number in rgvsli[0].ihvo or ihvoEnd if it is not -1
				int ws = 0;
				bool fAssocPrev = false;
				int ihvoEnd = -1; // paragraph # to end at, if it doesn't exist, get unspecified interop error
				ITsTextProps ttpIns = null;
				bool fInstall  = true; // make it the view's default selection

				//int iHeight = rbox.get_Height();
				int iHeight = rbox.Height;

				sel = rbox.MakeTextSelection
					(ihvoRoot, clevels, rgvsli, tagTextProp, cpropPrevious, ichAnchor, ichEnd,
					ws, fAssocPrev, ihvoEnd, ttpIns, fInstall);
				isNotNull(sel,"failed to select text");
				//areEqual(true, sel.get_IsValid(), "selection is not valid");
				areEqual(true, sel.IsValid, "selection is not valid");
				ITsTextProps ttp = spyOnSelection(sel);
				if(ttp != null) spyOnTextProps(ttp);
			}
			string strSeparator = "|";
			ITsString tssFromApp;
			sel.GetSelectionString(out tssFromApp, strSeparator);
			ITsStreamWrapper tsw = TsStreamWrapperClass.Create();
			//UCOMIStream strm = tsw.get_Stream();
			System.Runtime.InteropServices.ComTypes.IStream strm = tsw.Stream;
			// Copy the string to our address space.
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.ReplaceTsString(0,0, tssFromApp);
			ITsString tss = bldr.GetString();
			int icchIndent = 2;
			int iws = 0;
			bool fWriteObjData = true;
			//tsw.WriteTssAsXml(tss2, icchIndent, iws, fWriteObjData);
			//ISilDataAccess da = rbox.get_DataAccess();
			ISilDataAccess da = rbox.DataAccess;
			//ILgWritingSystemFactory wsf = da.get_WritingSystemFactory();
			ILgWritingSystemFactory wsf = da.WritingSystemFactory;
			tss.WriteAsXml(strm, wsf, icchIndent, iws, fWriteObjData);
			//m_text = tsw.get_Contents(); // XML formatted string
			m_text = tsw.Contents; // XML formatted string
			return m_text != null;
		}

		/// <summary>
		/// Gets the GUI Model root node and finds the model of the view to select text from.
		/// The model view is recursively followed using the symbols in the level path
		/// array. Information from the model populates the SelLevInfo array as recursions return.
		/// </summary>
		/// <param name="lpath">Level path array.</param>
		/// <param name="rgvsli">The FW views system navigation array.</param>
		/// <param name="clevels">Number of levels in the FW views system navigation array.</param>
		/// <returns>True if the navigation array is filled. False if something goes wrong.</returns>
		private bool DrillToContent(GuiPath lpath, out SelLevInfo[] rgvsli, out int clevels)
		{
			bool status = true;
			clevels = 0;
			rgvsli = null;
			XmlElement root = null; // Application.GuiModelRoot;
			status = root != null;
			if (status)
			{
				// Use lpath to get to the right place in the gui model
				string xPath = "//" + lpath.Type + "[@name='" + lpath.Name + "']";
				XmlNode viewRoot = root.SelectSingleNode(xPath);
				isNotNull(viewRoot,"could not find "+m_loc+" in the GUI Model");
				int passes = 0; // count the levels
				checkNextLevel(viewRoot, lpath, passes, out rgvsli, out clevels);
			}
			return status;
		}

		/// <summary>
		/// checkNextLevel follows the GUI model 'level' nodes as indicated via the level path, lpath,
		/// to create a FW views navigation array to the string to be selected.
		/// If lpath does not contain a symbol for the current level, the first encountered in the
		/// GUI model is used.
		/// The last level of the GUI model must be specified to obtain a string.
		/// If not, an interop error is raised. This needs further investigation.
		/// Information from the model populates the SelLevInfo array as recursions return.
		/// </summary>
		/// <param name="viewRoot">The a GUI model view or level XML node.</param>
		/// <param name="lpath">Level path array</param>
		/// <param name="passes">The recursion count.</param>
		/// <param name="rgvsli">The FW views system navigation array.</param>
		/// <param name="clevels">Number of levels in the FW views system navigation array.</param>
		private void checkNextLevel(XmlNode viewRoot, GuiPath lpath, int passes, out SelLevInfo[] rgvsli, out int clevels)
		{
			XmlNodeList nLevelList = viewRoot.SelectNodes("level");
			isNotNull(nLevelList,"found no levels in Gui Model view.");
			XmlNode xLevel = nLevelList[0];
			lpath = lpath.Next;
			// if lpath = null, then set fisrt level only.
			if (lpath == null)
			{ // end recursion
				if (passes == 0) clevels = 1;
				else             clevels = passes;
				rgvsli = new SelLevInfo[clevels];
				if (passes == 0)
				{ // create at least the first level
					rgvsli[0].ihvo = 0;
					rgvsli[0].tag  = Convert.ToInt32(xLevel.Attributes["flid"].Value);
					rgvsli[0].cpropPrevious = 0;
				}
				return;
			}
			string num = "0"; //ihvo
			// Does the lpath type match a level symbol?
			// Try to get more specific by matching the user's symbol
			IEnumerator eLevel = nLevelList.GetEnumerator();
			bool OK = eLevel.MoveNext();
			while (OK && !((XmlNode)eLevel.Current).Attributes["symbol"].Value.Equals(lpath.Type))
				OK = eLevel.MoveNext();
			if (OK)
			{ // got a match: use the level obtained
				xLevel = (XmlNode)eLevel.Current;
				num = lpath.Name; // ihvo
			}
			checkNextLevel(xLevel, lpath, passes+1, out rgvsli, out clevels);

			// set the rgvsli array on the way out (post recurive)
			if (num == null) num = "0";
			int indx = clevels-passes-1; // the array elements are reversed
			rgvsli[indx].ihvo = Convert.ToInt32(num);
			rgvsli[indx].tag  = Convert.ToInt32(xLevel.Attributes["flid"].Value);
			rgvsli[indx].cpropPrevious = 0;
		}

		private ITsTextProps spyOnSelection(IVwSelection sel)
		{
			// See what got selected
			int ihvoRoot = 0; // first rootbox
			int cvsli = sel.CLevels(true); // selection levels-1 involved
			cvsli--;
			int tag;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd1;
			ITsTextProps ttp;
			ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative(cvsli, typeof(SelLevInfo));
			sel.AllTextSelInfo(out ihvoRoot, cvsli, rgvsliTemp, out tag, out cpropPrevious,
				out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
			SelLevInfo[] rgvsli = (SelLevInfo[])MarshalEx.NativeToArray(rgvsliTemp, cvsli,
				typeof(SelLevInfo));
			// use the debugger to look at the vars. Can't get into ttp here.
			return ttp;
		}

		private void spyOnTextProps(ITsTextProps ttp)
		{
			int tpt; // ??

			// look at integer props
			int cintProps = ttp.IntPropCount;
			for (int i = 0; i < cintProps; i++)
			{
				int nVar;
				int intProp = ttp.GetIntProp(i, out tpt, out nVar);
				int Value = ttp.GetIntPropValues(tpt, out nVar);
				Value = 34; // need something so Value can be looked at
			}

			// look at string props
			int cstrProps = ttp.StrPropCount;
			for (int i = 0; i < cstrProps; i++)
			{
				string strProp = ttp.GetStrProp(i, out tpt);
				string Value = ttp.GetStrPropValue(tpt);
				Value = "why?"; // need something so Value can be looked at
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_loc != null)  image += @" loc="""+Utilities.attrText(m_loc)+@"""";
			if (m_at != 0)      image += @" at="""+m_at+@"""";
			if (m_run != 0)     image += @" run="""+m_run+@"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_text != null) image += @" text="""+Utilities.attrText(m_text)+@"""";
			return image;
		}
	}
}
