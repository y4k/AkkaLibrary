// Include Guard ------------------------------------

#ifndef LOW_LEVEL_H
#define LOW_LEVEL_H

#include <bcm2835.h>
#include <stdint.h>
#include <signal.h>   // some kind of magic I guess.  also, keyboard interrupts
#include "config.h"


void writeByte(uint8_t byte);

void flushBuffer(int length = NUM_LEDS);



#endif  // LOW_LEVEL_H
