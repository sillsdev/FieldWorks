using System.Text;
using System;using Tools;
namespace SIL.FieldWorks.FDO.Validation {
/// <summary/>
public class Environment : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public Environment(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "Environment"; }}
/// <summary/>
public class LeftContext : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public LeftContext(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "LeftContext"; }}
/// <summary/>
public class RightContext : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public RightContext(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "RightContext"; }}
/// <summary/>
public class TermSequence : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public TermSequence(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "TermSequence"; }}
/// <summary/>
public class Term : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public Term(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "Term"; }}
/// <summary/>
public class OptionalSegment : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public OptionalSegment(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "OptionalSegment"; }}

/// <summary/>
public class OptionalSegment_1 : OptionalSegment {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_1(Parser yyq):base(yyq){}}

/// <summary/>
public class OptionalSegment_2 : OptionalSegment {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_2(Parser yyq):base(yyq){}}

/// <summary/>
public class OptionalSegment_2_1 : OptionalSegment_2 {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_2_1(Parser yyq):base(yyq){
				((PhonEnvParser
)yyq).CreateErrorMessage(PhonEnvParser.SyntaxErrType.missingClosingParen.ToString(),
	((TOKEN)(yyq.StackAt(2).m_value))
	.yytext,
	((error)(yyq.StackAt(0).m_value))
	.pos);
					((PhonEnvParser
)yyq).SyntaxErrorType = PhonEnvParser.SyntaxErrType.missingClosingParen;
					((PhonEnvParser
)yyq).Position =
	((error)(yyq.StackAt(0).m_value))
	.pos;
					}}

/// <summary/>
public class OptionalSegment_3 : OptionalSegment {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_3(Parser yyq):base(yyq){}}

/// <summary/>
public class OptionalSegment_4 : OptionalSegment {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_4(Parser yyq):base(yyq){}}

/// <summary/>
public class OptionalSegment_4_1 : OptionalSegment_4 {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_4_1(Parser yyq):base(yyq){
					((PhonEnvParser
)yyq).CreateErrorMessage(PhonEnvParser.SyntaxErrType.missingOpeningParen.ToString(),
	((TOKEN)(yyq.StackAt(1).m_value))
	.yytext,
	((Segment)(yyq.StackAt(2).m_value))
	.pos);
					((PhonEnvParser
)yyq).SyntaxErrorType = PhonEnvParser.SyntaxErrType.missingOpeningParen;
					((PhonEnvParser
)yyq).Position =
	((Segment)(yyq.StackAt(2).m_value))
	.pos;
					}}
/// <summary/>
public class Segment : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public Segment(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "Segment"; }}
/// <summary/>
public class Class : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public Class(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "Class"; }}

