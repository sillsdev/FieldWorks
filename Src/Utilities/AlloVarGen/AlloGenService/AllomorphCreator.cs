// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.AlloGenModel;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenService
{
	public class AllomorphCreator
	{
		LcmCache Cache { get; set; }
		List<WritingSystem> WritingSystems { get; set; } = new List<WritingSystem>();

		public AllomorphCreator(LcmCache cache, List<WritingSystem> writingSystems)
		{
			Cache = cache;
			WritingSystems = writingSystems;
		}

		public IMoStemAllomorph CreateAllomorph(ILexEntry entry, List<string> forms)
		{
			IMoStemAllomorph form = entry.Services.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(form);
			for (int i = 0; i < WritingSystems.Count && i < forms.Count; i++)
			{
				form.Form.set_String(WritingSystems[i].Handle, forms[i]);
			}
			return form;
		}

		public void AddEnvironments(IMoStemAllomorph form, List<AlloGenModel.Environment> envs)
		{
			foreach (AlloGenModel.Environment env in envs)
			{
				var phEnv = Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
					new Guid(env.Guid)
				);
				if (phEnv != null)
				{
					form.AllomorphEnvironments.Add((IPhEnvironment)phEnv);
				}
			}
		}

		public void AddStemName(IMoStemAllomorph form, string stemNameGuid)
		{
			var stemName = Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
				new Guid(stemNameGuid)
			);
			if (stemName != null)
			{
				form.StemNameRA = (IMoStemName)stemName;
			}
		}
	}
}
