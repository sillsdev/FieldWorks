/* LAST Modified 18th APR 2000 */
#include <stdio.h>
#include <string.h>
/*#include <stdlib.h>*/
/* If stdlib.h is included, it gives warning in bsearch. Hence commented AMBA
23/03/00 */

#define CHANDRA 1
#define VOWEL 2
#define CONS 3
#define MAWRA 4
#define HALAMWA 5
#define NUKWA 6
#define MY_NULL 999
#define DEFAULT 0
#define MAXSZ 1000
#define MAXBUFF 40

void iscii2font();
void map_syllable();
void error_default();
void handle_ascii();
int exceptional();
int exceptional_mAwrA();
int base_mark();
int submit_button();
int form();

int from_punc_lst(char *punc_lst,char c);
void call_err();
int category();
extern int bsearch();

void string2glyphs(
				   char *roman_fnt_nm,
				   char *indian_fnt_nm,
				   char *table,
				   int *sizeof_table,
				   const char *input_str,
				   char *output_str,
				   char *punc_lst,
				   int *html_flag_ptr,
				   int *head_flag_ptr,
				   int *body_flag_ptr,
				   int *font_flag_ptr,
				   int *form_flag_ptr)
{
int i,j=0,cnt;
char c,buffer[MAXSZ],outbuf[MAXSZ]; // ,outbuf_fnt[2*MAXSZ];

output_str[0] = '\0';
c = input_str[j++];
i=c;
while(c != '\0'){
	if(i<0){
		cnt = 0;
		while((i<0) && (c != '\0')){
		 buffer[cnt++] = c;
		 if(cnt > MAXSZ-1) { call_err();}
		 c = input_str[j++];
		 i=c;
			}
		buffer[cnt] = '\0';
			iscii2font(buffer,outbuf,table,sizeof_table);
/* rde don't use this stuff.
		if(((*font_flag_ptr == 0) && strcmp(indian_fnt_nm,roman_fnt_nm)) || ((!*body_flag_ptr) && (!*head_flag_ptr))) {
				 *font_flag_ptr = 1;
			 if(*body_flag_ptr == 0) *body_flag_ptr = 1;
			 if(*head_flag_ptr == 0) *head_flag_ptr = 1;
				 sprintf(outbuf_fnt,"<FONT FACE=\"%s\">%s",indian_fnt_nm,outbuf);
				 strcat(output_str,outbuf_fnt);
			 }
			 else
*/
			strcat(output_str,outbuf);
	}
	else if((i>=0) && (i<=32)){
		sprintf(outbuf,"%c",c);
		strcat(output_str,outbuf);
		c = input_str[j++];
		i=c;
		 }
		 else if(from_punc_lst(punc_lst,c)){
			 sprintf(outbuf,"%c",c);
			 strcat(output_str,outbuf);
			 c = input_str[j++];
			 i=c;
			  }
			  else {
			  cnt = 0;
			  while ((i>0) && (c != '\0') && (c != '\n')){
			  buffer[cnt++] = c;
			  if(cnt > MAXSZ-1) { call_err();}
			  c = input_str[j++];
			  i=c;
				 }
			 if(c == '\n') { buffer[cnt++] = c; c = input_str[j++]; i=c;}
			 if(cnt > MAXSZ-1) { call_err();}
			 buffer[cnt] = '\0';
			 if((*font_flag_ptr) && strcmp(indian_fnt_nm,roman_fnt_nm)){
				*font_flag_ptr = 0;
				strcat(output_str,"</FONT>");
			 }
				 handle_ascii(roman_fnt_nm,indian_fnt_nm,buffer,output_str,html_flag_ptr,head_flag_ptr,body_flag_ptr,form_flag_ptr);
			  }
	}
}

void call_err(){
printf("Buffer size exceeded 1000\n Exiting ...\n");
/*exit(1);*/
}

#pragma warning( disable:4312 )

