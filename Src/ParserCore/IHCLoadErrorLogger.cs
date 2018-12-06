// Copyright (c) 20147-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public interface IHCLoadErrorLogger
	{
		void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa);
		void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa);
		void InvalidPhoneme(IPhPhoneme phoneme);
		void DuplicateGrapheme(IPhPhoneme phoneme);
		void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa);
		void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa);
	}
}