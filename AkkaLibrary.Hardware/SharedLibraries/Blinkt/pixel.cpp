#include <iostream>
#include "pixel.h"
#include <unistd.h> // usleep

uint8_t Pixel::defaultBrightness = 3;

Pixel::Pixel()
{
  colour = Pixel::defaultBrightness;
}

Pixel::Pixel(uint8_t r, uint8_t g, uint8_t b, uint8_t br)  // Always move towards the uint32_t models and try to return those
{
  Pixel();
  setP(r, g, b, br);
}

Pixel::Pixel(uint32_t colourInfo){
  Pixel();
  setP(colourInfo);
}

void Pixel::setP(uint32_t colourInfo){
  colour = colourInfo;
}

void Pixel::setHexPixel(std::string hexValue, uint8_t brightness)
{  // expects value of form ffffff
  uint32_t result;
  if (hexValue.length() == 6)
    {
      result = stoi(hexValue, NULL, 16);
     
    }
  result <<=  8;
  setP(result + brightness);
}

uint32_t Pixel::getPixel()
{
  return colour;
}

void Pixel::setP(uint8_t r, uint8_t g, uint8_t b, uint8_t br){ // stores entire pixel set in Pixel.colour
  uint32_t result = 0;
  result = (br & 0b111);
  result |= ((uint32_t)r << 24);
  result |= ((uint32_t)g << 16);
  result |= ((uint16_t)b << 8);
  colour = result;
}

void Pixel::setBrightness(uint8_t br){
  uint32_t newValue = (getPixel() & 0xFFFFFF00) + br;
  setP(newValue);
}

uint8_t Pixel::getBrightness(){
  return (getPixel() & 0b111);
}

void Pixel::setColour(uint8_t r, uint8_t g, uint8_t b){  // calls setP with default brightness or over-ride brightness  
  setP(r, g, b, getBrightness());  // default brightness defined as static
}

PixelList::PixelList()
{
  for (int i = 0; i < 8; i++)
    {pVector.push_back(Pixel());}
    }

PixelList::PixelList(int length)
{
  for (int i = 0; i < length; i++)
    {pVector.push_back(Pixel());}
}

PixelList::~PixelList()
{
}

uint32_t PixelList::getPixel(int p)
{ return pVector[p].getPixel();
}

void PixelList::fade(int millisecs){ //!! check brightness of each pixel
  int uInterval = (millisecs)*1000;  
  uint8_t fadeBr;
  uint8_t minBr;
  
  for (int i = 0; i < 7; i++){ 
     //!! arbitrary j 
      for (int j = 0; j < 8; j++)
	{
	  fadeBr = pVector[j].getBrightness();
	  if (fadeBr > 0)
	    { fadeBr -= 1;
	    }
	  minBr += fadeBr;
	  pVector[j].setBrightness(fadeBr);
	}
      show();
      usleep(uInterval);
      if (minBr == 0){
	break;
      }
      minBr = 0;   
  } 
}

void PixelList::rise(int millisecs, int brightness)
{
  int uInterval = (millisecs * 1000);
  for (int i = 0; i < brightness; i++)
    {
      for (int j = 0; j < 8; j++){
	pVector[j].setBrightness(i+1);}
      show();
      usleep(uInterval);
    }
}

void PixelList::crossfade(PixelList otherParent, int steps)
{
  for (int i = 0; i < steps; i++)
    {
      for (int j = 0; j < NUM_LEDS; j++)
	{
	  uint8_t myRed = pVector[j].getPixel() >> 24;      // should have getRed() as a method - someone should write that.  Me.
	  uint8_t myGreen = pVector[j].getPixel() >> 16;
	  uint8_t myBlue = pVector[j].getPixel() >> 8;
	  uint8_t myBright = pVector[j].getPixel() & 0b00000111;

	  uint8_t otherRed = otherParent.getPixel(j) >> 24;
	  uint8_t otherGreen = otherParent.getPixel(j) >> 16;
	  uint8_t otherBlue = otherParent.getPixel(j) >> 8;
	  uint8_t otherBright = otherParent.getPixel(j) & 0b00000111;

	  int sign;
	  sign = myRed > otherRed ? -1 : 1;
	  myRed += sign * (std::abs(otherRed-myRed) / (steps - i));
	  sign = myGreen > otherGreen ? -1 : 1;
	  myGreen += sign * std::abs(otherGreen-myGreen) / (steps - i);
	  sign = myBlue > otherBlue ? -1 : 1;
	  myBlue += sign * std::abs(otherBlue-myBlue) / (steps - i);

	  if  (myBright > otherBright)  // need a method that takes into account steps and granularity
	    { myBright--;}
	  else if (myBright < otherBright)
	    { myBright ++;}
	  
	  setP(myRed, myGreen, myBlue, myBright, j);	  
	}
      show();
      usleep(1000000/steps);
    } 
}


void PixelList::setP(uint32_t colourInfo, int x)
{
  Pixel temp = pVector[x];
  temp.setP(colourInfo);
  pVector[x] = temp;
}


void PixelList::setFullPixel(uint32_t colourInfo, int x)
{
  Pixel temp = pVector[x];
  temp.setP(colourInfo);
  pVector[x] = temp;
}


void PixelList::setP(uint8_t r, uint8_t g, uint8_t b, uint8_t br, int x){
Pixel temp = pVector[x];
  temp.setP(r, g, b, br);
  pVector[x] = temp;

}

void PixelList::show()
{
  writeByte(0);
  writeByte(0);
  writeByte(0);
  writeByte(0);  // ensure clear buffer

  for (int n = 0; n < NUM_LEDS; n++)
    { 	   writeByte(APA_SOF | (pVector[n].getPixel() & 0b11111));
	   writeByte(pVector[n].getPixel() >> 8 & 0xFF);
	   writeByte(pVector[n].getPixel() >> 16 & 0xFF);
	   writeByte(pVector[n].getPixel() >> 24 & 0xFF);

	   // increment count
    }
  flushBuffer();
}
