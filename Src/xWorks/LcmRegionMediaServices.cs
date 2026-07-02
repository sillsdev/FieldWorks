// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Avalonia.Platform.Storage;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Media;
using AvControl = Avalonia.Controls.Control;
using TopLevel = Avalonia.Controls.TopLevel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19d: the host (xWorks) implementation of the LCModel-free media seam
	/// <see cref="IRegionMediaServices"/> the owned picture/audio fields drive — the only place allowed to
	/// touch WinForms / the file system / the audio device during coexistence. It resolves files into the
	/// project's media/pictures folders, shows the WinForms-owned Avalonia picture-properties dialog, picks
	/// image files through the Avalonia managed file picker (<see cref="IStorageProvider"/> off the hosting
	/// surface's <c>TopLevel</c>), and plays/records audio through libpalaso's <c>ISimpleAudioSession</c>
	/// (NAudio on Windows; the same device the legacy voice-WS slice uses).
	/// <para>Cross-platform record: <c>ISimpleAudioSession.CanRecord</c> reports device availability; where
	/// it is unavailable the record affordance is disabled (the view shows the "use the classic view"
	/// tooltip). // PARITY §19d (cross-platform record → avalonia-end-game): the recording UI here is a
	/// minimal WinForms record/stop modal; a richer cross-platform recorder is an avalonia-end-game
	/// concern.</para>
	/// </summary>
	public sealed class LcmRegionMediaServices : IRegionMediaServices
	{
		private readonly LcmCache _cache;
		private readonly Func<IWin32Window> _ownerProvider;
		private readonly Func<AvControl> _surfaceProvider;

		public LcmRegionMediaServices(LcmCache cache, Func<IWin32Window> ownerProvider,
			Func<AvControl> surfaceProvider)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_ownerProvider = ownerProvider;
			_surfaceProvider = surfaceProvider;
		}

		/// <inheritdoc />
		public System.Threading.Tasks.Task<string> PickImageFileAsync(string title)
		{
			var topLevel = _surfaceProvider == null ? null : TopLevel.GetTopLevel(_surfaceProvider());
			if (topLevel?.StorageProvider == null)
				return System.Threading.Tasks.Task.FromResult<string>(null);
			return PickAsync(topLevel.StorageProvider, title);
		}

		private static async System.Threading.Tasks.Task<string> PickAsync(IStorageProvider provider, string title)
		{
			var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = title,
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Images")
					{
						Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.tif", "*.tiff" }
					}
				}
			});
			var first = files?.FirstOrDefault();
			return first?.TryGetLocalPath();
		}

		/// <inheritdoc />
		public RegionPictureDialogResult ShowPictureProperties(RegionPictureMetadata current, bool isNew)
		{
			var owner = _ownerProvider?.Invoke();
			var viewModel = new PicturePropertiesDialogViewModel(current, isNew);
			var view = new PicturePropertiesDialogView(viewModel);
			// The "Choose image…" button runs the Avalonia file picker synchronously-enough for the modal:
			// the picker is pumped on the UI thread while the dialog stays open.
			view.ChooseImageRequested += (s, e) =>
			{
				var topLevel = _surfaceProvider == null ? null : TopLevel.GetTopLevel(_surfaceProvider());
				if (topLevel?.StorageProvider == null)
					return;
				var task = PickAsync(topLevel.StorageProvider, FwAvaloniaDialogsStrings.PicturePropertiesChooseImage);
				task.ContinueWith(t =>
				{
					if (t.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && t.Result != null)
						Avalonia.Threading.Dispatcher.UIThread.Post(() => viewModel.SetImageFile(t.Result));
				});
			};

			var accepted = AvaloniaDialogHost.ShowModal(owner, view, viewModel,
				FwAvaloniaDialogsStrings.PicturePropertiesTitle, 420, 340);
			if (accepted != true || viewModel.Result == null)
				return null;
			return viewModel.Result;
		}

		/// <inheritdoc />
		public bool CanRecordAudio
		{
			get
			{
				try
				{
					var probe = Path.Combine(Path.GetTempPath(), "fw-audio-probe.wav");
					using (var session = AudioFactory.CreateAudioSession(probe))
						return session.CanRecord;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		/// <inheritdoc />
		public void PlayAudio(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return;
			var path = ResolveMediaPath(fileName);
			if (!File.Exists(path))
				return;
			try
			{
				using (var session = AudioFactory.CreateAudioSession(path))
					session.Play();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine($"LcmRegionMediaServices.PlayAudio: {e.Message}");
			}
		}

		/// <inheritdoc />
		public string RecordAudio()
		{
			var mediaDir = MediaDir();
			Directory.CreateDirectory(mediaDir);
			var fileName = UniqueAudioFileName(mediaDir);
			var path = Path.Combine(mediaDir, fileName);
			try
			{
				using (var session = AudioFactory.CreateAudioSession(path))
				{
					if (!session.CanRecord)
						return null;
					if (!ShowRecordModal(session))
						return null;
				}
				return File.Exists(path) ? fileName : null;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine($"LcmRegionMediaServices.RecordAudio: {e.Message}");
				return null;
			}
		}

		// A minimal WinForms record/stop modal (the recording UI). Returns true on Save (the file was
		// recorded), false on Cancel. // PARITY §19d (cross-platform record → avalonia-end-game).
		private bool ShowRecordModal(ISimpleAudioSession session)
		{
			var owner = _ownerProvider?.Invoke();
			using (var dlg = new Form
			{
				Text = FwAvaloniaStrings.AudioRecord,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterParent,
				MinimizeBox = false,
				MaximizeBox = false,
				ClientSize = new System.Drawing.Size(280, 90)
			})
			{
				var recordButton = new Button { Text = FwAvaloniaStrings.AudioRecord, Left = 10, Top = 10, Width = 120 };
				var stopButton = new Button { Text = FwAvaloniaStrings.AudioClear, Left = 140, Top = 10, Width = 120, Enabled = false };
				var ok = new Button { Text = FwAvaloniaDialogsStrings.Ok, Left = 10, Top = 50, Width = 120, DialogResult = DialogResult.OK, Enabled = false };
				var cancel = new Button { Text = FwAvaloniaDialogsStrings.Cancel, Left = 140, Top = 50, Width = 120, DialogResult = DialogResult.Cancel };
				var recorded = false;
				recordButton.Click += (s, e) =>
				{
					session.StartRecording();
					recordButton.Enabled = false;
					stopButton.Enabled = true;
				};
				stopButton.Click += (s, e) =>
				{
					session.StopRecordingAndSaveAsWav();
					stopButton.Enabled = false;
					ok.Enabled = true;
					recorded = true;
				};
				dlg.Controls.AddRange(new Control[] { recordButton, stopButton, ok, cancel });
				dlg.AcceptButton = ok;
				dlg.CancelButton = cancel;
				var result = owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
				return result == DialogResult.OK && recorded;
			}
		}

		private string MediaDir() => LcmFileHelper.GetMediaDir(_cache.LangProject.LinkedFilesRootDir);

		private string ResolveMediaPath(string fileName)
			=> Path.Combine(MediaDir(), fileName.Normalize(NormalizationForm.FormC));

		private static string UniqueAudioFileName(string mediaDir)
		{
			for (var i = 0; ; i++)
			{
				var candidate = i == 0 ? "recording.wav" : $"recording{i}.wav";
				if (!File.Exists(Path.Combine(mediaDir, candidate)))
					return candidate;
			}
		}
	}
}
