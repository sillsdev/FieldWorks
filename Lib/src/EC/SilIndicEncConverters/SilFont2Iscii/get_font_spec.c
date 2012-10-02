/* Last Modified 24th MAY 2000 */
#include <stdio.h>
#include <string.h>
#define MAXBUFF 40
#define MAXSZ 80
#ifndef _MAX_PATH
#define _MAX_PATH   260
#endif

extern void *malloc();
extern char *getenv();
void get_punc_lst();
void get_dlmt();

void fgetline(s_ptr,fp)
char *s_ptr;
FILE *fp;
{
/*   Collects the string from the FILE fp, into the string s_ptr */
	char c;
	while((c = getc(fp)) != '\n' && !feof(fp)) *s_ptr++ = c;
	*s_ptr = '\0';
}

int load_map_table(const char *filepath, const char* filename, char *delimit,char *table) {
  FILE *fp;
  size_t i=0,len;
  char inpt[2*MAXBUFF];
  char tmp[2*MAXBUFF];

  char fspec[_MAX_PATH];
  strcpy(fspec,filepath);
  strcat(fspec,filename);
  if ((fp = fopen(fspec,"r")) == NULL) {
	perror(fspec);
	return -1;
  }

/*  table = (char *)malloc(300000*sizeof(char));*/
  tmp[0] = '\0';
  while(!feof(fp))
  {
	fgetline(inpt,fp);
	if(inpt[0] != '\0'){
	strcpy(tmp,strtok(inpt,delimit));
	len = strlen(tmp);
	table[2*MAXBUFF*i] = '\0';
	strncat(table+2*MAXBUFF*i,tmp,len);
	table[2*MAXBUFF*i+len] = '\0';
	strcpy(tmp,strtok(NULL,delimit));
	len = strlen(tmp);
	table[MAXBUFF+2*MAXBUFF*i] = '\0';
	strncat(table+MAXBUFF+2*MAXBUFF*i,tmp,len);
	table[MAXBUFF+2*MAXBUFF*i+len] = '\0';
	i++;
   }
  }
  return (int)i;
}

int get_fnt_spec(
				 const char *filepath,
				 const char *filename,
				 char *roman_fnt_nm,
				 char *indian_fnt_nm,
				 char *table,char *punc_lst)
{
FILE *fp;
char map_fl_nm[MAXSZ],tmp[MAXSZ],tmp1[MAXSZ],dlmt[10];
int sizeof_table;
char fspec[_MAX_PATH];
strcpy(fspec,filepath);
strcat(fspec,filename);
if ((fp = fopen(fspec,"r")) == NULL) {
	  perror(fspec);
	  return -1;
}
fgetline(indian_fnt_nm,fp);
fgetline(roman_fnt_nm,fp);
fgetline(map_fl_nm,fp);
fgetline(tmp1,fp);
fgetline(tmp,fp);
fclose(fp);

/*strcpy(home_dir,getenv("HOME"));
strcat(home_dir,map_fl_nm);

sizeof_table = load_map_table(home_dir,":",table);*/
get_dlmt(tmp1,dlmt);
sizeof_table = load_map_table(filepath,map_fl_nm,dlmt,table);
get_punc_lst(punc_lst,tmp);
return sizeof_table;
}

void get_punc_lst(char *punc_lst,char *tmp){
char c;
int i,k;
c = tmp[0];
i=0;
while((c != '=') && ( c != '\0')){
i++;
c=tmp[i];
}
i++;
if(c == '\0') { punc_lst[0] = -1;}
else{
k=0;
c=tmp[i];
while(c != '\0'){
if(c != ' ') { punc_lst[k] = c; k++;}
i++;
c=tmp[i];
}
punc_lst[k] = -1;
}
}
void get_dlmt(char *tmp,char *dlmt){
char c;
int i,k;
c = tmp[0];
i=0;
while((c != '"') && ( c != '\0')){
i++;
c=tmp[i];
}
i++;
if(c == '\0') { strcpy(dlmt,":");}/* Default delimiter*/
else{
k=0;
c=tmp[i];
while((c != '\0') && (c != '"')){
dlmt[k] = c;
k++;
i++;
c=tmp[i];
}
dlmt[k] = '\0';
}
}
