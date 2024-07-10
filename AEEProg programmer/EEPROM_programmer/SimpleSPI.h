/*
* simple SPI library
*
* Version 1.0  - Jan 22nd 2022
* Author: Martin1701
*
*
*
* License: MIT License (see LICENSE.txt)
*/
#ifndef SIMPLESPI_H_
#define SIMPLESPI_H_

/* hardware SPI pins (change to fit your microcontroller)
these work for ATmega32u4
*/
// MISO
#define MISO_PIN PB4
#define MISO_PORT DDRB
// MOSI
#define MOSI_PIN PB3
#define MOSI_PORT DDRB
// SCK
#define SCK_PIN PB5
#define SCK_PORT DDRB
// no SS, this library is master mode only


#include <avr/io.h>

// using directly stored register bits
typedef enum {
	SPI_MODE_0 = 0x00,
	SPI_MODE_1 = 0x04,
	SPI_MODE_2 = 0x0C,
	SPI_MODE_3 = 0x08,
}SPI_MODE;

/* first 4 bits contain value of SPI2X bit in SPSR register
second 4 bits contain values for SPR1 and SPR0 bits for SPCR register
*/
typedef enum {
	SPI_PRESCALER__4 = 0x00,
	SPI_PRESCALER__16 = 0x01,
	SPI_PRESCALER__64 = 0x02,
	SPI_PRESCALER__128 = 0x03,
	SPI_PRESCALER__2 = 0x10,
	SPI_PRESCALER__8 = 0x11,
	SPI_PRESCALER__32 = 0x12,
	//SPI_PRESCALER__64 = 0x13, // /64 can be set using 2 values
}SPI_PRESCALER;

/* functions */

/**
@brief	set up SPI (mode, frequency)
@param  MODE - mode of SPI (0, 1, 2, 3)
@param  PRESCALER - SPI frequency prescaler
@return none
*/
void SPI_Begin(SPI_MODE MODE, SPI_PRESCALER PRESCALER);
/**
@brief	change SPI mode and prescaler
@param  MODE - mode of SPI (0, 1, 2, 3)
@param  PRESCALER - SPI frequency prescaler
@return none
*/
void SPI_Change(SPI_MODE MODE,  SPI_PRESCALER PRESCALER);
/**
@brief	send one byte via SPI
@param	Byte - one byte of data
@return   none
*/
void SPI_SendByte(const uint8_t Byte);
/**
@brief	receive incoming byte via SPI
@param	void
@return	none
*/
uint8_t SPI_ReceiveByte(void);
/**
@brief	send and receive byte simultaneously
@param	Byte - one byte
@return	Byte - byte of received data
*/
uint8_t SPI_TransferByte(const uint8_t Byte);
/**
@brief	send byte, then wait for returning byte
@param	Byte - one byte
@return	Byte - byte of received data
*/
uint8_t SPI_SendThenReceive(const uint8_t Byte);


#endif /* SIMPLESPI_H_ */