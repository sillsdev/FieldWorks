// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	internal sealed class OccurrenceComparer : IComparer
	{
		private readonly LcmCache m_cache;
		private ISilDataAccessManaged m_sda;

		/// <summary />
		public OccurrenceComparer(LcmCache cache, ISilDataAccessManaged sda)
		{
			m_cache = cache;
			m_sda = sda;
		}

		#region IComparer Members

		/// <summary />
		public int Compare(object x1, object y1)
		{
			var x = (IManyOnePathSortItem)x1;
			var y = (IManyOnePathSortItem)y1;
			int hvoX = m_sda.get_ObjectProp(x.KeyObject, ConcDecorator.kflidTextObject);
			int hvoY = m_sda.get_ObjectProp(y.KeyObject, ConcDecorator.kflidTextObject);
			if (hvoX == hvoY)
			{
				// In the same text object, we can compare offsets.
				int offsetX = m_sda.get_IntProp(x.KeyObject, ConcDecorator.kflidBeginOffset);
				int offsetY = m_sda.get_IntProp(y.KeyObject, ConcDecorator.kflidBeginOffset);
				return offsetX - offsetY;
			}
			hvoX = m_sda.get_ObjectProp(x.KeyObject, ConcDecorator.kflidParagraph);
			hvoY = m_sda.get_ObjectProp(y.KeyObject, ConcDecorator.kflidParagraph);
			if (hvoX == hvoY)
			{
				// In the same paragraph (and not nested in the same caption), we can compare offsets.
				int offsetX = m_sda.get_IntProp(x.KeyObject, ConcDecorator.kflidBeginOffset);
				int offsetY = m_sda.get_IntProp(y.KeyObject, ConcDecorator.kflidBeginOffset);
				return offsetX - offsetY;
			}

			// While owning objects are the same type, get the owner of each, if they are the same,
			// compare their position in owner. Special case to put heading before body.
			// If owners are not the same type, do some trick that will make FLEx texts come before Scripture.
			var paraRepo = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>();
			ICmObject objX = paraRepo.GetObject(hvoX);
			ICmObject objY = paraRepo.GetObject(hvoY);
			for (;;)
			{
				var ownerX = objX.Owner;
				var ownerY = objY.Owner;
				if (ownerX == null)
				{
					if (ownerY == null)
						return hvoY - hvoX; // totally arbitrary but at least consistent
					return -1; // also arbitrary
				}
				if (ownerY == null)
					return 1; // arbitrary, object with shorter chain comes first.
				if (ownerX == ownerY)
				{
					var flidX = objX.OwningFlid;
					var flidY = objY.OwningFlid;
					if (flidX != flidY)
					{
						return flidX - flidY; // typically body and heading.
					}
					var indexX = m_cache.MainCacheAccessor.GetObjIndex(ownerX.Hvo, flidX, objX.Hvo);
					var indexY = m_cache.MainCacheAccessor.GetObjIndex(ownerY.Hvo, flidX, objY.Hvo);
					return indexX - indexY;
				}
				var clsX = ownerX.ClassID;
				var clsY = ownerY.ClassID;
				if (clsX != clsY)
				{
					// Typically one is in Scripture, the other in a Text.
					// Arbitrarily order things by the kind of parent they're in.
					// Enhance JohnT: this will need improvement if we go to hierarchical
					// structures like nested sections or a folder organization of texts.
					// We could loop all the way up, and then back down till we find a pair
					// of owners that are different.
					// (We reverse the usual X - Y in order to put Texts before Scripture
					// in this list as in the Texts list in FLEx.)
					return clsY - clsX;
				}
				objX = ownerX;
				objY = ownerY;
			}
		}

		#endregion
	}
}