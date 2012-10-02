# Makefile for IDL grammar
IDLFILE=idl
IDHFILE=idh
SURVEYORFILE=SurveyorTags

OUTFILES="$(OUTDIR)\$(IDLFILE)Parser.cs" "$(OUTDIR)\$(IDHFILE)Parser.cs" "$(OUTDIR)\$(SURVEYORFILE)Parser.cs"
#ANTLR_OPTS=-trace

all: $(OUTFILES)

rebuild: clean all

clean:
	@del "$(OUTDIR)\$(IDLFILE)Parser.cs"
	@del "$(OUTDIR)\$(IDLFILE)Lexer.cs"
	@del "$(OUTDIR)\$(IDLFILE)TokenTypes.cs"
	@del "$(OUTDIR)\$(IDLFILE)TokenTypes.txt"
	@del "$(OUTDIR)\$(IDHFILE)Parser.cs"
	@del "$(OUTDIR)\$(IDHFILE)Lexer.cs"
	@del "$(OUTDIR)\$(IDHFILE)ParserTokenTypes.cs"
	@del "$(OUTDIR)\$(IDHFILE)ParserTokenTypes.txt"
	@del "$(OUTDIR)\$(SURVEYORFILE)Parser.cs"
	@del "$(OUTDIR)\$(SURVEYORFILE)Lexer.cs"
	@del "$(OUTDIR)\$(SURVEYORFILE)ParserTokenTypes.cs"
	@del "$(OUTDIR)\$(SURVEYORFILE)ParserTokenTypes.txt"

"$(OUTDIR)\$(IDLFILE)Parser.cs" : $(IDLFILE).g
	@echo Building $(@F)
	@java antlr.Tool $(ANTLR_OPTS) -o "$(OUTDIR)" "%s"

"$(OUTDIR)\$(IDHFILE)Parser.cs" : $(IDHFILE).g
	@echo Building $(@F)
	@java antlr.Tool $(ANTLR_OPTS) -o "$(OUTDIR)" "%s"

"$(OUTDIR)\$(SURVEYORFILE)Parser.cs" : $(SURVEYORFILE).g
	@echo Building $(@F)
	@java antlr.Tool $(ANTLR_OPTS) -o "$(OUTDIR)" "%s"
