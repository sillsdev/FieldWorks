// SelectionProperitesChanger.cs created with MonoDevelop
// User: hindlet at 9:10 A 22/09/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Collections.Generic;

namespace SIL.FieldWorks.WorldPad
{

	/// <summary>
	/// Class that contains a collection of static helper methods that allow minipulating of properties for a given Selection.
	/// </summary>
	public class PropertiesHelper
	{

		// delegate type for supplying user way of modifing a Selection property.
		public delegate void ModifySelectionPropsDelagate(ITsPropsBldr qtpb);

		public delegate ITsPropsBldr ModifyParagraphPropsDelagate(ITsPropsBldr qtpb);

		// should returing all the properties in a selection.
		public static bool GetProperties(IVwSelection selection, FwTextPropType propType, string[] results)
		{
			// TODO implement
			return false;
		}

		// Returns false if more than one type of property
		// Get a Non text Property - ie from the VwPropertyStore not the ITsTextProps - this will get Paragraph Props.
		public static bool GetSingleNonTextProperty(IVwSelection selection, FwTextPropType propType, out int result, out int type)
		{
			result = 0;
			type = 0;

			if (selection != null)
			{
				ITsPropsBldr qtpb;
				int cttp;

				// get the current SelectionProps array size from views
				selection.GetSelectionProps(0, null, null, out cttp);
				ITsTextProps[] qttpArray = new ITsTextProps[cttp];
				IVwPropertyStore[] propStoreArray = new IVwPropertyStore[cttp];

				// Get the current SelectionProps array
				ArrayPtr nativePtr = MarshalEx.ArrayToNative(qttpArray);
				ArrayPtr nativePropStorePtr = MarshalEx.ArrayToNative(propStoreArray);

				selection.GetSelectionProps(cttp, nativePtr , nativePropStorePtr, out cttp);

				qttpArray = (ITsTextProps[])MarshalEx.NativeToArray(nativePtr, cttp, typeof(ITsTextProps));
				propStoreArray = (IVwPropertyStore[])MarshalEx.NativeToArray(nativePropStorePtr, cttp, typeof(IVwPropertyStore));

				for(int i=0;i<cttp;++i) // for each item in the Selection array set the Font Family
				{
					qtpb = propStoreArray[i].TextProps.GetBldr();
					int temp;
					qtpb.GetIntPropValues((int)propType, out type, out temp);
					if (i > 0 && temp != result)
					{
						// multiple different properties use call to GetProperties
						return false;
					}
					result = temp;
					qttpArray[i] = qtpb.GetTextProps();
				}

				return true;
			}

			return false;


		}

		// Returns false if more than one type of property
		public static bool GetSingleProperty(IVwSelection selection, FwTextPropType propType, out int result, out int type)
		{
			result = 0;
			type = 0;

			if (selection != null)
			{
				ITsPropsBldr qtpb;
				int cttp;

				// get the current SelectionProps array size from views
				selection.GetSelectionProps(0, null, null, out cttp);
				ITsTextProps[] qttpArray = new ITsTextProps[cttp];
				IVwPropertyStore[] tempArray = new IVwPropertyStore[cttp];

				// Get the current SelectionProps array
				ArrayPtr nativePtr = MarshalEx.ArrayToNative(qttpArray);
				selection.GetSelectionProps(cttp, nativePtr , MarshalEx.ArrayToNative(tempArray), out cttp);
				qttpArray = (ITsTextProps[])MarshalEx.NativeToArray(nativePtr, cttp, typeof(ITsTextProps));

				for(int i=0;i<cttp;++i) // for each item in the Selection array set the Font Family
				{
					qtpb = qttpArray[i].GetBldr();
					int temp;
					qtpb.GetIntPropValues((int)propType, out type, out temp);
					if (i > 0 && temp != result)
					{
						// multiple different properties use call to GetProperties
						return false;
					}
					result = temp;
					qttpArray[i] = qtpb.GetTextProps();
				}

				return true;
			}

			return false;

		}

