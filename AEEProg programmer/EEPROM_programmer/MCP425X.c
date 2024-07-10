#include "MCP425X.h"

void MCP425X_init(void) {
	DDRB |= (1<<PB3);
	SS_DDR |= (1<<SS);
	
	SPI_Begin(SPI_MODE_0, SPI_PRESCALER__2);
	
	SS_PORT &= ~(1<< SS);
	// enable both potentiometers and all of their pins
	SPI_SendByte(0x40);
	SPI_SendByte(0xFF);
	SS_PORT |= (1<< SS);
}

void MCP425X_set(wiper selectedwiper, uint8_t value) {
	if(selectedwiper == wiper_0) {
		SS_PORT &= ~(1<< SS);
		SPI_SendByte(0x00);
		SPI_SendByte(value);
		SS_PORT |= (1<< SS);
	} else { // wiper 1
		SS_PORT &= ~(1<< SS);
		SPI_SendByte(0x10);
		SPI_SendByte(value);
		SS_PORT |= (1<< SS);
	}
}