void error_default(char *wrd, char *out,char *table,int *sizeof_table) {
		unsigned int i,found_partial_syll,left_boundary;
		char test_wrd[10],iwrd[4],left_map[MAXSZ],right_map[MAXSZ];
	char *tag;

	out[0] = '\0';
	left_map[0] = '\0';
	right_map[0] = '\0';
	found_partial_syll = 0;
		for(i = 0; i < strlen(wrd) && !found_partial_syll; i++) {
		strcpy(test_wrd,wrd+i);
		 /*   tag = (struct tabl *)bsearch(test_wrd,table,*sizeof_table,2*MAXBUFF, strcmp);*/
			tag = (char *)bsearch(test_wrd,table,*sizeof_table,2*MAXBUFF,strcmp);
			if(tag != NULL) found_partial_syll = 1;
		}
	if(found_partial_syll) {
		left_boundary = i-1;
		strcat(right_map,tag+MAXBUFF);
		i=0;
			while(i < left_boundary) {
		iwrd[0] = wrd[i];
		if(category(wrd[i+1]) == HALAMWA) {
		   /*iwrd[1] = wrd[i+1]; iwrd[2] = '1'; iwrd[3] = '\0'; */
		   iwrd[1] = wrd[i+1]; iwrd[2] = '\0';
			  /* tag = (struct tabl *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);*/
			   tag = (char *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);
			   if(tag != NULL) {strcat(left_map,tag+MAXBUFF); i=i+2;}
			   else {
					  iwrd[1] = '\0'; i++;
			  /*    tag = (struct tabl *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);*/
				  tag = (char *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);
				  if(tag != NULL) strcat(left_map,tag+MAXBUFF);
				   }
				}
			else {
				   iwrd[1] = '\0'; i++;
			   /*tag = (struct tabl *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);*/
			   tag = (char *)bsearch(iwrd,table,*sizeof_table,2*MAXBUFF, strcmp);
			   if(tag != NULL) strcat(left_map,tag+MAXBUFF);
				}
			}
	}
	strcpy(out,left_map);
	strcat(out,right_map);
}

int category(int c)
{
if(c>0) return DEFAULT;
c = c+256;	/* 256 is added, else c is -ve for ISCII 8 bit */
if((c > 160) && (c < 164)) return CHANDRA;
if((c > 163) && (c < 179)) return VOWEL;
if((c > 178) && (c < 217)) return CONS;
if((c > 217) && (c < 232)) return MAWRA;
if(c == 232) return HALAMWA;
if(c == 233) return NUKWA;
if(c == 256) return MY_NULL;
else return 0;
}


void map_syllable(char *input,int count,char *output,char *table,int *sizeof_table){
	char string1[MAXSZ];
	char *tag;
	strncpy(string1,input,count);
	string1[count] = '\0';
	tag = (char *)bsearch(string1,table,*sizeof_table,2*MAXBUFF,strcmp);
		/*printf("tag = %s;table = %c",tag,table[0]);*/
	if(tag != NULL) strcpy(output,tag+MAXBUFF);
	else error_default(string1,output,table,sizeof_table);
}

