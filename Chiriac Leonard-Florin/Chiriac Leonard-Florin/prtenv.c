#include<stdlib.h>
#include<stdio.h>

void main(){
 char* shell = getenv("NEW001");
 if (shell)
 printf("%x\n", (unsigned int)shell);
 }
