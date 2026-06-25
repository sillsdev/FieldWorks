// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading.Tasks;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// §19d: the LCModel-free media seam the owned picture/audio fields drive — file picking, the
	/// picture-properties dialog, and audio play/record. The host (xWorks) supplies the real
	/// implementation (Avalonia <c>IStorageProvider</c> file pick, the WinForms-owned Avalonia
	/// picture-properties dialog, and the NAudio-backed player/recorder); tests supply a fake that
	/// records calls and returns canned files. A null services bundle disables the affordances (read-only
	/// display), exactly like a null edit context. This keeps the view free of any LCModel / file-system /
	/// device dependency — the affordances exist, but the doing lives behind the seam.
	/// </summary>
	public interface IRegionMediaServices
	{
		/// <summary>
		/// Asks the user to choose an image file (Avalonia managed file picker). Returns the chosen
		/// absolute path, or null when the user cancels. <paramref name="title"/> is the picker title.
		/// </summary>
		Task<string> PickImageFileAsync(string title);

		/// <summary>
		/// Shows the picture-properties dialog seeded with <paramref name="current"/> (null for a new
		/// picture). Returns the edited metadata + the chosen image file on OK, or null on Cancel. For a
		/// NEW picture the result's <see cref="RegionPictureDialogResult.SourceFile"/> is the picked image;
		/// for an existing one it is null unless the user replaced the file.
		/// </summary>
		RegionPictureDialogResult ShowPictureProperties(RegionPictureMetadata current, bool isNew);

		/// <summary>Whether audio recording is available on this platform (Windows: yes; others: deferred).</summary>
		bool CanRecordAudio { get; }

		/// <summary>
		/// Plays the audio file whose project-relative name is <paramref name="fileName"/> (resolved to the
		/// project media folder by the host). A no-op when the file is missing.
		/// </summary>
		void PlayAudio(string fileName);

		/// <summary>
		/// Records audio to a NEW file in the project media folder and returns its project-relative name
		/// (the value written into the voice-WS alternative), or null when recording was cancelled or is
		/// unavailable. The host owns the actual device (NAudio on Windows) and the recording UI; the view
		/// only invokes this and writes the returned name through the edit context.
		/// </summary>
		string RecordAudio();
	}

	/// <summary>§19d: the result of the picture-properties dialog (LCModel-free).</summary>
	public sealed class RegionPictureDialogResult
	{
		public RegionPictureDialogResult(RegionPictureMetadata metadata, string sourceFile)
		{
			Metadata = metadata ?? new RegionPictureMetadata();
			SourceFile = sourceFile;
		}

		/// <summary>The edited caption/description/license/creator.</summary>
		public RegionPictureMetadata Metadata { get; }

		/// <summary>
		/// The chosen image file (absolute path) — non-null for a new picture or when the user replaced the
		/// file of an existing one; null when only metadata changed on an existing picture.
		/// </summary>
		public string SourceFile { get; }
	}
}
