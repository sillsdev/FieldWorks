// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The Insert Sound or Movie command: which pronunciation takes the files, and the media
	/// links it gets. Choosing the files stays with the caller.
	/// </summary>
	public static class MediaFileInsertion
	{
		/// <summary>The strings-table key of the media folder's name.</summary>
		public const string MediaFolderKey = "kstidMediaFolder";

		/// <summary>
		/// The entry whose pronunciation takes the files: the one <paramref name="rowObject"/>
		/// is or belongs to, else <paramref name="root"/> when that is an entry; null when
		/// neither is.
		/// </summary>
		public static ILexEntry TargetEntry(ICmObject rowObject, ICmObject root)
		{
			var entry = rowObject as ILexEntry ?? rowObject?.OwnerOfClass<ILexEntry>();
			return entry ?? root as ILexEntry;
		}

		/// <summary>
		/// The pronunciation the files go to: <paramref name="rowObject"/> when it is one,
		/// else the first pronunciation of <see cref="TargetEntry"/>; null when there is no
		/// entry or it has no pronunciation yet (see <see cref="FirstOrCreatePronunciation"/>).
		/// </summary>
		public static ILexPronunciation TargetPronunciation(ICmObject rowObject, ICmObject root)
		{
			if (rowObject is ILexPronunciation pronunciation)
				return pronunciation;
			return TargetEntry(rowObject, root)?.PronunciationsOS.FirstOrDefault();
		}

		/// <summary>
		/// The entry's first pronunciation, created in its own undoable step when it has none.
		/// The default pronunciation writing system is read first, which initializes the
		/// pronunciation writing systems (FWR-2086).
		/// </summary>
		public static ILexPronunciation FirstOrCreatePronunciation(ILexEntry entry, string undoText, string redoText)
		{
			if (entry.PronunciationsOS.Count == 0)
			{
				var cache = entry.Cache;
				_ = cache.DefaultPronunciationWs;
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(undoText, redoText, cache.ActionHandlerAccessor,
					() => entry.PronunciationsOS.Add(cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create()));
			}
			return entry.PronunciationsOS[0];
		}

		/// <summary>
		/// Links each file to <paramref name="pronunciation"/> as a new media object whose
		/// file sits in the project's media folder, in one undoable step. The links follow
		/// the existing ones in reverse name order, since the file dialog lists the last
		/// picked file first. Empty names are skipped.
		/// </summary>
		/// <returns>The media objects created, in their order in the pronunciation.</returns>
		public static IReadOnlyList<ICmMedia> Insert(ILexPronunciation pronunciation, IEnumerable<string> fileNames,
			string undoText, string redoText)
		{
			var created = new List<ICmMedia>();
			var names = fileNames.Where(f => !string.IsNullOrEmpty(f)).ToList();
			if (names.Count == 0 || !pronunciation.IsValidObject)
				return created;
			var cache = pronunciation.Cache;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(undoText, redoText, cache.ActionHandlerAccessor, () =>
			{
				var folder = DomainObjectServices.FindOrCreateFolder(cache, LangProjectTags.kflidMedia, MediaFolderName());
				var end = pronunciation.MediaFilesOS.Count;
				foreach (var name in names)
				{
					var media = cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
					pronunciation.MediaFilesOS.Insert(end, media);
					media.MediaFileRA = DomainObjectServices.FindOrCreateFile(folder, name);
					created.Insert(0, media);
				}
			});
			return created;
		}

		/// <summary>The media folder's name: the strings-table entry, else the default
		/// folder.</summary>
		public static string MediaFolderName()
		{
			var name = StringTable.Table.GetString(MediaFolderKey);
			return string.IsNullOrEmpty(name) || name == "*" + MediaFolderKey + "*" ? CmFolderTags.LocalMedia : name;
		}
	}
}
