// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class AtomicReferenceVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;
		private string m_textStyle;

		public AtomicReferenceVc(LcmCache cache, int flid, string displayNameProperty)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AtomicReferenceView.kFragAtomicRef:
					// Display a paragraph with a single item.
					var hvoProp = HvoOfObjectToDisplay(vwenv, hvo);
					if (hvoProp == 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, 18000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
						vwenv.NoteDependency(new int[] {hvo}, new int[] {m_flid}, 1);
					}
					else
					{
						vwenv.OpenParagraph();		// vwenv.OpenMappedPara();
						DisplayObjectProperty(vwenv, hvoProp);
						vwenv.CloseParagraph();
					}
					break;
				case AtomicReferenceView.kFragObjName:
				{
					// Display one reference.
					var wsf = m_cache.WritingSystemFactory;
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
					ITsString tss;
					Debug.Assert(hvo != 0);
					// Use reflection to get a prebuilt name if we can.  Otherwise
					// settle for piecing together a string.
					Debug.Assert(m_cache != null);
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					Debug.Assert(obj != null);
					var type = obj.GetType();
					var pi = type.GetProperty("TsName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
					if (pi != null)
					{
						tss = (ITsString)pi.GetValue(obj, null);
					}
					else
					{
						if (!string.IsNullOrEmpty(m_displayNameProperty))
						{
							pi = type.GetProperty(m_displayNameProperty, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
						}
						var ws = wsf.GetWsFromStr(obj.SortKeyWs);
						if (ws == 0)
						{
							ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
						}
						if (pi != null)
						{
							var info = pi.GetValue(obj, null);
							// handle the object type
							if (info is string)
							{
								tss = TsStringUtils.MakeString((string)info, ws);
							}
							else if (info is IMultiUnicode)
							{
								var accessor = info as IMultiUnicode;
								tss = accessor.get_String(ws); // try the requested one (or default analysis)
								if (tss == null || tss.Length == 0)
								{
									tss = accessor.BestAnalysisVernacularAlternative; // get something
								}
							}
							else
							{
								tss = info is ITsString ? (ITsString)info : null;
							}
						}
						else
						{
							tss = obj.ShortNameTSS; // prefer this, which is hopefully smart about wss.
							if (tss == null || tss.Length == 0)
							{
								tss = TsStringUtils.MakeString(obj.ShortName, ws);
							}
						}
					}
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);

					}
					vwenv.AddString(tss);
				}
					break;
				default:
					throw new ArgumentException(@"Don't know what to do with the given frag.", nameof(frag));
			}
		}

		protected virtual void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjProp(m_flid, this, AtomicReferenceView.kFragObjName);
		}

		protected virtual int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			return vwenv.DataAccess.get_ObjectProp(hvo, m_flid);
		}
		public string TextStyle
		{
			get
			{
				var sTextStyle = "Default Paragraph Characters";
				if (!string.IsNullOrEmpty(m_textStyle))
				{
					sTextStyle = m_textStyle;
				}
				return sTextStyle;
			}
			set
			{
				m_textStyle = value;
			}
		}
	}
}