		// Returns false if more than one type of property
		public static bool GetSingleProperty(IVwSelection selection, FwTextPropType propType, out string result)
		{
			result = string.Empty;

			if (selection != null)
			{
				ITsPropsBldr qtpb;
				int cttp;

				// get the current SelectionProps array size from views
				selection.GetSelectionProps(0, null, null, out cttp);
				ITsTextProps[] qttpArray = new ITsTextProps[cttp];
				IVwPropertyStore[] tempArray = new IVwPropertyStore[cttp];

				// Get the current SelectionProps array
				ArrayPtr nativePtr = MarshalEx.ArrayToNative(qttpArray);
				selection.GetSelectionProps(cttp, nativePtr , MarshalEx.ArrayToNative(tempArray), out cttp);
				qttpArray = (ITsTextProps[])MarshalEx.NativeToArray(nativePtr, cttp, typeof(ITsTextProps));

				for(int i=0;i<cttp;++i) // for each item in the Selection array
				{
					qtpb = qttpArray[i].GetBldr();
					string temp;
					qtpb.GetStrPropValue((int)propType, out temp);
					if (i > 0 && temp != result)
					{
						// multiple different properties use call to GetProperties
						return false;
					}
					result = temp;
					qttpArray[i] = qtpb.GetTextProps();
				}

				return true;
			}

			return false;

		}
		/// <summary>
		/// Modifys a Selection bassed upon a given delagate.
		/// This modifies TextProperties.
		/// </summary>
		/// <param name="view">
		/// A <see cref="WorldPadView"/>
		/// </param>
		/// <param name="fun">
		/// A <see cref="ModifySelectionPropsDelagate"/>
		/// </param>
		/// <returns>
		/// retuns false if Selection properties couldn't be changed.
		/// </returns>
		public static bool ChangeSelectionProperties(WorldPadView view, ModifySelectionPropsDelagate fun)
		{
			if (view != null && view.GetRootBox() != null && view.GetRootBox().Selection != null)
			{
				view.VwGraphicsGTK.BeginDraw();

				ITsPropsBldr qtpb;
				int cttp;

				// get the current SelectionProps array size from views
				view.GetRootBox().Selection.GetSelectionProps(0, null, null, out cttp);
				ITsTextProps[] qttpArray = new ITsTextProps[cttp];
				IVwPropertyStore[] tempArray = new IVwPropertyStore[cttp];

				// Get the current SelectionProps array
				ArrayPtr nativePtr = MarshalEx.ArrayToNative(qttpArray);
				view.GetRootBox().Selection.GetSelectionProps(cttp, nativePtr , MarshalEx.ArrayToNative(tempArray), out cttp);
				qttpArray = (ITsTextProps[])MarshalEx.NativeToArray(nativePtr, cttp, typeof(ITsTextProps));
				for(int i=0;i<cttp;++i) // for each item in the Selection array set the Font Family
				{
					qtpb = qttpArray[i].GetBldr();
					fun(qtpb); // apply the user suplied delegate.
					qttpArray[i] = qtpb.GetTextProps();
				}

				// set the current Selection Props array
				view.GetRootBox().Selection.SetSelectionProps(cttp, qttpArray);

				view.VwGraphicsGTK.EndDraw();

				return true;
			}

			return false;
		}

