#include <avr/io.h>
#include "SimpleSPI.h"

#ifndef MCP425X_H_
#define MCP425X_H_

#define SS PB2
#define SS_PORT PORTB
#define SS_DDR DDRB

typedef enum {
	wiper_0,
	wiper_1
	}wiper;

void MCP425X_init(void);

void MCP425X_set(wiper selectedwiper, uint8_t value);

#endif /* MCP425X_H_ */