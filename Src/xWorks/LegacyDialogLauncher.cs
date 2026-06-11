// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D4 — the host seam a dialog-launcher row calls: the PANE
	/// stays WinForms-free, and the host (RecordEditView, the only place allowed to touch
	/// WinForms during coexistence) runs the existing legacy dialog. The dialog edits through its
	/// own unit of work; the host's <see cref="AvaloniaRegionRefreshController"/> is subscribed to
	/// the real PropChanged bus, so a committed dialog edit re-renders the region without the
	/// launcher doing anything further (verified: the controller's IsRelevant walk covers any
	/// object owned by the displayed entry — MSAs and their feature structures are).
	/// </summary>
	public interface ILegacyDialogLauncher
	{
		/// <summary>
		/// Runs the legacy dialog (or player) for the slice identified by
		/// <paramref name="node"/>.CustomEditorClass over <paramref name="obj"/>. Returns whether
		/// anything changed (the dialog committed); refresh rides PropChanged either way.
		/// </summary>
		bool LaunchFor(ICmObject obj, ViewNode node);
	}

	/// <summary>
	/// winforms-free-lexeme-editor.md D4 — the host-injected services a region editor plugin may
	/// use beyond (object, node, edit context, cache). Carried as one parameter object so future
	/// host services extend it without touching the plugin contract again; threaded from
	/// RecordEditView through <see cref="FullEntryRegionComposer.Compose"/> into the plugin
	/// control factories. Null-safe by design: composing without services (tests, preview hosts)
	/// hands plugins null and launcher rows render their button disabled.
	/// </summary>
	public sealed class RegionEditorServices
	{
		/// <summary>The legacy-dialog seam (D4); null when the host offers none.</summary>
		public ILegacyDialogLauncher LegacyDialogLauncher { get; set; }
	}

	/// <summary>
	/// RecordEditView's <see cref="ILegacyDialogLauncher"/>: the sanctioned WinForms carve-out.
	/// Dispatches on the node's legacy class identity:
	/// <list type="bullet">
	/// <item>MSA inflection features → <c>SIL.FieldWorks.LexText.Controls.MsaInflectionFeatureListDlg</c>
	/// (reflection through <see cref="DynamicLoader"/>, exactly the layouts' own load lane — xWorks
	/// cannot reference LexTextControls), the same SetDlgInfo recipe
	/// MsaInflectionFeatureListDlgLauncher.HandleChooser uses; the dialog commits through its own
	/// UOW in OnClosing, so returning true is "the dialog said OK".</item>
	/// <item>Phonological features → <c>PhonologicalFeatureChooserDlg</c>, same recipe
	/// (PhonologicalFeatureListDlgLauncher.HandleChooser).</item>
	/// <item>AudioVisual media → plays the file the way AudioVisualLauncher.HandleChooser does
	/// (SoundPlayer for wav, the OS default app otherwise); never a data change.</item>
	/// </list>
	/// Any open fenced edit session is settled first (<c>beforeLaunch</c>): a legacy dialog
	/// opening its own UOW while the fence holds the write lock would throw, the same hazard the
	/// undo guard exists for.
	/// </summary>
	public sealed class WinFormsLegacyDialogLauncher : ILegacyDialogLauncher
	{
		private const string LexTextControlsDll = "LexTextControls.dll";
		private const string MsaDialogClass = "SIL.FieldWorks.LexText.Controls.MsaInflectionFeatureListDlg";
		private const string PhonDialogClass = "SIL.FieldWorks.LexText.Controls.PhonologicalFeatureChooserDlg";
		// MsaInflectionFeatureListDlgLauncher.HandleChooser's string-table path, verbatim.
		private const string FeatureChooserXPath =
			"/group[@id='Linguistics']/group[@id='Morphology']/group[@id='FeatureChooser']/";

		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly Func<Form> _ownerForm;
		private readonly Action _beforeLaunch;

		public WinFormsLegacyDialogLauncher(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, Func<Form> ownerForm, Action beforeLaunch = null)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_mediator = mediator;
			_propertyTable = propertyTable;
			_ownerForm = ownerForm;
			_beforeLaunch = beforeLaunch;
		}

		public bool LaunchFor(ICmObject obj, ViewNode node)
		{
			if (obj == null || node == null)
				return false;
			try
			{
				_beforeLaunch?.Invoke();
				switch (node.CustomEditorClass)
				{
					case DialogLauncherPlugins.MsaFeatureSliceClassName:
						return LaunchFeatureDialog(obj, node, MsaDialogClass, isMsaDialog: true);
					case DialogLauncherPlugins.PhonologicalFeatureSliceClassName:
						return LaunchFeatureDialog(obj, node, PhonDialogClass, isMsaDialog: false);
					case DialogLauncherPlugins.AudioVisualSliceClassName:
						return PlayMedia(obj);
					default:
						Logger.WriteEvent(
							$"WinFormsLegacyDialogLauncher: no dialog mapped for '{node.CustomEditorClass}'.");
						return false;
				}
			}
			catch (Exception e)
			{
				// Never take the pane down for a launcher failure; the row simply does nothing.
				Logger.WriteEvent($"WinFormsLegacyDialogLauncher: launch failed for '{node.CustomEditorClass}': {e}");
				return false;
			}
		}

		// The MSA/Phon feature-structure dialogs share one recipe: resolve (fs, flid) the way the
		// legacy slice's Install does, hand them to SetDlgInfo (the fs overload when one exists,
		// the owner+flid overload when the dialog must create it), ShowDialog over the host form.
		// OK commits inside the dialog's own UOW (OnClosing); Yes is the "go configure features"
		// jump link.
		private bool LaunchFeatureDialog(ICmObject obj, ViewNode node, string dialogClassName, bool isMsaDialog)
		{
			var fs = DialogLauncherPlugins.ResolveFeatureStructure(obj, node, _cache, out var flid);
			if (flid == 0)
				return false;

			var dialog = DynamicLoader.CreateObject(LexTextControlsDll, dialogClassName) as Form;
			if (dialog == null)
			{
				Logger.WriteEvent($"WinFormsLegacyDialogLauncher: could not create '{dialogClassName}'.");
				return false;
			}

			using (dialog)
			{
				var type = dialog.GetType();
				if (fs != null)
				{
					// Existing feature structure: both dialogs take (cache, mediator,
					// propertyTable, IFsFeatStruc[, owningFlid]).
					var args = isMsaDialog
						? new object[] { _cache, _mediator, _propertyTable, fs, flid }
						: new object[] { _cache, _mediator, _propertyTable, fs };
					var argTypes = isMsaDialog
						? new[] { typeof(LcmCache), typeof(Mediator), typeof(PropertyTable), typeof(IFsFeatStruc), typeof(int) }
						: new[] { typeof(LcmCache), typeof(Mediator), typeof(PropertyTable), typeof(IFsFeatStruc) };
					Invoke(type, dialog, "SetDlgInfo", argTypes, args);
				}
				else
				{
					// No feature structure yet: the dialog creates it under (owner, flid).
					Invoke(type, dialog, "SetDlgInfo",
						new[] { typeof(LcmCache), typeof(Mediator), typeof(PropertyTable), typeof(ICmObject), typeof(int) },
						new object[] { _cache, _mediator, _propertyTable, obj, flid });
				}

				if (isMsaDialog)
				{
					// The launcher's own title/prompt/link strings (HandleChooser, verbatim path).
					dialog.Text = StringTable.Table.GetStringWithXPath("InflectionFeatureTitle", FeatureChooserXPath);
					SetProperty(type, dialog, "Prompt",
						StringTable.Table.GetStringWithXPath("InflectionFeaturePrompt", FeatureChooserXPath));
					SetProperty(type, dialog, "LinkText",
						StringTable.Table.GetStringWithXPath("InflectionFeatureLink", FeatureChooserXPath));
				}

				var result = dialog.ShowDialog(_ownerForm?.Invoke());
				switch (result)
				{
					case DialogResult.OK:
						// Committed by the dialog's own UOW; PropChanged re-renders the region.
						return true;
					case DialogResult.Yes:
						// "Configure features" jump. The MSA dialog exposes the POS to jump to
						// (the LT-7167 FollowLink fallback — the only lane available without a
						// sibling VectorReferenceLauncher slice); the Phon dialog owns its jump.
						if (isMsaDialog)
						{
							if (_mediator != null
								&& type.GetProperty("HighestPOS")?.GetValue(dialog) is ICmObject pos)
							{
#pragma warning disable 618 // legacy lane: PostMessage is how the launcher posts FollowLink
								_mediator.PostMessage("FollowLink", new FwLinkArgs("posEdit", pos.Guid));
#pragma warning restore 618
							}
						}
						else
						{
							type.GetMethod("HandleJump")?.Invoke(dialog, null);
						}
						return false;
					default:
						return false;
				}
			}
		}

		// AudioVisualLauncher.HandleChooser, minus the WinForms slice: SoundPlayer for a real wav
		// (sniffed by RIFF/WAVE header, like legacy), the OS default app for everything else.
		private static bool PlayMedia(ICmObject obj)
		{
			var file = DialogLauncherPlugins.ResolveMediaFile(obj);
			if (file == null)
				return false;
			var path = FileUtils.ActualFilePath(file.AbsoluteInternalPath);
			if (!System.IO.File.Exists(path))
			{
				Logger.WriteEvent($"WinFormsLegacyDialogLauncher: media file '{path}' not found.");
				return false;
			}

			if (IsWavFile(path))
			{
				using (var player = new System.Media.SoundPlayer(path))
					player.Play();
			}
			else
			{
				using (System.Diagnostics.Process.Start(path))
				{
				}
			}
			return false; // playing media never changes data
		}

		// Legacy AudioVisualLauncher.IsWavFile: look inside the file, not at the extension.
		private static bool IsWavFile(string path)
		{
			using (var fs = System.IO.File.OpenRead(path))
			{
				var cbFile = (int)fs.Length;
				var rgb = new byte[12];
				if (fs.Read(rgb, 0, 12) < 12)
					return false;
				if (rgb[0] == 'R' && rgb[1] == 'I' && rgb[2] == 'F' && rgb[3] == 'F'
					&& rgb[8] == 'W' && rgb[9] == 'A' && rgb[10] == 'V' && rgb[11] == 'E')
				{
					var cbSize = rgb[4] + (rgb[5] << 8) + (rgb[6] << 16) + (rgb[7] << 24);
					return cbSize == cbFile - 8;
				}
				return false;
			}
		}

		private static void Invoke(Type type, object target, string methodName, Type[] argTypes, object[] args)
		{
			var method = type.GetMethod(methodName, argTypes);
			if (method == null)
				throw new MissingMethodException(type.FullName, methodName);
			method.Invoke(target, args);
		}

		private static void SetProperty(Type type, object target, string propertyName, string value)
		{
			type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
				?.SetValue(target, value);
		}
	}
}
