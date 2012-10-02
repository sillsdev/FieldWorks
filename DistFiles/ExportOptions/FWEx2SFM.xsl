<?xml version="1.0" encoding="UTF-8"?>
<!--Transforms from ExtendedFieldWorksExport.XML to Standard Format-->
<!--
September 7, 2002 Larry Hayashi
What this XSL transform does:
1. Exports FW document view to a Shoebox standard format database.
2. Will mark language data other than ENGLISH using the Shoebox standard: English data here  | FRN {data goes in here} English data here.
If you are using another language other than ENGLISH as your default analysis language, then change ENG to the three letter code of that Analysis language in the Run template below.
3. Exports only the fields and records in the current document view. If you want ALL of your data exported, you must create a document view with visibility of all fields set to "When data present" or "Always" and set your filter to off.
4. Exports to a Shoebox database type defined for FieldWorks data. A Shoebox *.typ file is included on the FieldWorks CD.

What this XSL transform does not do:
1. By default, it does not handle characters that are not part of the standard Windows 1252 standard. FieldWorks exports Unicode. Shoebox 5 does not handle unicode. It only handles 8-bit data. There is no way for FW to know what legacy font you might be using and what the codepoint for characters above 127 might be. However, if you know that you are using a Windows Codepage or ISO-8859 standard other than 1252 or 8859-1 respectively, or you are using a Unicode compliant file reader,  you can change the output encoding below from WINDOWS-1252 to any of the following:

MSXML has native support for the following encodings:

UTF-8
UTF-16
UCS-2
UCS-4
ISO-10646-UCS-2
UNICODE-1-1-UTF-8
UNICODE-2-0-UTF-16
UNICODE-2-0-UTF-8

It also recognizes (internally using the WideCharToMultibyte API function for mappings) the following encodings:
US-ASCII

ISO-8859-1
ISO-8859-2 Latin alphabet No.2
ISO-8859-3 Latin alphabet No. 3
ISO-8859-4 Latin alphabet No. 4
ISO-8859-5 Latin/Cyrillic alphabet
ISO-8859-6 Latin/Arabic alphabet
ISO-8859-7 Latin/Greek alphabet
ISO-8859-8 Latin/Hebrew alphabet
ISO-8859-9 Latin alphabet No. 5
WINDOWS-1250 Windows Eastern European
WINDOWS-1251 Windows Cyrillic
WINDOWS-1252 Windows ANSI
WINDOWS-1253 Windows Greek
WINDOWS-1254 Windows Turkish
WINDOWS-1255 Windows Hebrew
WINDOWS-1256 Windows Arabic
WINDOWS-1257 Windows Baltic
WINDOWS-1258 Windows Vietnamese

There is an excellent overview of the differences between these char sets at: http://www.microsoft.com/globaldev/reference/cphome.asp.

If you have characters that are not supported by the output encoding this transform will break as soon as it comes across that character. If you're missing a bunch of data after a particular character in a record in the export, that's why.

2. It does not dump out a Shoebox Project - rather it dumps out a db file that can be opened into a Shoebox project.
We recommend using the above mentioned FieldWor.typ file in your Shoebox project.

3. For more information on XSL and XML, refer to http://msdn.microsoft.com/xml


