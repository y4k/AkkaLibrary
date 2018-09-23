#include <stdint.h>
//#include <functional>
#include <bcm2835.h>
//#include <iostream>
#include <string>
#include <linux/types.h>
#include <signal.h> // some kind of magic I guess
//#include <chrono>  // milliseconds
//#include <thread>  // sleep_for
#include <unistd.h>  // usleep
#include <stdlib.h>
#include "pixel.h"
#include "low_level.h"

void clear(void);
void set_pixel_uint32(uint8_t led, uint32_t color);
void set_pixel(uint8_t led, uint8_t r, uint8_t g, uint8_t b);
void set_pixel_brightness(uint8_t led, uint8_t brightness);
uint32_t rgbb(uint8_t r, uint8_t g, uint8_t b, uint8_t brightness);
uint32_t rgb(uint8_t r, uint8_t g, uint8_t b);
void stop(void);
int start(void);
void show(void);
void dieNicely(int dummy);
