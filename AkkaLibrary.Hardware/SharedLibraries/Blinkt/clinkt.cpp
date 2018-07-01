/*
created by diana probst from skeleton of original blinkt python library from pimoroni
*/

//#include <functional>
#include <bcm2835.h>
#include <stdint.h>
//#include <iostream>
#include <string>
#include <linux/types.h>
#include <signal.h> // some kind of magic I guess
//#include <chrono>  // milliseconds
//#include <thread>  // sleep_for
#include <unistd.h>  // usleep
#include <stdlib.h>
#include "pixel.h"
#include "clinkt.h"
#include "low_level.h"


////#ifdef TEST
volatile int running = 0;  // I tried removing this and bad things happened
//#endif


int x;

#ifdef TEST
void sigint_handler(int dummy){
  running = 0;
  return;
}
#endif

#ifndef TEST //!! Really no clue what this bit does
void sigint_handler(int dummy){
  running = 0;
  return;
}
#endif

void stop(void){
	bcm2835_spi_end();

	bcm2835_close();
}

int start(void){

	if(!bcm2835_init()) return 1;

#ifdef TEST
	printf("GPIO Initialized\n");
#endif

	bcm2835_gpio_fsel(MOSI, BCM2835_GPIO_FSEL_OUTP);
	bcm2835_gpio_write(MOSI, LOW);
	bcm2835_gpio_fsel(SCLK, BCM2835_GPIO_FSEL_OUTP);
	bcm2835_gpio_write(SCLK, LOW);
	
	return 0;
	
}

void dieNicely(int dummy)
  {
    /**************************************************************************//**
     cleans up a bit on exit
     we do not have many static functions and we have a lot of loops
so this is designed to be called from keyboard interrupt
we'll let the OS clean up local variables
	**********************************************************************/
    PixelList Die;
    for (int i = 0; i < 8; i++)
      {
	setPixel(Die, 0, i);
      
      }
    Die.show();
    std::cout << "\nDead.\n";
    stop();
    exit(0);
  }
  

#ifdef TEST

int main(){

	int z;

	running = 1;

	signal(SIGINT, sigint_handler);

	if (start()){
	  std::cout << "Unable to start apa102\n";
		return 1;
	}

	printf("Running test cycle\n");

	PixelList TestList;
	
	uint32_t red = 0b11111111000000000000000000000111;
	uint32_t green = 0b00000000111111110000000000000111;
	uint32_t blue = 0b00000000000000001111111100000111;
  
	
	int colour = 0;

	while(running){

		for(z = 0; z < NUM_LEDS; z++){		
			switch(colour){
			case 0: setPixel(TestList, red, z); break;
			case 1: setPixel(TestList, green, z); break;
			case 2: setPixel(TestList, blue, z); break;
			}
		}

		TestList.show();
		TestList.show();
		
		usleep(1000000);  // keep this high enough to differentiate colours

		colour+=1;
                colour%=3;

	}

	usleep(1000);

	stop();

	return 0;

}
#endif
