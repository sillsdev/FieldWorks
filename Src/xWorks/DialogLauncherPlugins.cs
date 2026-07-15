// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D4 — the generic dialog-launcher plugin: a claimed slice
	/// renders as an Avalonia value row (<see cref="FwDialogLauncherField"/>: read-only value text
	/// + "..." button) whose button calls the host-injected <see cref="ILegacyDialogLauncher"/>
	/// seam with the row's (object, node). The pane stays WinForms-free; the WinForms dialog is
	/// the sanctioned coexistence carve-out and lives behind the seam. Without injected services
	/// (context.Services null, or no launcher in them) the value still renders and the button is
	/// disabled with a tooltip. The fenced edit context is unused: the dialog commits through its
	/// own UOW and the refresh controller re-renders via PropChanged.
	/// </summary>
	public sealed class LauncherRegionPlugin : IRegionEditorPlugin
	{
		private readonly Func<ICmObject, ViewNode, LcmCache, string> _valueReader;

		public LauncherRegionPlugin(string legacyClassName,
			Func<ICmObject, ViewNode, LcmCache, string> valueReader)
		{
			LegacyClassName = legacyClassName;
			_valueReader = valueReader ?? throw new ArgumentNullException(nameof(valueReader));
		}

		public string LegacyClassName { get; }

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var obj = context?.Target;
			var node = context?.Node;
			if (obj == null || node == null)
				return null;

			string value;
			try
			{
				value = _valueReader(obj, node, context.Cache) ?? string.Empty;
			}
			catch (Exception e)
			{
				// A broken value read degrades to an empty value, never a missing row.
				Logger.WriteEvent($"LauncherRegionPlugin ({LegacyClassName}): value read failed: {e}");
				value = string.Empty;
			}

			var launcher = context.Services?.LegacyDialogLauncher;
			Action launch = launcher == null
				? (Action)null
				: () =>
				{
					try
					{
						// The result is intentionally unobserved: a committed dialog raises
						// PropChanged and the host's refresh controller recomposes the region.
						launcher.LaunchFor(obj, node);
					}
					catch (Exception e)
					{
						Logger.WriteEvent($"LauncherRegionPlugin ({LegacyClassName}): launch failed: {e}");
					}
				};
			return new FwDialogLauncherField(value, node.Label ?? node.Field, launch);
		}
	}

	/// <summary>
	/// The D4 launcher-routed slice classes and their plugin/value-reader recipes. Value display
	/// matches each legacy slice's own view: the MSA launcher view renders the feature structure's
	/// ShortName (CmAnalObjectVc kfragShortName over MsaInflectionFeatureListDlgLauncherView), the
	/// phonological launcher's deParams say displayProperty="LongName", and AudioVisualVc renders
	/// the media file's AbsoluteInternalPath.
	/// </summary>
	public static class DialogLauncherPlugins
	{
		/// <summary>MSA "Inflection Features"/"Required Features" launchers (MorphologyParts.xml).</summary>
		public const string MsaFeatureSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.MsaInflectionFeatureListDlgLauncherSlice";

		/// <summary>Phonological features launcher (PhPhoneme/PhNCFeatures, MorphologyParts.xml).</summary>
		public const string PhonologicalFeatureSliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.PhonologicalFeatureListDlgLauncherSlice";

		/// <summary>Pronunciation media (CmMedia-Detail-MediaFile, LexEntryParts.xml).</summary>
		public const string AudioVisualSliceClassName =
			"SIL.FieldWorks.Common.Framework.DetailControls.AudioVisualSlice";

		public static LauncherRegionPlugin CreateMsaInflectionFeatures()
			=> new LauncherRegionPlugin(MsaFeatureSliceClassName,
				(obj, node, cache) => ResolveFeatureStructure(obj, node, cache, out _)?.ShortName);

		public static LauncherRegionPlugin CreatePhonologicalFeatures()
			=> new LauncherRegionPlugin(PhonologicalFeatureSliceClassName,
				(obj, node, cache) => ResolveFeatureStructure(obj, node, cache, out _)?.LongName);

		public static LauncherRegionPlugin CreateAudioVisual()
			=> new LauncherRegionPlugin(AudioVisualSliceClassName,
				(obj, node, cache) => ReadMediaFilePath(obj));

		/// <summary>
		/// The (feature structure, flid) resolution every launcher site shares, mirroring
		/// MsaInflectionFeatureListDlgLauncherSlice.Install / GetFeatureStructureFromMSA: a layout
		/// `field=` resolves to the owning atomic flid on the row's object and the structure is
		/// its current value (possibly null — the dialog creates it); without a field the row's
		/// object IS the structure (FsFeatStruc-Detail-FeatureSpecs) and the flid is FeatureSpecs.
		/// </summary>
		internal static IFsFeatStruc ResolveFeatureStructure(ICmObject obj, ViewNode node,
			LcmCache cache, out int flid)
		{
			flid = 0;
			if (obj == null || cache == null)
				return null;

			if (!string.IsNullOrEmpty(node?.Field))
			{
				try
				{
					flid = cache.DomainDataByFlid.MetaDataCache.GetFieldId2(obj.ClassID, node.Field, true);
				}
				catch (Exception)
				{
					flid = 0;
				}
			}

			if (flid != 0)
			{
				var hvoFs = cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
				return hvoFs == 0
					? null
					: cache.ServiceLocator.ObjectRepository.GetObject(hvoFs) as IFsFeatStruc;
			}

			if (obj is IFsFeatStruc fs)
			{
				flid = FsFeatStrucTags.kflidFeatureSpecs;
				return fs;
			}
			return null;
		}

		/// <summary>
		/// The media file behind an AudioVisual row: legacy initializes its launcher with
		/// <c>Media.MediaFileRA</c>, so the row's CmMedia (or a CmFile directly) resolves here.
		/// </summary>
		internal static ICmFile ResolveMediaFile(ICmObject obj)
		{
			switch (obj)
			{
				case ICmFile file:
					return file;
				case ICmMedia media:
					return media.MediaFileRA;
				default:
					return null;
			}
		}

		// AudioVisualVc displays file.AbsoluteInternalPath; fall back to the project-relative
		// InternalPath when the absolute resolution throws (no linked-files root in odd hosts).
		internal static string ReadMediaFilePath(ICmObject obj)
		{
			var file = ResolveMediaFile(obj);
			if (file == null)
				return string.Empty;
			try
			{
				return file.AbsoluteInternalPath ?? string.Empty;
			}
			catch (Exception)
			{
				return file.InternalPath ?? string.Empty;
			}
		}
	}
}
