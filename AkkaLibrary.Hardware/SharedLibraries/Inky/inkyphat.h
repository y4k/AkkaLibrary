// Include Guard
#ifndef INKYPHAT_H
#define INKYPHAT_H

#include <stdint.h>
#include <vector>
#include <string>
#include <wiringPi.h>
#include <wiringPiSPI.h>

#include "constants.h"

// Define some constants

const uint8_t _SPI_COMMAND = LOW;
const uint8_t _SPI_DATA = HIGH;

const uint8_t _V2_RESET = 0x12;

const uint8_t _BOOSTER_SOFT_START = 0x06;
const uint8_t _POWER_SETTING = 0x01;
const uint8_t _POWER_OFF = 0x02;
const uint8_t _POWER_ON = 0x04;
const uint8_t _PANEL_SETTING = 0x00;
const uint8_t _OSCILLATOR_CONTROL = 0x30;
const uint8_t _TEMP_SENSOR_ENABLE = 0x41;
const uint8_t _RESOLUTION_SETTING = 0x61;
const uint8_t _VCOM_DC_SETTING = 0x82;
const uint8_t _VCOM_DATA_INTERVAL_SETTING = 0x50;
const uint8_t _DATA_START_TRANSMISSION_1 = 0x10;
const uint8_t _DATA_START_TRANSMISSION_2 = 0x13;
const uint8_t _DATA_STOP = 0x11;
const uint8_t _DISPLAY_REFRESH = 0x12;
const uint8_t _DEEP_SLEEP = 0x07;

const uint8_t _PARTIAL_ENTER = 0x91;
const uint8_t _PARTIAL_EXIT = 0x92;
const uint8_t _PARTIAL_CONFIG = 0x90;

const uint8_t _POWER_SAVE = 0xe3;

const uint8_t WHITE = 0;
const uint8_t BLACK = 1;
const uint8_t RED = 2;
const uint8_t YELLOW = 2;

const int WIDTH = 104;
const int HEIGHT = 212;

class InkyPhat
{
  private:
    int inky_version = 2;
    int width = WIDTH;
    int height = HEIGHT;
    
    uint8_t command_pin = COMMAND_PIN;
    uint8_t reset_pin = RESET_PIN;
    uint8_t busy_pin = BUSY_PIN;
    uint8_t cs_pin = CS0_PIN;
        
    uint8_t border = 0b00000000;
    std::vector< uint8_t > palette;

    int _display_init();
    int _display_update(std::vector<uint8_t> buf_black, std::vector<uint8_t> buf_red);
    int _display_fini();
    int _busy_wait();
    int reset();
    int _send_command(uint8_t command);
    int _send_command(uint8_t command, uint8_t data);
    int _send_command(uint8_t command, std::vector<uint8_t> data);
    int _send_command(std::vector< uint8_t > command, std::vector< uint8_t > data);
    int _send_data(std::vector< uint8_t > data);
    int _spi_write(uint8_t level, std::vector< uint8_t > data);

  public:
    InkyPhat();
    ~InkyPhat();
    int update(std::vector< std::vector< uint8_t > > pixels);
};

#endif