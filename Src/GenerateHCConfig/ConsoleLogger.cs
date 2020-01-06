// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;

namespace GenerateHCConfig
{
	internal class ConsoleLogger : ILcmUI, IHCLoadErrorLogger
	{
		public ConsoleLogger(ISynchronizeInvoke synchronizeInvoke)
		{
			SynchronizeInvoke = synchronizeInvoke;
		}

		public ISynchronizeInvoke SynchronizeInvoke { get; }

		public bool ConflictingSave()
		{
			throw new NotSupportedException();
		}

		public DateTime LastActivityTime => DateTime.Now;

		public FileSelection ChooseFilesToUse()
		{
			throw new NotSupportedException();
		}

		public bool RestoreLinkedFilesInProjectFolder()
		{
			throw new NotSupportedException();
		}

		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			throw new NotSupportedException();
		}

		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			Console.WriteLine(message);
		}

		public void ReportException(Exception error, bool isLethal)
		{
			Console.WriteLine(error.Message);
		}

		public void ReportDuplicateGuids(string errorText)
		{
			Console.WriteLine(errorText);
		}

		public void DisplayCircularRefBreakerReport(string msg, string caption)
		{
			Console.WriteLine("{0}: {1}", caption, msg);
		}

		public bool Retry(string msg, string caption)
		{
			throw new NotSupportedException();
		}

		public bool OfferToRestore(string projectPath, string backupPath)
		{
			throw new NotSupportedException();
		}

		public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The form \"{0}\" contains an undefined phoneme at {1}.", str, errorPos);
		}

		public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The affix process \"{0}\" is invalid.", affixProcess.Form.BestVernacularAlternative.Text);
		}

		public void InvalidPhoneme(IPhPhoneme phoneme)
		{
			Console.WriteLine("The phoneme \"{0}\" does not contain any valid graphemes.", phoneme.Name.BestAnalysisVernacularAlternative.Text);
		}

		public void DuplicateGrapheme(IPhPhoneme phoneme)
		{
			Console.WriteLine("The phoneme \"{0}\" has the same grapheme as another phoneme.", phoneme.Name.BestAnalysisVernacularAlternative.Text);
		}

		public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The environment \"{0}\" is invalid. Reason: {1}", env.StringRepresentation.Text, reason);
		}

		public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The reduplication form \"{0}\" is invalid. Reason: {1}", form.Form.VernacularDefaultWritingSystem.Text, reason);
		}
	}
}