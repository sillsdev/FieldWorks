// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class AudioVisualVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;

		public AudioVisualVc(LcmCache cache, int flid, string displayNameProperty)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AudioVisualView.kfragPathname:
					// Display the filename.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
					Debug.Assert(hvo != 0);
					Debug.Assert(m_cache != null);
					var file = m_cache.ServiceLocator.GetInstance<ICmFileRepository>().GetObject(hvo);
					Debug.Assert(file != null);
					var path = file.AbsoluteInternalPath;
					var tss = TsStringUtils.MakeString(path, m_cache.WritingSystemFactory.UserWs);
					vwenv.OpenParagraph();
					vwenv.NoteDependency( new [] { m_cache.LangProject.Hvo, file.Hvo}, new [] {LangProjectTags.kflidLinkedFilesRootDir, CmFileTags.kflidInternalPath}, 2);
					vwenv.AddString(tss);
					vwenv.CloseParagraph();
					break;

				default:
					throw new ArgumentException(@"Don't know what to do with the given frag.", nameof(frag));
			}
		}
	}
}