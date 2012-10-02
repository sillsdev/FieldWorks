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
// File: SimpleListChooser.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
// for FwLink (in FdoUiLowLevel assembly)
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// SimpleListChooser is (now) a trivial override of ReallySimpleListChooser, adding the one bit
	/// of functionality that couldn't be put in the XmlViews project because it depends on DLLs
	/// which XmlViews can't reference.
	/// </summary>
	public class SimpleListChooser : ReallySimpleListChooser
	{
		/// <summary>
		/// Constructor for use with designer
		/// </summary>
		public SimpleListChooser()
			: base()
		{
		}

		/// <summary>
		/// (Deprecated) constructor for use with changing or setting a value
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName)
			: base(persistProvider, labels, currentHvo, fieldName)
		{
		}

		/// <summary>
		/// deprecated constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName, string nullLabel)
			: base(cache, persistProvider, labels, currentHvo, fieldName, nullLabel)
		{
		}

		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName, string nullLabel, IVwStylesheet stylesheet)
			: base(cache, persistProvider, labels, currentHvo, fieldName, nullLabel, stylesheet)
		{
		}
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName)
			: base(cache, persistProvider, labels, currentHvo, fieldName)
		{
		}

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName)
			: base(persistProvider, labels, fieldName)
		{
		}

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public SimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, IVwStylesheet stylesheet)
			: base(persistProvider, labels, fieldName, stylesheet)
		{
		}

		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		/// <param name="cache"></param>
		/// <param name="rghvoChosen">use null or int[0] if empty</param>
		public SimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, FdoCache cache, int[] rghvoChosen)
			: base(persistProvider, labels, fieldName, cache, rghvoChosen)
		{
		}

		/// <summary>
		///
		/// </summary>
		protected override void AddSimpleLink(string sLabel, string sTool, XmlNode node)
		{
			switch (sTool)
			{
				case "MakeInflAffixSlotChooserCommand":
					{
						string sTarget = XmlUtils.GetAttributeValue(node, "target");
						int hvoPos = 0;
						string sTopPOS = DetailControlsStrings.ksQuestionable;
						if (sTarget == null || sTarget.ToLower() == "owner")
						{
							hvoPos = m_hvoTextParam;
							sTopPOS = TextParam;
						}
						else if (sTarget.ToLower() == "toppos")
						{
							hvoPos = GetHvoOfHighestPOS(m_hvoTextParam, out sTopPOS);
						}
						sLabel = String.Format(sLabel, sTopPOS);
						bool fOptional = XmlUtils.GetOptionalBooleanAttributeValue(node, "optional",
							false);
						string sTitle = m_mediator.StringTbl.GetString(
							fOptional ? "OptionalSlot" : "ObligatorySlot",
							"Linguistics/Morphology/TemplateTable");
						AddLink(sLabel, LinkType.kSimpleLink,
							new MakeInflAffixSlotChooserCommand(m_cache, true, sTitle, hvoPos,
							fOptional, m_mediator));
					}
					break;
				default:
					base.AddSimpleLink(sLabel, sTool, node);
					break;
			}
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimpleListChooser));
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).BeginInit();
			this.SuspendLayout();
			//
			// m_labelsTreeView
			//
			this.m_labelsTreeView.LineColor = System.Drawing.Color.Black;
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.Images.SetKeyName(0, "");
			this.m_imageList.Images.SetKeyName(1, "Create Entry.ico");
			//
			// SimpleListChooser
			//
			resources.ApplyResources(this, "$this");
			this.Name = "SimpleListChooser";
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).EndInit();
			this.ResumeLayout(false);

		}
	}
}