void iscii2font(char *str,char *outstr,char *table,int *sizeof_table)
{
int syll_count;
int cons_flag,mAwrA_flag,halaMwa_flag;
char map_out[MAXSZ];

cons_flag = 0;
halaMwa_flag = 0;
mAwrA_flag = 0;
outstr[0] = '\0';

while(str[0] != '\0'){
syll_count = 1;
	if(exceptional(str[0])){
		if(category(str[syll_count]) == NUKWA) syll_count++;
			else if(category(str[1]) == CHANDRA)  syll_count++;
		map_syllable(str,syll_count,map_out,table,sizeof_table);
			strcat(outstr,map_out);
		str = str+syll_count;
	}
	else{
	switch(category(str[0])){
	case MAWRA:
		   if(category(str[1]) == CHANDRA)  syll_count = 2;
		/*printf("ERROR"); */
		map_syllable(str,syll_count,map_out,table,sizeof_table);
		strcat(outstr,map_out);
		str = str+syll_count;
		break;
	case CHANDRA :
		/*printf("ERROR"); */
		map_syllable(str,syll_count,map_out,table,sizeof_table);
		strcat(outstr,map_out);
		str = str+syll_count;
		break;
	case VOWEL :
			if(category(str[1]) == CHANDRA)  syll_count = 2;
		map_syllable(str,syll_count,map_out,table,sizeof_table);
		strcat(outstr,map_out);
		str = str+syll_count;
		break;
	case CONS :cons_flag = 1;
		   while(cons_flag){
			switch(category(str[syll_count])){
			case CHANDRA : syll_count++;
					  map_syllable(str,syll_count,map_out,table,sizeof_table);
					  strcat(outstr,map_out);
					  str = str+syll_count;
					  cons_flag = 0;
					  break;
			case MAWRA : syll_count++;
					 if(category(str[syll_count]) == CHANDRA) syll_count++;
					 else if((category(str[syll_count]) == NUKWA) && exceptional_mAwrA(str[0])) syll_count++;
						 map_syllable(str,syll_count,map_out,table,sizeof_table);
					 strcat(outstr,map_out);
						 str = str+syll_count;
					 cons_flag = 0;
					  break;
			case NUKWA : syll_count++;
					  break;
			case VOWEL : map_syllable(str,syll_count,map_out,table,sizeof_table);
					 strcat(outstr,map_out);
					  str = str+syll_count;
					  cons_flag = 0;
					  break;
			case CONS :  if(halaMwa_flag) {
					halaMwa_flag = 0;
					syll_count++;
					 }
					 else{
					  map_syllable(str,syll_count,map_out,table,sizeof_table);
					  strcat(outstr,map_out);
					  str = str+syll_count;
					  cons_flag = 0;
					  }
					  break;
			case HALAMWA : syll_count++;
					halaMwa_flag = 1;
					  break;
			case DEFAULT :
			case MY_NULL : map_syllable(str,syll_count,map_out,table,sizeof_table);
					  strcat(outstr,map_out);
					  str = str+syll_count;
					  cons_flag = 0;
					  halaMwa_flag = 0;
					  mAwrA_flag = 0;
					  break;
			}
		  }
		  break;
	case DEFAULT :
	case HALAMWA :
	case NUKWA :
		/*printf("ERROR"); */
		map_syllable(str,syll_count,map_out,table,sizeof_table);
			strcat(outstr,map_out);
		str = str+syll_count;
		break;
  }
 }
 }
}

int from_punc_lst(char punc_lst[], char c){
int i=0;
while(punc_lst[i] != -1){
if(c == punc_lst[i]) return 1;
else i++;
}
return 0;
}

int html_start_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
((in_str[i+1] == 'h')||(in_str[i+1] == 'H')) &&
((in_str[i+2] == 't')||(in_str[i+2] == 'T')) &&
((in_str[i+3] == 'm')||(in_str[i+3] == 'M')) &&
((in_str[i+4] == 'l')||(in_str[i+4] == 'L')) &&
(in_str[i+5] == '>'))
return 1;
else if ((in_str[i] == '<') &&
((in_str[i+1] == 'i')||(in_str[i+1] == 'I')) &&
((in_str[i+2] == 'h')||(in_str[i+2] == 'H')) &&
((in_str[i+3] == 't')||(in_str[i+3] == 'T')) &&
((in_str[i+4] == 'm')||(in_str[i+4] == 'M')) &&
((in_str[i+5] == 'l')||(in_str[i+5] == 'L')) &&
(in_str[i+6] == '>')) {
in_str[i] = ' '; /* Destructive:  Not a good solution*/
in_str[i+1] = '<'; /* Destructive:  Not a good solution*/
return 2;
}
i++;
}
return 0;
}

int head_start_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
((in_str[i+1] == 'h')||(in_str[i+1] == 'H')) &&
((in_str[i+2] == 'e')||(in_str[i+2] == 'E')) &&
((in_str[i+3] == 'a')||(in_str[i+3] == 'A')) &&
((in_str[i+4] == 'd')||(in_str[i+4] == 'D')) &&
(in_str[i+5] == '>'))
return 1;
i++;
}
return 0;
}

