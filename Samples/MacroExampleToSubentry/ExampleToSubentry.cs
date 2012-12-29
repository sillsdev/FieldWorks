using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.XWorks;

namespace MacroExampleToSubentry
{
	/// <summary>
	/// Sample FLEx macro, does a (somewhat simplistic) promotion of an example sentence to a subentry. It should at least make an MSA.
	/// To make this work, build it and drop the resulting DLL into the EXE directory.
	/// </summary>
	public class ExampleToSubentry : IFlexMacro
	{
		public string CommandName
		{
			get { return "Example to Subentry"; }
		}

		public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
		{
			return target is ILexExampleSentence || target.OwnerOfClass<ILexExampleSentence>() != null;
		}

		public void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length)
		{
			var example = target as ILexExampleSentence ?? target.OwnerOfClass<ILexExampleSentence>();
			var sense = example.OwnerOfClass<ILexSense>();
			var entry = sense.Entry;
			if (!(entry.LexemeFormOA is IMoStemAllomorph))
			{
				MessageBox.Show("This macro only works on stems");
				return;
			}
			var newEntry = entry.Services.GetInstance<ILexEntryFactory>().Create();
			var newSense = entry.Services.GetInstance<ILexSenseFactory>().Create();
			newEntry.SensesOS.Add(newSense);
			newSense.ExamplesOS.Add(example); // moves the chosen example
			// Would be nice to use CopyObject, but currently not public
			newEntry.LexemeFormOA = entry.Services.GetInstance<IMoStemAllomorphFactory>().Create();
			foreach (var ws in entry.LexemeFormOA.Form.AvailableWritingSystemIds)
				newEntry.LexemeFormOA.Form.set_String(ws, entry.LexemeFormOA.Form.get_String(ws));
			foreach (var ws in sense.Gloss.AvailableWritingSystemIds)
				newSense.Gloss.set_String(ws, sense.Gloss.get_String(ws));
			foreach (var ws in sense.Definition.AvailableWritingSystemIds)
				newSense.Definition.set_String(ws, sense.Gloss.get_String(ws));
			// Enhance JohnT: maybe there is more stuff we want to copy?

			//Now make it a subentry
			var ler = entry.Services.GetInstance<ILexEntryRefFactory>().Create();
			newEntry.EntryRefsOS.Add(ler);
			ler.RefType = LexEntryRefTags.krtComplexForm; // must be a complex form to be a subentry
			ler.ComponentLexemesRS.Add(entry);
			ler.PrimaryLexemesRS.Add(entry);
		}

		public Keys PreferredFunctionKey
		{
			get { return Keys.F8; }
		}
	}
}
