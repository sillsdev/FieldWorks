// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System;using Tools;
namespace YYClass {
//%+GStuff
public class GStuff : TOKEN{
public override string yyname() { return "GStuff"; }
public GStuff(Parser yyq):base(yyq){ }}
//%+Stuff
public class Stuff : TOKEN{
public override string yyname() { return "Stuff"; }
public Stuff(Parser yyq):base(yyq){ }}
//%+Item
public class Item : TOKEN{
public override string yyname() { return "Item"; }
public Item(Parser yyq):base(yyq){ }}
//%+ClassBody
public class ClassBody : TOKEN{
public override string yyname() { return "ClassBody"; }
public ClassBody(Parser yyq):base(yyq){ }}
//%+Cons
public class Cons : TOKEN{
public override string yyname() { return "Cons"; }
public Cons(Parser yyq):base(yyq){ }}
//%+Call
public class Call : TOKEN{
public override string yyname() { return "Call"; }
public Call(Parser yyq):base(yyq){ }}
//%+BaseCall
public class BaseCall : TOKEN{
public override string yyname() { return "BaseCall"; }
public BaseCall(Parser yyq):base(yyq){ }}
//%+Name
public class Name : TOKEN{
public override string yyname() { return "Name"; }
public Name(Parser yyq):base(yyq){ }}

public class ClassBody_1 : ClassBody {
  public ClassBody_1(Parser yyq):base(yyq){}}

public class ClassBody_2 : ClassBody {
  public ClassBody_2(Parser yyq):base(yyq){}}

public class ClassBody_2_1 : ClassBody_2 {
  public ClassBody_2_1(Parser yyq):base(yyq){ yytext=
	((GStuff)(yyq.StackAt(1).m_value))
	.yytext; }}

public class GStuff_1 : GStuff {
  public GStuff_1(Parser yyq):base(yyq){}}

public class GStuff_2 : GStuff {
  public GStuff_2(Parser yyq):base(yyq){}}

public class GStuff_2_1 : GStuff_2 {
  public GStuff_2_1(Parser yyq):base(yyq){ yytext=""; }}

public class GStuff_3 : GStuff {
  public GStuff_3(Parser yyq):base(yyq){}}

public class GStuff_4 : GStuff {
  public GStuff_4(Parser yyq):base(yyq){}}

public class GStuff_4_1 : GStuff_4 {
  public GStuff_4_1(Parser yyq):base(yyq){ yytext=
	((GStuff)(yyq.StackAt(1).m_value))
	.yytext+
	((Cons)(yyq.StackAt(0).m_value))
	.yytext; }}

public class GStuff_5 : GStuff {
  public GStuff_5(Parser yyq):base(yyq){}}

public class GStuff_6 : GStuff {
  public GStuff_6(Parser yyq):base(yyq){}}

public class GStuff_6_1 : GStuff_6 {
  public GStuff_6_1(Parser yyq):base(yyq){ yytext=
	((GStuff)(yyq.StackAt(1).m_value))
	.yytext+
	((Item)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Stuff_1 : Stuff {
  public Stuff_1(Parser yyq):base(yyq){}}

public class Stuff_2 : Stuff {
  public Stuff_2(Parser yyq):base(yyq){}}

public class Stuff_2_1 : Stuff_2 {
  public Stuff_2_1(Parser yyq):base(yyq){ yytext=""; }}

public class Stuff_3 : Stuff {
  public Stuff_3(Parser yyq):base(yyq){}}

public class Stuff_4 : Stuff {
  public Stuff_4(Parser yyq):base(yyq){}}

public class Stuff_4_1 : Stuff_4 {
  public Stuff_4_1(Parser yyq):base(yyq){ yytext=
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+
	((Item)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Cons_1 : Cons {
  public Cons_1(Parser yyq):base(yyq){}}

public class Cons_2 : Cons {
  public Cons_2(Parser yyq):base(yyq){}}

public class Cons_2_1 : Cons_2 {
  public Cons_2_1(Parser yyq):base(yyq){
			cs0syntax yy = (cs0syntax)yyq;
			if (
	((Name)(yyq.StackAt(4).m_value))
	.yytext.Trim()!=yy.Cls)
					yytext=
	((Name)(yyq.StackAt(4).m_value))
	.yytext+"("+
	((Stuff)(yyq.StackAt(2).m_value))
	.yytext+")";
			else {
				if (
	((Stuff)(yyq.StackAt(2).m_value))
	.yytext.Length==0) {
					yytext=
	((Name)(yyq.StackAt(4).m_value))
	.yytext+"("+yy.Ctx+")"; yy.defconseen=true;
				} else
					yytext=
	((Name)(yyq.StackAt(4).m_value))
	.yytext+"("+yy.Ctx+","+
	((Stuff)(yyq.StackAt(2).m_value))
	.yytext+")";
				if (
	((BaseCall)(yyq.StackAt(0).m_value))
	.yytext.Length==0)
					yytext+=":base("+yy.Par+")";
				else
					yytext+=":"+
	((BaseCall)(yyq.StackAt(0).m_value))
	.yytext.Substring(0,4)+"("+yy.Par+","+
	((BaseCall)(yyq.StackAt(0).m_value))
	.yytext.Substring(4)+")";
				}
			}}

public class Call_1 : Call {
  public Call_1(Parser yyq):base(yyq){}}

public class Call_2 : Call {
  public Call_2(Parser yyq):base(yyq){}}

public class Call_2_1 : Call_2 {
  public Call_2_1(Parser yyq):base(yyq){
			if (
	((Name)(yyq.StackAt(3).m_value))
	.yytext.Trim()!=((cs0syntax)yyq).Cls)
					yytext=
	((Name)(yyq.StackAt(3).m_value))
	.yytext+"("+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+")";
			else {
				if (
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext.Length==0)
					yytext=
	((Name)(yyq.StackAt(3).m_value))
	.yytext+"("+((cs0syntax)yyq).Par+")";
				else
					yytext=
	((Name)(yyq.StackAt(3).m_value))
	.yytext+"("+((cs0syntax)yyq).Par+","+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+")";
				}
			}}

public class BaseCall_1 : BaseCall {
  public BaseCall_1(Parser yyq):base(yyq){}}

public class BaseCall_2 : BaseCall {
  public BaseCall_2(Parser yyq):base(yyq){}}

public class BaseCall_2_1 : BaseCall_2 {
  public BaseCall_2_1(Parser yyq):base(yyq){ yytext=""; }}

public class BaseCall_3 : BaseCall {
  public BaseCall_3(Parser yyq):base(yyq){}}

public class BaseCall_4 : BaseCall {
  public BaseCall_4(Parser yyq):base(yyq){}}

public class BaseCall_4_1 : BaseCall_4 {
  public BaseCall_4_1(Parser yyq):base(yyq){ yytext="base"+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext; }}

public class BaseCall_5 : BaseCall {
  public BaseCall_5(Parser yyq):base(yyq){}}

public class BaseCall_6 : BaseCall {
  public BaseCall_6(Parser yyq):base(yyq){}}

public class BaseCall_6_1 : BaseCall_6 {
  public BaseCall_6_1(Parser yyq):base(yyq){ yytext="this"+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext; }}

public class Name_1 : Name {
  public Name_1(Parser yyq):base(yyq){}}

public class Name_2 : Name {
  public Name_2(Parser yyq):base(yyq){}}

public class Name_2_1 : Name_2 {
  public Name_2_1(Parser yyq):base(yyq){ yytext=" "+
	((ID)(yyq.StackAt(0).m_value))
	.yytext+" "; }}

public class Name_3 : Name {
  public Name_3(Parser yyq):base(yyq){}}

public class Name_4 : Name {
  public Name_4(Parser yyq):base(yyq){}}

public class Name_4_1 : Name_4 {
  public Name_4_1(Parser yyq):base(yyq){ yytext=
	((ID)(yyq.StackAt(3).m_value))
	.yytext+"["+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+"]"; }}

public class Item_1 : Item {
  public Item_1(Parser yyq):base(yyq){}}

public class Item_2 : Item {
  public Item_2(Parser yyq):base(yyq){}}

public class Item_2_1 : Item_2 {
  public Item_2_1(Parser yyq):base(yyq){ yytext=
	((ANY)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Item_3 : Item {
  public Item_3(Parser yyq):base(yyq){}}

public class Item_4 : Item {
  public Item_4(Parser yyq):base(yyq){}}

public class Item_4_1 : Item_4 {
  public Item_4_1(Parser yyq):base(yyq){ yytext=
	((Name)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Item_5 : Item {
  public Item_5(Parser yyq):base(yyq){}}

public class Item_6 : Item {
  public Item_6(Parser yyq):base(yyq){}}

public class Item_6_1 : Item_6 {
  public Item_6_1(Parser yyq):base(yyq){ yytext=";\n"; }}

public class Item_7 : Item {
  public Item_7(Parser yyq):base(yyq){}}

public class Item_8 : Item {
  public Item_8(Parser yyq):base(yyq){}}

public class Item_8_1 : Item_8 {
  public Item_8_1(Parser yyq):base(yyq){ yytext=" base "; }}

public class Item_9 : Item {
  public Item_9(Parser yyq):base(yyq){}}

public class Item_10 : Item {
  public Item_10(Parser yyq):base(yyq){}}

public class Item_10_1 : Item_10 {
  public Item_10_1(Parser yyq):base(yyq){ yytext=" this "; }}

public class Item_11 : Item {
  public Item_11(Parser yyq):base(yyq){}}

public class Item_12 : Item {
  public Item_12(Parser yyq):base(yyq){}}

public class Item_12_1 : Item_12 {
  public Item_12_1(Parser yyq):base(yyq){ yytext=" this["+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+"]"; }}

public class Item_13 : Item {
  public Item_13(Parser yyq):base(yyq){}}

public class Item_14 : Item {
  public Item_14(Parser yyq):base(yyq){}}

public class Item_14_1 : Item_14 {
  public Item_14_1(Parser yyq):base(yyq){ yytext=":"; }}

public class Item_15 : Item {
  public Item_15(Parser yyq):base(yyq){}}

public class Item_16 : Item {
  public Item_16(Parser yyq):base(yyq){}}

public class Item_16_1 : Item_16 {
  public Item_16_1(Parser yyq):base(yyq){ yytext=" new "+
	((Call)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Item_17 : Item {
  public Item_17(Parser yyq):base(yyq){}}

public class Item_18 : Item {
  public Item_18(Parser yyq):base(yyq){}}

public class Item_18_1 : Item_18 {
  public Item_18_1(Parser yyq):base(yyq){ yytext=" new "+
	((Name)(yyq.StackAt(0).m_value))
	.yytext; }}

public class Item_19 : Item {
  public Item_19(Parser yyq):base(yyq){}}

public class Item_20 : Item {
  public Item_20(Parser yyq):base(yyq){}}

public class Item_20_1 : Item_20 {
  public Item_20_1(Parser yyq):base(yyq){ yytext="("+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+")"; }}

public class Item_21 : Item {
  public Item_21(Parser yyq):base(yyq){}}

public class Item_22 : Item {
  public Item_22(Parser yyq):base(yyq){}}

public class Item_22_1 : Item_22 {
  public Item_22_1(Parser yyq):base(yyq){ yytext="{"+
	((Stuff)(yyq.StackAt(1).m_value))
	.yytext+"}\n"; }}
public class yycs0syntax : Symbols {
  public override object Action(Parser yyq,SYMBOL yysym, int yyact) {
	switch(yyact) {
	 case -1: break; //// keep compiler happy
}  return null; }
public yycs0syntax ():base() { arr = new int[] {
101,19,102,4,18,
67,0,108,0,97,
0,115,0,115,0,
66,0,111,0,100,
0,121,0,1,2,
103,17,1,133,101,
2,0,104,5,48,
1,98,105,17,1,
98,106,19,107,4,
10,83,0,116,0,
117,0,102,0,102,
0,1,2,2,0,
1,97,108,17,1,
97,109,19,110,4,
12,76,0,80,0,
65,0,82,0,69,
0,78,0,1,1,
2,0,1,96,111,
17,1,96,112,19,
113,4,8,84,0,
72,0,73,0,83,
0,1,1,2,0,
1,95,114,17,1,
95,115,19,116,4,
10,67,0,79,0,
76,0,79,0,78,
0,1,1,2,0,
1,94,117,17,1,
94,118,19,119,4,
12,82,0,80,0,
65,0,82,0,69,
0,78,0,1,1,
2,0,1,83,120,
17,1,83,106,2,
0,1,82,121,17,
1,82,109,2,0,
1,81,122,17,1,
81,123,19,124,4,
8,78,0,97,0,
109,0,101,0,1,
2,2,0,1,56,
125,17,1,56,126,
19,127,4,8,67,
0,97,0,108,0,
108,0,1,2,2,
0,1,54,128,17,
1,54,118,2,0,
1,47,129,17,1,
47,130,19,131,4,
8,73,0,116,0,
101,0,109,0,1,
2,2,0,1,45,
132,17,1,45,133,
19,134,4,12,82,
0,66,0,82,0,
65,0,67,0,75,
0,1,1,2,0,
1,132,135,17,1,
132,136,19,137,4,
12,82,0,66,0,
82,0,65,0,67,
0,69,0,1,1,
2,0,1,134,138,
17,1,134,139,22,
140,4,6,69,0,
79,0,70,0,1,
6,2,0,1,124,
141,17,1,124,118,
2,0,1,131,142,
17,1,131,143,19,
144,4,8,67,0,
111,0,110,0,115,
0,1,2,2,0,
1,35,145,17,1,
35,106,2,0,1,
34,146,17,1,34,
147,19,148,4,12,
76,0,66,0,82,
0,65,0,67,0,
75,0,1,1,2,
0,1,33,149,17,
1,33,150,19,151,
4,4,73,0,68,
0,1,1,2,0,
1,32,152,17,1,
32,153,19,154,4,
6,65,0,78,0,
89,0,1,1,2,
0,1,31,155,17,
1,31,123,2,0,
1,30,156,17,1,
30,157,19,158,4,
18,83,0,69,0,
77,0,73,0,67,
0,79,0,76,0,
79,0,78,0,1,
1,2,0,1,29,
159,17,1,29,160,
19,161,4,8,66,
0,65,0,83,0,
69,0,1,1,2,
0,1,27,162,17,
1,27,133,2,0,
1,133,103,1,112,
163,17,1,112,109,
2,0,1,126,164,
17,1,126,165,19,
166,4,16,66,0,
97,0,115,0,101,
0,67,0,97,0,
108,0,108,0,1,
2,2,0,1,130,
167,17,1,130,130,
2,0,1,22,168,
17,1,22,106,2,
0,1,21,169,17,
1,21,147,2,0,
1,20,170,17,1,
20,112,2,0,1,
19,171,17,1,19,
115,2,0,1,109,
172,17,1,109,118,
2,0,1,15,173,
17,1,15,106,2,
0,1,14,174,17,
1,14,109,2,0,
1,13,175,17,1,
13,123,2,0,1,
12,176,17,1,12,
177,19,178,4,6,
78,0,69,0,87,
0,1,1,2,0,
1,10,179,17,1,
10,118,2,0,1,
111,180,17,1,111,
160,2,0,1,8,
181,17,1,8,106,
2,0,1,7,182,
17,1,7,109,2,
0,1,113,183,17,
1,113,106,2,0,
1,5,184,17,1,
5,136,2,0,1,
4,185,17,1,4,
106,2,0,1,3,
186,17,1,3,187,
19,188,4,12,76,
0,66,0,82,0,
65,0,67,0,69,
0,1,1,2,0,
1,2,189,17,1,
2,190,19,191,4,
12,71,0,83,0,
116,0,117,0,102,
0,102,0,1,2,
2,0,1,1,192,
17,1,1,187,2,
0,1,0,193,17,
1,0,0,2,0,
194,5,94,195,4,
18,73,0,116,0,
101,0,109,0,95,
0,49,0,52,0,
95,0,49,0,196,
18,195,197,5,9,
1,22,198,15,0,
129,1,113,199,15,
0,129,1,15,200,
15,0,129,1,83,
201,15,0,129,1,
35,202,15,0,129,
1,8,203,15,0,
129,1,98,204,15,
0,129,1,4,205,
15,0,129,1,2,
206,15,0,167,207,
4,12,67,0,97,
0,108,0,108,0,
95,0,49,0,208,
18,207,209,5,1,
1,12,210,15,0,
125,110,211,18,110,
212,5,42,1,98,
213,15,0,182,1,
97,214,16,215,14,
216,4,20,37,0,
83,0,116,0,117,
0,102,0,102,0,
95,0,50,0,95,
0,49,0,1,5,
217,19,218,4,18,
83,0,116,0,117,
0,102,0,102,0,
95,0,50,0,95,
0,49,0,1,3,
1,1,1,0,219,
21,1,5,1,96,
220,15,0,108,1,
94,221,16,222,14,
223,4,26,37,0,
66,0,97,0,115,
0,101,0,67,0,
97,0,108,0,108,
0,95,0,50,0,
95,0,49,0,1,
5,224,19,225,4,
24,66,0,97,0,
115,0,101,0,67,
0,97,0,108,0,
108,0,95,0,50,
0,95,0,49,0,
1,3,1,1,1,
0,226,21,1,9,
1,83,227,15,0,
182,1,82,228,16,
215,1,0,219,1,
81,229,15,0,121,
1,56,230,16,231,
14,232,4,20,37,
0,73,0,116,0,
101,0,109,0,95,
0,49,0,54,0,
95,0,49,0,1,
5,233,19,234,4,
18,73,0,116,0,
101,0,109,0,95,
0,49,0,54,0,
95,0,49,0,1,
3,1,3,1,2,
235,21,1,21,1,
54,236,16,237,14,
238,4,18,37,0,
67,0,97,0,108,
0,108,0,95,0,
50,0,95,0,49,
0,1,5,239,19,
240,4,16,67,0,
97,0,108,0,108,
0,95,0,50,0,
95,0,49,0,1,
3,1,5,1,4,
241,21,1,8,1,
47,242,16,243,14,
244,4,20,37,0,
83,0,116,0,117,
0,102,0,102,0,
95,0,52,0,95,
0,49,0,1,5,
245,19,246,4,18,
83,0,116,0,117,
0,102,0,102,0,
95,0,52,0,95,
0,49,0,1,3,
1,3,1,2,247,
21,1,6,1,45,
248,16,249,14,250,
4,18,37,0,78,
0,97,0,109,0,
101,0,95,0,52,
0,95,0,49,0,
1,5,251,19,252,
4,16,78,0,97,
0,109,0,101,0,
95,0,52,0,95,
0,49,0,1,3,
1,5,1,4,253,
21,1,13,1,126,
254,16,255,14,256,
4,18,37,0,67,
0,111,0,110,0,
115,0,95,0,50,
0,95,0,49,0,
1,5,257,19,258,
4,16,67,0,111,
0,110,0,115,0,
95,0,50,0,95,
0,49,0,1,3,
1,6,1,5,259,
21,1,7,1,35,
260,15,0,182,1,
34,261,16,215,1,
0,219,1,33,262,
16,263,14,264,4,
18,37,0,78,0,
97,0,109,0,101,
0,95,0,50,0,
95,0,49,0,1,
5,265,19,266,4,
16,78,0,97,0,
109,0,101,0,95,
0,50,0,95,0,
49,0,1,3,1,
2,1,1,267,21,
1,12,1,32,268,
16,269,14,270,4,
18,37,0,73,0,
116,0,101,0,109,
0,95,0,50,0,
95,0,49,0,1,
5,271,19,272,4,
16,73,0,116,0,
101,0,109,0,95,
0,50,0,95,0,
49,0,1,3,1,
2,1,1,273,21,
1,14,1,31,274,
16,275,14,276,4,
18,37,0,73,0,
116,0,101,0,109,
0,95,0,52,0,
95,0,49,0,1,
5,277,19,278,4,
16,73,0,116,0,
101,0,109,0,95,
0,52,0,95,0,
49,0,1,3,1,
2,1,1,279,21,
1,15,1,30,280,
16,281,14,282,4,
18,37,0,73,0,
116,0,101,0,109,
0,95,0,54,0,
95,0,49,0,1,
5,283,19,284,4,
16,73,0,116,0,
101,0,109,0,95,
0,54,0,95,0,
49,0,1,3,1,
2,1,1,285,21,
1,16,1,29,286,
16,287,14,288,4,
18,37,0,73,0,
116,0,101,0,109,
0,95,0,56,0,
95,0,49,0,1,
5,289,19,290,4,
16,73,0,116,0,
101,0,109,0,95,
0,56,0,95,0,
49,0,1,3,1,
2,1,1,291,21,
1,17,1,27,292,
16,293,14,294,4,
20,37,0,73,0,
116,0,101,0,109,
0,95,0,49,0,
50,0,95,0,49,
0,1,5,295,19,
296,4,18,73,0,
116,0,101,0,109,
0,95,0,49,0,
50,0,95,0,49,
0,1,3,1,5,
1,4,297,21,1,
19,1,131,298,16,
299,14,300,4,22,
37,0,71,0,83,
0,116,0,117,0,
102,0,102,0,95,
0,52,0,95,0,
49,0,1,5,301,
19,302,4,20,71,
0,83,0,116,0,
117,0,102,0,102,
0,95,0,52,0,
95,0,49,0,1,
3,1,3,1,2,
303,21,1,3,1,
130,304,16,305,14,
306,4,22,37,0,
71,0,83,0,116,
0,117,0,102,0,
102,0,95,0,54,
0,95,0,49,0,
1,5,307,19,308,
4,20,71,0,83,
0,116,0,117,0,
102,0,102,0,95,
0,54,0,95,0,
49,0,1,3,1,
3,1,2,309,21,
1,4,1,22,310,
15,0,182,1,21,
311,16,215,1,0,
219,1,20,312,16,
313,14,314,4,20,
37,0,73,0,116,
0,101,0,109,0,
95,0,49,0,48,
0,95,0,49,0,
1,5,315,19,316,
4,18,73,0,116,
0,101,0,109,0,
95,0,49,0,48,
0,95,0,49,0,
1,3,1,2,1,
1,317,21,1,18,
1,19,318,16,319,
14,320,4,20,37,
0,73,0,116,0,
101,0,109,0,95,
0,49,0,52,0,
95,0,49,0,1,
5,321,19,195,1,
3,1,2,1,1,
322,21,1,20,1,
124,323,16,324,14,
325,4,26,37,0,
66,0,97,0,115,
0,101,0,67,0,
97,0,108,0,108,
0,95,0,52,0,
95,0,49,0,1,
5,326,19,327,4,
24,66,0,97,0,
115,0,101,0,67,
0,97,0,108,0,
108,0,95,0,52,
0,95,0,49,0,
1,3,1,6,1,
5,328,21,1,10,
1,2,329,15,0,
182,1,15,330,15,
0,182,1,14,331,
16,215,1,0,219,
1,13,332,15,0,
174,1,4,333,15,
0,182,1,10,334,
16,335,14,336,4,
20,37,0,73,0,
116,0,101,0,109,
0,95,0,50,0,
48,0,95,0,49,
0,1,5,337,19,
338,4,18,73,0,
116,0,101,0,109,
0,95,0,50,0,
48,0,95,0,49,
0,1,3,1,4,
1,3,339,21,1,
23,1,5,340,16,
341,14,342,4,20,
37,0,73,0,116,
0,101,0,109,0,
95,0,50,0,50,
0,95,0,49,0,
1,5,343,19,344,
4,18,73,0,116,
0,101,0,109,0,
95,0,50,0,50,
0,95,0,49,0,
1,3,1,4,1,
3,345,21,1,24,
1,8,346,15,0,
182,1,7,347,16,
215,1,0,219,1,
113,348,15,0,182,
1,112,349,16,215,
1,0,219,1,111,
350,15,0,163,1,
3,351,16,215,1,
0,219,1,109,352,
16,353,14,354,4,
26,37,0,66,0,
97,0,115,0,101,
0,67,0,97,0,
108,0,108,0,95,
0,54,0,95,0,
49,0,1,5,355,
19,356,4,24,66,
0,97,0,115,0,
101,0,67,0,97,
0,108,0,108,0,
95,0,54,0,95,
0,49,0,1,3,
1,6,1,5,357,
21,1,11,1,1,
358,16,359,14,360,
4,22,37,0,71,
0,83,0,116,0,
117,0,102,0,102,
0,95,0,50,0,
95,0,49,0,1,
5,361,19,362,4,
20,71,0,83,0,
116,0,117,0,102,
0,102,0,95,0,
50,0,95,0,49,
0,1,3,1,1,
1,0,363,21,1,
2,364,4,12,73,
0,116,0,101,0,
109,0,95,0,49,
0,365,18,364,197,
366,4,12,73,0,
116,0,101,0,109,
0,95,0,50,0,
367,18,366,197,113,
368,18,113,369,5,
41,1,98,370,15,
0,170,1,97,214,
1,95,371,15,0,
111,1,94,221,1,
83,372,15,0,170,
1,82,228,1,81,
373,16,275,1,1,
279,1,56,230,1,
54,236,1,47,242,
1,45,248,1,126,
254,1,35,374,15,
0,170,1,34,261,
1,33,262,1,32,
268,1,31,274,1,
30,280,1,29,286,
1,27,292,1,131,
298,1,130,304,1,
22,375,15,0,170,
1,21,311,1,20,
312,1,19,318,1,
124,323,1,15,376,
15,0,170,1,14,
331,1,13,377,16,
378,14,379,4,20,
37,0,73,0,116,
0,101,0,109,0,
95,0,49,0,56,
0,95,0,49,0,
1,5,380,19,381,
4,18,73,0,116,
0,101,0,109,0,
95,0,49,0,56,
0,95,0,49,0,
1,3,1,3,1,
2,382,21,1,22,
1,2,383,15,0,
170,1,10,334,1,
5,340,1,8,384,
15,0,170,1,7,
347,1,113,385,15,
0,170,1,112,349,
1,4,386,15,0,
170,1,3,351,1,
109,352,1,1,358,
387,4,20,66,0,
97,0,115,0,101,
0,67,0,97,0,
108,0,108,0,95,
0,50,0,388,18,
387,389,5,1,1,
94,390,15,0,164,
391,4,20,66,0,
97,0,115,0,101,
0,67,0,97,0,
108,0,108,0,95,
0,51,0,392,18,
391,389,393,4,12,
73,0,116,0,101,
0,109,0,95,0,
54,0,394,18,393,
197,395,4,20,66,
0,97,0,115,0,
101,0,67,0,97,
0,108,0,108,0,
95,0,49,0,396,
18,395,389,397,4,
20,66,0,97,0,
115,0,101,0,67,
0,97,0,108,0,
108,0,95,0,54,
0,398,18,397,389,
134,399,18,134,400,
5,19,1,45,248,
1,35,401,15,0,
132,1,34,261,1,
33,262,1,32,268,
1,31,274,1,30,
280,1,29,286,1,
27,292,1,22,402,
15,0,162,1,21,
311,1,20,312,1,
19,318,1,13,377,
1,10,334,1,56,
230,1,54,236,1,
5,340,1,47,242,
403,4,20,66,0,
97,0,115,0,101,
0,67,0,97,0,
108,0,108,0,95,
0,52,0,404,18,
403,389,405,4,20,
66,0,97,0,115,
0,101,0,67,0,
97,0,108,0,108,
0,95,0,53,0,
406,18,405,389,296,
407,18,296,197,381,
408,18,381,197,137,
409,18,137,410,5,
26,1,45,248,1,
32,268,1,131,298,
1,130,304,1,124,
323,1,81,373,1,
33,262,1,126,254,
1,31,274,1,30,
280,1,29,286,1,
27,292,1,20,312,
1,19,318,1,109,
352,1,13,377,1,
10,334,1,56,230,
1,54,236,1,4,
411,15,0,184,1,
5,340,1,94,221,
1,3,351,1,2,
412,15,0,135,1,
1,358,1,47,242,
240,413,18,240,209,
278,414,18,278,197,
415,4,22,67,0,
108,0,97,0,115,
0,115,0,66,0,
111,0,100,0,121,
0,95,0,49,0,
416,18,415,417,5,
1,1,0,418,15,
0,103,419,4,16,
71,0,83,0,116,
0,117,0,102,0,
102,0,95,0,51,
0,420,18,419,421,
5,1,1,1,422,
15,0,189,423,4,
16,71,0,83,0,
116,0,117,0,102,
0,102,0,95,0,
52,0,424,18,423,
421,425,4,16,71,
0,83,0,116,0,
117,0,102,0,102,
0,95,0,53,0,
426,18,425,421,427,
4,16,71,0,83,
0,116,0,117,0,
102,0,102,0,95,
0,54,0,428,18,
427,421,344,429,18,
344,197,234,430,18,
234,197,431,4,12,
78,0,97,0,109,
0,101,0,95,0,
50,0,432,18,431,
433,5,10,1,22,
434,15,0,155,1,
113,435,15,0,155,
1,15,436,15,0,
155,1,35,437,15,
0,155,1,83,438,
15,0,155,1,12,
439,15,0,175,1,
8,440,15,0,155,
1,98,441,15,0,
155,1,4,442,15,
0,155,1,2,443,
15,0,122,158,444,
18,158,445,5,40,
1,98,446,15,0,
156,1,97,214,1,
94,221,1,83,447,
15,0,156,1,82,
228,1,81,373,1,
56,230,1,54,236,
1,47,242,1,45,
248,1,126,254,1,
35,448,15,0,156,
1,34,261,1,33,
262,1,32,268,1,
31,274,1,30,280,
1,29,286,1,27,
292,1,131,298,1,
130,304,1,22,449,
15,0,156,1,21,
311,1,20,312,1,
19,318,1,124,323,
1,15,450,15,0,
156,1,14,331,1,
13,377,1,2,451,
15,0,156,1,10,
334,1,5,340,1,
8,452,15,0,156,
1,7,347,1,113,
453,15,0,156,1,
112,349,1,4,454,
15,0,156,1,3,
351,1,109,352,1,
1,358,166,455,18,
166,389,272,456,18,
272,197,290,457,18,
290,197,178,458,18,
178,459,5,40,1,
98,460,15,0,176,
1,97,214,1,94,
221,1,83,461,15,
0,176,1,82,228,
1,81,373,1,56,
230,1,54,236,1,
47,242,1,45,248,
1,126,254,1,35,
462,15,0,176,1,
34,261,1,33,262,
1,32,268,1,31,
274,1,30,280,1,
29,286,1,27,292,
1,131,298,1,130,
304,1,22,463,15,
0,176,1,21,311,
1,20,312,1,19,
318,1,124,323,1,
15,464,15,0,176,
1,14,331,1,13,
377,1,2,465,15,
0,176,1,10,334,
1,5,340,1,8,
466,15,0,176,1,
7,347,1,113,467,
15,0,176,1,112,
349,1,4,468,15,
0,176,1,3,351,
1,109,352,1,1,
358,469,4,16,71,
0,83,0,116,0,
117,0,102,0,102,
0,95,0,50,0,
470,18,469,421,151,
471,18,151,472,5,
41,1,98,473,15,
0,149,1,97,214,
1,94,221,1,83,
474,15,0,149,1,
82,228,1,81,373,
1,56,230,1,54,
236,1,47,242,1,
45,248,1,126,254,
1,35,475,15,0,
149,1,34,261,1,
33,262,1,32,268,
1,31,274,1,30,
280,1,29,286,1,
27,292,1,131,298,
1,130,304,1,22,
476,15,0,149,1,
21,311,1,20,312,
1,19,318,1,124,
323,1,2,477,15,
0,149,1,15,478,
15,0,149,1,14,
331,1,13,377,1,
12,479,15,0,149,
1,10,334,1,5,
340,1,8,480,15,
0,149,1,7,347,
1,113,481,15,0,
149,1,112,349,1,
4,482,15,0,149,
1,3,351,1,109,
352,1,1,358,483,
4,14,83,0,116,
0,117,0,102,0,
102,0,95,0,49,
0,484,18,483,485,
5,8,1,21,486,
15,0,168,1,112,
487,15,0,183,1,
14,488,15,0,173,
1,82,489,15,0,
120,1,34,490,15,
0,145,1,7,491,
15,0,181,1,97,
492,15,0,105,1,
3,493,15,0,185,
494,4,14,73,0,
116,0,101,0,109,
0,95,0,49,0,
54,0,495,18,494,
197,119,496,18,119,
497,5,25,1,45,
248,1,83,498,15,
0,117,1,82,228,
1,33,262,1,32,
268,1,31,274,1,
30,280,1,29,286,
1,27,292,1,113,
499,15,0,141,1,
20,312,1,19,318,
1,112,349,1,15,
500,15,0,128,1,
14,331,1,13,377,
1,7,347,1,10,
334,1,56,230,1,
8,501,15,0,179,
1,54,236,1,5,
340,1,98,502,15,
0,172,1,97,214,
1,47,242,258,503,
18,258,504,5,1,
1,2,505,15,0,
142,362,506,18,362,
421,507,4,12,73,
0,116,0,101,0,
109,0,95,0,53,
0,508,18,507,197,
509,4,12,73,0,
116,0,101,0,109,
0,95,0,55,0,
510,18,509,197,511,
4,12,73,0,116,
0,101,0,109,0,
95,0,56,0,512,
18,511,197,284,513,
18,284,197,188,514,
18,188,515,5,41,
1,98,516,15,0,
186,1,97,214,1,
94,221,1,83,517,
15,0,186,1,82,
228,1,81,373,1,
56,230,1,54,236,
1,47,242,1,45,
248,1,126,254,1,
35,518,15,0,186,
1,34,261,1,33,
262,1,32,268,1,
31,274,1,30,280,
1,29,286,1,27,
292,1,131,298,1,
130,304,1,22,519,
15,0,186,1,21,
311,1,20,312,1,
19,318,1,124,323,
1,15,520,15,0,
186,1,14,331,1,
13,377,1,2,521,
15,0,186,1,10,
334,1,5,340,1,
8,522,15,0,186,
1,7,347,1,113,
523,15,0,186,1,
112,349,1,4,524,
15,0,186,1,3,
351,1,109,352,1,
1,358,1,0,525,
15,0,192,116,526,
18,116,527,5,40,
1,98,528,15,0,
171,1,97,214,1,
94,529,15,0,114,
1,83,530,15,0,
171,1,82,228,1,
81,373,1,56,230,
1,54,236,1,47,
242,1,45,248,1,
126,254,1,35,531,
15,0,171,1,34,
261,1,33,262,1,
32,268,1,31,274,
1,30,280,1,29,
286,1,27,292,1,
131,298,1,130,304,
1,22,532,15,0,
171,1,21,311,1,
20,312,1,19,318,
1,124,323,1,15,
533,15,0,171,1,
14,331,1,13,377,
1,2,534,15,0,
171,1,10,334,1,
5,340,1,8,535,
15,0,171,1,7,
347,1,113,536,15,
0,171,1,112,349,
1,4,537,15,0,
171,1,3,351,1,
109,352,1,1,358,
107,538,18,107,485,
308,539,18,308,421,
266,540,18,266,433,
131,541,18,131,197,
252,542,18,252,433,
102,543,18,102,417,
544,4,12,78,0,
97,0,109,0,101,
0,95,0,51,0,
545,18,544,433,546,
4,12,78,0,97,
0,109,0,101,0,
95,0,49,0,547,
18,546,433,548,4,
12,67,0,111,0,
110,0,115,0,95,
0,50,0,549,18,
548,504,550,4,12,
67,0,97,0,108,
0,108,0,95,0,
50,0,551,18,550,
209,552,4,12,78,
0,97,0,109,0,
101,0,95,0,52,
0,553,18,552,433,
218,554,18,218,485,
161,555,18,161,556,
5,41,1,98,557,
15,0,159,1,97,
214,1,95,558,15,
0,180,1,94,221,
1,83,559,15,0,
159,1,82,228,1,
81,373,1,56,230,
1,54,236,1,47,
242,1,45,248,1,
126,254,1,35,560,
15,0,159,1,34,
261,1,33,262,1,
32,268,1,31,274,
1,30,280,1,29,
286,1,27,292,1,
131,298,1,130,304,
1,22,561,15,0,
159,1,21,311,1,
20,312,1,19,318,
1,124,323,1,15,
562,15,0,159,1,
14,331,1,13,377,
1,2,563,15,0,
159,1,10,334,1,
5,340,1,8,564,
15,0,159,1,7,
347,1,113,565,15,
0,159,1,112,349,
1,4,566,15,0,
159,1,3,351,1,
109,352,1,1,358,
302,567,18,302,421,
225,568,18,225,389,
327,569,18,327,389,
570,4,26,67,0,
108,0,97,0,115,
0,115,0,66,0,
111,0,100,0,121,
0,95,0,50,0,
95,0,49,0,571,
18,570,417,338,572,
18,338,197,144,573,
18,144,504,246,574,
18,246,485,575,4,
14,73,0,116,0,
101,0,109,0,95,
0,49,0,50,0,
576,18,575,197,577,
4,12,73,0,116,
0,101,0,109,0,
95,0,51,0,578,
18,577,197,579,4,
12,73,0,116,0,
101,0,109,0,95,
0,52,0,580,18,
579,197,581,4,14,
83,0,116,0,117,
0,102,0,102,0,
95,0,52,0,582,
18,581,485,583,4,
14,83,0,116,0,
117,0,102,0,102,
0,95,0,51,0,
584,18,583,485,585,
4,14,83,0,116,
0,117,0,102,0,
102,0,95,0,50,
0,586,18,585,485,
587,4,14,73,0,
116,0,101,0,109,
0,95,0,49,0,
56,0,588,18,587,
197,589,4,12,73,
0,116,0,101,0,
109,0,95,0,57,
0,590,18,589,197,
356,591,18,356,389,
592,4,14,73,0,
116,0,101,0,109,
0,95,0,50,0,
50,0,593,18,592,
197,594,4,14,73,
0,116,0,101,0,
109,0,95,0,50,
0,48,0,595,18,
594,197,596,4,14,
73,0,116,0,101,
0,109,0,95,0,
50,0,49,0,597,
18,596,197,154,598,
18,154,599,5,40,
1,98,600,15,0,
152,1,97,214,1,
94,221,1,83,601,
15,0,152,1,82,
228,1,81,373,1,
56,230,1,54,236,
1,47,242,1,45,
248,1,126,254,1,
35,602,15,0,152,
1,34,261,1,33,
262,1,32,268,1,
31,274,1,30,280,
1,29,286,1,27,
292,1,131,298,1,
130,304,1,22,603,
15,0,152,1,21,
311,1,20,312,1,
19,318,1,124,323,
1,15,604,15,0,
152,1,14,331,1,
13,377,1,2,605,
15,0,152,1,10,
334,1,5,340,1,
8,606,15,0,152,
1,7,347,1,113,
607,15,0,152,1,
112,349,1,4,608,
15,0,152,1,3,
351,1,109,352,1,
1,358,191,609,18,
191,421,127,610,18,
127,209,611,4,22,
67,0,108,0,97,
0,115,0,115,0,
66,0,111,0,100,
0,121,0,95,0,
50,0,612,18,611,
417,613,4,16,71,
0,83,0,116,0,
117,0,102,0,102,
0,95,0,49,0,
614,18,613,421,148,
615,18,148,616,5,
2,1,20,617,15,
0,169,1,33,618,
15,0,146,619,4,
12,67,0,111,0,
110,0,115,0,95,
0,49,0,620,18,
619,504,621,4,14,
73,0,116,0,101,
0,109,0,95,0,
49,0,49,0,622,
18,621,197,623,4,
14,73,0,116,0,
101,0,109,0,95,
0,49,0,48,0,
624,18,623,197,625,
4,14,73,0,116,
0,101,0,109,0,
95,0,49,0,51,
0,626,18,625,197,
124,627,18,124,433,
628,4,14,73,0,
116,0,101,0,109,
0,95,0,49,0,
53,0,629,18,628,
197,630,4,14,73,
0,116,0,101,0,
109,0,95,0,49,
0,52,0,631,18,
630,197,632,4,14,
73,0,116,0,101,
0,109,0,95,0,
49,0,55,0,633,
18,632,197,316,634,
18,316,197,635,4,
14,73,0,116,0,
101,0,109,0,95,
0,49,0,57,0,
636,18,635,197,140,
637,18,140,638,5,
1,1,132,639,16,
640,14,641,4,28,
37,0,67,0,108,
0,97,0,115,0,
115,0,66,0,111,
0,100,0,121,0,
95,0,50,0,95,
0,49,0,1,5,
642,19,570,1,3,
1,4,1,3,643,
21,1,1,644,5,
0,2,1,0};
new Sfactory(this,"Call_2",new SCreator(Call_2_factory));
new Sfactory(this,"Call_1",new SCreator(Call_1_factory));
new Sfactory(this,"Item_1",new SCreator(Item_1_factory));
new Sfactory(this,"Item_2",new SCreator(Item_2_factory));
new Sfactory(this,"BaseCall_2",new SCreator(BaseCall_2_factory));
new Sfactory(this,"BaseCall_3",new SCreator(BaseCall_3_factory));
new Sfactory(this,"Item_6",new SCreator(Item_6_factory));
new Sfactory(this,"BaseCall_1",new SCreator(BaseCall_1_factory));
new Sfactory(this,"BaseCall_6",new SCreator(BaseCall_6_factory));
new Sfactory(this,"BaseCall_4",new SCreator(BaseCall_4_factory));
new Sfactory(this,"BaseCall_5",new SCreator(BaseCall_5_factory));
new Sfactory(this,"Item_12_1",new SCreator(Item_12_1_factory));
new Sfactory(this,"Item_18_1",new SCreator(Item_18_1_factory));
new Sfactory(this,"Call_2_1",new SCreator(Call_2_1_factory));
new Sfactory(this,"Item_4_1",new SCreator(Item_4_1_factory));
new Sfactory(this,"ClassBody_1",new SCreator(ClassBody_1_factory));
new Sfactory(this,"GStuff_3",new SCreator(GStuff_3_factory));
new Sfactory(this,"GStuff_4",new SCreator(GStuff_4_factory));
new Sfactory(this,"GStuff_5",new SCreator(GStuff_5_factory));
new Sfactory(this,"GStuff_6",new SCreator(GStuff_6_factory));
new Sfactory(this,"Item_22_1",new SCreator(Item_22_1_factory));
new Sfactory(this,"Item_14_1",new SCreator(Item_14_1_factory));
new Sfactory(this,"Item_16_1",new SCreator(Item_16_1_factory));
new Sfactory(this,"Name_2",new SCreator(Name_2_factory));
new Sfactory(this,"BaseCall",new SCreator(BaseCall_factory));
new Sfactory(this,"Name_1",new SCreator(Name_1_factory));
new Sfactory(this,"Item_8_1",new SCreator(Item_8_1_factory));
new Sfactory(this,"GStuff_2",new SCreator(GStuff_2_factory));
new Sfactory(this,"Stuff_2",new SCreator(Stuff_2_factory));
new Sfactory(this,"Stuff_1",new SCreator(Stuff_1_factory));
new Sfactory(this,"Cons_2_1",new SCreator(Cons_2_1_factory));
new Sfactory(this,"GStuff_2_1",new SCreator(GStuff_2_1_factory));
new Sfactory(this,"Item_5",new SCreator(Item_5_factory));
new Sfactory(this,"Item_6_1",new SCreator(Item_6_1_factory));
new Sfactory(this,"Stuff",new SCreator(Stuff_factory));
new Sfactory(this,"GStuff_6_1",new SCreator(GStuff_6_1_factory));
new Sfactory(this,"Name_2_1",new SCreator(Name_2_1_factory));
new Sfactory(this,"Item",new SCreator(Item_factory));
new Sfactory(this,"Name_4_1",new SCreator(Name_4_1_factory));
new Sfactory(this,"Item_18",new SCreator(Item_18_factory));
new Sfactory(this,"ClassBody",new SCreator(ClassBody_factory));
new Sfactory(this,"Name_3",new SCreator(Name_3_factory));
new Sfactory(this,"error",new SCreator(error_factory));
new Sfactory(this,"Cons_2",new SCreator(Cons_2_factory));
new Sfactory(this,"Item_2_1",new SCreator(Item_2_1_factory));
new Sfactory(this,"Name_4",new SCreator(Name_4_factory));
new Sfactory(this,"Stuff_2_1",new SCreator(Stuff_2_1_factory));
new Sfactory(this,"GStuff_4_1",new SCreator(GStuff_4_1_factory));
new Sfactory(this,"BaseCall_2_1",new SCreator(BaseCall_2_1_factory));
new Sfactory(this,"BaseCall_4_1",new SCreator(BaseCall_4_1_factory));
new Sfactory(this,"ClassBody_2_1",new SCreator(ClassBody_2_1_factory));
new Sfactory(this,"Item_20_1",new SCreator(Item_20_1_factory));
new Sfactory(this,"Cons",new SCreator(Cons_factory));
new Sfactory(this,"Stuff_4_1",new SCreator(Stuff_4_1_factory));
new Sfactory(this,"Item_13",new SCreator(Item_13_factory));
new Sfactory(this,"Item_12",new SCreator(Item_12_factory));
new Sfactory(this,"Item_3",new SCreator(Item_3_factory));
new Sfactory(this,"Item_4",new SCreator(Item_4_factory));
new Sfactory(this,"Stuff_4",new SCreator(Stuff_4_factory));
new Sfactory(this,"Stuff_3",new SCreator(Stuff_3_factory));
new Sfactory(this,"Item_7",new SCreator(Item_7_factory));
new Sfactory(this,"Item_8",new SCreator(Item_8_factory));
new Sfactory(this,"Item_9",new SCreator(Item_9_factory));
new Sfactory(this,"BaseCall_6_1",new SCreator(BaseCall_6_1_factory));
new Sfactory(this,"Item_22",new SCreator(Item_22_factory));
new Sfactory(this,"Item_20",new SCreator(Item_20_factory));
new Sfactory(this,"Item_21",new SCreator(Item_21_factory));
new Sfactory(this,"GStuff",new SCreator(GStuff_factory));
new Sfactory(this,"Call",new SCreator(Call_factory));
new Sfactory(this,"ClassBody_2",new SCreator(ClassBody_2_factory));
new Sfactory(this,"GStuff_1",new SCreator(GStuff_1_factory));
new Sfactory(this,"Cons_1",new SCreator(Cons_1_factory));
new Sfactory(this,"Item_11",new SCreator(Item_11_factory));
new Sfactory(this,"Item_10",new SCreator(Item_10_factory));
new Sfactory(this,"Item_10_1",new SCreator(Item_10_1_factory));
new Sfactory(this,"Name",new SCreator(Name_factory));
new Sfactory(this,"Item_15",new SCreator(Item_15_factory));
new Sfactory(this,"Item_14",new SCreator(Item_14_factory));
new Sfactory(this,"Item_17",new SCreator(Item_17_factory));
new Sfactory(this,"Item_16",new SCreator(Item_16_factory));
new Sfactory(this,"Item_19",new SCreator(Item_19_factory));
}
public static object Call_2_factory(Parser yyp) { return new Call_2(yyp); }
public static object Call_1_factory(Parser yyp) { return new Call_1(yyp); }
public static object Item_1_factory(Parser yyp) { return new Item_1(yyp); }
public static object Item_2_factory(Parser yyp) { return new Item_2(yyp); }
public static object BaseCall_2_factory(Parser yyp) { return new BaseCall_2(yyp); }
public static object BaseCall_3_factory(Parser yyp) { return new BaseCall_3(yyp); }
public static object Item_6_factory(Parser yyp) { return new Item_6(yyp); }
public static object BaseCall_1_factory(Parser yyp) { return new BaseCall_1(yyp); }
public static object BaseCall_6_factory(Parser yyp) { return new BaseCall_6(yyp); }
public static object BaseCall_4_factory(Parser yyp) { return new BaseCall_4(yyp); }
public static object BaseCall_5_factory(Parser yyp) { return new BaseCall_5(yyp); }
public static object Item_12_1_factory(Parser yyp) { return new Item_12_1(yyp); }
public static object Item_18_1_factory(Parser yyp) { return new Item_18_1(yyp); }
public static object Call_2_1_factory(Parser yyp) { return new Call_2_1(yyp); }
public static object Item_4_1_factory(Parser yyp) { return new Item_4_1(yyp); }
public static object ClassBody_1_factory(Parser yyp) { return new ClassBody_1(yyp); }
public static object GStuff_3_factory(Parser yyp) { return new GStuff_3(yyp); }
public static object GStuff_4_factory(Parser yyp) { return new GStuff_4(yyp); }
public static object GStuff_5_factory(Parser yyp) { return new GStuff_5(yyp); }
public static object GStuff_6_factory(Parser yyp) { return new GStuff_6(yyp); }
public static object Item_22_1_factory(Parser yyp) { return new Item_22_1(yyp); }
public static object Item_14_1_factory(Parser yyp) { return new Item_14_1(yyp); }
public static object Item_16_1_factory(Parser yyp) { return new Item_16_1(yyp); }
public static object Name_2_factory(Parser yyp) { return new Name_2(yyp); }
public static object BaseCall_factory(Parser yyp) { return new BaseCall(yyp); }
public static object Name_1_factory(Parser yyp) { return new Name_1(yyp); }
public static object Item_8_1_factory(Parser yyp) { return new Item_8_1(yyp); }
public static object GStuff_2_factory(Parser yyp) { return new GStuff_2(yyp); }
public static object Stuff_2_factory(Parser yyp) { return new Stuff_2(yyp); }
public static object Stuff_1_factory(Parser yyp) { return new Stuff_1(yyp); }
public static object Cons_2_1_factory(Parser yyp) { return new Cons_2_1(yyp); }
public static object GStuff_2_1_factory(Parser yyp) { return new GStuff_2_1(yyp); }
public static object Item_5_factory(Parser yyp) { return new Item_5(yyp); }
public static object Item_6_1_factory(Parser yyp) { return new Item_6_1(yyp); }
public static object Stuff_factory(Parser yyp) { return new Stuff(yyp); }
public static object GStuff_6_1_factory(Parser yyp) { return new GStuff_6_1(yyp); }
public static object Name_2_1_factory(Parser yyp) { return new Name_2_1(yyp); }
public static object Item_factory(Parser yyp) { return new Item(yyp); }
public static object Name_4_1_factory(Parser yyp) { return new Name_4_1(yyp); }
public static object Item_18_factory(Parser yyp) { return new Item_18(yyp); }
public static object ClassBody_factory(Parser yyp) { return new ClassBody(yyp); }
public static object Name_3_factory(Parser yyp) { return new Name_3(yyp); }
public static object error_factory(Parser yyp) { return new error(yyp); }
public static object Cons_2_factory(Parser yyp) { return new Cons_2(yyp); }
public static object Item_2_1_factory(Parser yyp) { return new Item_2_1(yyp); }
public static object Name_4_factory(Parser yyp) { return new Name_4(yyp); }
public static object Stuff_2_1_factory(Parser yyp) { return new Stuff_2_1(yyp); }
public static object GStuff_4_1_factory(Parser yyp) { return new GStuff_4_1(yyp); }
public static object BaseCall_2_1_factory(Parser yyp) { return new BaseCall_2_1(yyp); }
public static object BaseCall_4_1_factory(Parser yyp) { return new BaseCall_4_1(yyp); }
public static object ClassBody_2_1_factory(Parser yyp) { return new ClassBody_2_1(yyp); }
public static object Item_20_1_factory(Parser yyp) { return new Item_20_1(yyp); }
public static object Cons_factory(Parser yyp) { return new Cons(yyp); }
public static object Stuff_4_1_factory(Parser yyp) { return new Stuff_4_1(yyp); }
public static object Item_13_factory(Parser yyp) { return new Item_13(yyp); }
public static object Item_12_factory(Parser yyp) { return new Item_12(yyp); }
public static object Item_3_factory(Parser yyp) { return new Item_3(yyp); }
public static object Item_4_factory(Parser yyp) { return new Item_4(yyp); }
public static object Stuff_4_factory(Parser yyp) { return new Stuff_4(yyp); }
public static object Stuff_3_factory(Parser yyp) { return new Stuff_3(yyp); }
public static object Item_7_factory(Parser yyp) { return new Item_7(yyp); }
public static object Item_8_factory(Parser yyp) { return new Item_8(yyp); }
public static object Item_9_factory(Parser yyp) { return new Item_9(yyp); }
public static object BaseCall_6_1_factory(Parser yyp) { return new BaseCall_6_1(yyp); }
public static object Item_22_factory(Parser yyp) { return new Item_22(yyp); }
public static object Item_20_factory(Parser yyp) { return new Item_20(yyp); }
public static object Item_21_factory(Parser yyp) { return new Item_21(yyp); }
public static object GStuff_factory(Parser yyp) { return new GStuff(yyp); }
public static object Call_factory(Parser yyp) { return new Call(yyp); }
public static object ClassBody_2_factory(Parser yyp) { return new ClassBody_2(yyp); }
public static object GStuff_1_factory(Parser yyp) { return new GStuff_1(yyp); }
public static object Cons_1_factory(Parser yyp) { return new Cons_1(yyp); }
public static object Item_11_factory(Parser yyp) { return new Item_11(yyp); }
public static object Item_10_factory(Parser yyp) { return new Item_10(yyp); }
public static object Item_10_1_factory(Parser yyp) { return new Item_10_1(yyp); }
public static object Name_factory(Parser yyp) { return new Name(yyp); }
public static object Item_15_factory(Parser yyp) { return new Item_15(yyp); }
public static object Item_14_factory(Parser yyp) { return new Item_14(yyp); }
public static object Item_17_factory(Parser yyp) { return new Item_17(yyp); }
public static object Item_16_factory(Parser yyp) { return new Item_16(yyp); }
public static object Item_19_factory(Parser yyp) { return new Item_19(yyp); }
}
public class cs0syntax : Parser {
public cs0syntax ():base(new yycs0syntax (),new cs0tokens()) {}
public cs0syntax (Symbols syms):base(syms,new cs0tokens()) {}
public cs0syntax (Symbols syms,ErrorHandler erh):base(syms,new cs0tokens(erh)) {}

	public string Out;
	public string Cls;
	public string Par;
	public string Ctx;
	public bool defconseen = false;

 }
}