/// <summary/>
public class Class_1 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_1(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_2 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_2(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_2_1 : Class_2 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_2_1(Parser yyq):base(yyq){
				((PhonEnvParser
)yyq).CreateErrorMessage(PhonEnvParser.SyntaxErrType.missingClosingSquareBracket.ToString(),
	((TOKEN)(yyq.StackAt(2).m_value))
	.yytext,
	((error)(yyq.StackAt(0).m_value))
	.pos);
		   ((PhonEnvParser
)yyq).SyntaxErrorType = PhonEnvParser.SyntaxErrType.missingClosingSquareBracket;
		   ((PhonEnvParser
)yyq).Position =
	((error)(yyq.StackAt(0).m_value))
	.pos;
		   }}

/// <summary/>
public class Class_3 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_3(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_4 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_4(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_4_1 : Class_4 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_4_1(Parser yyq):base(yyq){
		   ((PhonEnvParser
)yyq).CreateErrorMessage(PhonEnvParser.SyntaxErrType.missingOpeningSquareBracket.ToString(),
	((TOKEN)(yyq.StackAt(1).m_value))
	.yytext,
	((Ident)(yyq.StackAt(2).m_value))
	.pos);
		   ((PhonEnvParser
)yyq).SyntaxErrorType = PhonEnvParser.SyntaxErrType.missingOpeningSquareBracket;
		   ((PhonEnvParser
)yyq).Position =
	((Ident)(yyq.StackAt(2).m_value))
	.pos;
		   }}

/// <summary/>
public class Class_5 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_5(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_6 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_6(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_6_1 : Class_6 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_6_1(Parser yyq):base(yyq){


			if (!((PhonEnvParser
)yyq).IsValidClass(
	((Ident)(yyq.StackAt(1).m_value))
	.yytext))
			{

				((PhonEnvParser
)yyq).CreateErrorMessage("class",
	((Ident)(yyq.StackAt(1).m_value))
	.yytext,
	((Ident)(yyq.StackAt(1).m_value))
	.pos);
				((PhonEnvParser
)yyq).ThrowError(
	((Ident)(yyq.StackAt(1).m_value))
	.pos);
			}
			}}

/// <summary/>
public class Class_7 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_7(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_8 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_8(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_8_1 : Class_8 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_8_1(Parser yyq):base(yyq){


			StringBuilder sb = new StringBuilder();
			sb.Append(
	((Ident)(yyq.StackAt(2).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(1).m_value))
	.yytext);
			if (!((PhonEnvParser
)yyq).IsValidClass(sb.ToString()))
			{

				((PhonEnvParser
)yyq).CreateErrorMessage("class", sb.ToString(),
	((Ident)(yyq.StackAt(2).m_value))
	.pos);
				((PhonEnvParser
)yyq).ThrowError(
	((Ident)(yyq.StackAt(2).m_value))
	.pos);
			}
			}}

/// <summary/>
public class Class_9 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_9(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_10 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_10(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_10_1 : Class_10 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_10_1(Parser yyq):base(yyq){
			StringBuilder sb = new StringBuilder();
			sb.Append(
	((Ident)(yyq.StackAt(3).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(2).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(1).m_value))
	.yytext);
			if (!((PhonEnvParser
)yyq).IsValidClass(sb.ToString()))
			{

				((PhonEnvParser
)yyq).CreateErrorMessage("class", sb.ToString(),
	((Ident)(yyq.StackAt(3).m_value))
	.pos);
				((PhonEnvParser
)yyq).ThrowError(
	((Ident)(yyq.StackAt(3).m_value))
	.pos);
			}
			}}

/// <summary/>
public class Class_11 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_11(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_12 : Class {
/// <summary/>
/// <param name='yyq'></param>
  public Class_12(Parser yyq):base(yyq){}}

/// <summary/>
public class Class_12_1 : Class_12 {
/// <summary/>
/// <param name='yyq'></param>
  public Class_12_1(Parser yyq):base(yyq){


			StringBuilder sb = new StringBuilder();
			sb.Append(
	((Ident)(yyq.StackAt(4).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(3).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(2).m_value))
	.yytext);
			sb.Append(" ");
			sb.Append(
	((Ident)(yyq.StackAt(1).m_value))
	.yytext);
			if (!((PhonEnvParser
)yyq).IsValidClass(sb.ToString()))
			{

				((PhonEnvParser
)yyq).CreateErrorMessage("class", sb.ToString(),
	((Ident)(yyq.StackAt(4).m_value))
	.pos);
				((PhonEnvParser
)yyq).ThrowError(
	((Ident)(yyq.StackAt(4).m_value))
	.pos);
			}
			}}
/// <summary/>
public class Literal : SYMBOL {
/// <summary/>
/// <param name='yyq'></param>
	public Literal(Parser yyq):base(yyq) { }
/// <summary/>
  public override string yyname() { return "Literal"; }}

/// <summary/>
public class Literal_1 : Literal {
/// <summary/>
/// <param name='yyq'></param>
  public Literal_1(Parser yyq):base(yyq){}}

/// <summary/>
public class Literal_2 : Literal {
/// <summary/>
/// <param name='yyq'></param>
  public Literal_2(Parser yyq):base(yyq){}}

/// <summary/>
public class Literal_2_1 : Literal_2 {
/// <summary/>
/// <param name='yyq'></param>
  public Literal_2_1(Parser yyq):base(yyq){
			int iPos =
	((Ident)(yyq.StackAt(0).m_value))
	.pos;
			if (!((PhonEnvParser
)yyq).IsValidSegment(
	((Ident)(yyq.StackAt(0).m_value))
	.yytext, ref iPos))
			{

				((PhonEnvParser
)yyq).CreateErrorMessage("segment",
	((Ident)(yyq.StackAt(0).m_value))
	.yytext, iPos);
				((PhonEnvParser
)yyq).ThrowError(iPos);
			}
			}}
/// <summary/>
public class yyPhonEnvParser
: Symbols {
/// <summary/>
/// <param name='yyq'></param>
/// <param name='yysym'></param>
/// <param name='yyact'></param>
  public override object Action(Parser yyq,SYMBOL yysym, int yyact) {
	switch(yyact) {
	 case -1: break; //// keep compiler happy
}  return null; }

/// <summary/>
public class Environment_1 : Environment {
/// <summary/>
/// <param name='yyq'></param>
  public Environment_1(Parser yyq):base(yyq){}}

/// <summary/>
public class Environment_2 : Environment {
/// <summary/>
/// <param name='yyq'></param>
  public Environment_2(Parser yyq):base(yyq){}}

/// <summary/>
public class RightContext_1 : RightContext {
/// <summary/>
/// <param name='yyq'></param>
  public RightContext_1(Parser yyq):base(yyq){}}

/// <summary/>
public class RightContext_2 : RightContext {
/// <summary/>
/// <param name='yyq'></param>
  public RightContext_2(Parser yyq):base(yyq){}}

/// <summary/>
public class RightContext_3 : RightContext {
/// <summary/>
/// <param name='yyq'></param>
  public RightContext_3(Parser yyq):base(yyq){}}

/// <summary/>
public class Environment_3 : Environment {
/// <summary/>
/// <param name='yyq'></param>
  public Environment_3(Parser yyq):base(yyq){}}

/// <summary/>
public class LeftContext_1 : LeftContext {
/// <summary/>
/// <param name='yyq'></param>
  public LeftContext_1(Parser yyq):base(yyq){}}

/// <summary/>
public class LeftContext_2 : LeftContext {
/// <summary/>
/// <param name='yyq'></param>
  public LeftContext_2(Parser yyq):base(yyq){}}

/// <summary/>
public class LeftContext_3 : LeftContext {
/// <summary/>
/// <param name='yyq'></param>
  public LeftContext_3(Parser yyq):base(yyq){}}

/// <summary/>
public class TermSequence_1 : TermSequence {
/// <summary/>
/// <param name='yyq'></param>
  public TermSequence_1(Parser yyq):base(yyq){}}

/// <summary/>
public class TermSequence_2 : TermSequence {
/// <summary/>
/// <param name='yyq'></param>
  public TermSequence_2(Parser yyq):base(yyq){}}

/// <summary/>
public class Term_1 : Term {
/// <summary/>
/// <param name='yyq'></param>
  public Term_1(Parser yyq):base(yyq){}}

/// <summary/>
public class OptionalSegment_5 : OptionalSegment {
/// <summary/>
/// <param name='yyq'></param>
  public OptionalSegment_5(Parser yyq):base(yyq){}}

/// <summary/>
public class Term_2 : Term {
/// <summary/>
/// <param name='yyq'></param>
  public Term_2(Parser yyq):base(yyq){}}

/// <summary/>
public class Segment_1 : Segment {
/// <summary/>
/// <param name='yyq'></param>
  public Segment_1(Parser yyq):base(yyq){}}

/// <summary/>
public class Segment_2 : Segment {
/// <summary/>
/// <param name='yyq'></param>
  public Segment_2(Parser yyq):base(yyq){}}
/// <summary/>
public yyPhonEnvParser
():base() { arr = new int[] {
101,19,102,4,22,
69,0,110,0,118,
0,105,0,114,0,
111,0,110,0,109,
0,101,0,110,0,
116,0,1,2,103,
17,1,76,101,2,
0,104,5,40,1,
77,105,17,1,77,
106,22,107,4,6,
69,0,79,0,70,
0,1,6,2,0,
1,76,103,1,75,
108,17,1,75,109,
19,110,4,24,82,
0,105,0,103,0,
104,0,116,0,67,
0,111,0,110,0,
116,0,101,0,120,
0,116,0,1,2,
2,0,1,64,111,
17,1,64,112,20,
113,4,2,95,0,
1,1,2,0,1,
63,114,17,1,63,
109,2,0,1,62,
115,17,1,62,116,
20,117,4,2,35,
0,1,1,2,0,
1,61,118,17,1,
61,116,2,0,1,
60,119,17,1,60,
120,19,121,4,24,
84,0,101,0,114,
0,109,0,83,0,
101,0,113,0,117,
0,101,0,110,0,
99,0,101,0,1,
2,2,0,1,51,
122,17,1,51,112,
2,0,1,50,123,
17,1,50,124,19,
125,4,22,76,0,
101,0,102,0,116,
0,67,0,111,0,
110,0,116,0,101,
0,120,0,116,0,
1,2,2,0,1,
49,126,17,1,49,
120,2,0,1,48,
127,17,1,48,120,
2,0,1,39,128,
17,1,39,116,2,
0,1,37,129,17,
1,37,120,2,0,
1,29,130,17,1,
29,131,19,132,4,
8,84,0,101,0,
114,0,109,0,1,
2,2,0,1,28,
133,17,1,28,134,
19,135,4,30,79,
0,112,0,116,0,
105,0,111,0,110,
0,97,0,108,0,
83,0,101,0,103,
0,109,0,101,0,
110,0,116,0,1,
2,2,0,1,27,
136,17,1,27,137,
20,138,4,2,41,
0,1,1,2,0,
1,26,139,17,1,
26,140,19,141,4,
10,101,0,114,0,
114,0,111,0,114,
0,1,2,2,0,
1,25,142,17,1,
25,143,19,144,4,
14,83,0,101,0,
103,0,109,0,101,
0,110,0,116,0,
1,2,2,0,1,
20,145,17,1,20,
146,20,147,4,2,
40,0,1,1,2,
0,1,19,148,17,
1,19,140,2,0,
1,18,149,17,1,
18,137,2,0,1,
17,150,17,1,17,
143,2,0,1,16,
151,17,1,16,152,
19,153,4,10,67,
0,108,0,97,0,
115,0,115,0,1,
2,2,0,1,15,
154,17,1,15,155,
19,156,4,14,76,
0,105,0,116,0,
101,0,114,0,97,
0,108,0,1,2,
2,0,1,14,157,
17,1,14,140,2,
0,1,13,158,17,
1,13,159,20,160,
4,2,93,0,1,
1,2,0,1,12,
161,17,1,12,159,
2,0,1,11,162,
17,1,11,159,2,
0,1,10,163,17,
1,10,159,2,0,
1,9,164,17,1,
9,165,19,166,4,
10,73,0,100,0,
101,0,110,0,116,
0,1,1,2,0,
1,8,167,17,1,
8,165,2,0,1,
7,168,17,1,7,
165,2,0,1,6,
169,17,1,6,165,
2,0,1,5,170,
17,1,5,171,20,
172,4,2,91,0,
1,1,2,0,1,
4,173,17,1,4,
140,2,0,1,3,
174,17,1,3,159,
2,0,1,2,175,
17,1,2,165,2,
0,1,1,176,17,
1,1,177,20,178,
4,2,47,0,1,
1,2,0,1,0,
179,17,1,0,0,
2,0,180,5,55,
181,4,14,67,0,
108,0,97,0,115,
0,115,0,95,0,
55,0,182,18,181,
183,5,6,1,64,
184,15,0,151,1,
51,185,15,0,151,
1,39,186,15,0,
151,1,20,187,15,
0,151,1,1,188,
15,0,151,1,29,
189,15,0,151,190,
4,18,83,0,101,
0,103,0,109,0,
101,0,110,0,116,
0,95,0,50,0,
191,18,190,192,5,
6,1,64,193,15,
0,150,1,51,194,
15,0,150,1,39,
195,15,0,150,1,
20,196,15,0,142,
1,1,197,15,0,
150,1,29,198,15,
0,150,199,4,14,
67,0,108,0,97,
0,115,0,115,0,
95,0,53,0,200,
18,199,183,201,4,
14,67,0,108,0,
97,0,115,0,115,
0,95,0,52,0,
202,18,201,183,203,
4,18,83,0,101,
0,103,0,109,0,
101,0,110,0,116,
0,95,0,49,0,
204,18,203,192,153,
205,18,153,183,206,
4,12,84,0,101,
0,114,0,109,0,
95,0,50,0,207,
18,206,208,5,5,
1,64,209,15,0,
130,1,51,210,15,
0,130,1,39,211,
15,0,130,1,1,
212,15,0,130,1,
29,213,15,0,130,
214,4,12,84,0,
101,0,114,0,109,
0,95,0,49,0,
215,18,214,208,216,
4,16,67,0,108,
0,97,0,115,0,
115,0,95,0,49,
0,49,0,217,18,
216,183,218,4,20,
67,0,108,0,97,
0,115,0,115,0,
95,0,49,0,48,
0,95,0,49,0,
219,18,218,183,220,
4,14,67,0,108,
0,97,0,115,0,
115,0,95,0,54,
0,221,18,220,183,
125,222,18,125,223,
5,1,1,1,224,
15,0,123,225,4,
14,67,0,108,0,
97,0,115,0,115,
0,95,0,51,0,
226,18,225,183,227,
4,18,67,0,108,
0,97,0,115,0,
115,0,95,0,52,
0,95,0,49,0,
228,18,227,183,229,
4,38,79,0,112,
0,116,0,105,0,
111,0,110,0,97,
0,108,0,83,0,
101,0,103,0,109,
0,101,0,110,0,
116,0,95,0,52,
0,95,0,49,0,
230,18,229,231,5,
5,1,64,232,15,
0,133,1,51,233,
15,0,133,1,39,
234,15,0,133,1,
1,235,15,0,133,
1,29,236,15,0,
133,237,4,34,79,
0,112,0,116,0,
105,0,111,0,110,
0,97,0,108,0,
83,0,101,0,103,
0,109,0,101,0,
110,0,116,0,95,
0,49,0,238,18,
237,231,239,4,34,
79,0,112,0,116,
0,105,0,111,0,
110,0,97,0,108,
0,83,0,101,0,
103,0,109,0,101,
0,110,0,116,0,
95,0,50,0,240,
18,239,231,241,4,
34,79,0,112,0,
116,0,105,0,111,
0,110,0,97,0,
108,0,83,0,101,
0,103,0,109,0,
101,0,110,0,116,
0,95,0,51,0,
242,18,241,231,243,
4,34,79,0,112,
0,116,0,105,0,
111,0,110,0,97,
0,108,0,83,0,
101,0,103,0,109,
0,101,0,110,0,
116,0,95,0,52,
0,244,18,243,231,
245,4,34,79,0,
112,0,116,0,105,
0,111,0,110,0,
97,0,108,0,83,
0,101,0,103,0,
109,0,101,0,110,
0,116,0,95,0,
53,0,246,18,245,
231,107,247,18,107,
248,5,22,1,37,
249,16,250,14,251,
4,26,37,0,84,
0,101,0,114,0,
109,0,83,0,101,
0,113,0,117,0,
101,0,110,0,99,
0,101,0,1,5,
120,1,2,1,2,
252,21,1,11,1,
28,253,16,254,14,
255,4,10,37,0,
84,0,101,0,114,
0,109,0,1,5,
131,1,1,1,1,
256,21,1,12,1,
29,257,16,258,14,
251,1,5,120,1,
1,1,1,259,21,
1,10,1,75,260,
16,261,14,262,4,
24,37,0,69,0,
110,0,118,0,105,
0,114,0,111,0,
110,0,109,0,101,
0,110,0,116,0,
1,5,101,1,3,
1,3,263,21,1,
1,1,27,264,16,
265,14,266,4,32,
37,0,79,0,112,
0,116,0,105,0,
111,0,110,0,97,
0,108,0,83,0,
101,0,103,0,109,
0,101,0,110,0,
116,0,1,5,134,
1,3,1,3,267,
21,1,14,1,26,
268,16,269,14,270,
4,40,37,0,79,
0,112,0,116,0,
105,0,111,0,110,
0,97,0,108,0,
83,0,101,0,103,
0,109,0,101,0,
110,0,116,0,95,
0,50,0,95,0,
49,0,1,5,271,
19,272,4,38,79,
0,112,0,116,0,
105,0,111,0,110,
0,97,0,108,0,
83,0,101,0,103,
0,109,0,101,0,
110,0,116,0,95,
0,50,0,95,0,
49,0,1,3,1,
4,1,3,273,21,
1,15,1,63,274,
16,275,14,262,1,
5,101,1,4,1,
4,276,21,1,3,
1,13,277,16,278,
14,279,4,20,37,
0,67,0,108,0,
97,0,115,0,115,
0,95,0,54,0,
95,0,49,0,1,
5,280,19,281,4,
18,67,0,108,0,
97,0,115,0,115,
0,95,0,54,0,
95,0,49,0,1,
3,1,4,1,3,
282,21,1,21,1,
14,283,16,284,14,
285,4,20,37,0,
67,0,108,0,97,
0,115,0,115,0,
95,0,50,0,95,
0,49,0,1,5,
286,19,287,4,18,
67,0,108,0,97,
0,115,0,115,0,
95,0,50,0,95,
0,49,0,1,3,
1,4,1,3,288,
21,1,19,1,19,
289,16,290,14,291,
4,40,37,0,79,
0,112,0,116,0,
105,0,111,0,110,
0,97,0,108,0,
83,0,101,0,103,
0,109,0,101,0,
110,0,116,0,95,
0,52,0,95,0,
49,0,1,5,292,
19,229,1,3,1,
4,1,3,293,21,
1,16,1,62,294,
16,295,14,296,4,
26,37,0,82,0,
105,0,103,0,104,
0,116,0,67,0,
111,0,110,0,116,
0,101,0,120,0,
116,0,1,5,109,
1,1,1,1,297,
21,1,7,1,17,
298,16,299,14,255,
1,5,131,1,1,
1,1,300,21,1,
13,1,16,301,16,
302,14,303,4,16,
37,0,83,0,101,
0,103,0,109,0,
101,0,110,0,116,
0,1,5,143,1,
1,1,1,304,21,
1,17,1,15,305,
16,306,14,303,1,
5,143,1,1,1,
1,307,21,1,18,
1,61,308,16,309,
14,296,1,5,109,
1,2,1,2,310,
21,1,9,1,60,
311,16,312,14,296,
1,5,109,1,1,
1,1,313,21,1,
8,1,12,314,16,
315,14,316,4,20,
37,0,67,0,108,
0,97,0,115,0,
115,0,95,0,56,
0,95,0,49,0,
1,5,317,19,318,
4,18,67,0,108,
0,97,0,115,0,
115,0,95,0,56,
0,95,0,49,0,
1,3,1,5,1,
4,319,21,1,22,
1,11,320,16,321,
14,322,4,22,37,
0,67,0,108,0,
97,0,115,0,115,
0,95,0,49,0,
48,0,95,0,49,
0,1,5,323,19,
218,1,3,1,6,
1,5,324,21,1,
23,1,10,325,16,
326,14,327,4,22,
37,0,67,0,108,
0,97,0,115,0,
115,0,95,0,49,
0,50,0,95,0,
49,0,1,5,328,
19,329,4,20,67,
0,108,0,97,0,
115,0,115,0,95,
0,49,0,50,0,
95,0,49,0,1,
3,1,7,1,6,
330,21,1,24,1,
4,331,16,332,14,
333,4,20,37,0,
67,0,108,0,97,
0,115,0,115,0,
95,0,52,0,95,
0,49,0,1,5,
334,19,227,1,3,
1,4,1,3,335,
21,1,20,1,51,
336,16,337,14,262,
1,5,101,1,3,
1,3,338,21,1,
2,1,2,339,16,
340,14,341,4,24,
37,0,76,0,105,
0,116,0,101,0,
114,0,97,0,108,
0,95,0,50,0,
95,0,49,0,1,
5,342,19,343,4,
22,76,0,105,0,
116,0,101,0,114,
0,97,0,108,0,
95,0,50,0,95,
0,49,0,1,3,
1,2,1,1,344,
21,1,25,121,345,
18,121,346,5,5,
1,64,347,15,0,
119,1,51,348,15,
0,119,1,39,349,
15,0,127,1,1,
350,15,0,126,1,
29,351,15,0,129,
352,4,28,84,0,
101,0,114,0,109,
0,83,0,101,0,
113,0,117,0,101,
0,110,0,99,0,
101,0,95,0,49,
0,353,18,352,346,
329,354,18,329,183,
355,4,28,84,0,
101,0,114,0,109,
0,83,0,101,0,
113,0,117,0,101,
0,110,0,99,0,
101,0,95,0,50,
0,356,18,355,346,
357,4,28,82,0,
105,0,103,0,104,
0,116,0,67,0,
111,0,110,0,116,
0,101,0,120,0,
116,0,95,0,50,
0,358,18,357,359,
5,2,1,64,360,
15,0,108,1,51,
361,15,0,114,362,
4,28,82,0,105,
0,103,0,104,0,
116,0,67,0,111,
0,110,0,116,0,
101,0,120,0,116,
0,95,0,51,0,
363,18,362,359,135,
364,18,135,231,365,
4,28,82,0,105,
0,103,0,104,0,
116,0,67,0,111,
0,110,0,116,0,
101,0,120,0,116,
0,95,0,49,0,
366,18,365,359,367,
4,26,69,0,110,
0,118,0,105,0,
114,0,111,0,110,
0,109,0,101,0,
110,0,116,0,95,
0,51,0,368,18,
367,369,5,1,1,
0,370,15,0,103,
371,4,16,67,0,
108,0,97,0,115,
0,115,0,95,0,
49,0,50,0,372,
18,371,183,156,373,
18,156,374,5,6,
1,64,375,15,0,
154,1,51,376,15,
0,154,1,39,377,
15,0,154,1,20,
378,15,0,154,1,
1,379,15,0,154,
1,29,380,15,0,
154,272,381,18,272,
231,382,4,18,76,
0,105,0,116,0,
101,0,114,0,97,
0,108,0,95,0,
50,0,383,18,382,
374,110,384,18,110,
359,287,385,18,287,
183,281,386,18,281,
183,387,4,26,69,
0,110,0,118,0,
105,0,114,0,111,
0,110,0,109,0,
101,0,110,0,116,
0,95,0,49,0,
388,18,387,369,389,
4,26,69,0,110,
0,118,0,105,0,
114,0,111,0,110,
0,109,0,101,0,
110,0,116,0,95,
0,50,0,390,18,
389,369,391,4,26,
76,0,101,0,102,
0,116,0,67,0,
111,0,110,0,116,
0,101,0,120,0,
116,0,95,0,49,
0,392,18,391,223,
393,4,18,76,0,
105,0,116,0,101,
0,114,0,97,0,
108,0,95,0,49,
0,394,18,393,374,
395,4,26,76,0,
101,0,102,0,116,
0,67,0,111,0,
110,0,116,0,101,
0,120,0,116,0,
95,0,51,0,396,
18,395,223,397,4,
26,76,0,101,0,
102,0,116,0,67,
0,111,0,110,0,
116,0,101,0,120,
0,116,0,95,0,
50,0,398,18,397,
223,343,399,18,343,
374,318,400,18,318,
183,144,401,18,144,
192,402,4,14,67,
0,108,0,97,0,
115,0,115,0,95,
0,57,0,403,18,
402,183,404,4,14,
67,0,108,0,97,
0,115,0,115,0,
95,0,56,0,405,
18,404,183,132,406,
18,132,208,166,407,
18,166,408,5,24,
1,39,409,15,0,
175,1,29,410,15,
0,175,1,28,253,
1,27,264,1,26,
268,1,64,411,15,
0,175,1,20,412,
15,0,175,1,19,
289,1,4,331,1,
17,298,1,16,301,
1,15,305,1,14,
283,1,13,277,1,
12,314,1,11,320,
1,10,325,1,8,
413,15,0,164,1,
7,414,15,0,167,
1,6,415,15,0,
168,1,5,416,15,
0,169,1,51,417,
15,0,175,1,2,
339,1,1,418,15,
0,175,102,419,18,
102,369,141,420,18,
141,421,5,4,1,
18,422,15,0,148,
1,6,423,15,0,
157,1,3,424,15,
0,173,1,25,425,
15,0,139,426,4,
14,67,0,108,0,
97,0,115,0,115,
0,95,0,50,0,
427,18,426,183,428,
4,14,67,0,108,
0,97,0,115,0,
115,0,95,0,49,
0,429,18,428,183,
430,4,16,67,0,
108,0,97,0,115,
0,115,0,95,0,
49,0,48,0,431,
18,430,183,432,5,
7,147,433,18,147,
434,5,19,1,39,
435,15,0,145,1,
29,436,15,0,145,
1,28,253,1,27,
264,1,26,268,1,
64,437,15,0,145,
1,19,289,1,17,
298,1,16,301,1,
15,305,1,14,283,
1,13,277,1,12,
314,1,11,320,1,
10,325,1,4,331,
1,51,438,15,0,
145,1,2,339,1,
1,439,15,0,145,
113,440,18,113,441,
5,21,1,39,442,
16,443,14,444,4,
24,37,0,76,0,
101,0,102,0,116,
0,67,0,111,0,
110,0,116,0,101,
0,120,0,116,0,
1,5,124,1,1,
1,1,445,21,1,
4,1,37,249,1,
29,257,1,28,253,
1,27,264,1,26,
268,1,19,289,1,
17,298,1,16,301,
1,15,305,1,14,
283,1,13,277,1,
12,314,1,11,320,
1,10,325,1,2,
339,1,1,446,15,
0,111,1,4,331,
1,50,447,15,0,
122,1,49,448,16,
449,14,444,1,5,
124,1,1,1,1,
450,21,1,5,1,
48,451,16,452,14,
444,1,5,124,1,
2,1,2,453,21,
1,6,138,454,18,
138,455,5,11,1,
17,456,15,0,149,
1,16,301,1,15,
305,1,14,283,1,
13,277,1,12,314,
1,11,320,1,10,
325,1,2,339,1,
4,331,1,25,457,
15,0,136,160,458,
18,160,459,5,5,
1,9,460,15,0,
163,1,8,461,15,
0,162,1,7,462,
15,0,161,1,6,
463,15,0,158,1,
2,464,15,0,174,
178,465,18,178,466,
5,1,1,0,467,
15,0,176,117,468,
18,117,469,5,20,
1,37,249,1,29,
257,1,28,253,1,
27,264,1,26,268,
1,17,298,1,19,
289,1,60,470,15,
0,118,1,64,471,
15,0,115,1,16,
301,1,15,305,1,
14,283,1,13,277,
1,12,314,1,11,
320,1,10,325,1,
4,331,1,51,472,
15,0,115,1,2,
339,1,1,473,15,
0,128,172,474,18,
172,475,5,20,1,
39,476,15,0,170,
1,29,477,15,0,
170,1,28,253,1,
27,264,1,26,268,
1,64,478,15,0,
170,1,20,479,15,
0,170,1,19,289,
1,17,298,1,16,
301,1,15,305,1,
14,283,1,13,277,
1,12,314,1,11,
320,1,10,325,1,
4,331,1,51,480,
15,0,170,1,2,
339,1,1,481,15,
0,170,2,1,0};
new Sfactory(this,"Class_7",new SCreator(Class_7_factory));
new Sfactory(this,"Class_6",new SCreator(Class_6_factory));
new Sfactory(this,"Class_5",new SCreator(Class_5_factory));
new Sfactory(this,"Class_4",new SCreator(Class_4_factory));
new Sfactory(this,"Segment_1",new SCreator(Segment_1_factory));
new Sfactory(this,"Class",new SCreator(Class_factory));
new Sfactory(this,"Segment_2",new SCreator(Segment_2_factory));
new Sfactory(this,"Term_2",new SCreator(Term_2_factory));
new Sfactory(this,"Class_3",new SCreator(Class_3_factory));
new Sfactory(this,"Class_2",new SCreator(Class_2_factory));
new Sfactory(this,"Class_10_1",new SCreator(Class_10_1_factory));
new Sfactory(this,"LeftContext",new SCreator(LeftContext_factory));
new Sfactory(this,"RightContext_2",new SCreator(RightContext_2_factory));
new Sfactory(this,"Class_4_1",new SCreator(Class_4_1_factory));
new Sfactory(this,"Term_1",new SCreator(Term_1_factory));
new Sfactory(this,"OptionalSegment_4_1",new SCreator(OptionalSegment_4_1_factory));
new Sfactory(this,"LeftContext_1",new SCreator(LeftContext_1_factory));
new Sfactory(this,"OptionalSegment_1",new SCreator(OptionalSegment_1_factory));
new Sfactory(this,"OptionalSegment_2",new SCreator(OptionalSegment_2_factory));
new Sfactory(this,"OptionalSegment_3",new SCreator(OptionalSegment_3_factory));
new Sfactory(this,"OptionalSegment_4",new SCreator(OptionalSegment_4_factory));
new Sfactory(this,"OptionalSegment_5",new SCreator(OptionalSegment_5_factory));
new Sfactory(this,"TermSequence",new SCreator(TermSequence_factory));
new Sfactory(this,"Class_12_1",new SCreator(Class_12_1_factory));
new Sfactory(this,"TermSequence_1",new SCreator(TermSequence_1_factory));
new Sfactory(this,"Class_12",new SCreator(Class_12_factory));
new Sfactory(this,"TermSequence_2",new SCreator(TermSequence_2_factory));
new Sfactory(this,"Class_11",new SCreator(Class_11_factory));
new Sfactory(this,"RightContext_3",new SCreator(RightContext_3_factory));
new Sfactory(this,"OptionalSegment",new SCreator(OptionalSegment_factory));
new Sfactory(this,"RightContext_1",new SCreator(RightContext_1_factory));
new Sfactory(this,"Environment_1",new SCreator(Environment_1_factory));
new Sfactory(this,"Literal",new SCreator(Literal_factory));
new Sfactory(this,"OptionalSegment_2_1",new SCreator(OptionalSegment_2_1_factory));
new Sfactory(this,"RightContext",new SCreator(RightContext_factory));
new Sfactory(this,"Class_2_1",new SCreator(Class_2_1_factory));
new Sfactory(this,"Literal_2",new SCreator(Literal_2_factory));
new Sfactory(this,"Environment_2",new SCreator(Environment_2_factory));
new Sfactory(this,"Environment_3",new SCreator(Environment_3_factory));
new Sfactory(this,"Literal_1",new SCreator(Literal_1_factory));
new Sfactory(this,"LeftContext_3",new SCreator(LeftContext_3_factory));
new Sfactory(this,"LeftContext_2",new SCreator(LeftContext_2_factory));
new Sfactory(this,"Literal_2_1",new SCreator(Literal_2_1_factory));
new Sfactory(this,"Class_8_1",new SCreator(Class_8_1_factory));
new Sfactory(this,"Segment",new SCreator(Segment_factory));
new Sfactory(this,"Class_9",new SCreator(Class_9_factory));
new Sfactory(this,"Class_8",new SCreator(Class_8_factory));
new Sfactory(this,"Term",new SCreator(Term_factory));
new Sfactory(this,"Environment",new SCreator(Environment_factory));
new Sfactory(this,"error",new SCreator(error_factory));
new Sfactory(this,"Class_6_1",new SCreator(Class_6_1_factory));
new Sfactory(this,"Class_1",new SCreator(Class_1_factory));
new Sfactory(this,"Class_10",new SCreator(Class_10_factory));
}
/// <summary/>
public static object Class_7_factory(Parser yyp) { return new Class_7(yyp); }
/// <summary/>
public static object Class_6_factory(Parser yyp) { return new Class_6(yyp); }
/// <summary/>
public static object Class_5_factory(Parser yyp) { return new Class_5(yyp); }
/// <summary/>
public static object Class_4_factory(Parser yyp) { return new Class_4(yyp); }
/// <summary/>
public static object Segment_1_factory(Parser yyp) { return new Segment_1(yyp); }
/// <summary/>
public static object Class_factory(Parser yyp) { return new Class(yyp); }
/// <summary/>
public static object Segment_2_factory(Parser yyp) { return new Segment_2(yyp); }
/// <summary/>
public static object Term_2_factory(Parser yyp) { return new Term_2(yyp); }
/// <summary/>
public static object Class_3_factory(Parser yyp) { return new Class_3(yyp); }
/// <summary/>
public static object Class_2_factory(Parser yyp) { return new Class_2(yyp); }
/// <summary/>
public static object Class_10_1_factory(Parser yyp) { return new Class_10_1(yyp); }
/// <summary/>
public static object LeftContext_factory(Parser yyp) { return new LeftContext(yyp); }
/// <summary/>
public static object RightContext_2_factory(Parser yyp) { return new RightContext_2(yyp); }
/// <summary/>
public static object Class_4_1_factory(Parser yyp) { return new Class_4_1(yyp); }
/// <summary/>
public static object Term_1_factory(Parser yyp) { return new Term_1(yyp); }
/// <summary/>
public static object OptionalSegment_4_1_factory(Parser yyp) { return new OptionalSegment_4_1(yyp); }
/// <summary/>
public static object LeftContext_1_factory(Parser yyp) { return new LeftContext_1(yyp); }
/// <summary/>
public static object OptionalSegment_1_factory(Parser yyp) { return new OptionalSegment_1(yyp); }
/// <summary/>
public static object OptionalSegment_2_factory(Parser yyp) { return new OptionalSegment_2(yyp); }
/// <summary/>
public static object OptionalSegment_3_factory(Parser yyp) { return new OptionalSegment_3(yyp); }
/// <summary/>
public static object OptionalSegment_4_factory(Parser yyp) { return new OptionalSegment_4(yyp); }
/// <summary/>
public static object OptionalSegment_5_factory(Parser yyp) { return new OptionalSegment_5(yyp); }
/// <summary/>
public static object TermSequence_factory(Parser yyp) { return new TermSequence(yyp); }
/// <summary/>
public static object Class_12_1_factory(Parser yyp) { return new Class_12_1(yyp); }
/// <summary/>
public static object TermSequence_1_factory(Parser yyp) { return new TermSequence_1(yyp); }
/// <summary/>
public static object Class_12_factory(Parser yyp) { return new Class_12(yyp); }
/// <summary/>
public static object TermSequence_2_factory(Parser yyp) { return new TermSequence_2(yyp); }
/// <summary/>
public static object Class_11_factory(Parser yyp) { return new Class_11(yyp); }
/// <summary/>
public static object RightContext_3_factory(Parser yyp) { return new RightContext_3(yyp); }
/// <summary/>
public static object OptionalSegment_factory(Parser yyp) { return new OptionalSegment(yyp); }
/// <summary/>
public static object RightContext_1_factory(Parser yyp) { return new RightContext_1(yyp); }
/// <summary/>
public static object Environment_1_factory(Parser yyp) { return new Environment_1(yyp); }
/// <summary/>
public static object Literal_factory(Parser yyp) { return new Literal(yyp); }
/// <summary/>
public static object OptionalSegment_2_1_factory(Parser yyp) { return new OptionalSegment_2_1(yyp); }
/// <summary/>
public static object RightContext_factory(Parser yyp) { return new RightContext(yyp); }
/// <summary/>
public static object Class_2_1_factory(Parser yyp) { return new Class_2_1(yyp); }
/// <summary/>
public static object Literal_2_factory(Parser yyp) { return new Literal_2(yyp); }
/// <summary/>
public static object Environment_2_factory(Parser yyp) { return new Environment_2(yyp); }
/// <summary/>
public static object Environment_3_factory(Parser yyp) { return new Environment_3(yyp); }
/// <summary/>
public static object Literal_1_factory(Parser yyp) { return new Literal_1(yyp); }
/// <summary/>
public static object LeftContext_3_factory(Parser yyp) { return new LeftContext_3(yyp); }
/// <summary/>
public static object LeftContext_2_factory(Parser yyp) { return new LeftContext_2(yyp); }
/// <summary/>
public static object Literal_2_1_factory(Parser yyp) { return new Literal_2_1(yyp); }
/// <summary/>
public static object Class_8_1_factory(Parser yyp) { return new Class_8_1(yyp); }
/// <summary/>
public static object Segment_factory(Parser yyp) { return new Segment(yyp); }
/// <summary/>
public static object Class_9_factory(Parser yyp) { return new Class_9(yyp); }
/// <summary/>
public static object Class_8_factory(Parser yyp) { return new Class_8(yyp); }
/// <summary/>
public static object Term_factory(Parser yyp) { return new Term(yyp); }
/// <summary/>
public static object Environment_factory(Parser yyp) { return new Environment(yyp); }
/// <summary/>
public static object error_factory(Parser yyp) { return new error(yyp); }
/// <summary/>
public static object Class_6_1_factory(Parser yyp) { return new Class_6_1(yyp); }
/// <summary/>
public static object Class_1_factory(Parser yyp) { return new Class_1(yyp); }
/// <summary/>
public static object Class_10_factory(Parser yyp) { return new Class_10(yyp); }
}
/// <summary/>
public class PhonEnvParser
: Parser {
/// <summary/>
public PhonEnvParser
():base(new yyPhonEnvParser
(),new tokens()) {}
/// <summary/>
public PhonEnvParser
(Symbols syms):base(syms,new tokens()) {}
/// <summary/>
public PhonEnvParser
(Symbols syms,ErrorHandler erh):base(syms,new tokens(erh)) {}
#region
#endregion
bool m_fSuccess;
System.Collections.SortedList m_NaturalClasses;
System.Collections.SortedList m_Segments;
string m_sErrorMessage;
string m_sInput;
int m_pos;
	/// <summary/>
	public enum SyntaxErrType
	{
	/// <summary/>
		unknown,
	/// <summary/>
		missingOpeningParen,
	/// <summary/>
		missingClosingParen,
	/// <summary/>
		missingOpeningSquareBracket,
	/// <summary/>
		missingClosingSquareBracket,
	}
SyntaxErrType m_syntaxErrType;
	/// <summary/>
		public void ResetNaturalClasses(string[] saSegments)
		{
			ResetSortedList(ref m_NaturalClasses, saSegments);
		}
	/// <summary/>
		public void ResetSegments(string[] saSegments)
		{
			ResetSortedList(ref m_Segments, saSegments);
		}
	/// <summary/>
		public void ResetSortedList(ref System.Collections.SortedList list, string[] saContents)
		{
			list = new System.Collections.SortedList();
			foreach (string s in saContents)
				if (!list.ContainsKey(s))
					list.Add(s, s);

#if TestingOnly
			Console.WriteLine("sorted list contains:");
			for ( int i = 0; i < list.Count; i++ )
			{
				Console.WriteLine( "  {0}:{1}", list.GetKey(i), list.GetByIndex(i) );
			}
#endif
		}
	/// <summary/>
		public bool IsValidClass(string sClass)
		{
			char[] digit = new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
			string sClassLookUp = sClass;
			int i = sClass.LastIndexOf("^");
			if (i > 0)
			{
				if (i+2 == sClass.Length)
				{
					int j = sClass.LastIndexOfAny(digit);
					if (j > 0)
						sClassLookUp = sClass.Substring(0, sClass.Length - 2);
				}
			}
			return m_NaturalClasses.Contains(sClassLookUp);
		}
	/// <summary/>
		public bool IsValidSegment(string sSegment, ref int iPos)
		{
			if (m_Segments.Contains(sSegment))
				return true;
			else
				return HasAValidSequenceOfSegments(sSegment, ref iPos);
		}
		private bool HasAValidSequenceOfSegments(string sSequence, ref int iPos)
		{
			if (sSequence.Length == 0)
				return true;
			for (int len = 1; len < sSequence.Length + 1; len++)
			{
				if (m_Segments.Contains(sSequence.Substring(0, len)))
				{
					iPos += len;
					if (HasAValidSequenceOfSegments(sSequence.Substring(len), ref iPos))
						return true;
				}
			}
			return false;
		}
	/// <summary/>
		public void CreateErrorMessage(string sType, string sItem, int pos)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("<phonEnv status=");
			sb.Append('"');
			sb.Append(sType);
			sb.Append('"');
			sb.Append(" pos=");
			sb.Append('"');
			sb.Append(pos.ToString());
			sb.Append('"');
			sb.Append(">");
			sb.Append(m_sInput);
			sb.Append("</phonEnv>");
			m_sErrorMessage = sb.ToString();
			m_fSuccess = false;
#if TestingOnly
			Console.WriteLine(m_sErrorMessage);
#endif
		}
	/// <summary/>
		public void ThrowError(int iPos)
		{
			m_pos = iPos;
			CSToolsException exc = new CSToolsException(iPos, "");
			throw (exc);
		}
	/// <summary/>
	public string Input
	{
		get
		{
			return m_sInput;
		}
		set
		{
			this.m_sInput = value;
		}
	}
	/// <summary/>
	public bool Success
	{
		get
		{
			return m_fSuccess;
		}
		set
		{
			m_fSuccess = value;
		}
	}
	/// <summary/>
	public string ErrorMessage
	{
		get
		{
			return m_sErrorMessage;
		}
	}
	/// <summary/>
	public int Position
	{
		get
		{
			return m_pos;
		}
		set
		{
			m_pos = value;
		}
	}
	/// <summary/>
	public SyntaxErrType SyntaxErrorType
	{
		get
		{
			return m_syntaxErrType;
		}
		set
		{
			m_syntaxErrType = value;
		}
	}

 }
}
