// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.RenderVerification
{
	/// <summary>
	/// Represents the timing result for a single render operation.
	/// </summary>
	public class RenderTimingResult
	{
		/// <summary>Gets or sets the scenario identifier.</summary>
		public string ScenarioId { get; set; }

		/// <summary>Gets or sets whether this was a cold render.</summary>
		public bool IsColdRender { get; set; }

		/// <summary>Gets or sets the render duration in milliseconds.</summary>
		public double DurationMs { get; set; }

		/// <summary>Gets or sets the timestamp of the render.</summary>
		public DateTime Timestamp { get; set; }
	}

	/// <summary>
	/// Specifies which view constructor pipeline a scenario exercises.
	/// </summary>
	public enum RenderViewType
	{
		/// <summary>Scripture view (StVc / GenericScriptureVc).</summary>
		Scripture,

		/// <summary>Lexical entry view (LexEntryVc with nested senses).</summary>
		LexEntry
	}

	/// <summary>
	/// Represents a render scenario configuration.
	/// </summary>
	public class RenderScenario
	{
		/// <summary>Gets or sets the unique scenario identifier.</summary>
		public string Id { get; set; }

		/// <summary>Gets or sets the human-readable description.</summary>
		public string Description { get; set; }

		/// <summary>Gets or sets the root object HVO for the view.</summary>
		public int RootObjectHvo { get; set; }

		/// <summary>Gets or sets the root field ID.</summary>
		public int RootFlid { get; set; }

		/// <summary>Gets or sets the fragment ID for the view constructor.</summary>
		public int FragmentId { get; set; } = 1;

		/// <summary>Gets or sets the path to the expected snapshot image.</summary>
		public string ExpectedSnapshotPath { get; set; }

		/// <summary>Gets or sets category tags for filtering.</summary>
		public string[] Tags { get; set; } = Array.Empty<string>();

		/// <summary>
		/// Gets or sets the view type (Scripture or LexEntry).
		/// Determines which view constructor pipeline is used for rendering.
		/// </summary>
		public RenderViewType ViewType { get; set; } = RenderViewType.Scripture;

		/// <summary>
		/// Gets or sets whether to simulate the XmlVc ifdata double-render pattern.
		/// Only applies to <see cref="RenderViewType.LexEntry"/> scenarios.
		/// </summary>
		public bool SimulateIfDataDoubleRender { get; set; }
	}
}
