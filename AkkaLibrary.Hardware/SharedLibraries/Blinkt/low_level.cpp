#include "low_level.h" // <bcm2835.h>, <stdint.h>
#include "config.h"    // NUM_LEDs (and other goodies to pass onwards)

void writeByte(uint8_t byte){
	int n;
	for(n = 0; n < 8; n++){
		bcm2835_gpio_write(MOSI, (byte & (1 << (7-n))) > 0);
		bcm2835_gpio_write(SCLK, HIGH);
		bcm2835_gpio_write(SCLK, LOW);
	}
}

void flushBuffer(int length)
{
  /************************************************************************//**
     ASA_SOF LED string does not need precise timing but the payback for that is having to pass in clock timings.

     The documentation is spectacularly unhelpful in how to do this.  However, writing several blank bytes flushes the buffer that is held within the LED array/string.

     This function flushes the buffer, and can be over-ridden in size to flush loner buffers for longer strings.

     Default length is NUM_LEDS as defined in config.h
  ********************************************************************** **/

  for (int i =0; i < (length/2) + 1; i++)  // initial guess at length of buffer needed
    {
      writeByte(0);
    }

}
