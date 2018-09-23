// Include Guard ------------------------------
#ifndef PIXEL_H
#define PIXEL_H

#include <stdint.h>
#include <iostream>
#include "bcm2835.h"
#include <signal.h>
#include "low_level.h"
#include <vector>
#include "config.h"

class Pixel
   {
   private:
     static uint8_t defaultBrightness;
     uint8_t brightness;
   public:
     // First, the constructor:
     Pixel();
     Pixel(uint8_t r, uint8_t g, uint8_t b, uint8_t br = defaultBrightness);
     Pixel(uint32_t colourInfo);
     uint32_t colour;
     void setP(uint8_t r, uint8_t g, uint8_t b, uint8_t br);
     void setP(uint32_t colourInfo);
     void setHexPixel(std::string hexValue, uint8_t brightness = 3);
     uint32_t getPixel();   /// returns Pixel.colour
     
     void setBrightness(uint8_t br);
     uint8_t getBrightness();

     void setColour(uint8_t r, uint8_t g, uint8_t b);  // calls setFullPixel with current brightness
   };


class PixelList
 {
 private:
   std::vector<Pixel> pVector;
 public:
   //  constructor:
   PixelList(void);
   PixelList(int length);
   //  destructor:
   ~PixelList(void);
   void setP(uint32_t pixel, int x = 0);
   void setFullPixel(uint32_t pixel, int x);
   void setP(uint8_t r, uint8_t g, uint8_t b, uint8_t br, int x = 0);
   uint32_t getPixel(int p);
   int vectorLength = 13;
   void show();
   void fade(int millisecs = 500);
   void rise(int millisecs = 500, int brightnesss = 3);   //! arbitrary number
   void crossfade(PixelList otherParent, int steps = 5);      //! more arbitrary numbers
 };

inline void setPixel(PixelList& plist, uint32_t p = 7, int x = 0)
{
  plist.setP(p, x);
}


#endif // PIXEL_H
