// Copyright (c) 2003-2019 SIL International
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
	internal class VectorReferenceVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;
		protected string m_displayWs;
		private string m_textStyle;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		internal VectorReferenceVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			Reuse(flid, displayNameProperty, displayWs);
		}

		/// <summary>
		/// Set to the same state as if constructed with these arguments. (Cache should not change.)
		/// </summary>
		internal void Reuse(int flid, string displayNameProperty, string displayWs)
		{
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;
		}

		/// <summary>
		/// This is the basic method needed for the view constructor.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case VectorReferenceView.kfragTargetVector:
					// Check for an empty vector.
					if (hvo == 0 || m_cache.DomainDataByFlid.get_VecSize(hvo, m_flid) == 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, 18000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
						if (hvo != 0)
						{
							vwenv.NoteDependency(new[] { hvo }, new[] { m_flid }, 1);
						}
					}
					else
					{
						if (!string.IsNullOrEmpty(TextStyle))
						{
							vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
						}
						vwenv.OpenParagraph();
						vwenv.AddObjVec(m_flid, this, frag);
						vwenv.CloseParagraph();
					}
					break;
				case VectorReferenceView.kfragTargetObj:
					// Display one object from the vector.
					{
						var wsf = m_cache.WritingSystemFactory;
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
						ITsString tss;
						Debug.Assert(hvo != 0);
#if USEBESTWS
					if (m_displayWs != null && m_displayWs.StartsWith("best"))
					{
						// The flid can be a variety of types, so deal with those.
						Debug.WriteLine("Using 'best ws': " + m_displayWs);
						int magicWsId = LgWritingSystem.GetMagicWsIdFromName(m_displayWs);
						int actualWS = m_cache.LanguageProject.ActualWs(magicWsId, hvo, m_flid);
						Debug.WriteLine("Actual ws: " + actualWS.ToString());
					}
					else
					{
#endif
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
								var s = pi.GetValue(obj, null);
								if (s is ITsString)
								{
									tss = (ITsString)s;
								}
								else
								{
									tss = TsStringUtils.MakeString((string)s, ws);
								}
							}
							else
							{
								// ShortNameTss sometimes gets PropChanged, so worth letting the view know that's
								// what we're inserting.
								var flid = Cache.MetaDataCacheAccessor.GetFieldId2(obj.ClassID, "ShortNameTSS", true);
								vwenv.AddStringProp(flid, this);
								break;
							}
#if USEBESTWS
						}
#endif
						}
						if (!string.IsNullOrEmpty(TextStyle))
						{
							vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
						}
						vwenv.AddString(tss);
					}
					break;
				default:
					throw new ArgumentException("Don't know what to do with the given frag.", nameof(frag));
			}
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			var da = vwenv.DataAccess;
			var count = da.get_VecSize(hvo, tag);
			for (var i = 0; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this, VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
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