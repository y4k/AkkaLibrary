#ifndef BLINKT_H
#define BLINKT_H

#include <bcm2835.h>  // communicate with gpio
#include <iostream>   // genuinely only here to output the word 'Dead'
#include <stdint.h>   // fixed width ints to hold Pixel values
#include <unistd.h>   // usleep
#include <stdlib.h>   // basically everything I think   definitely ramd
#include "pixel.h"    // Pixel and PixelList classes
#include "clinkt.h"   // just leave this one in
#include "low_level.h"// signal, flushBuffer, and other goodies

#include <array>  // size() I think


/*
 * Mimics and exposes the Pixel list functions.
 */
extern "C"
{
	int init();
	int shutdown();

	int on_all(uint8_t r, uint8_t g, uint8_t b, uint8_t br);
	int on_pixel(uint8_t pixel, uint8_t r, uint8_t g, uint8_t b, uint8_t br);
	int off_all();
	int off_pixel(uint8_t pixel);

	int set_pixels(uint8_t r, uint8_t g, uint8_t b, uint8_t br);

	int set_pixel(uint8_t pixel, uint8_t r, uint8_t g, uint8_t b, uint8_t br);
	int update();

	// void fade(int millisecs = 500);
	// void rise(int millisecs = 500, int brightnesss = 3);   //! arbitrary number
	// void crossfade(PixelList otherParent, int steps = 5);      //! more arbitrary numbers
}
#endif // BLINKT_H
