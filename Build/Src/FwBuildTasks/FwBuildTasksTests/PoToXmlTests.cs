// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FwBuildTasks;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class PoToXmlTests
	{
		#region FrenchPoData
		internal const string FrenchPoData = @"#  Copyright (c) 2005-2020 SIL International
#  This software is licensed under the LGPL, version 2.1 or later
#  (http://www.gnu.org/licenses/lgpl-2.1.html)
msgid """"
msgstr """"
""Project-Id-Version: FieldWorks 8.1.3\\n""
""Report-Msgid-Bugs-To: FlexErrors@sil.org\\n""
""POT-Creation-Date: 2015-02-03T11:52:27.7123661-06:00\\n""
""PO-Revision-Date: 2015-02-03 19:31-0800\\n""
""Last-Translator: John Doe <john_doe@nowhere.org>\\n""
""Language-Team: French <John_Doe@nowhere.org>\\n""
""MIME-Version: 1.0\\n""
""Content-Type: text/plain; charset=UTF-8\\n""
""Content-Transfer-Encoding: 8bit\\n""
""X-Poedit-Language: French\\n""

#. separate name and abbreviation (space dash space)
#. /Language Explorer/Configuration/Parts/CmPossibilityParts.xml::/PartInventory/bin/part[@id=""CmPossibility-Jt-AbbrAndName""]/lit
#. /Src/FDO/Strings.resx::ksNameAbbrSep
msgid "" - ""
msgstr "" - ""

#. Used in Description on the General tab of the Style dialog box to separate detail items
#. Separator to use between multiple slot names when an irregularly inflected form variant fills two or more inflectional affix slots
#. /Src/FDO/Strings.resx::ksListSep
#. /Src/LexText/ParserUI/ParserUIStrings.resx::ksSlotNameSeparator
msgid "", ""
msgstr "", ""

#. /Language Explorer/Configuration/Parts/CellarParts.xml::/PartInventory/bin/part[@id=""CmPossibility-Jt-AbbreviationDot""]/lit
msgid "". ""
msgstr "". ""

#. /Src/LexText/Interlinear/ITextStrings.resx::ksEndTagSymbol
#. /Src/LexText/LexTextControls/OccurrenceDlg.resx::m_bracketLabel.Text
msgid ""]""
msgstr ""]""

#. Used in CustomListDlg - Display items by combobox
#. /Language Explorer/Configuration/Parts/ReversalParts.xml::/PartInventory/bin/part[@id=""CmPossibility-Detail-AbbreviationRevPOS""]/slice/@label
#. /Src/xWorks/xWorksStrings.resx::ksAbbreviation
msgid ""Abbreviation""
msgstr ""Abréviation""

# JDX:JN
#. /|strings-en.xml::/PossibilityListItemTypeNames/DomainTypes|
msgid ""Academic Domain""
msgstr ""Domaine technique""

#. Label for possible class to add custom fields to
#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""MoAlloAdhocProhib-Jt-Type""]/if/lit
#. /Src/xWorks/xWorksStrings.resx::Allomorph
msgid ""Allomorph""
msgstr ""Allomorphe""

#. /|strings-en.xml::/AlternativeTitles/MoForm-Plural|
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Allomorphs""
msgstr ""Les allomorphes""

#. /|strings-en.xml::/Linguistics/Morphology/Adjacency/Anywhere|
msgid ""anywhere around""
msgstr ""n'importe où autour""

#. /Src/FwCoreDlgs/AddCnvtrDlg.resx::label1.Text
msgid ""&Available Converters:""
msgstr ""Convertisseurs disponibles:""

#. /|strings-en.xml::/DialogStrings/EditMorphBreaks-Example1|
msgid ""blackbird → {0}black{1} {0}bird{1}""
msgstr ""blackbird → {0}black{1} {0}bird{1}""

