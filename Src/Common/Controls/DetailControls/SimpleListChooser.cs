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
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;
// for FwLink (in FdoUiLowLevel assembly)

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// deprecated constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">use null if empty.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			IHelpTopicProvider helpTopicProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel)
			: base(cache, helpTopicProvider, persistProvider, labels, currentObj, fieldName, nullLabel)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">use null if empty.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			IHelpTopicProvider helpTopicProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel, IVwStylesheet stylesheet)
			: base(cache, helpTopicProvider, persistProvider, labels, currentObj, fieldName, nullLabel, stylesheet)
		{
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">use null if emtpy.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			IHelpTopicProvider helpTopicProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName)
			: base(cache, helpTopicProvider, persistProvider, labels, currentObj, fieldName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IHelpTopicProvider helpTopicProvider)
			: base(persistProvider, labels, fieldName, helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IVwStylesheet stylesheet,
			IHelpTopicProvider helpTopicProvider)
			: base(persistProvider, labels, fieldName, stylesheet, helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">use null or ICmObject[0] if empty</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, FdoCache cache,
			IEnumerable<ICmObject> chosenObjs, IHelpTopicProvider helpTopicProvider)
			: base(persistProvider, labels, fieldName, cache, chosenObjs, helpTopicProvider)
		{
		}

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

		/// <summary>
		/// Get tree view of the chooser
		/// </summary>
		public TreeView TreeView
		{
			get { return m_labelsTreeView; }
		}

	}
}