int body_start_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
((in_str[i+1] == 'b')||(in_str[i+1] == 'B')) &&
((in_str[i+2] == 'o')||(in_str[i+2] == 'O')) &&
((in_str[i+3] == 'd')||(in_str[i+3] == 'D')) &&
((in_str[i+4] == 'y')||(in_str[i+4] == 'Y')) &&
(in_str[i+5] == '>'))
return 1;
i++;
}
return 0;
}

int body_end_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
(in_str[i+1] == '/') &&
((in_str[i+2] == 'b')||(in_str[i+2] == 'B')) &&
((in_str[i+3] == 'o')||(in_str[i+3] == 'O')) &&
((in_str[i+4] == 'd')||(in_str[i+4] == 'D')) &&
((in_str[i+5] == 'y')||(in_str[i+5] == 'Y')) &&
(in_str[i+6] == '>'))
return 1;
i++;
}
return 0;
}

int head_end_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
(in_str[i+1] == '/') &&
((in_str[i+2] == 'h')||(in_str[i+2] == 'H')) &&
((in_str[i+3] == 'e')||(in_str[i+3] == 'E')) &&
((in_str[i+4] == 'a')||(in_str[i+4] == 'A')) &&
((in_str[i+5] == 'd')||(in_str[i+5] == 'D')) &&
(in_str[i+6] == '>'))
return 1;
i++;
}
return 0;
}

int html_end_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
(in_str[i+1] == '/') &&
((in_str[i+2] == 'h')||(in_str[i+2] == 'H')) &&
((in_str[i+3] == 't')||(in_str[i+3] == 'T')) &&
((in_str[i+4] == 'm')||(in_str[i+4] == 'M')) &&
((in_str[i+5] == 'l')||(in_str[i+5] == 'L')) &&
(in_str[i+6] == '>'))
return 1;
else if ((in_str[i] == '<') &&
(in_str[i+1] == '/') &&
((in_str[i+2] == 'i')||(in_str[i+2] == 'I')) &&
((in_str[i+3] == 'h')||(in_str[i+3] == 'H')) &&
((in_str[i+4] == 't')||(in_str[i+4] == 'T')) &&
((in_str[i+5] == 'm')||(in_str[i+5] == 'M')) &&
((in_str[i+6] == 'l')||(in_str[i+6] == 'L')) &&
(in_str[i+7] == '>')) {
in_str[i] = ' '; /* Destructive:  Not a good solution*/
in_str[i+1] = '<'; /* Destructive:  Not a good solution*/
in_str[i+2] = '/'; /* Destructive:  Not a good solution*/
return 2;
}
i++;
}
return 0;
}

int iscii_range(char *roman_fnt_nm,char *indian_fnt_nm,char * punc_lst,int i){
	if (!strcmp(roman_fnt_nm,indian_fnt_nm) && (i<0)) return 1;/*names same*/
	else if (strcmp(roman_fnt_nm,indian_fnt_nm) && ((i<33) || from_punc_lst(punc_lst,(char)i))) return 1;
	else return 0;
}

int ascii_range(int i){
	if (i >= 0) return 1;
	else return 0;
}