#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""MoMorphSynAnalysis-Detail-MainEdit""]/slice/@label
msgid ""Category Info.""
msgstr ""Catégorie info.""

#. /|strings-en.xml::/Linguistics/Morphology/TemplateTable/SlotChooserTitle|
msgid ""Choose Slot""
msgstr ""Choisir case""

#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Citation Form""
msgstr ""Autonyme""

#. Button text for Cancel/Close button
#. /Src/Common/FieldWorks/ShareProjectsFolderDlg.resx::m_btnClose.Text
#. /Src/FwCoreDlgs/FwCoreDlgs.resx::kstidClose
msgid ""Close""
msgstr ""Fermer""

# JDX (was ""Date de Création"")
#. field name for Data Notebook records
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
#. /Src/FwResources/FwStrings.resx::kstidDateCreated
msgid ""Date Created""
msgstr ""Créé le""

# JDX (was ""Date de Modification"")
#. field name for Data Notebook records
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
#. /Src/FwResources/FwStrings.resx::kstidDateModified
msgid ""Date Modified""
msgstr ""Modifié le""

#. /Src/FwCoreDlgs/AddCnvtrDlg.resx::$this.Text
msgid ""Encoding Converters""
msgstr ""Convertisseurs d'encodage""

#. /|strings-en.xml::/ClassNames/LexEntry|
#. /Language Explorer/Configuration/Main.xml::/window/contextMenus/menu/menu/item/@label
#. /Src/LexText/LexTextControls/LexTextControls.resx::ksEntry
msgid ""Entry""
msgstr ""Entrée""

#. /|strings-en.xml::/AlternativeTitles/PhEnvironment-Plural|
#. /Language Explorer/Configuration/Grammar/Edit/toolConfiguration.xml::/root/tools/tool/@label
msgid ""Environments""
msgstr ""Environnements""

#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@headerlabel
#. /Language Explorer/Configuration/Parts/LexSenseParts.xml::/PartInventory/bin/part[@id=""LexSense-Detail-MsaCombo""]/slice/@label
#. /Src/LexText/LexTextControls/MSAGroupBox.resx::m_groupBox.Text
msgid ""Grammatical Info.""
msgstr ""Info. grammaticale""

# JDX
#. /Language Explorer/Configuration/Parts/LexEntryParts.xml::/PartInventory/bin/part[@id=""LexEntry-Jt-GrammaticalFunctionsSummary""]/para/lit
msgid ""Grammatical Info. Details""
msgstr ""Détails d'info. grammaticale""

#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Headword""
msgstr ""Entrée de dictionnaire""

#. This is the text for the Help menu on the main menu bar.
#. This is the help category for toolbar/menu items. This string is used in the dialog that allows users to customize their toolbars.
#. /Src/UnicodeCharEditor/CharEditorWindow.resx::m_btnHelp.Text
#. /Src/xWorks/ExportDialog.resx::buttonHelp.Text
msgid ""Help""
msgstr ""Aide""