Current problems:
1. Initial export from FieldWorks does not handle custom fields properly, thus neither does this transform.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:silfw="http://fieldworks.sil.org/2002/silfw/Codes" xmlns:user="http://mycompany.com/mynamespace" exclude-result-prefixes="silfw">
	<silfw:file title="Standard format - Shoebox (*.db) -- CP1252" outputext="db" description="Converts FwExport format to Standard format" views="dataentry document"/>
	<xsl:output method="text" omit-xml-declaration="yes" encoding="WINDOWS-1252"/>
	<!--Strip all white space and leaves it up to the stylesheet text elements below to put in appropriate spacing-->
	<xsl:strip-space elements="*"/>
	<!--The following script is to fix a bug in Shoebox import. Shoebox 5 will arbitrarily break lines longer than a 1000 characters into 1000 character sections not taking into account word boundaries.
	This script goes through and finds the nearest word break before the 1000 character cut off and creates an additional field for the following text.
	It also checks for inline markup and repeats it where necessary when a break occurs in the middle of an inline marked section.
	-->
	<msxsl:script language="javascript" implements-prefix="user"><![CDATA[
function chunkIt(p_FieldName, p_GonnaBeChunked){
	//  var p_FieldName=document.InputFields.FieldName.value
   // var p_GonnaBeChunked=document.InputFields.Data.value
   var v_Output="";
   var v_LoopCounter=1;
   var v_FieldNameString;
   var v_VertBarBeforeLOP;
   var v_PrecedingEndMarkUp;
   var v_PostFieldInlineMarkUp;
   var v_LastSpace;
   var v_BulkOfChunk;
   var v_RemainderOfChunk;
   var v_LengthOfBulk;


//********* Check if the Chunk is more than 1000 characters *****************
if (p_GonnaBeChunked.length > 950) {

	while (p_GonnaBeChunked.length > 950) {
		v_LastSpace = p_GonnaBeChunked.lastIndexOf(" ",950); //Find the space character closest to the end of the 950 character chunk
		v_BulkOfChunk = p_GonnaBeChunked.substring(0,v_LastSpace);
		v_RemainderOfChunk = p_GonnaBeChunked.substring(v_LastSpace,950);
		v_LengthOfBulk = v_BulkOfChunk.length;

		var v_LastCloseParen = v_BulkOfChunk.lastIndexOf(String.fromCharCode(125),v_LengthOfBulk);
		var v_LastOpenParen = v_BulkOfChunk.lastIndexOf(String.fromCharCode(123),v_LengthOfBulk);

		//********* Here we test for Shoebox inline markup ********************************************
		//********* that may have been interrupted across the 1000 character boundary *****************
		//********* We look for an open parentheses closer to the end of the BulkOfChunk  *****************
		//********* than a closed parentheses.*****************
		if (v_LastOpenParen > v_LastCloseParen){

			//********* Of course an open parentheses may be just that and not used for *******************
			//********* Shoebox markup at all ... so here we also look for the bar | occurring ************
			//********* within 8 characters of the open parentheses. If we find it we can be  *****************
			//********* pretty sure that it is inline markup. *********************************************
			var v_VertBarBeforeLOP = v_BulkOfChunk.lastIndexOf("|", v_LastOpenParen)
			if (v_LastOpenParen - v_VertBarBeforeLOP < 8){
				var v_InlineMarkup = v_BulkOfChunk.substring(v_LastOpenParen, v_VertBarBeforeLOP)

				v_CloseParen = String.fromCharCode(125);
				v_BarInlineMarkupOpenParen = v_InlineMarkup + String.fromCharCode(123);

				//********* When we output this interrupted inline markup, we ******************************
				//********* add a closed parentheses to the end of v_BulkOfChunk while adding *****************
				//********* a paragraph return, a backslash, the fieldname and the inline markup for **************
				//********* the next v_RemainderOfChunk. This remainder already has the close parenthesis for *****
				//********* this added inline markup. **************************************************************

				v_FieldNameString = v_CloseParen + String.fromCharCode(13) + String.fromCharCode(92) + p_FieldName + " " + v_BarInlineMarkupOpenParen
				//Reseting values below. Is this necessary.
				v_LastCloseParen = 951;
				v_LastOpenParen = "";
				} //If LastOpenParen is not in proximity to VertBar then its not markup and should be ignored.

			//********* If we don't find it then we just treat this as ordinary text **********************
			//********* and not worry about inline markup. ************************************************
			}

		//********* Apart from inline markup we just want to the info. ********************************************
		//********* that may have been interrupted across the 950 character boundary *****************
		//********* We look for an open parentheses closer to the end of the BulkOfChunk  *****************
		//********* than a closed parentheses.*****************
		else {
				if (v_LoopCounter !=1){
				v_FieldNameString = String.fromCharCode(13) +String.fromCharCode(92) + p_FieldName + " ";
				}

				else{
					v_FieldNameString = String.fromCharCode(13) + String.fromCharCode(92) + p_FieldName + " ";
					v_Output ="";}
				v_CloseParen="";
				v_BarInlineMarkupOpenParen="";

			}


		v_Output = v_Output + v_BulkOfChunk + v_FieldNameString;
		p_GonnaBeChunked = p_GonnaBeChunked.substring(v_LastSpace);
		v_LoopCounter++;
	} //end of while

	//Add the last chunk followed by no markup.
	v_Output = v_Output + p_GonnaBeChunked;
	return v_Output
}
//********* If the Chunk is less than 1000 characters *****************
//********* then we need to determine if its the last *****************
//********* chunk of a series of chunks. If so we add the field? *****************
else
	{return p_GonnaBeChunked}
}
		   ]]></msxsl:script>
	<xsl:template match="/">
		<xsl:apply-templates/>
	</xsl:template>
	<!--Ignore Languages container-->
	<xsl:template match="Languages"/>
	<!--Ignore Styles container - we don't care about formatting for this-->
	<xsl:template match="Styles"/>
	<!--Find entry and indicate with a \Entry field marker-->
	<xsl:template match="Entry">
		<xsl:choose>
			<!--If this is the very first entry, do not put returns before it.-->
			<xsl:when test="position()=1">
				<xsl:text>\entry</xsl:text>
				<xsl:text>&#32;</xsl:text>
				<xsl:value-of select="@dateCreated"/>
				<xsl:text>&#13;\etyp</xsl:text>
				<xsl:text>&#32;</xsl:text>
				<xsl:value-of select="@type"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="@level=0">
						<xsl:text>&#13;</xsl:text>
						<xsl:text>&#13;</xsl:text>
						<xsl:text>\entry</xsl:text>
						<xsl:text>&#32;</xsl:text>
						<xsl:value-of select="@dateCreated"/>
						<xsl:text>&#13;\etyp</xsl:text>
						<xsl:text>&#32;</xsl:text>
						<xsl:value-of select="@type"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>&#13;</xsl:text>
						<xsl:text>&#13;</xsl:text>
						<xsl:text>\sub</xsl:text>
						<xsl:text>&#32;</xsl:text>
						<xsl:number level="multiple" format="1.1" count="Entry[@level!=0]"/>
						<xsl:text>&#13;\etyp </xsl:text>
						<xsl:value-of select="@type"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
		<!--xsl:choose-->
		<!--If level is equal to 0 just put entry-->
		<!--xsl:when test="@level=0"/-->
		<!--If level is not equal to 0, append the level number to \entry -> \entry1, \entry2
			This is used to represent subentries and subsubentries, etc.-->
		<!--xsl:otherwise>
		<xsl:value-of select="@level"/>
	  </xsl:otherwise>
	</xsl:choose-->
		<xsl:apply-templates/>
		<xsl:choose>
			<!--If this is the very first entry, do not put returns before it.-->
			<xsl:when test="position()=1">
				<xsl:text>&#13;</xsl:text>
				<xsl:text>\-entry</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="@level=0">
						<xsl:text>&#13;</xsl:text>
						<xsl:text>\-entry</xsl:text>
					</xsl:when>
					<xsl:otherwise>
						<xsl:text>&#13;</xsl:text>
						<xsl:text>\-sub</xsl:text>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<!--Data is delimited by the element Field. Find it.-->
	<xsl:template match="Field">
		<!--Set variable fieldName to @name - the name of the field-->
		<xsl:variable name="fieldName">
			<xsl:choose>
				<xsl:when test="starts-with(@name, 'custom')">
					<xsl:value-of select="../Run[@namedStyle='Label']"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@name"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:choose>
			<!--If there are Items in the field element, then each item gets its own field marker and we place the contents of the enclosed Run after the field.-->
			<xsl:when test="Item">
				<xsl:for-each select="Item/Run">
					<xsl:text>&#13;</xsl:text>
					<xsl:text>\</xsl:text>
					<xsl:choose>
						<!--Participants is an odd field in that the fieldname sometimes includes the Role of the participant. Here we separate this out.-->
						<xsl:when test="starts-with($fieldName,'Participants:')">
							<xsl:text>part</xsl:text>
						</xsl:when>
						<xsl:otherwise>
							<xsl:call-template name="AbbrevSFCodes">
								<xsl:with-param name="LongCode" select="$fieldName"/>
							</xsl:call-template>
							<!--xsl:value-of select="$fieldName"/-->
						</xsl:otherwise>
					</xsl:choose>
					<xsl:text>&#32;</xsl:text>
					<!--Output the value of the Item element.-->
					<xsl:value-of select="."/>
					<!--Refer to above note on Participants-->
					<xsl:if test="starts-with($fieldName,'Participants:')">
						<xsl:variable name="Role" select="substring-after($fieldName, 'Participants:')"/>
						<xsl:if test="$Role!=''">
							<xsl:text>&#13;</xsl:text>
							<xsl:text>\role</xsl:text>
							<xsl:text>&#32;</xsl:text>
							<xsl:value-of select="$Role"/>
						</xsl:if>
					</xsl:if>
				</xsl:for-each>
			</xsl:when>
			<!-- Handle the presence of language codes properly.  See CLE-49. -->
			<xsl:when test="Run[@namedStyle='Language Code']">
				<xsl:for-each select="Run">
					<xsl:choose>
						<xsl:when test="@namedStyle='Language Code'">
							<xsl:text>&#13;\</xsl:text>
							<xsl:call-template name="AbbrevSFCodes">
								<xsl:with-param name="LongCode" select="$fieldName"/>
							</xsl:call-template>
							<xsl:text>_</xsl:text>
							<xsl:value-of select="."/>
							<xsl:text>&#32;</xsl:text>
						</xsl:when>
						<xsl:when test="@namedStyle=''">
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="."/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<!--Otherwise, output the name of the field-->
				<xsl:text>&#13;</xsl:text>
				<xsl:text>\</xsl:text>
				<!--xsl:value-of select="$fieldName"/-->
				<xsl:call-template name="AbbrevSFCodes">
					<xsl:with-param name="LongCode" select="$fieldName"/>
				</xsl:call-template>
				<xsl:text>&#32;</xsl:text>
				<!--Then output the value of the Runs inside the Field elemet.
				Unless of course that Run has the @namedStyle of Label.-->
				<xsl:for-each select="Run">
					<xsl:choose>
						<xsl:when test="@namedStyle='Label'"/>
						<!--The other Runs are data-->
						<!--xsl:when test="@nameStyle!='Label'">
							<xsl:value-of select="."/>
						</xsl:when-->
						<xsl:otherwise>
							<xsl:call-template name="Run"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:for-each>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="Field[@type='StText']">
		<xsl:variable name="fieldName">
			<xsl:choose>
				<xsl:when test="starts-with(@name, 'custom')">
					<xsl:value-of select="StTxtPara/StyleRules15/Prop/@namedStyle"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@name"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:text>&#13;</xsl:text>
		<!--xsl:value-of select="$fieldName"/-->
		<xsl:for-each select="StTxtPara/Contents16/Str/Field">
			<xsl:text>\</xsl:text>
			<xsl:variable name="AbbrevFieldName">
				<xsl:call-template name="AbbrevSFCodes">
					<xsl:with-param name="LongCode" select="$fieldName"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:value-of select="$AbbrevFieldName"/>
			<xsl:text>&#32;</xsl:text>
			<!--This variable is added here to store the entire Run which is then subdivided into 900 character sections due to a bug in Shoebox 5
			The bug limits imported text fields to 1000 character blocks. It chops the block there and puts a line break regardless of whether or not it is in the middle of a word.
			Thus we want to chunk the Run into 900 character sections.-->
			<xsl:variable name="FullRun">
				<xsl:for-each select="Run">
					<xsl:call-template name="Run"/>
				</xsl:for-each>
			</xsl:variable>
			<xsl:value-of select="user:chunkIt(string($AbbrevFieldName), string($FullRun))"/>
			<xsl:if test="position()!=last()">
				<xsl:text>&#13;</xsl:text>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="Run">
		<xsl:choose>
			<xsl:when test="@enc!='ENG'">
				<xsl:text>|</xsl:text>
				<xsl:value-of select="@enc"/>
				<xsl:text>{</xsl:text>
				<xsl:value-of select="."/>
				<xsl:text>}</xsl:text>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="."/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="text()"/>
	<!--This template converts the long field name exported from FieldWorks to a short SF code-->
	<xsl:template name="AbbrevSFCodes">
		<xsl:param name="LongCode"/>
		<!--	!AnthroCodes*anth
