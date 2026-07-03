// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>One named XML source file (layout or parts inventory) for a coverage run.</summary>
	public sealed class LayoutSourceFile
	{
		public LayoutSourceFile(string fileName, XElement root)
		{
			FileName = fileName;
			Root = root;
		}

		public string FileName { get; }

		public XElement Root { get; }
	}

	/// <summary>
	/// Measures how much of the shipped XML Parts/Layout vocabulary the <see cref="XmlLayoutImporter"/>
	/// actually consumes (task 4.9). Two lenses:
	/// 1. A raw vocabulary census over the source files: every element and attribute occurrence,
	///    classified handled/unhandled from the importer's published attribute sets.
	/// 2. An import run over every detail layout, aggregating the importer's diagnostics by code.
	/// The result renders to a deterministic markdown report so coverage is a tracked number, not an
	/// assumption. This gates 7.x scaling and 9.x XML-retirement claims.
	/// </summary>
	public static class LayoutImportCoverage
	{
		// B3: if/ifnot/choice/where/otherwise import as typed Conditional/ChoiceGroup nodes.
		private static readonly HashSet<string> HandledLayoutFileElements =
			new HashSet<string>(StringComparer.Ordinal)
			{
				"LayoutInventory", "layoutType", "layout", "part", "indent",
				"if", "ifnot", "choice", "where", "otherwise"
			};

		// B7: chooserLink imports as typed metadata; chooserInfo is handled as the link container
		// (its OTHER attributes — title/text/guicontrol/… — stay measured as unhandled below).
		private static readonly HashSet<string> HandledPartsFileElements =
			new HashSet<string>(StringComparer.Ordinal)
			{
				"PartInventory", "bin", "part", "slice", "seq", "obj",
				"if", "ifnot", "choice", "where", "otherwise",
				"chooserInfo", "chooserLink"
			};

		/// <summary>
		/// Runs the census and the import pass and returns the aggregated result. The optional
		/// <paramref name="baseClassMap"/> (subclass → base class, see <see cref="BuildBaseClassMap"/>)
		/// mirrors the metadata-driven class-hierarchy walk the production compile lane threads in from
		/// the MDC (<c>FullEntryRegionComposer.CompileForClass</c>), so the measured unresolved-part
		/// count reflects what production resolution actually drops.
		/// </summary>
		public static LayoutImportCoverageReport Run(
			IReadOnlyList<LayoutSourceFile> layoutFiles,
			IReadOnlyList<LayoutSourceFile> partsFiles,
			IReadOnlyDictionary<string, string> baseClassMap = null)
		{
			var report = new LayoutImportCoverageReport();

			foreach (var file in layoutFiles)
			{
				CensusFile(file, isPartsFile: false, report);
			}

			foreach (var file in partsFiles)
			{
				CensusFile(file, isPartsFile: true, report);
			}

			// Merge every parts inventory into one resolver so cross-class part refs resolve.
			var mergedParts = new XElement("PartInventory", partsFiles.Select(f => f.Root));
			var resolver = new DictionaryPartResolver(mergedParts, baseClassMap);
			var importer = new XmlLayoutImporter();

			foreach (var file in layoutFiles)
			{
				foreach (var layout in file.Root.Descendants("layout"))
				{
					if (((string)layout.Attribute("type") ?? "") != "detail")
					{
						report.NonDetailLayoutsSkipped++;
						continue;
					}

					var model = importer.Import(layout, resolver);
					report.DetailLayoutsImported++;
					report.NodesProduced += CountNodes(model.Roots);
					foreach (var diag in model.Diagnostics)
					{
						report.CountDiagnostic(diag);
					}
				}
			}

			return report;
		}

		/// <summary>
		/// Builds the subclass → base class map from the LCModel master model XML
		/// (<c>&lt;EntireModel&gt;&lt;CellarModule&gt;&lt;class id=... base=...&gt;</c>). This is the same
		/// hierarchy the legacy detail lane walks via <c>IFwMetaDataCache.GetBaseClsId</c>
		/// (<c>DataTree.cs:2444-2461</c>), derived from metadata instead of a hand-maintained list.
		/// </summary>
		public static IReadOnlyDictionary<string, string> BuildBaseClassMap(XElement masterModel)
		{
			var map = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var cls in masterModel.Descendants("class"))
			{
				var id = (string)cls.Attribute("id");
				var baseClass = (string)cls.Attribute("base");
				if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(baseClass) && id != baseClass)
				{
					map[id] = baseClass;
				}
			}

			return map;
		}

		private static void CensusFile(LayoutSourceFile file, bool isPartsFile, LayoutImportCoverageReport report)
		{
			var handledElements = isPartsFile ? HandledPartsFileElements : HandledLayoutFileElements;
			foreach (var el in file.Root.DescendantsAndSelf())
			{
				var elementName = el.Name.LocalName;
				var elementHandled = handledElements.Contains(elementName);
				report.CountElement(elementName, elementHandled);

				foreach (var attr in el.Attributes())
				{
					var handled = elementHandled && IsAttributeHandled(elementName, attr.Name.LocalName, isPartsFile);
					report.CountAttribute(elementName, attr.Name.LocalName, handled);
				}
			}
		}

		private static bool IsAttributeHandled(string elementName, string attributeName, bool isPartsFile)
		{
			switch (elementName)
			{
				case "LayoutInventory":
				case "PartInventory":
				case "layoutType":
					return true; // inventory wrappers: attributes carry no per-node semantics to import
				case "bin":
					return attributeName == "class";
				case "layout":
					return XmlLayoutImporter.HandledLayoutAttributes.Contains(attributeName);
				case "part":
					// In a parts file the <part> definition carries an id; in a layout it is a caller ref.
					return isPartsFile
						? attributeName == "id" || XmlLayoutImporter.HandledCallerPartAttributes.Contains(attributeName)
						: XmlLayoutImporter.HandledCallerPartAttributes.Contains(attributeName);
				case "indent":
					return attributeName == "indent";
				case "slice":
					return XmlLayoutImporter.HandledSliceAttributes.Contains(attributeName);
				case "seq":
				case "obj":
					return XmlLayoutImporter.HandledObjSeqAttributes.Contains(attributeName);
				case "if":
				case "ifnot":
				case "where":
					// B3: the parsed condition vocabulary; publishing-only condition forms
					// (stringaltequals etc.) stay measured as unhandled.
					return XmlLayoutImporter.HandledConditionAttributes.Contains(attributeName);
				case "chooserLink":
					// B7: the typed jump-link vocabulary.
					return XmlLayoutImporter.HandledChooserLinkAttributes.Contains(attributeName);
				case "chooserInfo":
					// B7 remainder: only the chooserLink children import; chooserInfo's own
					// attributes (title/text/textparam/flidTextParam/guicontrol/helpBrowser) do not.
					return false;
				case "choice":
				case "otherwise":
					return false; // these carry no attributes in the shipped files
				default:
					return false;
			}
		}

		private static int CountNodes(IReadOnlyList<ViewNode> nodes)
		{
			var count = 0;
			foreach (var node in nodes)
			{
				count += 1 + CountNodes(node.Children);
			}

			return count;
		}
	}

	/// <summary>Aggregated coverage numbers; renders to deterministic markdown.</summary>
	public sealed class LayoutImportCoverageReport
	{
		private readonly SortedDictionary<string, int> _handledElements = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private readonly SortedDictionary<string, int> _unhandledElements = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private readonly SortedDictionary<string, int> _handledAttributes = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private readonly SortedDictionary<string, int> _unhandledAttributes = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private readonly SortedDictionary<string, int> _diagnosticsByCode = new SortedDictionary<string, int>(StringComparer.Ordinal);
		private readonly SortedDictionary<string, int> _unresolvedPartRefs = new SortedDictionary<string, int>(StringComparer.Ordinal);

		private static readonly System.Text.RegularExpressions.Regex UnresolvedPartMessage =
			new System.Text.RegularExpressions.Regex("part ref '(?<ref>[^']*)' for class '(?<class>[^']*)'");

		public int DetailLayoutsImported { get; internal set; }

		public int NonDetailLayoutsSkipped { get; internal set; }

		public int NodesProduced { get; internal set; }

		public IReadOnlyDictionary<string, int> DiagnosticsByCode => _diagnosticsByCode;

		/// <summary>Distinct unresolved part identities (<c>{class}-{ref}</c>) with occurrence counts.</summary>
		public IReadOnlyDictionary<string, int> UnresolvedPartRefs => _unresolvedPartRefs;

		public IReadOnlyDictionary<string, int> UnhandledAttributes => _unhandledAttributes;

		public IReadOnlyDictionary<string, int> UnhandledElements => _unhandledElements;

		public int HandledElementOccurrences => _handledElements.Values.Sum();

		public int UnhandledElementOccurrences => _unhandledElements.Values.Sum();

		public int HandledAttributeOccurrences => _handledAttributes.Values.Sum();

		public int UnhandledAttributeOccurrences => _unhandledAttributes.Values.Sum();

		public double ElementCoveragePercent => Percent(HandledElementOccurrences, UnhandledElementOccurrences);

		public double AttributeCoveragePercent => Percent(HandledAttributeOccurrences, UnhandledAttributeOccurrences);

		internal void CountElement(string elementName, bool handled)
			=> Increment(handled ? _handledElements : _unhandledElements, elementName);

		internal void CountAttribute(string elementName, string attributeName, bool handled)
			=> Increment(handled ? _handledAttributes : _unhandledAttributes, $"{elementName}@{attributeName}");

		internal void CountDiagnostic(ViewDiagnostic diagnostic)
		{
			Increment(_diagnosticsByCode, $"{diagnostic.Code} ({diagnostic.Severity})");
			if (diagnostic.Code == "unresolved-part")
			{
				var match = UnresolvedPartMessage.Match(diagnostic.Message);
				Increment(_unresolvedPartRefs, match.Success
					? $"{match.Groups["class"].Value}-{match.Groups["ref"].Value}"
					: diagnostic.Message);
			}
		}

		private static void Increment(SortedDictionary<string, int> map, string key)
		{
			map.TryGetValue(key, out var current);
			map[key] = current + 1;
		}

		private static double Percent(int handled, int unhandled)
		{
			var total = handled + unhandled;
			return total == 0 ? 100.0 : 100.0 * handled / total;
		}

		/// <summary>Renders the report as deterministic markdown (no timestamps, sorted tables).</summary>
		public string ToMarkdown()
		{
			var sb = new StringBuilder();
			sb.AppendLine("# XML Layout Import Coverage Report");
			sb.AppendLine();
			sb.AppendLine("Generated by `LayoutImportCoverageTests` over the shipped layout/parts files under");
			sb.AppendLine("`DistFiles/Language Explorer/Configuration/Parts/` (task 4.9). Regenerate by running that test;");
			sb.AppendLine("the content is deterministic. Coverage percentages count *occurrences* in the shipped files,");
			sb.AppendLine("so they weight the vocabulary by how often real layouts use it.");
			sb.AppendLine();
			sb.AppendLine("## Summary");
			sb.AppendLine();
			sb.AppendLine($"- Detail layouts imported: **{DetailLayoutsImported}** (non-detail layouts skipped: {NonDetailLayoutsSkipped})");
			sb.AppendLine($"- Typed nodes produced: **{NodesProduced}**");
			sb.AppendLine($"- Element occurrence coverage: **{ElementCoveragePercent:F1}%** ({HandledElementOccurrences} handled / {UnhandledElementOccurrences} unhandled)");
			sb.AppendLine($"- Attribute occurrence coverage: **{AttributeCoveragePercent:F1}%** ({HandledAttributeOccurrences} handled / {UnhandledAttributeOccurrences} unhandled)");
			sb.AppendLine();
			AppendTable(sb, "Import diagnostics by code", "Code (severity)", _diagnosticsByCode);
			AppendTable(sb, "Unresolved part refs (B10)", "Class-Ref", _unresolvedPartRefs);
			AppendTable(sb, "Unhandled elements (census)", "Element", _unhandledElements);
			AppendTable(sb, "Unhandled attributes (census)", "Element@Attribute", _unhandledAttributes);
			// Exactly one trailing newline: the repo whitespace check rejects blank lines at EOF.
			return sb.ToString().TrimEnd('\r', '\n') + "\n";
		}

		private static void AppendTable(StringBuilder sb, string title, string keyHeader, SortedDictionary<string, int> map)
		{
			sb.AppendLine($"## {title}");
			sb.AppendLine();
			if (map.Count == 0)
			{
				sb.AppendLine("(none)");
				sb.AppendLine();
				return;
			}

			sb.AppendLine($"| {keyHeader} | Count |");
			sb.AppendLine("|---|---|");
			foreach (var pair in map.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
			{
				sb.AppendLine($"| `{pair.Key}` | {pair.Value} |");
			}

			sb.AppendLine();
		}
	}
}
