#include <stdio.h>
#include <string.h>
/*#include <stdlib.h>*/
/* If stdlib.h is included, it gives warning in bsearch. Hence commented AMBA
23/03/00 */

#define MAXSZ 1000
#define MAXBUFF 40

extern int bsearch();

void wrd_glyphs2iscii(char *table,int *sizeof_table,char *input_str){
size_t i;
int found;
char test_wrd[MAXSZ],curr_wrd[MAXSZ];
char *tag;
  strcpy(curr_wrd,input_str);
  while(curr_wrd[0] != '\0'){
	found = 0;
	for(i = strlen(curr_wrd); i>0 && !found; i--) {
	  strncpy(test_wrd,curr_wrd,i);
	  test_wrd[i] = '\0';
	  tag = (char *)bsearch(test_wrd,table,*sizeof_table,2*MAXBUFF,strcmp);
	  if(tag != NULL) {
	printf("%s",tag+MAXBUFF);
	strcpy(curr_wrd,curr_wrd+i);
	found = 1;
	  }
	}
	if(!found){printf("%c",curr_wrd[0]); strcpy(curr_wrd,curr_wrd+1);}
 }
}
