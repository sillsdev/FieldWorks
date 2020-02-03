// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// List used in tools: "Reversal Indexes" & "Bulk Edit Reversal Entries".
	/// </summary>
	internal sealed class AllReversalEntriesRecordList : ReversalListBase
	{
		internal const string AllReversalEntries = "AllReversalEntries";
		private IReversalIndexEntry _newItem;

		/// <summary />
		internal AllReversalEntriesRecordList(StatusBar statusBar, ILcmServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
			: base(AllReversalEntries, statusBar, decorator, true, new VectorPropertyParameterObject(reversalIndex, "AllEntries", ReversalIndexTags.kflidEntries))
		{
			m_fontName = serviceLocator.WritingSystemManager.Get(reversalIndex.WritingSystem).DefaultFontName;
			m_oldLength = 0;
		}

		private void SelectNewItem()
		{
			JumpToRecord(_newItem.Hvo);
		}

		#region Overrides of RecordList

		/// <summary />
		protected override bool CanInsertClass(string className)
		{
			if (base.CanInsertClass(className))
			{
				return true;
			}
			return className == "ReversalIndexEntry";
		}

		/// <summary />
		protected override bool CreateAndInsert(string className)
		{
			if (className != "ReversalIndexEntry")
			{
				return base.CreateAndInsert(className);
			}
			_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			var reversalIndex = (IReversalIndex)OwningObject;
			reversalIndex.EntriesOC.Add(_newItem);
			var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
			extensions?.DoAtEndOfPropChanged(SelectNewItem);
			return true;
		}

		/// <summary />
		protected override IEnumerable<int> GetObjectSet()
		{
			var reversalIndex = OwningObject as IReversalIndex;
			Debug.Assert(reversalIndex != null && reversalIndex.IsValidObject, "The owning IReversalIndex object is invalid!?");
			return new List<int>(reversalIndex.AllEntries.Select(rie => rie.Hvo));
		}

		/// <summary>
		/// Delete the current object.
		/// In some cases thingToDelete is not actually the current object, but it should always
		/// be related to it.
		/// </summary>
		protected override void DeleteCurrentObject(ICmObject thingToDelete = null)
		{
			base.DeleteCurrentObject(thingToDelete);

			ReloadList();
		}

		/// <summary />
		protected override string PropertyTableId(string sorterOrFilter)
		{
			var reversalPub = PropertyTable.GetValue<string>("ReversalIndexPublicationLayout");
			if (reversalPub == null)
			{
				return null; // there is no current Reversal Index; don't try to find Properties (sorter & filter) for a nonexistent Reversal Index
			}
			var reversalLang = reversalPub.Substring(reversalPub.IndexOf('-') + 1); // strip initial "publishReversal-"
			// Dependent lists do not have owner/property set. Rather they have class/field.
			var className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
			var fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
			if (string.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
			{
				return $"{className}.{fieldName}-{reversalLang}_{sorterOrFilter}";
			}
			return $"{className}.{PropertyName}-{reversalLang}_{sorterOrFilter}";
		}

		#endregion

		#region Overrides of ReversalListBase

		/// <summary />
		protected override ICmObject NewOwningObject(IReversalIndex ri)
		{
			return ri;
		}

		#endregion
	}
}