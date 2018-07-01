#include "blinkt.h"

PixelList pixels(NUM_LEDS);
bool initialised = false;

extern "C"
{
int init()
{
	if( start() )
	{
		return 1;
	}

	initialised = true;

	for (int j = 0; j < NUM_LEDS; j++)
	{
		pixels.setP(0,0,0,3,j); 
	}
	pixels.show();
	return 0;
}

int on_all(uint8_t r, uint8_t g, uint8_t b, uint8_t br)
{
	if(!initialised)
	{
		return 1;
	}

	for (int j = 0; j < NUM_LEDS; j++)
	{
		pixels.setP(r,g,b,br,j); 
	}
	pixels.show();
	return 0;
}

int off_all()
{
	if(!initialised)
	{
		return 1;
	}
	for (int j = 0; j < NUM_LEDS; j++)
	{
		pixels.setP(0,0,0,3,j); 
	}
	pixels.show();
	return 0;
}

int on_pixel(uint8_t pixel, uint8_t r, uint8_t g, uint8_t b, uint8_t br)
{
	if(!initialised)
	{
		return 1;
	}
	if(pixel >= NUM_LEDS)
	{
		return 1;
	}
	pixels.setP(r,g,b,br,pixel);
	pixels.show();
	return 0;
}

int off_pixel(uint8_t pixel)
{
	if(!initialised)
	{
		return 1;
	}
	if(pixel >= NUM_LEDS)
	{
		return 1;
	}
	pixels.setP(0,0,0,3,pixel);
	pixels.show();
	return 0;
}

int set_pixels(uint8_t r, uint8_t g, uint8_t b, uint8_t br)
{
	if(!initialised)
	{
		return 1;
	}
	for (int pixel; pixel < NUM_LEDS;pixel++)
	{
		pixels.setP(r, g, b, br, pixel);
	}
	return 0;
}

int set_pixel(uint8_t pixel, uint8_t r, uint8_t g, uint8_t b, uint8_t br)
{
	if(!initialised)
	{
		return 1;
	}
	if(pixel >= NUM_LEDS)
	{
		return 1;
	}
	pixels.setP(r,g,b,br,pixel);
	return 0;
}

int update()
{
	if(!initialised)
	{
		return 1;
	}
	pixels.show();
	return 0;
}

int shutdown()
{
	off_all();
	stop();
	initialised = false;
	return 0;
}
}
