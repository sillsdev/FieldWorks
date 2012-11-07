// --------------------------------------------------------------------------------------------
// Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
//
// File: SemantikcDomainReferenceLauncher.cs
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal class SemanticDomainReferenceLauncher : PossibilityVectorReferenceLauncher
	{
		//#region event handler declarations

		//public event EventHandler ChoicesMade;

		//#endregion event handler declarations

		#region Properties


		#endregion // Properties

		#region Construction, Initialization, and Disposing

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SemanticDomainReferenceLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			//InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		#endregion // Construction, Initialization, and Disposing

		/// <summary>
		/// Handle launching of the standard chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method, if the SimpleListChooser is not suitable.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			string displayWs = "analysis vernacular";
			//string displayWs = "best analysis";
			string postDialogMessageTrigger = null;

			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
				{
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
					postDialogMessageTrigger = XmlUtils.GetAttributeValue(node, "postChangeMessageTrigger", null);
				}
			}
			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid),
				m_displayNameProperty, displayWs);


			using (SemanticDomainsSimpleListChooser chooser = GetSemDomainsChooser(labels))
			{
				var sliceField = XmlUtils.GetAttributeValue(m_configurationNode, "field");

				//make the FwTextBox visible
				chooser.DisplayTextSearchBox = true;
				chooser.DisplayWs = displayWs;

				chooser.Cache = m_cache;
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid); // may set TextParamHvo
				if (m_configurationNode != null)
				{
					// Handle the default case ("owner") for text parameters.

					// This (old approach) works only if
					// all of the list items are owned by the same object as the first one in the
					// list.  (Later elements can be owned by elements owned by that first owner,
					// if you know what I mean.)
					//if (candidates.Count != 0)
					//    chooser.TextParamHvo = m_cache.GetOwnerOfObject((int)candidates[0]);
					// JohnT: this approach depends on a new FDO method.
					ICmObject referenceTargetOwner = m_obj.ReferenceTargetOwner(m_flid);
					if (referenceTargetOwner != null)
						chooser.TextParamHvo = referenceTargetOwner.Hvo;
					chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());
					chooser.InitializeExtras(m_configurationNode, Mediator);
				}

				var res = chooser.ShowDialog(MainControl.FindForm());
				if (DialogResult.Cancel == res)
					return;

				if (m_configurationNode != null)
					chooser.HandleAnyJump();

				if (chooser.ChosenOne != null)
					AddItem(chooser.ChosenOne.Object);
				else if (chooser.ChosenObjects != null)
					SetItems(chooser.ChosenObjects);
			}

			//if the configuration file says that we should put up a message dialog after a change has been made,
			//do that now.
			if (postDialogMessageTrigger != null)
				XCore.XMessageBoxExManager.Trigger(postDialogMessageTrigger);
			// If the configuration file says to refresh the slice list, do that now.
			//if (ChoicesMade != null)
			//    ChoicesMade(this, new EventArgs());
		}

		/// <summary>
		/// Get the SimpleListChooser/
		/// </summary>
		/// <param name="labels">List of objects to show in the chooser.</param>
		/// <returns>The SimpleListChooser.</returns>
		protected SemanticDomainsSimpleListChooser GetSemDomainsChooser(IEnumerable<ObjectLabel> labels)
		{
			SemanticDomainsSimpleListChooser x = new SemanticDomainsSimpleListChooser(m_persistProvider, labels,
																		m_fieldName, m_mediator.HelpTopicProvider);
			x.NullLabel.DisplayName = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}
	}
}