		// Check for presence of proper paragraph properties. Return false if neither
		// selection nor paragraph property. Otherwise return true.
		// Port of AfVwRootSite::IsParagraphProps
		public static bool IsParagraphProps(IVwRootBox rootBox, out IVwSelection vwSel, out int hvoText, out int tagText, out VwPropertyStore[] vqvps, out int hvoAnchor, out int ihvoEnd)
		{

			bool fOk;
			int clev; // Count of levels of view objects;
			int cpropPrevious;
			int hvoEnd;
			int tagEnd;
			int cpropPrevEnd;

			vwSel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			hvoAnchor = 0;
			ihvoEnd = 0;

			// Get the selection. Can't do command unless we have one.
			if (rootBox == null)
				return false;

			vwSel = rootBox.Selection;

			if (vwSel == null)
				return false;

			// Commit any outstanding edits.
			fOk = vwSel.Commit();
			if (fOk == false)
				return false;

			// Get selection info. We need a two-level or more selection.
			clev = vwSel.CLevels(false); // Anchor
			if (clev < 2)
				return false;

			clev = vwSel.CLevels(true); // Endpoint
			if (clev < 2)
				return false;

			IVwPropertyStore vps;
			// At this point, we know how to do this command only for structured text paragraphs.
			vwSel.PropInfo(false, 1, out hvoText, out tagText, out hvoAnchor, out cpropPrevious, out vps);

			if (tagText != (int)CellarModuleDefns.kflidStText_Paragraphs)
				return false;

			// And nothing bizarre about other values...
			if (cpropPrevious != 0)
				return false;

			vwSel.PropInfo(true, 1, out hvoEnd, out tagEnd, out ihvoEnd, out cpropPrevEnd, out vps);
			if (tagEnd != tagText || hvoText != hvoEnd || cpropPrevious != cpropPrevEnd)
				return false;

			int cvps;
			vwSel.GetParaProps(0, null, out cvps);

			vqvps = new VwPropertyStore[cvps];
			ArrayPtr ptr = MarshalEx.ArrayToNative(vqvps);

			vwSel.GetParaProps(cvps, ptr, out cvps);
			vqvps = (VwPropertyStore[]) MarshalEx.NativeToArray(ptr, cvps, typeof(VwPropertyStore));

			return true;
		}

		// Get the view selection and paragraph properties. Return false if there is neither a
		// selection nor a paragraph property. Otherwise return true.
		// Port of AfVwRootSite::GetParagraphProps
		public static bool GetParagraphProps(IVwRootBox rootBox, out IVwSelection vwSel, out int hvoText, out int tagText, out VwPropertyStore[] vqvps,
											 out int ihvoFirst, out int ihvoLast, out ISilDataAccess sda, out ITsTextProps[] vqttp)
		{
			int ihvoAnchor, ihvoEnd;
			vqttp = null;
			sda = null;
			ihvoLast = 0;
			ihvoFirst = 0;

			if (!IsParagraphProps(rootBox, out vwSel, out hvoText, out tagText, out vqvps, out ihvoAnchor, out ihvoEnd))
				return false;

			ihvoFirst = ihvoAnchor;
			ihvoLast = ihvoEnd;
			if (ihvoFirst > ihvoLast)
			{
				ihvoFirst = ihvoLast;
				ihvoLast = ihvoAnchor;
			}
			sda = rootBox.DataAccess;
			if (sda == null)
				return true;

			List<ITsTextProps> propList = new List<ITsTextProps>();
			for( int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				ITsTextProps ttp;
				int hvoPara;
				hvoPara = sda.get_VecItem(hvoText, tagText, ihvo);
				object unkTtp;
				unkTtp = sda.get_UnknownProp(hvoPara, (int)CellarModuleDefns.kflidStPara_StyleRules);
				if (unkTtp != null)
				{
					propList.Add((ITsTextProps)unkTtp);
				}
			}

			vqttp = propList.ToArray();

			return true;
		}

		// Allow applying of Paragraph Proerties to the current selection
		public static bool FormatParas(IVwRootBox rootBox,ModifyParagraphPropsDelagate fun)
		{
			return FormatParas(rootBox, fun, false, false, 0, "");
		}


