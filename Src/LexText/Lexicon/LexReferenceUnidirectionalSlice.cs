// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using XCore;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// </summary>
	public class LexReferenceUnidirectionalSlice : CustomReferenceVectorSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceUnidirectionalSlice"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "LexReferenceUnidirectionalLauncher gets added to panel's Controls collection and disposed there")]
		public LexReferenceUnidirectionalSlice()
			: base(new LexReferenceUnidirectionalLauncher())
		{
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			base.FinishInit();

			var hvoDisplayParent = XmlUtils.GetMandatoryIntegerAttributeValue(m_configurationNode, "hvoDisplayParent");
			((LexReferenceUnidirectionalLauncher)Control).DisplayParent = hvoDisplayParent != 0
				? m_cache.ServiceLocator.GetObject(hvoDisplayParent) : null;
		}

		#region ILexReferenceSlice Members

		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteFromReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}


		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferenceUnidirectionalLauncher)Control).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}