#  This message does not actually appear anywhere, but is here for unit testing!
msgid ""Junk Test Message""
msgstr """"

#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Lexeme Form""
msgstr ""Forme de lexème""

# @
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Morph Type""
msgstr ""Type de morph""

#. Used in CustomListDlg - Display items by combobox
#. /Language Explorer/Configuration/Grammar/areaConfiguration.xml::/root/controls/parameters/guicontrol/parameters/columns/column/@label
#. /Src/LexText/LexTextControls/LexOptionsDlg.resx::m_chName.Text
#. /Src/LexText/LexTextControls/LexTextControls.resx::ksName
msgid ""Name""
msgstr ""Nom""

# JDX
#. /|strings-en.xml::/EmptyTitles/No-RnGenericRecs|
#. /|strings-en.xml::/Misc/No Records|
#. /Src/LexText/Morphology/MEStrings.resx::ksNoRecords
msgid ""No Records""
msgstr ""Aucun enregistrement""

# JDX
#. {1} will be a long string...don't leave it out.
#. /Src/Utilities/Reporting/ReportingStrings.resx::kstidPleaseEmailThisTo0WithASuitableSubject
msgid """"
""Please email this report to {0} with a suitable subject:\\n""
""\\n""
""{1}""
msgstr """"
""Veuillez envoyer ce rapport à {0} avec un sujet approprié:\\n""
""\\n""
""{1}""

#. Tooltip for the properties menu item
#. /Src/FwCoreDlgs/AddCnvtrDlg.resx::propertiesTab.Text
msgid ""Properties""
msgstr ""Propriétés""

# JDX
#. /|strings-en.xml::/AlternativeTitles/PubSettings|
msgid ""Publication Settings""
msgstr ""Configuration de publication""

# JDX:JN
#. /Language Explorer/Configuration/Lexicon/Dictionary/toolConfiguration.xml::/root/reusableControls/control/parameters/configureLayouts/layoutType/@label
msgid ""Root-based (complex forms as subentries)""
msgstr ""Basé sur les radicals (formes complexes comme sous-entrées)""

#. /|strings-en.xml::/AlternativeTitles/SemanticDomain-Plural|
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Semantic Domains""
msgstr ""Domaines sémantiques""

#. /|strings-en.xml::/ClassNames/LexSense|
#. /Language Explorer/Configuration/Main.xml::/window/contextMenus/menu/menu/item/@label
#. /Src/LexText/LexTextControls/LexTextControls.resx::ksSense
#. /Src/xWorks/xWorksStrings.resx::Sense
msgid ""Sense""
msgstr ""Sens""

#. /|strings-en.xml::/AlternativeTitles/LexSense-Plural|
#. /Language Explorer/Configuration/Parts/ReversalParts.xml::/PartInventory/bin/part[@id=""ReversalIndexEntry-Detail-CurrentSenses""]/slice/@label
#. /Src/LexText/Lexicon/LexEdStrings.resx::ksSenses
msgid ""Senses""
msgstr ""Les sens""

# JDX
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
#. /Src/Common/Controls/XMLViews/XMLViewsStrings.resx::ksShowAsHeadwordIn
msgid ""Show As Headword In""
msgstr ""Afficher comme entrée de dictionnaire dans""

#. /|strings-en.xml::/Linguistics/Morphology/Adjacency/Somewhere to right|
msgid ""somewhere after""
msgstr ""quelque part après""

#. /|strings-en.xml::/Linguistics/Morphology/Adjacency/Somewhere to left|
msgid ""somewhere before""
msgstr ""quelque part avant""

# JDX:JN
#. /|strings-en.xml::/Linguistics/Morphology/TemplateTable/Stem|
#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""MoInflAffixTemplate-Jt-TemplateTabley""]/table/if/row/cell/para/lit
msgid ""STEM""
msgstr ""BASE""

#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""PhEnvironment-Detail-StringRepresentation""]/slice/@label
msgid ""String Representation""
msgstr ""Représentation de chaîne""

#  cfr contexte
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Summary Definition""
msgstr ""Résumé de la définition""

# JDX
#. /|strings-en.xml::/Linguistics/Morphology/TemplateTable/SlotChooserInstructionalText|
msgid ""The following slots are available for the category {0}.""
msgstr ""Les cases suivantes sont disponibles pour la categorie {0}.""

# JDX:JN
#. /|strings-en.xml::/DialogStrings/ChangeMorphTypeLoseStemNameGramInfo|
msgid ""The stem name and some grammatical information will be lost! Do you still want to continue?""
msgstr ""Le nom de base et certaine information grammaticale seront perdus! Voulez-vous continuer?""

# JDX
#. /Language Explorer/Configuration/Lexicon/browseDialogColumns.xml::/doc/browseColumns/column/@label
msgid ""Variants""
msgstr ""Variantes""

#. Used in the diffTool window when there is a writing system difference in the text
#. /Src/TE/DiffView/TeDiffViewResources.resx::kstidWritingSystemDiff
msgid ""Writing System Difference""
msgstr """"