		// Port of AfVwRootSite::FormatParas
		// fCanDoRtl Unused.
		// fOuterRtl Unused.
		// varInt Unused.
		// varString varString Unsued
		protected static bool FormatParas(IVwRootBox rootBox,ModifyParagraphPropsDelagate fun, bool fCanDoRtl, bool fOuterRtl, int varInt, string varString)
		{
			IVwSelection vwsel;
			int hvoText;
			int tagText;
			VwPropertyStore[] vps;
			int ihvoFirst, ihvoLast;
			ISilDataAccess sda;
			ITsTextProps[] ttp;

			// Get the paragraph properties from the selection. If there is neither a selection nor a
			// paragraph property, return false.
			if (!GetParagraphProps(rootBox, out vwsel, out hvoText, out tagText, out vps, out ihvoFirst, out ihvoLast, out sda, out ttp))
				return false;

			// If there are no TsTextProps for the paragraph(s), return true. There is nothing to
			// format
			if (ttp.Length == 0)
				return true;

			int cttp = ttp.Length;

			ITsTextProps[] ttpHard = new ITsTextProps[cttp];
			IVwPropertyStore[] vpsSoft = new IVwPropertyStore[cttp];
			ArrayPtr ptrHard = MarshalEx.ArrayToNative(ttpHard);
			ArrayPtr ptrSoft = MarshalEx.ArrayToNative(vpsSoft);
			vwsel.GetHardAndSoftParaProps(cttp, ttp, ptrHard, ptrSoft, out cttp);
			ttpHard = (ITsTextProps[])MarshalEx.NativeToArray(ptrHard, cttp, typeof(ITsTextProps));
			vpsSoft = (IVwPropertyStore[])MarshalEx.NativeToArray(ptrSoft, cttp, typeof(IVwPropertyStore));

			ILgWritingSystemFactory wsf = new LgWritingSystemFactory();

			{
				ITsPropsBldr tpb;
				for (int ittp = 0; ittp <ttp.Length; ++ittp)
				{
					tpb = null;
					if (ttp[ittp] != null)
						tpb = ttp[ittp].GetBldr();
					else
						tpb = new TsPropsBldr();

					// Apply the user supplied delagate to allow changes to the Properties
					tpb = fun(tpb);

					ttp[ittp] = tpb.GetTextProps();
				}
			}

			// Narrow the range of TsTextProps to only include those that are not NULL.
			int ihvoFirstMod = -1;
			int ihvoLastMod = -1;

			for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				// TODO Finish this section.
				ITsTextProps ttpCurrent;
				ttpCurrent = ttp[ihvo - ihvoFirst];
				IVwPropertyStore vpsSoftCurrent = vpsSoft[ihvo - ihvoFirst];
				if (ttpCurrent != null)
				{
					string strNamedStyle = ttpCurrent.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (strNamedStyle.Length > 0)
					{
						ITsPropsBldr tpbCurrent = ttpCurrent.GetBldr();
						tpbCurrent.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Normal");
						ttpCurrent = tpbCurrent.GetTextProps();
					}

					ihvoLastMod = ihvo;
					if (ihvoFirstMod < 0)
						ihvoFirstMod = ihvo;
					int hvoPara = 0;
					hvoPara = sda.get_VecItem(hvoText, tagText, ihvo);

					ITsTextProps ttpRet;
					// TODO Add Call to RemoveRedudantHardFormatting
					sda.SetUnknown(hvoPara, (int)CellarModuleDefns.kflidStPara_StyleRules, ttpCurrent);
				}

			}

			if (ihvoFirstMod < 0)
				return true;

			// If we modified anything, force redraw by faking a property change.
			// This will destroy the selection, so first, save it

			using (SelectionState tempState = new SelectionState(vwsel))
			{

				int chvoChanged = ihvoLastMod - ihvoFirstMod + 1;
				sda.PropChanged(rootBox, (int)PropChangeType.kpctNotifyMeThenAll, hvoText, tagText, ihvoFirstMod, chvoChanged, chvoChanged);

				tempState.Apply(rootBox);
			}

			return true;

		}

	}
}
