#include "SimpleUART.h"
#include <string.h>
#include <avr/interrupt.h>

#define util_ExtractByte0to8(x)    (uint8_t) ((x) & 0xFFu)
#define util_ExtractByte8to16(x)   (uint8_t) (((x)>>8) & 0xFFu)
#define util_ExtractByte16to28(x)  (uint8_t) (((x)>>16) & 0xFFu)
#define util_ExtractByte28to32(x)  (uint8_t) (((x)>>28) & 0xFFu)

#define RX_UKONCENE 1
#define RX_NEUKONCENE 2


#define	BUF_SIZE 128 					// Velkost rx pola
char rx_buff[BUF_SIZE];	//inicializacia rx pola
volatile unsigned char rx_pos,rx_stav = RX_NEUKONCENE;


void UART_Init(uint32_t baudRate)
{
	UCSRB = (1<<RXEN)|(1<<TXEN)|(1<<RXCIE);			// Enable Receiver, Transmitter and interrupt
	UCSRC= (1<<URSEL) | (1<<UCSZ1) | (1<<UCSZ0);	// Asynchronous mode 8-bit data and 1-stop bit
	UCSRA= 0x00;									// Clear the UASRT status register
	UART_SetBaudRate(baudRate);						// Configure Baud rate
}

void UART_SetBaudRate(uint32_t baudRate)
{
	uint16_t RegValue;

	if((baudRate >= C_MinBaudRate_U32) && (baudRate<=C_MaxBaudRate_U32))
	{
		/* Check if the requested Baud rate is within range,
		If yes then calculate the value to be loaded into baud rate generator. */
		RegValue = M_GetBaudRateGeneratorValue(baudRate);
	}
	else
	{
		/*	 Invalid Baud rate requested, hence set it to default Baud rate of 9600 */
		RegValue = M_GetBaudRateGeneratorValue(9600);
	}

	UBRRL = util_ExtractByte0to8(RegValue); // rework this
	UBRRH = util_ExtractByte8to16(RegValue);
}
uint8_t UART_RxByte(void)
{
	while(util_IsBitCleared(UCSRA,RXC));  // Wait till the data is received
	return(UDR);                          // return the received char
}
void UART_TxByte(uint8_t data)
{
	while(util_IsBitCleared(UCSRA,UDRE));	// Wait till Transmitter(UDR) register becomes Empty
	UDR = data;								// Load the data to be transmitted
}
void UART_TxString(char* ptr) {
	while (*ptr)
	UART_TxByte(*ptr++);
}
uint8_t UART_RxString(char* rx_pole){
	
	// ak je prijimanie retazca znakov ukoncene...
	if(rx_stav == RX_UKONCENE) {
		// skopiruj buffer do pola
		strcpy(rx_pole, rx_buff);
		//zacni prijimanie retazca znova
		rx_stav = RX_NEUKONCENE;
		rx_pos = 0;
		// navratova hodnota, boli prijate data
		return 1;

		}else{
		// data neboli prijate...
		rx_pole = 0;
		return 0;
	}

}
ISR(USART_RXC_vect)
{

	// ak pretieklo rx pole tak ho vynuluj
	if (rx_pos == BUF_SIZE) rx_pos = 0;

	// ak neprisiel ukoncovaci znak
	if(rx_stav != RX_UKONCENE){
		
		rx_buff[rx_pos] = UDR;	// ulozenie znaku do buffer-a
		
		//ak prisiel ukoncovaci znak
		if ((rx_buff[rx_pos] == '\r') | (rx_buff[rx_pos] == '\n')){
			rx_buff[rx_pos+1] = '\0';	// ukoncenie retazca
			rx_stav = RX_UKONCENE;
			}else{
			rx_pos++;	 // inkrementuj pocitadlo
		}
	}
}