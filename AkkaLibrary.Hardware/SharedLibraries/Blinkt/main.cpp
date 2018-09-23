/*
created by diana probst from skeleton of original blinkt python library from pimoroni
*/

// "The thing with C++ is to avoid using auto, because it's a complete bugger and you never bloody know what you're getting." - Roger Smith

#include <bcm2835.h>
#include <stdint.h>
#include <iostream>
#include <string>
#include <linux/types.h>
#include <signal.h> // some kind of magic I guess
#include <unistd.h>  // usleep
#include <stdlib.h>
#include "pixel.h"
#include "clinkt.h"


#ifndef TEST


int main(){

  
  //  running = 1;
  
  //  signal(SIGINT, sigint_handler);
  
  if (start()){
    std::cout << "Unable to start apa102: bcm not initialising?\n";
    return 1;
  }

  // defaults and useful pixels
  
  PixelList Blinkt;
  Pixel myPixel = Pixel(255,255,255);
  uint32_t red = 0xFF000003;               // hex codes fit in neatly, of course
  uint32_t green = 0x00FF003;
  uint32_t blue = 0x0000FF03;
  uint32_t white = 0b11111111111111111111111100000011; // Last 0ctet takes APA framing and brightness
  
  setPixel(Blinkt, blue);    // default position is Pixel 0
  setPixel(Blinkt, blue+green, 1); // Pixels can be set by number
  setPixel(Blinkt, 0b00000000111111110000000000000111, 2);
  setPixel(Blinkt, green + red, 3);
  setPixel(Blinkt, red, 4);
  setPixel(Blinkt, 0, 5);  // blank pixel 'spacer'
  setPixel(Blinkt, white, 6);
  setPixel(Blinkt, myPixel.getPixel(), 7);  // pre-defined Pixels can be called

  Blinkt.show();
  usleep(1000000);
  
  PixelList AllWhite;
  for (int i = 0; i < 8; i++)
    { setPixel(AllWhite, white, i); }

  AllWhite.show();
  usleep(1000000);
  
  PixelList AllBlack;

  for (int i = 0; i < 8; i++)
    { setPixel(AllBlack, 0, i); }

  AllBlack.show();
  usleep(1000000);

  stop(); // ends gpio nicely
  
  return 0;	
}
#endif
