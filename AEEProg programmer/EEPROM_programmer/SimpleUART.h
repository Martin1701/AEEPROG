/*
 * SimpleUART.h
 *
 * Created: 25. 4. 2022 14:42:54
 *  Author: acer
 */ 


#ifndef SIMPLEUART_H_
#define SIMPLEUART_H_

#include <avr/io.h>

#define C_MinBaudRate_U32 2400
#define C_MaxBaudRate_U32 115200UL

#define M_GetBaudRateGeneratorValue(baudrate)  (((F_CPU -((baudrate) * 8L)) / ((baudrate) * 16UL)))
#define  util_GetBitMask(bit)          (1<<(bit))
#define  util_IsBitCleared(x,bit)      (((x)&(util_GetBitMask(bit)))==0u)

void UART_Init(uint32_t v_baudRate_u32);
void UART_SetBaudRate(uint32_t v_baudRate_u32);

uint8_t UART_RxByte(void);
void UART_TxByte(uint8_t data);

void UART_TxString(char* ptr);
uint8_t UART_RxString(char* rx_pole);
	
#endif /* SIMPLEUART_H_ */