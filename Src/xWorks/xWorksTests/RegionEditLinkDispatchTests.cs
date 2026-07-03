// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// GAP 1 / B7 — the host's link dispatch: a chooser jump link translates to EXACTLY the legacy
	/// chooser's <c>FwLinkArgs</c> (<c>ReallySimpleListChooser.cs:900</c>:
	/// <c>new FwLinkArgs(sTool, m_guidLink)</c>, with <c>m_guidLink == Guid.Empty</c> unless a
	/// flidTextParam resolved a target). The mediator hop itself
	/// (<c>m_mediator.PostMessage("FollowLink", …)</c>, ReallySimpleListChooser.cs:1657) is one
	/// obsolete-API call inside <c>RecordEditView.OnRegionLinkRequested</c> and needs a live
	/// xCore mediator + FwXWindow to observe; it is exercised by the manual/UIA lanes, so the unit
	/// seam here is the translation the message carries.
	/// </summary>
	[TestFixture]
	public class RegionEditLinkDispatchTests
	{
		private static RegionLinkRequest Request(string tool, string targetGuid = null)
			=> new RegionLinkRequest(
				new LexicalEditRegionField("LexEntry/Normal/#0@1", "Publish Entry In", "PublishIn",
					null, RegionFieldKind.ReferenceVector, EditorClassification.Known, "PublishIn",
					null, SurfaceRouting.Inherit, null, null, null),
				new RegionChooserLink("Edit the Publications list", tool, targetGuid));

		[Test]
		public void BuildFollowLinkArgs_PlainToolJump_UsesGuidEmpty_LikeTheLegacyChooser()
		{
			var args = RecordEditView.BuildFollowLinkArgs(Request("publicationsEdit"));

			Assert.That(args.ToolName, Is.EqualTo("publicationsEdit"));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty),
				"no target on the link — the legacy m_guidLink default");
		}

		[Test]
		public void BuildFollowLinkArgs_WithATargetGuid_ParsesIt()
		{
			var guid = Guid.NewGuid();
			var args = RecordEditView.BuildFollowLinkArgs(Request("posEdit", guid.ToString()));

			Assert.That(args.ToolName, Is.EqualTo("posEdit"));
			Assert.That(args.TargetGuid, Is.EqualTo(guid));
		}

		[Test]
		public void BuildFollowLinkArgs_GarbageTarget_FallsBackToGuidEmpty()
		{
			var args = RecordEditView.BuildFollowLinkArgs(Request("publicationsEdit", "not-a-guid"));

			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
		}
	}
}
