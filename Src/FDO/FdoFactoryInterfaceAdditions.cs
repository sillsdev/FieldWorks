using System;
using System.Collections.Generic;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Internal interface for use by the persistence code to bootstrap a new ICmAgent.
	/// </summary>
	internal interface ICmAgentFactoryInternal : ICmAgentFactory
	{
		/// <summary>
		/// Create a new ICmAgent instance with the given parameters.
		/// The owner will the the language project (AnalyzingAgents property)
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="isHuman"></param>
		/// <param name="version">Optional version information. (May be null.)</param>
		/// <returns></returns>
		ICmAgent Create(Guid guid, int hvo, bool isHuman, string version);
	}

	/// <summary>
	/// Add smart create methods
	/// </summary>
	public partial interface ICmTranslationFactory
	{
		/// <summary>
		/// Create a well-formed ICmTranslation which has an owner and Type property set
		/// </summary>
		ICmTranslation Create(IStTxtPara owner, ICmPossibility translationType);

		/// <summary>
		/// Create a well-formed ICmTranslation which has an owner and Type property set
		/// </summary>
		ICmTranslation Create(ILexExampleSentence owner, ICmPossibility translationType);
	}

	public partial interface ICmTranslation
	{
		/// <summary>
		/// Get a set of all writing systems used for this translation.
		/// </summary>
		HashSet<IWritingSystem> AvailableWritingSystems { get; }
	}

	/// <summary>
	/// Public interface for use by the custom list code to create a new CmPossibilityList.
	/// </summary>
	public partial interface ICmPossibilityListFactory
	{
		/// <summary>
		/// Create a new unowned (Custom) ICmPossibilityList instance.
		/// </summary>
		/// <param name="listName"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		ICmPossibilityList CreateUnowned(string listName, int ws);

		/// <summary>
		/// Create a new unowned (Custom) ICmPossibilityList instance.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="listName"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		ICmPossibilityList CreateUnowned(Guid guid, string listName, int ws);
	}

	/// <summary>
	/// Internal interface for use by the persistence code to bootstrap a new CmPossibilityList.
	/// </summary>
	internal interface ICmPossibilityListFactoryInternal : ICmPossibilityListFactory
	{
		/// <summary>
		/// Create a new ICmPossibilityList instance with the given parameters.
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <returns></returns>
		ICmPossibilityList Create(Guid guid, int hvo);
	}

	public partial interface ILexRefTypeFactory
	{
		/// <summary>
		/// Constructor to build a ILexRefType with specific attributes
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		ILexRefType Create(Guid guid, ILexRefType owner);
	}

	/// <summary>
	/// Internal interface for use by merging code to create a copy of a CmPerson that exists in another project.
	/// </summary>
	public partial interface ICmPersonFactory
	{
		/// <summary>
		/// Create a new ICmPerson instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmPerson Create(Guid guid, ICmPossibilityList owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface ICmPossibilityFactory
	{
		/// <summary>
		/// Create a new ICmPossibility instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmPossibility Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new ICmPossibility instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		ICmPossibility Create(Guid guid, ICmPossibility owner);
	}

	/// <summary>
	/// Internal interface for use by the persistence code to bootstrap a new CmPossibility.
	/// </summary>
	internal interface ICmPossibilityFactoryInternal : ICmPossibilityFactory
	{
		/// <summary>
		/// Create a new ICmPossibility instance with the given parameters.
		/// This will add the new ICmPossibility to the owning list at the given index
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning list. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <returns></returns>
		ICmPossibility Create(Guid guid, int hvo, ICmPossibilityList owner, int index);

		/// <summary>
		/// Create a new ICmPossibility instance with the given parameters.
		/// This will add the new ICmPossibility to SubPossibilities of the owner at the given index
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="hvo"></param>
		/// <param name="owner"></param>
		/// <param name="index"></param>
		ICmPossibility Create(Guid guid, int hvo, ICmPossibility owner, int index);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface ICmAnthroItemFactory
	{
		/// <summary>
		/// Create a new ICmAnthroItem instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmAnthroItem Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new ICmAnthroItem instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		ICmAnthroItem Create(Guid guid, ICmAnthroItem owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface ICmLocationFactory
	{
		/// <summary>
		/// Create a new Location instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmLocation Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new Location instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		ICmLocation Create(Guid guid, ICmLocation owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface ICmSemanticDomainFactory
	{
		/// <summary>
		/// Create a new ICmSemanticDomain instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		ICmSemanticDomain Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new ICmSemanticDomain instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		ICmSemanticDomain Create(Guid guid, ICmSemanticDomain owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface IPartOfSpeechFactory
	{
		/// <summary>
		/// Create a new IPartOfSpeech instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		IPartOfSpeech Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new IPartOfSpeech instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		IPartOfSpeech Create(Guid guid, IPartOfSpeech owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface ICmAnnotationDefnFactory
	{
		/// <summary>
		/// Create a new ICmAnnotationDefn instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		ICmAnnotationDefn Create(Guid guid, ICmAnnotationDefn owner);
	}

	internal interface ICmAnnotationDefnFactoryInternal : ICmAnnotationDefnFactory
	{
		/// <summary>
		/// Create a new ICmAnnotationDefn instance with the given parameters.
		/// This will add the new ICmAnnotationDefn to the owning list at the given index
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning list. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <returns></returns>
		ICmAnnotationDefn Create(Guid guid, int hvo, ICmPossibilityList owner, int index);

		/// <summary>
		/// Create a new ICmAnnotationDefn instance with the given parameters.
		/// This will add the new ICmAnnotationDefn to SubPossibilities of the owner at the given index
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="hvo"></param>
		/// <param name="owner"></param>
		/// <param name="index"></param>
		ICmAnnotationDefn Create(Guid guid, int hvo, ICmPossibility owner, int index);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface IFsFeatStrucTypeFactory
	{
		/// <summary>
		/// Create a new instance with the given guid and owner.
		/// It will be added to the end of the owner's TypesOC.
		/// </summary>
		IFsFeatStrucType Create(Guid guid, IFsFeatureSystem owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface IFsClosedFeatureFactory
	{
		/// <summary>
		/// Create a new instance with the given guid and owner.
		/// It will be added to the end of the owner's FeaturesOC.
		/// </summary>
		IFsClosedFeature Create(Guid guid, IFsFeatureSystem owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface IFsComplexFeatureFactory
	{
		/// <summary>
		/// Create a new instance with the given guid and owner.
		/// It will be added to the end of the owner's FeaturesOC.
		/// </summary>
		IFsComplexFeature Create(Guid guid, IFsFeatureSystem owner);
	}

	/// <summary>
	/// Methods added for importing objects with known/fixed guids.
	/// </summary>
	public partial interface IFsSymFeatValFactory
	{
		/// <summary>
		/// Create a new instance with the given guid and owner.
		/// It will be added to the end of the owner's ValuesOC.
		/// </summary>
		IFsSymFeatVal Create(Guid guid, IFsClosedFeature owner);
	}
	/// <summary>
	/// IMoMorphType factory additions.
	/// </summary>
	public partial interface IMoMorphTypeFactory
	{
		/// <summary>
		/// Create a new IMoMorphType instance with the given guid and owner.
		/// It will be added to the end of the Possibilities list.
		/// </summary>
		IMoMorphType Create(Guid guid, ICmPossibilityList owner);
		/// <summary>
		/// Create a new IMoMorphType instance with the given guid and owner.
		/// It will be added to the end of the SubPossibilities list.
		/// </summary>
		IMoMorphType Create(Guid guid, IMoMorphType owner);
	}

	/// <summary>
	/// Internal interface for use by the persistence code to bootstrap a new CmPossibility.
	/// </summary>
	internal interface IMoMorphTypeFactoryInternal : IMoMorphTypeFactory
	{
		/// <summary>
		/// Create a new IMoMorphType instance with the given parameters.
		/// This will add the new IMoMorphType to the owning list at the given index
		/// </summary>
		/// <param name="guid">Globally defined guid.</param>
		/// <param name="hvo">Some session unique HVO id.</param>
		/// <param name="owner">Owning list. (Must not be null.)</param>
		/// <param name="index">Index in owning property.</param>
		/// <param name="name"></param>
		/// <param name="nameWs"></param>
		/// <param name="abbreviation"></param>
		/// <param name="abbreviationWs"></param>
		/// <param name="prefix"></param>
		/// <param name="postfix"></param>
		/// <param name="secondaryOrder"></param>
		/// <returns></returns>
		void Create(Guid guid, int hvo, ICmPossibilityList owner, int index, ITsString name, int nameWs, ITsString abbreviation, int abbreviationWs, string prefix, string postfix, int secondaryOrder);
	}

	/// <summary>
	/// Internal interface for creating ILexEntryType objects.
	/// </summary>
	internal interface ILexEntryTypeFactoryInternal : ILexEntryTypeFactory
	{
		/// <summary>
		/// Create ILexEntryType instance.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="hvo"></param>
		/// <param name="owner"></param>
		/// <param name="index"></param>
		/// <param name="name"></param>
		/// <param name="nameWs"></param>
		/// <param name="abbreviation"></param>
		/// <param name="abbreviationWs"></param>
		/// <returns></returns>
		void Create(Guid guid, int hvo, ICmPossibilityList owner, int index, ITsString name, int nameWs, ITsString abbreviation, int abbreviationWs);
	}

	/// <summary>
	/// IStStyle factory additions.
	/// </summary>
	public partial interface IStStyleFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style on the specified style list.
		/// </summary>
		/// <param name="styleList">The style list to add the style to</param>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// <param name="isBuiltin">True for a builtin style, otherwise, false.</param>
		/// <returns>The new created (and properly owned style.</returns>
		/// ------------------------------------------------------------------------------------
		IStStyle Create(IFdoOwningCollection<IStStyle> styleList, string name,
			ContextValues context, StructureValues structure, FunctionValues function,
			bool isCharStyle, int userLevel, bool isBuiltin);

		/// <summary>
		/// Create a new style with a fixed guid.
		/// </summary>
		/// <param name="cache">project cache</param>
		/// <param name="guid">the factory set guid</param>
		/// <returns>A style interface</returns>
		IStStyle Create(FdoCache cache, Guid guid);
	}

	public partial interface ICmBaseAnnotationFactory
	{
		/// <summary>
		/// Create an ownerless object.  This is used in import.
		/// </summary>
		ICmBaseAnnotation CreateOwnerless();
	}

	public partial interface ICmIndirectAnnotationFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new indirect annotation
		/// </summary>
		/// <param name="annType">The type of indirect annotation</param>
		/// <param name="cbaAppliesTo">Zero or more annotations to which this annotation applies
		/// (typically a single segment)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ICmIndirectAnnotation Create(ICmAnnotationDefn annType, params ICmAnnotation[] cbaAppliesTo);
		/// <summary>
		/// Create an ownerless object.  This is used in import.
		/// </summary>
		ICmIndirectAnnotation CreateOwnerless();
	}

	public partial interface IConstChartTagFactory
	{
		/// <summary>
		/// Creates a new Chart Marker from a list of Chart Marker possibilities
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="marker"></param>
		/// <returns></returns>
		IConstChartTag Create(IConstChartRow row, int insertAt, ICmPossibility column, ICmPossibility marker);

		/// <summary>
		/// Creates a new user-added Missing Text Marker
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		IConstChartTag CreateMissingMarker(IConstChartRow row, int insertAt, ICmPossibility column);
	}

	public partial interface IConstChartClauseMarkerFactory
	{
		/// <summary>
		/// Creates a new Chart Clause Marker (reference to dependent/speech/song clauses)
		/// Caller needs to setup the rows with the correct parameters (ClauseType, etc.).
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="depClauses">The chart rows that are dependent/speech/song</param>
		/// <returns></returns>
		IConstChartClauseMarker Create(IConstChartRow row, int insertAt, ICmPossibility column,
			IEnumerable<IConstChartRow> depClauses);
	}

	public partial interface IConstChartMovedTextMarkerFactory
	{
		/// <summary>
		/// Creates a new Chart Moved Text Marker (shows where some text was moved from).
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="fPreposed">True if the CCWG was 'moved' earlier than its 'normal' position</param>
		/// <param name="wordGroup">The CCWG that was 'moved'</param>
		/// <returns></returns>
		IConstChartMovedTextMarker Create(IConstChartRow row, int insertAt, ICmPossibility column,
			bool fPreposed, IConstChartWordGroup wordGroup);
	}

	public partial interface IConstChartWordGroupFactory
	{
		/// <summary>
		/// Creates a new Chart Word Group from selected AnalysisOccurrence objects
		/// </summary>
		/// <param name="row"></param>
		/// <param name="insertAt"></param>
		/// <param name="column"></param>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		IConstChartWordGroup Create(IConstChartRow row, int insertAt, ICmPossibility column,
			AnalysisOccurrence begPoint, AnalysisOccurrence endPoint);
	}

	public partial interface IConstChartRowFactory
	{
		/// <summary>
		/// Creates a new Chart Row with the specified row number/letter label
		/// at the specified location in the specified chart.
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="insertAt"></param>
		/// <param name="rowLabel"></param>
		/// <returns></returns>
		IConstChartRow Create(IDsConstChart chart, int insertAt, ITsString rowLabel);
	}

	public partial interface ITextTagFactory
	{
		/// <summary>
		/// Creates a new TextTag object on a text with a possibility item, a beginning point in the text,
		/// and an ending point in the text.
		/// </summary>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		/// <param name="tagPoss"></param>
		/// <returns></returns>
		ITextTag CreateOnText(AnalysisOccurrence begPoint, AnalysisOccurrence endPoint, ICmPossibility tagPoss);
	}

	public partial interface IDsConstChartFactory
	{
		/// <summary>
		/// Creates a new Constituent Chart object on a language project with a particular template
		/// and based on a particular text.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="text"></param>
		/// <param name="template"></param>
		/// <returns></returns>
		IDsConstChart Create(IDsDiscourseData data, IStText text, ICmPossibility template);
	}

	public partial interface IVirtualOrderingFactory
	{
		/// <summary>
		/// Creates a new Virtual Ordering object for a particular property
		/// given a particular sequence of objects.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="fieldName"></param>
		/// <param name="desiredSequence"></param>
		/// <returns></returns>
		IVirtualOrdering Create(ICmObject parent, string fieldName, IEnumerable<ICmObject> desiredSequence);
	}

	public partial interface IWfiWordformFactory
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="tssForm"></param>
		/// <returns></returns>
		IWfiWordform Create(ITsString tssForm);
	}

	public partial interface IWfiAnalysisFactory
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="glossFactory">For creating a gloss for the first in analysis.Meanings</param>
		/// <returns></returns>
		IWfiAnalysis Create(IWfiWordform owner, IWfiGlossFactory glossFactory);
	}

	/// <summary>
	/// ILexEntry factory additions.
	/// </summary>
	public partial interface ILexEntryFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new entry.
		/// </summary>
		/// <param name="morphType">Type of the morph.</param>
		/// <param name="tssLexemeForm">The TSS lexeme form.</param>
		/// <param name="gloss">The gloss, will be set in default analysis</param>
		/// <param name="sandboxMSA">The dummy MSA.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ILexEntry Create(IMoMorphType morphType, ITsString tssLexemeForm, string gloss,
			SandboxGenericMSA sandboxMSA);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new entry.
		/// </summary>
		/// <param name="morphType">Type of the morph.</param>
		/// <param name="tssLexemeForm">The TSS lexeme form.</param>
		/// <param name="gloss">The gloss</param>
		/// <param name="sandboxMSA">The dummy MSA.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ILexEntry Create(IMoMorphType morphType, ITsString tssLexemeForm, ITsString gloss,
			SandboxGenericMSA sandboxMSA);

		/// <summary>
		/// Creates an entry with a form in default vernacular and a sense with a gloss in default analysis.
		/// </summary>
		/// <param name="entryFullForm">entry form including any markers</param>
		/// <param name="senseGloss"></param>
		/// <param name="msa"></param>
		/// <returns></returns>
		ILexEntry Create(string entryFullForm, string senseGloss, SandboxGenericMSA msa);

		/// <summary>
		///
		/// </summary>
		/// <param name="entryComponents"></param>
		/// <returns></returns>
		ILexEntry Create(LexEntryComponents entryComponents);

		/// <summary>
		/// Create a new entry with the given guid owned by the given owner.
		/// </summary>
		ILexEntry Create(Guid guid, ILexDb owner);
	}

	/// <summary>
	/// ILexSense factory additions.
	/// </summary>
	public partial interface ILexSenseFactory
	{
		/// <summary>
		/// Create a new sense and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sandboxMSA"></param>
		/// <param name="gloss">string to set in the DefaultAnalysis ws for the gloss</param>
		/// <returns></returns>
		ILexSense Create(ILexEntry entry, SandboxGenericMSA sandboxMSA, string gloss);

		/// <summary>
		/// Create a new sense and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sandboxMSA"></param>
		/// <param name="gloss"></param>
		/// <returns></returns>
		ILexSense Create(ILexEntry entry, SandboxGenericMSA sandboxMSA, ITsString gloss);

		/// <summary>
		/// Create a new sense with the given guid and owner.
		/// This is needed for LIFT import.
		/// </summary>
		ILexSense Create(Guid guid, ILexEntry owner);
		/// <summary>
		/// Create a new subsense with the given guid and owner.
		/// This is needed for LIFT import.
		/// </summary>
		ILexSense Create(Guid guid, ILexSense owner);

		/// <summary>
		/// This is invoked (using reflection) by an XmlRDEBrowseView when the user presses
		/// "Enter" in an RDE view that is displaying lexeme form and definition.
		/// (Maybe also on loss of focus, switch domain, etc?)
		/// It creates a new entry, lexeme form, and sense that are linked to the specified domain.
		/// Typically, later, a call to RDEMergeSense will be made to see whether this
		/// new entry should be merged into some existing sense.
		/// </summary>
		/// <param name="hvoDomain">database id of the semantic domain</param>
		/// <param name="columns"></param>
		/// <param name="rgtss"></param>
		/// <param name="stringTbl"></param>
		int RDENewSense(int hvoDomain, List<XmlNode> columns, ITsString[] rgtss, StringTable stringTbl);
	}

	/// <summary>
	/// ILexExampleSentence factory additions.
	/// </summary>
	public partial interface ILexExampleSentenceFactory
	{
		/// <summary>
		/// Create a new example sentence with the given guid.  This is needed for LIFT import.
		/// </summary>
		ILexExampleSentence Create(Guid guid, ILexSense owner);
	}

	/// <summary>
	/// IMoAffixAllomorph factory additions.
	/// </summary>
	public partial interface IMoAffixAllomorphFactory
	{
		/// <summary>
		/// Create a new circumfix allomorph and add it to the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sense"></param>
		/// <param name="lexemeForm"></param>
		/// <param name="morphType"></param>
		/// <returns></returns>
		IMoAffixAllomorph CreateCircumfix(ILexEntry entry, ILexSense sense, ITsString lexemeForm, IMoMorphType morphType);
	}

	public partial interface IMoStemMsaFactory
	{
		/// <summary>
		/// Create a new MoStemMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		IMoStemMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa);
	}

	public partial interface IMoDerivAffMsaFactory
	{
		/// <summary>
		/// Create a new MoDerivAffMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		IMoDerivAffMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa);
	}

	public partial interface IMoInflAffMsaFactory
	{
		/// <summary>
		/// Create a new MoInflAffMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		IMoInflAffMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa);
	}

	public partial interface IMoUnclassifiedAffixMsaFactory
	{
		/// <summary>
		/// Create a new MoUnclassifiedAffixMsa, based on the given sandbox MSA.
		/// </summary>
		/// <param name="entry">The entry.</param>
		/// <param name="sandboxMsa">The sandbox msa.</param>
		/// <returns></returns>
		IMoUnclassifiedAffixMsa Create(ILexEntry entry, SandboxGenericMSA sandboxMsa);
	}

	/// <summary>
	/// ICmPicture factory additions
	/// </summary>
	public partial interface ICmPictureFactory
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		ICmPicture Create(string sTextRepOfPicture, string sFolder);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given a text representation (e.g., from the clipboard).
		/// NOTE: The caption is put into the default vernacular writing system.
		/// </summary>
		/// <param name="sTextRepOfPicture">Clipboard representation of a picture</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The picture location parser (can be null).</param>
		/// ------------------------------------------------------------------------------------
		ICmPicture Create(string sTextRepOfPicture, string sFolder, int anchorLoc,
			IPictureLocationBridge locationParser);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a CmPicture for the given file, having the given caption, and located in
		/// the given folder.
		/// </summary>
		/// <param name="srcFilename">The path to the original filename (an internal copy will
		/// be made in this method)</param>
		/// <param name="captionTss">The caption</param>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored
		/// </param>
		/// ------------------------------------------------------------------------------------
		ICmPicture Create(string srcFilename, ITsString captionTss, string sFolder);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a Toolbox-style Standard Format import.
		/// </summary>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that the locationParser can use if
		/// necessary (can be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="descriptions">The descriptions in 0 or more writing systems.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="tssCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		ICmPicture Create(string sFolder, int anchorLoc, IPictureLocationBridge locationParser,
			Dictionary<int, string> descriptions, string srcFilename, string sLayoutPos,
			string sLocationRange, string sCopyright, ITsString tssCaption,
			PictureLocationRangeType locRangeType, string sScaleFactor);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new picture, given string representations of most of the parameters. Used
		/// for creating a picture from a USFM-style Standard Format import.
		/// </summary>
		/// <param name="sFolder">The name of the CmFolder where picture should be stored</param>
		/// <param name="anchorLoc">The anchor location that can be used to determine (may be 0).</param>
		/// <param name="locationParser">The location parser.</param>
		/// <param name="sDescription">Illustration description in English.</param>
		/// <param name="srcFilename">The picture filename.</param>
		/// <param name="sLayoutPos">The layout position (as a string).</param>
		/// <param name="sLocationRange">The location range (as a string).</param>
		/// <param name="sCopyright">The copyright.</param>
		/// <param name="sCaption">The caption, in the default vernacular writing system.</param>
		/// <param name="locRangeType">Assumed type of the location range.</param>
		/// <param name="sScaleFactor">The scale factor (as a string).</param>
		/// ------------------------------------------------------------------------------------
		ICmPicture Create(string sFolder, int anchorLoc, IPictureLocationBridge locationParser,
			string sDescription, string srcFilename, string sLayoutPos, string sLocationRange,
			string sCopyright, string sCaption,	PictureLocationRangeType locRangeType, string sScaleFactor);
	}

	/// <summary>
	/// IScrRefSystemFactory factory additions
	/// </summary>
	public partial interface IScrRefSystemFactory
	{
		/// <summary>
		/// Basic creation method for a ScrRefSystem.
		/// </summary>
		IScrRefSystem Create();
	}

	/// <summary>
	/// IScripture factory additions
	/// </summary>
	public partial interface IScriptureFactory
	{
		/// <summary>
		/// Basic creation method for an Scripture.
		/// </summary>
		/// <returns>A new, unowned, Scripture.</returns>
		IScripture Create();
	}

	public partial interface ISegmentFactory
	{
		/// <summary>
		/// Basic creation method for an Segment. This method should only be used in import situations when an offset from the file is relevant.
		/// The new segment is added at the end of the segments of the owner.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="initialOffset">An initial offset to use when creating the segment</param>
		/// <returns>A new, unowned, Segment with BeginOffset set to the given initial offset.</returns>
		ISegment Create(IStTxtPara owner, int initialOffset);

		/// <summary>
		/// Basic creation method for an Segment. This method should only be used in import situations when an offset from the file is relevant.
		/// The new segment is added at the end of the segments of the owner.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="initialOffset">An initial offset to use when creating the segment</param>
		/// <param name="cache">FdoCache to get an hvo from</param>
		/// <param name="guid">The guid to set this segment to.</param>
		/// <returns>A new, unowned, Segment with BeginOffset set to the given initial offset.</returns>
		ISegment Create(IStTxtPara owner, int initialOffset, FdoCache cache, Guid guid);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// IScrDraft factory additions
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial interface IScrDraftFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an empty saved version (containing no books)
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// ------------------------------------------------------------------------------------
		IScrDraft Create(string description);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an empty version (containing no books)
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// <param name="type">The type of version to create (saved or imported)</param>
		/// ------------------------------------------------------------------------------------
		IScrDraft Create(string description, ScrDraftType type);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a saved version, adding copies of the specified books.
		/// </summary>
		/// <param name="description">Description for the saved version</param>
		/// <param name="books">Books that are copied to the saved version</param>
		/// ------------------------------------------------------------------------------------
		IScrDraft Create(string description, IEnumerable<IScrBook> books);
	}

	/// <summary>
	/// IScrBook factory additions
	/// </summary>
	public partial interface IScrBookFactory
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the <see cref="IScripture"/>. Also creates a new StText for the ScrBook's Title
		/// property.
		/// </summary>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <param name="title">The title StText created for the new book</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the database</exception>
		IScrBook Create(int bookNumber, out IStText title);

		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the given sequence. Also creates a new StText for the ScrBook's Title
		/// property.
		/// </summary>
		/// <param name="booksOS">Owning sequence of books to add the new book to</param>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <param name="title">The title StText created for the new book</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the given sequence</exception>
		IScrBook Create(IFdoOwningSequence<IScrBook> booksOS, int bookNumber, out IStText title);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the <see cref="IScripture"/>.
		/// </summary>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the database</exception>
		/// ------------------------------------------------------------------------------------
		IScrBook Create(int bookNumber);

		/// <summary>
		/// Initializes a new instance of the <see cref="IScrBook"/> class and adds it to
		/// the given sequence.
		/// </summary>
		/// <param name="booksOS">Owning sequence of books to add the new book to</param>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the given sequence</exception>
		IScrBook Create(IFdoOwningSequence<IScrBook> booksOS, int bookNumber);
	}

	/// <summary>
	/// IScrTxtParaFactory factory additions
	/// </summary>
	public partial interface IScrTxtParaFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new ScrTxtPara with the specified style.
		/// </summary>
		/// <param name="owner">The owner for the created paragraph.</param>
		/// <param name="styleName">Name of the style to apply to the paragraph style rules.</param>
		/// ------------------------------------------------------------------------------------
		IScrTxtPara CreateWithStyle(IStText owner, string styleName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new ScrTxtPara with the specified style.
		/// </summary>
		/// <param name="owner">The owner for the created paragraph.</param>
		/// <param name="iPos">The index where the new paragraph should be inserted.</param>
		/// <param name="styleName">Name of the style to apply to the paragraph style rules.</param>
		/// ------------------------------------------------------------------------------------
		IScrTxtPara CreateWithStyle(IStText owner, int iPos, string styleName);
	}

	public partial interface IScrFootnoteFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new ScrFootnote owned by the given book created from the given string
		/// representation (Created from GetTextRepresentation())
		/// </summary>
		/// <param name="book">The book that owns the sequence of footnotes into which the
		/// new footnote is to be inserted</param>
		/// <param name="sTextRepOfFootnote">The given string representation of a footnote
		/// </param>
		/// <param name="footnoteIndex">0-based index where the footnote will be inserted</param>
		/// <param name="footnoteMarkerStyleName">style name for footnote markers</param>
		/// <returns>A ScrFootnote with the properties set to the properties in the
		/// given string representation</returns>
		/// ------------------------------------------------------------------------------------
		IScrFootnote CreateFromStringRep(IScrBook book, string sTextRepOfFootnote,
			int footnoteIndex, string footnoteMarkerStyleName);
	}

	/// <summary>
	/// IScrSection factory additions
	/// </summary>
	public partial interface IScrSectionFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. The section will be an intro
		/// section if the isIntro flag is set to <code>true</code>
		/// The contents of the first content paragraph are filled with a single run as
		/// requested. The start and end references for the section are set based on where it's
		/// being inserted in the book.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="contentText">The text to be used as the first para in the new section
		/// content</param>
		/// <param name="contentTextProps">The character properties to be applied to the first
		/// para in the new section content</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <returns>Created section</returns>
		/// ------------------------------------------------------------------------------------
		IScrSection CreateScrSection(IScrBook book, int iSection, string contentText,
			ITsTextProps contentTextProps, bool isIntro);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new section, to be owned by the given book. Since the IStTexts are empty,
		/// this version of the function is generic (i.e. the new section may be made either
		/// an intro section or a scripture text section by the calling code).
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <returns>The newly created <see cref="IScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		IScrSection CreateEmptySection(IScrBook book, int iSection);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a section with optional heading/content paragraphs.
		/// </summary>
		/// <param name="book">The book where the new section will be created</param>
		/// <param name="iSection">The zero-based index of the new section</param>
		/// <param name="isIntro">True to create an intro section, false to create a
		/// normal scripture section</param>
		/// <param name="createHeadingPara">if true, heading paragraph will be created</param>
		/// <param name="createContentPara">if true, content paragraph will be created</param>
		/// <returns>The newly created <see cref="IScrSection"/></returns>
		/// ------------------------------------------------------------------------------------
		IScrSection CreateSection(IScrBook book, int iSection, bool isIntro,
			bool createHeadingPara, bool createContentPara);
	}

	public partial interface ITextFactory
	{
		/// <summary>
		/// Basic creation method for a Text object.
		/// </summary>
		/// <returns>A new, unowned Text with the given guid</returns>
		IText Create(FdoCache cache, Guid guid);
	}

	public partial interface IRnGenericRecFactory
	{
		/// <summary>
		/// Creates a new record with the specified notebook as the owner.
		/// </summary>
		/// <param name="notebook">The notebook.</param>
		/// <param name="title">The title.</param>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		IRnGenericRec Create(IRnResearchNbk notebook, ITsString title, ICmPossibility type);

		/// <summary>
		/// Creates a new record with the specified record as the owner.
		/// </summary>
		/// <param name="record">The record.</param>
		/// <param name="title">The title.</param>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		IRnGenericRec Create(IRnGenericRec record, ITsString title, ICmPossibility type);
	}

	public partial interface ICmMediaURIFactory
	{
		/// <summary>
		/// Basic creation method for an CmMediaURI.
		/// </summary>
		/// <returns>A new, unowned CmMediaURI with the given guid</returns>
		ICmMediaURI Create(FdoCache cache, Guid guid);
	}

	public partial interface IScrImportSetFactory
	{
		/// <summary>
		/// Creates a new scripture import settings with the default paragraph characters style name.
		/// </summary>
		IScrImportSet Create(string defaultParaCharsStyleName, string stylesPath);
	}

	public partial interface IPhBdryMarkerFactory
	{
		/// <summary>
		/// Creates a boundary marker with the specified GUID.
		/// </summary>
		IPhBdryMarker Create(Guid guid, IPhPhonemeSet owner);
	}
}