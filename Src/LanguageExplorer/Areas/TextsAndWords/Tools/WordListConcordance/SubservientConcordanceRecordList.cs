// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.WordListConcordance
{
	internal sealed class SubservientConcordanceRecordList : SubservientRecordList
	{
		private ConcDecorator _decorator;

		/// <summary />
		internal SubservientConcordanceRecordList(string id, StatusBar statusBar, ConcDecorator decorator, bool usingAnalysisWs, int flid, IRecordList recordListProvidingRootObject)
			: base(id, statusBar, decorator, usingAnalysisWs, new VectorPropertyParameterObject(null, string.Empty, flid, false), recordListProvidingRootObject)
		{
			Guard.AgainstNull(decorator, nameof(decorator));

			_decorator = decorator;
		}

		protected override void ReallyResetOwner(ICmObject selectedObject)
		{
			if (ReferenceEquals(OwningObject, selectedObject))
			{
				return;
			}
			_decorator.UpdateAnalysisOccurrences((IAnalysis)selectedObject, true);
			((ObjectListPublisher)VirtualListPublisher).CacheVecProp(selectedObject.Hvo, _decorator.VecProp(selectedObject.Hvo, ConcDecorator.kflidWfOccurrences));
			base.ReallyResetOwner(selectedObject);
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Empty place holder, if anything needs to be disposed at some point. it goes here.
				// Does no harm, since compiler removes the empty block.
			}
			_decorator = null;

			base.Dispose(disposing);
		}
	}
}