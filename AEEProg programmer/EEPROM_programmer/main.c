/* TODO
implement watchdog timer, when reading/writing data
so programmer does not get stuck accidentaly - yeah, good point (M.H. 2023)
*/

#include <avr/io.h>
#include <util/delay.h>
#include <avr/interrupt.h>
#include <avr/portpins.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <util/delay.h>

#include "SimpleSPI.h"
#include "SimpleUART.h"
#include "MCP425X.h"


typedef enum {
	VPP, // programming voltage of eprom/eeprom (ADC0)
	VDD, // logical voltage of eprom/eeprom (ADC1)
} voltageVariant;
typedef enum {
	LSBFIRST,
	MSBFIRST,
}_bitOrder;
typedef enum {
	EPROM,
	EEPROM
}_memoryType;
typedef enum {
	INPUT,
	OUTPUT
}_pinDirection;
uint16_t maxAddress = 0;


void setup_hardware(void);
float getVoltage(voltageVariant voltage);
void setVoltage(voltageVariant voltage, float wantedVoltage);
void shiftOut(uint8_t dataPin, uint8_t clockPin, _bitOrder bitOrder, uint8_t val);
void setAddress(uint16_t address, bool outputEnable, bool writeEprom);
uint8_t read(uint16_t address);
void write(uint16_t address, uint8_t data, int16_t us_delay);
int8_t writeWithCheck(uint16_t address, uint8_t data);
_memoryType memoryType = EEPROM;


int main(void)
{	
	setup_hardware();
		
	char UART_RxBuf[64];
	UART_TxString("start\n");
	
	while (1)
	{
		if(UART_RxString(UART_RxBuf) == 1)
		{
			if(strcmp(UART_RxBuf, "start\n") == 0)
			{
				UART_TxString("start\n");
			}
			else if(strcmp(UART_RxBuf, "EPROM\n") == 0)
			{
				memoryType = EPROM;
				UART_TxString("EPROM\n");
			} else if(strcmp(UART_RxBuf, "EEPROM\n") == 0)
			{
				memoryType = EEPROM;
				UART_TxString("EEPROM\n");
			} else if(strcmp(UART_RxBuf, "maxAddr\n") == 0)
			{
				while(UART_RxString(UART_RxBuf) != 1);
				maxAddress = atoi(UART_RxBuf);
				UART_TxString(UART_RxBuf);
			} else if(strcmp(UART_RxBuf, "erase\n") == 0)
			{
				for (uint16_t address = 0; address <= maxAddress; address++)
				{
					if(writeWithCheck(address, 0xFF) == -1) {
						UART_TxString("err\n");
						break;
					}
					if(((address % (maxAddress / 50)) == 0) && address != 0) {
						UART_TxString("\n");
					}
				}
				UART_TxString("end\n");
			} else if(strcmp(UART_RxBuf, "write_I\n") == 0)
			{
				cli(); // interrupts, you dummy
				UART_TxString("ready\n");
				while(1) {
					uint8_t byteCount = UART_RxByte();

					uint8_t record[byteCount + 4];
					uint8_t checksum = byteCount;
					for (uint8_t i = 0; i < byteCount + 4; i++)
					{
						record[i] = UART_RxByte();
						checksum += record[i];
					}
					
					uint16_t address = ((uint16_t)record[0] << 8) + record[1];
					uint8_t recordType = record[2];
					
					//if(checksum != 0) {
					//// explanation:
					//// if you make sum of all bytes and sum it up with checksum, it should get you 0
					//UART_TxString("checksum\n");
					//}
					
					
					uint8_t j = 0;
					bool writeError = false;
					for(;j < byteCount; j++) {
						if(writeWithCheck(address + j, record[3 + j]) < 0)
						{
							writeError = true;
							break;
						}
					}
					UART_TxByte(j);
					//UART_TxString("next\n");
					if(recordType == 0x01 || writeError == true) { // end of file
						//sei(); // turn them back on, we still need them
						break;
					}
				}
			} else if(strcmp(UART_RxBuf, "read\n") == 0)
			{
				cli();
				UART_TxString("ready\n");
				uint16_t count = (uint16_t)UART_RxByte() << 8;
				count += UART_RxByte();
				
				for (uint16_t address = 0; address < count; address++)
				{
					UART_TxByte(read(address));
				}
				sei();
			} else if(strcmp(UART_RxBuf, "write\n") == 0)
			{
				cli();
				UART_TxString("ready\n");
				while(1) {
					uint8_t byteCount = UART_RxByte();
					if(byteCount == 0) break;
					
					uint16_t address = (uint16_t)UART_RxByte() << 8;
					address += UART_RxByte();
					
					uint8_t bytes[byteCount];
					for (int16_t i = 0; i < (int16_t)byteCount; i++)
					{
						bytes[i] = UART_RxByte();
					}
					uint8_t j = 0;
					bool writeError = false;
					for (; j < byteCount; j++)
					{
						if(writeWithCheck(address + j, bytes[j]) < 0) {
							writeError = true;
							break;
						}
					}
					UART_TxByte(j);
					if(writeError == true) {
						break;
					}
				}
			}
			sei();
		}
	}
}
void setup_hardware(void) {
	/* port configuration */
	DDRC |= (1<<PC2) | (1<<PC3) | (1<<PC4) | (1<<PC5); // A14/#WE, STR, DAT, CLK
	DDRB |= (1<<PB5);
	//DDRB |= (1<<PB1) | (1<<PB0);
	
	PORTB &= ~(1<<PB5);

	/* ADC setup */
	ADCSRA = (1<<ADEN) | (1<<ADPS2) | (1<<ADPS1) | (1<<ADPS0);
	
	/* digital potentiometer initialization to default values */
	MCP425X_init();
	//MCP425X_set(wiper_0, 128);
	//MCP425X_set(wiper_1, 128);
	
	UART_Init(74880);
	
	sei();
	setVoltage(VDD, 5.0f); // default logical voltage
	setVoltage(VPP, 5.0f);
	read(0x00);
}