#. /Src/TE/TeDialogs/FilesOverwriteDialog.resx::btnYesToAll.Text
msgid ""Yes to &All""
msgstr ""Oui a tous""

#. {0} is a vernacular string. Sometimes it might not render well in the message box font. It's remotely possible it might be empty.
#. /Src/FDO/Strings.resx::ksWordformUsedByChkRef
msgid ""You cannot delete this wordform because it is used as an occurrence of a biblical term ({0}) in Translation Editor.""
msgstr """"

# JDX
#. This text will be displayed if a user tries to exit the diff dialog before all the differences have been taken care of.
#. /Src/TE/TeResources/TeStrings.resx::kstidExitDiffMsg
msgid ""You still have {0} difference(s) left.  Are you sure you want to exit?""
msgstr ""Il reste {0} différences. Êtes-vous sûr de vouloir quitter?""

msgid ""You don't know how to translate this yet do you?""
msgstr ""Que?""
#, fuzzy

# JDX
#~ msgid ""Check for _Updates...""
#~ msgstr ""Rechercher les mises à jo_ur...""
# JDX
#~ msgid ""Get Lexicon and _Merge with this Project...""
#~ msgstr ""Obtenir un lexique et le fusionner avec ce projet...""
# JDX
#, fuzzy
#~ msgid ""Send this Lexicon for the first time...""
#~ msgstr ""Envoyer ce lexique pour la première fois...""

";
		#endregion FrenchPoData

		[Test]
		public void ReadPoData()
		{
			var srIn = new StringReader(FrenchPoData);
			var dictFrenchPo = PoToXml.ReadPoFile(srIn, null);
			var rgsPoStrings = dictFrenchPo.ToList();
			var postr0 = rgsPoStrings[0].Value;
			Assert.IsNotNull(postr0, "French po string[0] has data");
			Assert.IsNotNull(postr0.MsgId, "French po string[0] has MsgId data");
			Assert.AreEqual(1, postr0.MsgId.Count, "French po string[0] has one line of MsgId data");
			Assert.AreEqual(" - ", postr0.MsgId[0], "French po string[0] has the expected MsgId data");
			Assert.AreEqual(" - ", postr0.MsgIdAsString(), "French po string[0] is ' - '");
			Assert.AreEqual(1, postr0.MsgStr.Count, "French po string[0] has one line of MsgStr data");
			Assert.AreEqual(" - ", postr0.MsgStr[0], "French po string[0] MsgStr is ' - '");
			Assert.IsNull(postr0.UserComments, "French po string[0] has no User Comments (as expected)");
			Assert.IsNull(postr0.References, "French po string[0] has no Reference data (as expected)");
			Assert.IsNull(postr0.Flags, "French po string[0] has no Flags data (as expected)");
			Assert.IsNotNull(postr0.AutoComments, "French po string[0] has Auto Comments");
			Assert.AreEqual(3, postr0.AutoComments.Count, "French po string[0] has three lines of Auto Comments");
			Assert.AreEqual("separate name and abbreviation (space dash space)", postr0.AutoComments[0], "French po string[0] has the expected first line of Auto Comment");

			var postr5 = rgsPoStrings[5].Value;
			Assert.IsNotNull(postr5, "French po string[5] has data");
			Assert.IsNotNull(postr5.MsgId, "French po string[5] has MsgId data");
			Assert.AreEqual(1, postr5.MsgId.Count, "French po string[5] has one line of MsgId data");
			Assert.AreEqual("Academic Domain", postr5.MsgId[0], "French po string[5] has the expected MsgId data");
			Assert.AreEqual("Academic Domain", postr5.MsgIdAsString(), "French po string[5] is 'Academic Domain'");
			Assert.AreEqual(1, postr5.MsgStr.Count, "French po string[5] has one line of MsgStr data");
			Assert.AreEqual("Domaine technique", postr5.MsgStr[0], "French po string[5] has the expected MsgStr data");
			Assert.IsNotNull(postr5.UserComments, "French po string[5] has User Comments");
			Assert.AreEqual(1, postr5.UserComments.Count, "French po string[5] has one line of User Comments");
			Assert.AreEqual("JDX:JN", postr5.UserComments[0], "French po string[5] has the expected User Comment");
			Assert.IsNull(postr5.References, "French po string[5] has no Reference data (as expected)");
			Assert.IsNull(postr5.Flags, "French po string[5] has no Flags data (as expected)");
			Assert.IsNotNull(postr5.AutoComments, "French po string[5] has Auto Comments");
			Assert.AreEqual(1, postr5.AutoComments.Count, "French po string[5] has one line of Auto Comments");
			Assert.AreEqual("/|strings-en.xml::/PossibilityListItemTypeNames/DomainTypes|", postr5.AutoComments[0], "French po string[5] has the expected Auto Comment");

			var postr48 = rgsPoStrings[48].Value;
			Assert.IsNotNull(postr48, "French po string[48] has data");
			Assert.IsNotNull(postr48.MsgId, "French po string[48] has MsgId data");
			Assert.AreEqual(1, postr48.MsgId.Count, "French po string[48] has one line of MsgId data");
			Assert.AreEqual("You still have {0} difference(s) left.  Are you sure you want to exit?", postr48.MsgId[0], "French po string[48] has the expected MsgId data");
			Assert.AreEqual("You still have {0} difference(s) left.  Are you sure you want to exit?", postr48.MsgIdAsString(),
				"French po string[48] is 'You still have {0} difference(s) left.  Are you sure you want to exit?'");
			Assert.AreEqual(1, postr48.MsgStr.Count, "French po string[48] has one line of MsgStr data");
			Assert.AreEqual("Il reste {0} différences. Êtes-vous sûr de vouloir quitter?", postr48.MsgStr[0], "French po string[48] has the expected MsgStr data");
			Assert.IsNotNull(postr48.UserComments, "French po string[48] has User Comments");
			Assert.AreEqual(1, postr48.UserComments.Count, "French po string[48] has one line of User Comments");
			Assert.AreEqual("JDX", postr48.UserComments[0], "French po string[48] has the expected User Comment");
			Assert.IsNull(postr48.References, "French po string[48] has no Reference data (as expected)");
			Assert.IsNull(postr48.Flags, "French po string[48] has no Flags data (as expected)");
			Assert.IsNotNull(postr48.AutoComments, "French po string[48] has Auto Comments");
			Assert.AreEqual(2, postr48.AutoComments.Count, "French po string[48] has two lines of Auto Comments");
			Assert.AreEqual("This text will be displayed if a user tries to exit the diff dialog before all the differences have been taken care of.",
				postr48.AutoComments[0], "French po string[48] has the expected first line of Auto Comment");
			Assert.AreEqual("/Src/TE/TeResources/TeStrings.resx::kstidExitDiffMsg",
				postr48.AutoComments[1], "French po string[48] has the expected second line of Auto Comment");

			var postr49 = rgsPoStrings[49].Value;
			Assert.IsNotNull(postr49, "French po string[49] has data");
			Assert.IsNotNull(postr49.MsgId, "French po string[49] has MsgId data");
			Assert.AreEqual(1, postr49.MsgId.Count, "French po string[49] has one line of MsgId data");
			Assert.AreEqual("You don't know how to translate this yet do you?", postr49.MsgId[0], "French po string[49] has the expected MsgId data");
			Assert.AreEqual("Que?", postr49.MsgStrAsString());
			Assert.IsNotNull(postr49.Flags);
			Assert.AreEqual(postr49.Flags[0], "fuzzy");
			Assert.AreEqual(50, dictFrenchPo.Count);
		}

		[TestCase(@"/Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""AllomorphAdjacency""]/@captionformat", null)]
		[TestCase(@"/Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""AllomorphAdjacency""]", "AllomorphAdjacency")]
		public void FindContextHelpId(string comment, string id)
		{
			Assert.AreEqual(id, PoToXml.FindContextHelpId(comment));
		}

		#region StringsDeData
		private const string DeStringsDataBase = @"<?xml version='1.0' encoding='UTF-8'?>
<strings>
	<group id='Misc'>
		<string id='No Records' txt='Keine Ergebnisse'/>
	</group>
	<group id='ClassNames'>
		<string id='LexEntry' txt='Eintrag'/>
		<string id='LexSense' txt='Sinn'/>
	</group>
	<group id='PossibilityListItemTypeNames'>
		<string id='DomainTypes' txt='Akademischer Bereich'/>
	</group>
	<group id='DialogStrings'>
		<string id='EditMorphBreaks-Example1' txt='Hackbrett → {0}hack{1} {0}Brett{1}'/>
		<string id='ChangeMorphTypeLoseStemNameGramInfo' txt='Der Stamm und etwas grammatische Information wird verloren! Wollen Sie trotzdem vortfahren?'/>
	</group>
	<group id='Linguistics'>
		<group id='Morphology'>
			<group id='Adjacency'>
				<string id='Somewhere to left' txt='irgendwo vor'/>
				<string id='Somewhere to right' txt='irgendwo nach'/>
			</group>
			<group id='TemplateTable'>
				<string id='Stem' txt='STAMM'/>
				<string id='SlotChooserTitle' txt='Slot wählen'/>
				<string id='SlotChooserInstructionalText' txt='The following slots are available for the category {0}.'/>
			</group>
		</group>
	</group>";
		private const string DeStringsData = DeStringsDataBase + @"
</strings>";
		#endregion StringsDeData

		#region DePoData
		private const string DePoData = @"
# Created from FieldWorks sources
# Copyright (c) 2020 SIL International
# This software is licensed under the LGPL, version 2.1 or later
# (http://www.gnu.org/licenses/lgpl-2.1.html)
# " + @"
msgid """"
msgstr """"
""Project-Id-Version: FieldWorks 9.0.8\n""
""Report-Msgid-Bugs-To: FlexErrors@sil.org\n""
""POT-Creation-Date: 2020-02-26T15:35:41.1980734-06:00\n""
""PO-Revision-Date: \n""
""Last-Translator: Full Name <email@address>\n""
""Language-Team: Language <email@address>\n""
""MIME-Version: 1.0\n""
""Content-Type: text/plain; charset=UTF-8\n""
""Content-Transfer-Encoding: 8bit\n""
""X-Poedit-Language: de\n""
""X-Poedit-Country: DE\n""

#. /Language Explorer/Configuration/Parts/Cellar.fwlayout::/LayoutInventory/layout[""CmPossibility-jtview-bestAnalysisAbbr""]/@label
msgid ""Abbreviation (Best Analysis)""
msgstr ""Abkürzung (Bestes Analyse)""

#. /Language Explorer/DefaultConfigurations/Dictionary/Hybrid.fwdictconfig:://ConfigurationItem/@name
#. /Language Explorer/DefaultConfigurations/Dictionary/Lexeme.fwdictconfig:://ConfigurationItem/@name
#. /Language Explorer/DefaultConfigurations/Dictionary/Root.fwdictconfig:://ConfigurationItem/@name
#. (String used 143 times.)
msgid ""Comment""
msgstr ""Kommentar""

#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""CmdInsertCustomItem""]
#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""CmdInsertLexEntryType""]
#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""CmdInsertPossibility""]
#. (String used 4 times.)
msgid ""Create a new {0}.""
msgstr ""Ein neues {0} erstellen.""

#. :://ConfigurationItem/@name
#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""MoAlloAdhocProhib-Jt-Type""]/if/lit
#. /Language Explorer/Configuration/Parts/MorphologyParts.xml::/PartInventory/bin/part[@id=""MoAlloAdhocProhib-Jt-Type""]/if/span/lit
#. /Language Explorer/Configuration/Parts/Reversal.fwlayout::/LayoutInventory/layout[""MoForm-jtview-publishForReversal""]/part[@ref=""FormPub""]/@label
#. (String used 21 times, but those listed here suffice for testing)
msgid ""Allomorph""
msgstr ""Allomorph""

#. /Language Explorer/Configuration/Parts/WFIParts.xml::/PartInventory/bin/part[@id=""WfiAnalysis-Jt-HumanApprovedSummary""]/para/lit
msgid ""Analysis ""
msgstr ""Analyse ""

#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""AllomorphAdjacency""]/@captionformat
msgid ""Choose {0}""
msgstr ""{0} wählen""

#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""AllomorphAdjacency""]
msgid ""Click on the button.""
msgstr ""Klicken Sie auf die Taste.""

#. /Language Explorer/Configuration/ContextHelp.xml::/strings/item[@id=""CmdCreateProjectShortcut""]
msgid ""Create a desktop shortcut to this project.""
msgstr ""Eine Desktop-Verknüpfung zu diesem Projekt erstellen.""

#. /Language Explorer/Configuration/Main.xml::/window/commands/command/@label
msgid ""_About FLEx Bridge...""
msgstr ""_Über FLEx Bridge...""

#. /Language Explorer/Configuration/Main.xml::/window/commands/command/@label
msgid ""_About Language Explorer...""
msgstr ""_Über Language Explorer""

";
		#endregion DePoData

		[Test]
		public void StringsPreserved()
		{
			using (var testDir = new TemporaryFolder(GetType().Name))
			{
				var poFile = testDir.Combine("mesages.de.po");
				File.WriteAllText(poFile, DePoData);
				var stringsFile = testDir.Combine("strings-de.xml");
				File.WriteAllText(stringsFile, DeStringsData);

				// SUT
				PoToXml.StoreLocalizedStrings(poFile, stringsFile, null);

				var fullFileContent = File.ReadAllText(stringsFile);
				AssertThatXmlStartsWith(XDocument.Parse(DeStringsData).Root, XDocument.Parse(fullFileContent).Root);
				Assert.Greater(fullFileContent.Length, DeStringsData.Length + 400);
			}
		}

		[Test]
		[SuppressMessage("ReSharper", "PossibleNullReferenceException", Justification = "If it throws, we'll know to fix the test!")]
		public void NewStringsAdded()
		{
			using (var testDir = new TemporaryFolder(GetType().Name))
			{
				var poFile = testDir.Combine("mesages.de.po");
				File.WriteAllText(poFile, DePoData);
				var stringsFile = testDir.Combine("strings-de.xml");
				File.WriteAllText(stringsFile, DeStringsData);

				// SUT
				PoToXml.StoreLocalizedStrings(poFile, stringsFile, null);

				var result = File.ReadAllText(stringsFile);
				// The resulting file should contain the 5 original groups plus 3 new (attributes, literals, context help)
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/strings/group", 8);
				const string attGroupXpath = "/strings/group[@id='LocalizedAttributes']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(attGroupXpath, 1);
				const string attStringXpath = attGroupXpath + "/string";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(attStringXpath, 6);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					attStringXpath + "[@id='Abbreviation (Best Analysis)' and @txt='Abkürzung (Bestes Analyse)']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					attStringXpath + "[@id='Allomorph' and @txt='Allomorph']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					attStringXpath + "[@id='Choose {0}' and @txt='{0} wählen']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					attStringXpath + "[@id='Comment' and @txt='Kommentar']", 1);
				const string litGroupXpath = "/strings/group[@id='LocalizedLiterals']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(litGroupXpath, 1);
				const string litStringXpath = litGroupXpath + "/string";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(litStringXpath, 2);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					litStringXpath + "[@id='Allomorph' and @txt='Allomorph']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					litStringXpath + "[@id='Analysis ' and @txt='Analyse ']", 1);
				const string helpGroupXpath = "/strings/group[@id='LocalizedContextHelp']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(helpGroupXpath, 1);
				const string helpStringXpath = helpGroupXpath + "/string";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(helpStringXpath, 5);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='AllomorphAdjacency']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='AllomorphAdjacency' and @txt='Klicken Sie auf die Taste.']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='CmdInsertCustomItem' and @txt='Ein neues {0} erstellen.']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='CmdInsertLexEntryType' and @txt='Ein neues {0} erstellen.']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='CmdInsertPossibility' and @txt='Ein neues {0} erstellen.']", 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					helpStringXpath + "[@id='CmdCreateProjectShortcut' and @txt='Eine Desktop-Verknüpfung zu diesem Projekt erstellen.']", 1);
			}
		}

		private static void AssertThatXmlStartsWith(XElement expected, XElement actual)
		{
			if (expected == null)
				return;
			if (actual == null)
				Assert.Fail($"Expected XML starting with \n{expected}\n but was null");
			AssertThatXmlStartsWithHelper(expected, actual);
		}

		private static void AssertThatXmlEquals(XElement expected, XElement actual)
		{
			if (expected == null)
			{
				Assert.IsNull(actual, actual == null ? null : XmlToPo.ComputePathComment(actual, null, null));
				return;
			}
			if (actual == null)
				Assert.Fail($"Expected a node matching {ComputeXPath(expected)}, but was null");

			Assert.AreEqual(expected.Elements().Count(), actual.Elements().Count(),
				$"Incorrect number of children under {ComputeXPath(expected)}");
			AssertThatXmlStartsWithHelper(expected, actual);
		}

		/// <summary>
		/// Assert that all attributes of 'expected' have the same values on 'actual' and that there are no additional attributes.
		/// Assert that all of the elements under 'expected' appear in order under 'actual'.
		/// There may be additional child elements under 'actual' following the children of 'expected',
		/// but there may be no additional child elements before or between those matching the children of 'expected'.
		/// </summary>
		private static void AssertThatXmlStartsWithHelper(XElement expected, XElement actual)
		{
			// verify attributes
			var expectedAtts = expected.Attributes().ToArray();
			var actualAtts = actual.Attributes().ToArray();
			Assert.AreEqual(expectedAtts.Length, actualAtts.Length,
				$"Incorrect number of attributes on {ComputeXPath(expected)}");
			for (var i = 0; i < expectedAtts.Length; i++)
			{
				var message = ComputeXPath(expected, expectedAtts[i]);
				Assert.AreEqual(expectedAtts[i].Name, actualAtts[i].Name, message);
				Assert.AreEqual(expectedAtts[i].Value, actualAtts[i].Value, message);
			}

			// verify children
			using (var actualIter = actual.Elements().GetEnumerator())
			{
				foreach (var expectedElt in expected.Elements())
				{
					if (!actualIter.MoveNext())
						Assert.Fail($"No match found for {ComputeXPath(expectedElt)}");
					AssertThatXmlEquals(expectedElt, actualIter.Current);
				}
			}
		}

		private static string ComputeXPath(XElement element, XAttribute attribute = null)
		{
			var bldr = new StringBuilder();
			if (attribute != null)
				bldr.Append($"[@{attribute.Name.LocalName}='{attribute.Value}']");

			while (element != null)
			{
				bldr.Insert(0, $"/{element.Name.LocalName}[@id='{element.Attribute("id")?.Value}']");
				element = element.Parent;
			}

			return bldr.ToString();
		}
	}
}