void handle_ascii(char *roman_fnt_nm,char *indian_fnt_nm,char *input,char *output,int *html_flag_ptr,int *head_flag_ptr,int *body_flag_ptr,int *form_flag_ptr)
{
  char outbuf_fnt[2*MAXSZ];
   if(base_mark(input)){
		  strcat(output,input); /* ibase -> base */
   }
   else if ((!*html_flag_ptr) && (html_start_mark(input))) {
	  *html_flag_ptr = 1;
		  strcat(output,input); /* ihtml -> html */
	}
   else if((!*head_flag_ptr) && (head_start_mark(input))) {
		  *head_flag_ptr = 1;
		  strcat(output,input);
	  if(!strcmp(indian_fnt_nm,roman_fnt_nm)){/* same name*/
			 sprintf(outbuf_fnt,"\n<FONT FACE=\"%s\">\n",indian_fnt_nm);
			 strcat(output,outbuf_fnt);
	  }
   }
   else if(head_end_mark(input)){
	  if(!strcmp(indian_fnt_nm,roman_fnt_nm))/* same name*/
		 strcat(output,"\n</FONT>\n");
		 strcat(output,input);
	  }
   else if((!*body_flag_ptr) && body_start_mark(input)){
	   *body_flag_ptr = 1;
	   if(*head_flag_ptr) { *head_flag_ptr = 1;}
		   strcat(output,input);
			 if(!strcmp(indian_fnt_nm,roman_fnt_nm)){/* same name*/
			  sprintf(outbuf_fnt,"\n<FONT FACE = \"%s\">\n",indian_fnt_nm);
			  strcat(output,outbuf_fnt);}
			  else strcat(output,outbuf_fnt);
   }
   else if(body_end_mark(input)){
		  if(!strcmp(indian_fnt_nm,roman_fnt_nm))/* same name*/
		  strcat(output,"\n</FONT>\n");
		  strcat(output,input);
   }
   else if(html_end_mark(input)){
		  if(!strcmp(indian_fnt_nm,roman_fnt_nm))/* same name*/
		  strcat(output,"\n</FONT>\n");
		  strcat(output,input); /* ihtml -> html */
   }
   else if((*form_flag_ptr == 1) && submit_button(input)){
		strcat(output,input);
	strcat(output,"<INPUT  TYPE=  \"HIDDEN\"  NAME =\"font\"  SIZE =20 VALUE =\"");
		strcat(output, indian_fnt_nm);
		strcat(output,"\" MAXLENGTH=\"25\"><BR>");
   }
   else if (form(input)) {
		  *form_flag_ptr = 1;
		  strcat(output,input);
   }
   else strcat(output,input);
}

int base_mark(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
((in_str[i+1] == 'b')||(in_str[i+1] == 'B')) &&
((in_str[i+2] == 'a')||(in_str[i+2] == 'A')) &&
((in_str[i+3] == 's')||(in_str[i+3] == 'S')) &&
((in_str[i+4] == 'e')||(in_str[i+4] == 'E')))
return 1;
else if ((in_str[i] == '<') &&
((in_str[i+1] == 'i')||(in_str[i+1] == 'I')) &&
((in_str[i+2] == 'b')||(in_str[i+2] == 'B')) &&
((in_str[i+3] == 'a')||(in_str[i+3] == 'A')) &&
((in_str[i+4] == 's')||(in_str[i+4] == 'S')) &&
((in_str[i+5] == 'e')||(in_str[i+5] == 'E'))) {
in_str[i] = ' '; /* Destructive:  Not a good solution*/
in_str[i+1] = '<'; /* Destructive:  Not a good solution*/
return 2;
}
i++;
}
return 0;
}

int submit_button(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if (((in_str[i] == 's')||(in_str[i] == 'S')) &&
((in_str[i+1] == 'u')||(in_str[i+1] == 'U')) &&
((in_str[i+2] == 'b')||(in_str[i+2] == 'B')) &&
((in_str[i+3] == 'm')||(in_str[i+3] == 'M')) &&
((in_str[i+4] == 'i')||(in_str[i+4] == 'I')) &&
((in_str[i+5] == 't')||(in_str[i+5] == 'T')))
return 1;
i++;
}
return 0;
}

int form(char *in_str){
int i = 0;
while(in_str[i] != '\0'){
if ((in_str[i] == '<') &&
((in_str[i+1] == 'f')||(in_str[i+1] == 'F')) &&
((in_str[i+2] == 'o')||(in_str[i+2] == 'O')) &&
((in_str[i+3] == 'r')||(in_str[i+3] == 'R')) &&
((in_str[i+4] == 'm')||(in_str[i+4] == 'M')))
return 1;
i++;
}
return 0;
}

int exceptional(int c)
{
c = c+256;	/* 256 is added, else c is -ve for ISCII 8 bit */
if((c==166) || (c==167) ||(c==170) ||(c==161)||(c==234)) return 1;
else return 0;
}
int exceptional_mAwrA(int c)
{
c = c+256;	/* 256 is added, else c is -ve for ISCII 8 bit */
if((c==219) || (c==220) ||(c==239)) return 1;
else return 0;
}
