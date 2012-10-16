// *************************************
//Website:  http://wch.cn
//Email:    tech@wch.cn
//Author:   W.ch 2005.4
// *************************************

#include <reg52.h>
#include <intrins.h>

#include "CH452CMD.H"	// ���峣�������뼰�ⲿ�ӳ���

sbit	LED=P1^0;

void delayms(unsigned char i)
{	unsigned int	j;
	do{	for(j=0;j!=1000;j++)
		{;}
	}while(--i);
}

void init()
{	SCON=0x50;
	PCON=0x80;
	TMOD=0x21;
	TH1=0xF3;//        ;24MHz����, 9600bps,ͨ�����ڷ����������շ��صİ���ֵ��
	TR1=1;
	RI=0;
	TI=0;
// ����CH452�����ж�
//	CH452_DOUT=1;	// ���ø�����Ϊ���뷽��
	IE1=0;
	EX1=1;
	EA=1;
}

// INT1�жϷ������
void int1() interrupt 2 //using 1
{
	TI=0;
	SBUF=CH452_Read();  //������ֵͨ�����ڷ���PC�����
	while(!TI);
	TI=0;
	LED=!LED;
}

main()
{	unsigned char cmd,dat;
	unsigned short	command;
	delayms(50);	
	init();
	SBUF=CH452_Read();		//��ȡCH452�İ汾�ţ���ʽӦ��ʱ����Ҫ��
	while(!TI);
	TI=0;
	CH452_Write(CH452_SYSON2);	//�����Ʒ�ʽ�����SDA���������ж��������ô����Ӧ��ΪCH452_SYSON2W(0x04,0x23)
	CH452_Write(CH452_BCD);   // BCD����,8�������
	CH452_Write(CH452_DIG7 | 1);
	CH452_Write(CH452_DIG6 | 2);
	CH452_Write(CH452_DIG5 | 3);
	CH452_Write(CH452_DIG4 | 4);
	CH452_Write(CH452_DIG3 | 5);
	CH452_Write(CH452_DIG2 | 6);
	CH452_Write(CH452_DIG1 | 7);
	CH452_Write(CH452_DIG0 | 8);  // ��ʾ�ַ�8
	delayms(50);
	LED=!LED;
	while ( 1 ){  // PC������ͨѶ��Ƭ�����ٿ���CH452��ʾ
		while(!RI);			//�ȴ�������������
		cmd=SBUF;
		RI=0;
		if ( cmd & 0xE0 ) continue;  // ��Ч������
		while(!RI);	//���յڶ�����������,δ���ǳ�ʱ
		dat=SBUF;
		RI=0;
		command=cmd;
		command= command<<8 | dat;
		CH452_Write(command);
	}
}