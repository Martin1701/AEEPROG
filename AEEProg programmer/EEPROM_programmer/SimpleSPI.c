#include "SimpleSPI.h"

/* setup and config functions */
void SPI_Begin(SPI_MODE MODE, SPI_PRESCALER PRESCALER)
{
	/* Set MOSI and SCK output */
	MOSI_PORT |= (1<<MOSI_PIN);
	SCK_PORT |= (1<<SCK_PIN);
	/* Set MISO as input */
	MISO_PORT &= ~(1<<MISO_PIN);

	SPI_Change(MODE, PRESCALER);
}
void SPI_Change(SPI_MODE MODE, SPI_PRESCALER PRESCALER) {
	/* Enable SPI, Master mode, set clock rate and SPI mode */
	SPCR = (1<<SPE) | (1<<MSTR) | MODE | ((PRESCALER & 0x03));
	/* second part of setting clock */
	SPSR = (PRESCALER>>4);
}

/* data transfer functions */
void SPI_SendByte(const uint8_t Byte)
{
	SPDR = Byte;			/* Write data to SPI data register */
	while(!(SPSR & (1<<SPIF)));	/* Wait till transmission complete */
}

uint8_t SPI_ReceiveByte(void)
{
	SPDR = 0xFF;
	while (!(SPSR & (1 << SPIF)));
	return SPDR;
}

uint8_t SPI_TransferByte(const uint8_t Byte)
{
	SPDR = Byte;			/* Write data to SPI data register */
	asm volatile("nop");
	while(!(SPSR & (1<<SPIF)));	/* Wait till transmission complete */
	return SPDR;
}

uint8_t SPI_SendThenReceive(const uint8_t Byte)
{
	SPDR = Byte;
	asm volatile("nop");
	while (!(SPSR & (1 << SPIF)));
	SPDR = 0x00;
	while (!(SPSR & (1 << SPIF)));
	return SPDR;
}