float getVoltage(voltageVariant voltage) {
	float maxVoltage;
	if(voltage == VPP) { // ADC0
		ADMUX = (1<<REFS1) | (1<<REFS0);
		maxVoltage = 26.72f;
		} else {
		ADMUX = (1<<REFS1) | (1<<REFS0) | (1<<MUX0);
		maxVoltage = 26.23f;
	}
	uint16_t ADC_value = 0;
	for (uint8_t i = 0; i < 8; i++)
	{
		ADCSRA |= (1<<ADSC);
		while(!(ADCSRA & 0x10)); // wait till conversion completes
		ADCSRA |= (1<<ADIF); // reset flag
		ADC_value += ADC;
	}
	ADC_value /= 8;
	float u = (ADC_value / 1023.0f * maxVoltage); // internal reference corrected by measurement with multimeter
	return u ;
}


void setVoltage(voltageVariant voltage, float wantedVoltage) {
	uint8_t wiperValue = 135; // same for both
	wiper _wiper;
	voltageVariant _voltageVariant;
	float max_voltage;
	if(voltage == VPP) {
		_wiper = wiper_1;
		max_voltage = 25.0f;
		_voltageVariant = VPP;
		} else { // VDD
		_wiper = wiper_0;
		max_voltage = 18.0f;
		_voltageVariant = VDD;
	}
	
	if(wantedVoltage > max_voltage) return; // for safety
	else if(wantedVoltage < 4.5f) {
		MCP425X_set(_wiper, wiperValue);
		return;
	}
	
	float previousVoltage = 0;
	float sampledVoltage = 0;
	MCP425X_set(_wiper, wiperValue);
	_delay_ms(10);
	
	while(1) {
		wiperValue--;
		MCP425X_set(_wiper, wiperValue);
		_delay_ms(10); // wait for the voltage to settle down
		sampledVoltage = getVoltage(_voltageVariant);
		
		if(sampledVoltage > wantedVoltage) {
			if((sampledVoltage - wantedVoltage) > (wantedVoltage - previousVoltage)) {
				MCP425X_set(_wiper, wiperValue++);
				_delay_ms(10);
			}
			return; // voltage is set
		}
		previousVoltage = sampledVoltage;
	}
	
}
void setPinDirection(_pinDirection pinDirection) {
	if(pinDirection == OUTPUT) {
		DDRB |= 0x03;
		DDRD |= 0xFC;
		} else { // INPUT
		DDRB &= 0xFC;
		DDRD &= 0x03;
	}
}
uint8_t read(uint16_t address) {
	setAddress(address, true, false);
	setPinDirection(INPUT);
	PORTB = (0x03 & (PORTB & 0xFC)); // disable pull-ups
	PORTD = (0xFC & (PORTD & 0x03)); // disable pull-ups
	//_delay_us(10); // Address to Output Delay
	return ((PINB & 0x03) | (PIND & 0xFC));
}
void write(uint16_t address, uint8_t data, int16_t us_delay) {
	if(memoryType == EPROM) {
		// check datasheet for further adjustments
		setPinDirection(OUTPUT);
		
		PORTB = ((data & 0x03) | (PORTB & 0xFC)); // first two bits
		PORTD = ((data & 0xFC) | (PORTD & 0x03)); // all the other bits
		
		setAddress(address, false, true);
		
		for(; us_delay > 0; us_delay--) {
			_delay_us(1);
		}
		setAddress(address, false, false);
		}else if(memoryType == EEPROM) {
		setAddress(address, false, false);
		//_delay_us(10); // Address setup time
		setPinDirection(OUTPUT);
		PORTB = ((data & 0x03) | (PORTB & 0xFC)); // first two bits
		PORTD = ((data & 0xFC) | (PORTD & 0x03)); // all the other bits
		
		PORTC &= ~(1<<PC2); // WRITE_EN low
		_delay_us(1);
		PORTC |= (1<<PC2); // WRITE_EN high
		for(; us_delay > 0; us_delay--) {
			_delay_us(1);
		}
	}
}
int8_t writeWithCheck(uint16_t address, uint8_t data) {
	uint8_t writeCount = 0;
	while (read(address) != data)
	{
		write(address, data, 500);
		writeCount++;
		if (writeCount == 20)
		{
			return -1; // failed to write
		}
	}
	return 1; // success
}
void shiftOut(uint8_t dataPin, uint8_t clockPin, _bitOrder bitOrder, uint8_t val)
{
	for (uint8_t i = 0; i < 8; i++)  {
		if (bitOrder == LSBFIRST) {
			if(val & 0x01) {
				PORTC |= (1<<dataPin);
				} else {
				PORTC &= ~(1<<dataPin);
			}
			val >>= 1;
			} else {
			if(val & 0x80) {
				PORTC |= (1<<dataPin);
			}
			else {
				PORTC &= ~(1<<dataPin);
			}
			val <<= 1;
		}
		PORTC |= (1<<clockPin);
		PORTC &= ~(1<<clockPin);
	}
}

void setAddress(uint16_t address, bool outputEnable, bool writeEprom) {
	if(memoryType == EPROM) {
		if(address > 0x3FFF) {
			PORTC |= (1<<PC2);
			} else {
			PORTC &= ~(1<<PC2);
		}
		shiftOut(PC4, PC5, MSBFIRST, ((address >> 8) & 0x3F) | (outputEnable ? 0x00 : 0x40) | (writeEprom ? 0x00 : 0x80));
		shiftOut(PC4, PC5, MSBFIRST, address);
		} else {
		//shiftOut(PC4, PC5, MSBFIRST, ((address >> 8) & 0x3F) | (outputEnable ? 0x00 : 0x40));
		shiftOut(PC4, PC5, MSBFIRST, ((address >> 8) & 0x3F) | (outputEnable ? 0x00 : 0x40) | ((address > 0x3FFF) ? 0x00 : 0x80));
		shiftOut(PC4, PC5, MSBFIRST, address);
	}
	PORTC &= ~(1<<PC3);
	PORTC |= (1<<PC3);
	PORTC &= ~(1<<PC3);
	_delay_us(1);
}