!Conclusions*conc
!Confidence*conf
!DateOfEvent*date
!DateCreated*de
!Description*desc
!Discussion*disc
!DateModified*dt
!EntryType*etyp
!FurtherQuestions*fq
!Hypothesis*hypo
!Locations*loc
!ExternalMaterials*mtrl
!Participants*part
!ResearchPlan*plan
!PersonalNotes*pnt
!Restrictions*restr
!Role*role
!Researchers*rscr
!Sources*srce
!Status*stat
!TimeOfEvent*time
!Title*titl
!Type*type
!Weather*wr-->
		<xsl:choose>
			<xsl:when test="$LongCode='AnthroCodes'">
				<xsl:text>anth</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Conclusions'">
				<xsl:text>conc</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Confidence'">
				<xsl:text>conf</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='DateOfEvent'">
				<xsl:text>date</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='DateCreated'">
				<xsl:text>de</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Description'">
				<xsl:text>desc</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Discussion'">
				<xsl:text>disc</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='DateModified'">
				<xsl:text>dt</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='EntryType'">
				<xsl:text>etyp</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='FurtherQuestions'">
				<xsl:text>fq</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Hypothesis'">
				<xsl:text>hypo</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Locations'">
				<xsl:text>loc</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='ExternalMaterials'">
				<xsl:text>mtrl</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Participants'">
				<xsl:text>part</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='ResearchPlan'">
				<xsl:text>plan</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='PersonalNotes'">
				<xsl:text>pnt</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Restrictions'">
				<xsl:text>restr</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Role'">
				<xsl:text>role</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Researchers'">
				<xsl:text>rscr</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Sources'">
				<xsl:text>srce</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Status'">
				<xsl:text>stat</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='TimeOfEvent'">
				<xsl:text>time</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Title'">
				<xsl:text>titl</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Type'">
				<xsl:text>type</xsl:text>
			</xsl:when>
			<xsl:when test="$LongCode='Weather'">
				<xsl:text>wr</xsl:text>
			</xsl:when>
			<!--This handles custom fields that we can't possible know about in advance-->
			<xsl:otherwise>
				<xsl:variable name="LongCodeLength" select="string-length($LongCode)"/>
				<xsl:choose>
					<xsl:when test="substring($LongCode, ($LongCodeLength))= ':'">
						<xsl:value-of select="substring($LongCode, 1, $LongCodeLength - 1)"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="$LongCode"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
