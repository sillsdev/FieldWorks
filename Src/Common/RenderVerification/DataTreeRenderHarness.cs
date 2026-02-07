// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.RenderVerification
{
	/// <summary>
	/// Harness for rendering a full DataTree (the lex entry edit view with all WinForms chrome)
	/// and capturing composite bitmaps that include grey labels, icons, expand/collapse buttons,
	/// writing system indicators, section headers, and Views engine text content.
	/// </summary>
	/// <remarks>
	/// The DataTree is the real FLEx control that composes Slices from layout XML.
	/// This harness creates one programmatically, populates it via ShowObject(), and
	/// captures the result using CompositeViewCapture (DrawToBitmap + VwDrawRootBuffered overlay).
	/// </remarks>
	public class DataTreeRenderHarness : IDisposable
	{
		private readonly LcmCache m_cache;
		private readonly ICmObject m_rootObject;
		private readonly string m_layoutName;

		private DataTree m_dataTree;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private Form m_hostForm;
		private Inventory m_layoutInventory;
		private Inventory m_partInventory;
		private bool m_disposed;

		/// <summary>
		/// Gets the number of slices populated by ShowObject.
		/// </summary>
		public int SliceCount => m_dataTree?.Slices?.Count ?? 0;

		/// <summary>
		/// Gets the DataTree control for inspection.
		/// </summary>
		public DataTree DataTree => m_dataTree;

		/// <summary>
		/// Gets the last captured bitmap.
		/// </summary>
		public Bitmap LastCapture { get; private set; }

		/// <summary>
		/// Gets timing information from the last population.
		/// </summary>
		public DataTreeTimingInfo LastTiming { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DataTreeRenderHarness"/> class.
		/// </summary>
		/// <param name="cache">The LCM data cache.</param>
		/// <param name="rootObject">The root object to display (e.g. an ILexEntry).</param>
		/// <param name="layoutName">The layout name (e.g. "Normal").</param>
		public DataTreeRenderHarness(LcmCache cache, ICmObject rootObject, string layoutName = "Normal")
		{
			m_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			m_rootObject = rootObject ?? throw new ArgumentNullException(nameof(rootObject));
			m_layoutName = layoutName ?? "Normal";
		}

		/// <summary>
		/// Initializes the DataTree and populates it with slices for the root object.
		/// This is the equivalent of what FLEx does when you navigate to a lex entry.
		/// </summary>
		/// <param name="width">Width of the host form in pixels.</param>
		/// <param name="height">Height of the host form in pixels.</param>
		/// <param name="useProductionLayouts">If true, loads production layout XML from DistFiles.
		/// If false, uses test layouts from DetailControlsTests (simpler but less realistic).</param>
		public void PopulateSlices(int width = 1024, int height = 768, bool useProductionLayouts = true)
		{
			DisposeResources();

			var stopwatch = Stopwatch.StartNew();

			// Create XCore infrastructure required by DataTree
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);

			// Create the DataTree
			m_dataTree = new DataTree();
			m_dataTree.Init(m_mediator, m_propertyTable, null);

			// Host in a form for proper layout context. The form is shown offscreen
			// (Opacity=0) after ShowObject to trigger the full slice lifecycle:
			// OnPaint → HandleLayout1(fFull=false) → MakeSliceVisible → handle
			// creation → MakeRoot → VwRootBox creation.
			m_hostForm = new Form
			{
				FormBorderStyle = FormBorderStyle.None,
				ShowInTaskbar = false,
				ClientSize = new Size(width, height),
				StartPosition = FormStartPosition.Manual,
				Location = new Point(-2000, -2000) // offscreen
			};
			m_hostForm.Controls.Add(m_dataTree);
			m_dataTree.Dock = DockStyle.Fill;

			// Load layout inventories
			if (useProductionLayouts)
			{
				LoadProductionInventories();
			}
			else
			{
				LoadTestInventories();
			}

			// Set up the stylesheet
			var ss = new SIL.LCModel.DomainServices.LcmStyleSheet();
			ss.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles);
			m_dataTree.StyleSheet = ss;

			// Strip layout parts that cause native crashes or managed exceptions in
			// test context. Must be done after inventories are loaded but before
			// ShowObject, which reads from the layout inventory.
			if (useProductionLayouts)
			{
				StripProblematicLayoutParts();
			}

			// Initialize the DataTree with cache and inventories
			m_dataTree.Initialize(m_cache, false, m_layoutInventory, m_partInventory);

			// Create the form handle so controls can paint.
			m_hostForm.CreateControl();

			var initMs = stopwatch.Elapsed.TotalMilliseconds;

			// Populate the slices (this is the expensive operation we want to benchmark).
			// After ShowObject, slices exist but are Visible=false because CreateSlices
			// checks wasVisible = this.Visible at the start, and since the form isn't
			// shown, wasVisible is false and Show() is skipped at the end.
			var populateStopwatch = Stopwatch.StartNew();
			try
			{
				m_dataTree.ShowObject(m_rootObject, m_layoutName, null, m_rootObject, true);
			}
			catch (ApplicationException ex)
			{
				// Even after stripping known problematic parts, other parts may fail
				// due to missing test infrastructure. DataTree creates slices as it
				// encounters them, so the ones before the failure are still usable.
				Trace.TraceWarning(
					$"[DataTreeRenderHarness] ShowObject partially failed (continuing with " +
					$"{m_dataTree.Slices?.Count ?? 0} slices already created): {ex.Message}");
			}
			populateStopwatch.Stop();

			// Show the form to trigger the full WinForms lifecycle:
			// OnLayout → HandleLayout1 positions slices but does NOT make them visible
			// (fFull=true path). Only OnPaint → HandleLayout1(fFull=false) makes slices
			// visible. So we need to:
			// 1. Show the form (with Opacity=0 to avoid flicker)
			// 2. Make the DataTree visible (CreateSlices called Hide() on it)
			// 3. Pump paint messages so OnPaint fires
			m_hostForm.Opacity = 0;
			m_hostForm.Show();
			m_dataTree.Visible = true;
			m_dataTree.Invalidate();
			System.Windows.Forms.Application.DoEvents();

			stopwatch.Stop();

			LastTiming = new DataTreeTimingInfo
			{
				InitializationMs = initMs,
				PopulateSlicesMs = populateStopwatch.Elapsed.TotalMilliseconds,
				TotalMs = stopwatch.Elapsed.TotalMilliseconds,
				SliceCount = m_dataTree.Slices?.Count ?? 0,
				Timestamp = DateTime.UtcNow
			};

			// Collect slice diagnostics for debugging
			if (m_dataTree.Slices != null)
			{
				for (int i = 0; i < m_dataTree.Slices.Count; i++)
				{
					var slice = m_dataTree.Slices[i];
					var diag = new SliceDiagnosticInfo
					{
						Index = i,
						TypeName = slice.GetType().Name,
						Label = slice.Label ?? "(null)",
						Bounds = new Rectangle(slice.Location, slice.Size),
						Visible = slice.Visible,
					};
					// Check if it's a ViewSlice with a RootBox
					var viewSlice = slice as ViewSlice;
					if (viewSlice != null)
					{
						try { diag.HasRootBox = viewSlice.RootSite?.RootBox != null; }
						catch { diag.HasRootBox = false; }
					}
					LastTiming.SliceDiagnostics.Add(diag);
				}
			}
		}

		/// <summary>
		/// Captures the DataTree as a composite bitmap. WinForms chrome (grey labels, icons,
		/// section headers, separators) is captured via DrawToBitmap; Views engine content
		/// inside ViewSlices is overlaid via VwDrawRootBuffered for each RootSite.
		/// </summary>
		/// <returns>The composite bitmap, or null if capture failed.</returns>
		public Bitmap CaptureCompositeBitmap()
		{
			if (m_dataTree == null)
				throw new InvalidOperationException("Call PopulateSlices before capturing.");

			try
			{
				var bitmap = CompositeViewCapture.CaptureDataTree(m_dataTree);
				LastCapture = bitmap;
				return bitmap;
			}
			catch (Exception ex)
			{
				Trace.TraceWarning($"[DataTreeRenderHarness] Composite capture failed: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Saves the last captured bitmap to the specified path.
		/// </summary>
		public void SaveCapture(string outputPath, ImageFormat format = null)
		{
			if (LastCapture == null)
				throw new InvalidOperationException("No capture available. Call CaptureCompositeBitmap first.");

			var directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			LastCapture.Save(outputPath, format ?? ImageFormat.Png);
		}

		#region Layout Inventory Loading

		/// <summary>
		/// Removes part refs from the loaded layout inventory that reference slices with
		/// heavy external dependencies unavailable in test context. This prevents
		/// ProcessSubpartNode from throwing ApplicationException which would kill the
		/// entire ApplyLayout loop and lose all subsequent parts.
		/// </summary>
		/// <remarks>
		/// Known problematic parts:
		/// - "Etymologies" → SummarySlice → SummaryXmlView → native COM VwRootBox creation
		///   that crashes the test host with unrecoverable native exceptions.
		/// - "Messages" → MessageSlice (LexEdDll.dll) → ChorusSystem → L10NSharp.
		///   Throws managed ApplicationException.
		/// - "Senses", Section parts (VariantFormsSection, AlternateFormsSection,
		///   GrammaticalFunctionsSection, PublicationSection) → Create complex slice
		///   hierarchies with DynamicLoader, native COM Views, and expanding sections.
		///   These crash the test host with unhandled native exceptions in test context
		///   because the full FLEx COM infrastructure isn't initialized.
		/// </remarks>
		private void StripProblematicLayoutParts()
		{
			// Parts that crash or throw in test context. This includes all parts that
			// come after and including Messages in the Normal layout, since they involve
			// complex slice types (Senses with recursive sub-slices, expanding sections
			// with menus, DynamicLoader-loaded custom slices, etc.).
			var problematicPartRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Etymologies",           // SummarySlice → SummaryXmlView → native COM VwRootBox
				"Messages",              // L10NSharp/Chorus dependency
				"Senses",               // Recursive sense expansion → native COM crash
				"VariantFormsSection",   // Expanding section → native crash
				"AlternateFormsSection", // Expanding section → native crash
				"GrammaticalFunctionsSection", // Expanding section → native crash
				"PublicationSection"     // Expanding section → native crash
			};

			// Get the LexEntry detail Normal layout node from the inventory
			XmlNode layout = m_layoutInventory.GetElement("layout",
				new[] { "LexEntry", "detail", m_layoutName });
			if (layout == null)
				return;

			var toRemove = new List<XmlNode>();
			foreach (XmlNode child in layout.ChildNodes)
			{
				if (child.NodeType != XmlNodeType.Element || child.Name != "part")
					continue;
				string refAttr = child.Attributes?["ref"]?.Value;
				if (refAttr != null && problematicPartRefs.Contains(refAttr))
				{
					toRemove.Add(child);
				}
			}

			foreach (var node in toRemove)
			{
				Trace.TraceInformation(
					$"[DataTreeRenderHarness] Stripping problematic part ref=\"{node.Attributes?["ref"]?.Value}\" " +
					$"from {m_layoutName} layout (external dependency not available in test context).");
				layout.RemoveChild(node);
			}
		}

		private void LoadProductionInventories()
		{
			// The production path: DistFiles/Language Explorer/Configuration/Parts/
			string partDirectory = Path.Combine(FwDirectoryFinder.FlexFolder,
				Path.Combine("Configuration", "Parts"));

			if (!Directory.Exists(partDirectory))
			{
				throw new DirectoryNotFoundException(
					$"Production layout directory not found: {partDirectory}. " +
					$"FwDirectoryFinder.FlexFolder = {FwDirectoryFinder.FlexFolder}");
			}

			// Layout inventory: keyed by class+type+name, group label, part ref
			var layoutKeyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "name" },
				["group"] = new[] { "label" },
				["part"] = new[] { "ref" }
			};
			m_layoutInventory = new Inventory(new[] { partDirectory },
				"*.fwlayout", "/LayoutInventory/*", layoutKeyAttrs,
				"RenderVerification", Path.GetTempPath());

			// Parts inventory: keyed by part id
			var partKeyAttrs = new Dictionary<string, string[]>
			{
				["part"] = new[] { "id" }
			};
			m_partInventory = new Inventory(new[] { partDirectory },
				"*Parts.xml", "/PartInventory/bin/*", partKeyAttrs,
				"RenderVerification", Path.GetTempPath());
		}

		private void LoadTestInventories()
		{
			// Same pattern as DataTreeTests — load from DetailControlsTests test XML
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory,
				@"Common/Controls/DetailControls/DetailControlsTests");

			var layoutKeyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "name" },
				["group"] = new[] { "label" },
				["part"] = new[] { "ref" }
			};
			m_layoutInventory = new Inventory(new[] { partDirectory },
				"*.fwlayout", "/LayoutInventory/*", layoutKeyAttrs,
				"RenderVerification", Path.GetTempPath());

			var partKeyAttrs = new Dictionary<string, string[]>
			{
				["part"] = new[] { "id" }
			};
			m_partInventory = new Inventory(new[] { partDirectory },
				"*Parts.xml", "/PartInventory/bin/*", partKeyAttrs,
				"RenderVerification", Path.GetTempPath());
		}

		#endregion

		#region Dispose

		private void DisposeResources()
		{
			if (m_dataTree != null)
			{
				// DataTree gets disposed by form.Close since it's in Controls
				m_dataTree = null;
			}

			if (m_hostForm != null)
			{
				m_hostForm.Close();
				m_hostForm.Dispose();
				m_hostForm = null;
			}

			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}

			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
		}

		/// <summary>Releases all resources used by the harness.</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Releases the unmanaged resources and optionally releases the managed resources.</summary>
		protected virtual void Dispose(bool disposing)
		{
			if (m_disposed) return;

			if (disposing)
			{
				DisposeResources();
				LastCapture?.Dispose();
				LastCapture = null;
			}

			m_disposed = true;
		}

		#endregion
	}

	/// <summary>
	/// Timing information for a DataTree population operation.
	/// </summary>
	public class DataTreeTimingInfo
	{
		/// <summary>Time to create DataTree, Mediator, load inventories, and create form.</summary>
		public double InitializationMs { get; set; }

		/// <summary>Time for ShowObject (slice creation and layout).</summary>
		public double PopulateSlicesMs { get; set; }

		/// <summary>Total wall-clock time including initialization and population.</summary>
		public double TotalMs { get; set; }

		/// <summary>Number of slices created.</summary>
		public int SliceCount { get; set; }

		/// <summary>Timestamp of the operation.</summary>
		public DateTime Timestamp { get; set; }

		/// <summary>Diagnostic information about each slice.</summary>
		public List<SliceDiagnosticInfo> SliceDiagnostics { get; set; } = new List<SliceDiagnosticInfo>();
	}

	/// <summary>
	/// Diagnostic information about a single slice for debugging render capture issues.
	/// </summary>
	public class SliceDiagnosticInfo
	{
		/// <summary>Zero-based index.</summary>
		public int Index { get; set; }
		/// <summary>Concrete type name (e.g. ViewSlice, MultiStringSlice).</summary>
		public string TypeName { get; set; }
		/// <summary>Grey label text.</summary>
		public string Label { get; set; }
		/// <summary>Bounds in DataTree coordinates.</summary>
		public Rectangle Bounds { get; set; }
		/// <summary>Whether the slice is visible.</summary>
		public bool Visible { get; set; }
		/// <summary>Whether the slice is a ViewSlice with a RootBox.</summary>
		public bool HasRootBox { get; set; }
	}
}
