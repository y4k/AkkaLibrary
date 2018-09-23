#include<iostream>
#include <stdint.h>
#include <string>
#include <linux/types.h>
#include <signal.h> // some kind of magic I guess
#include <iostream>
#include <unistd.h>  // usleep
#include <stdlib.h>

#include "inkyphat.h"

using namespace std;

// VERSION 2 only
InkyPhat::InkyPhat()
{
    inky_version = 2;

    // Fill the pixel buffer.
    // Indexed using buffer[height][width] e.g buffer[y][x] or buffer[row][column]

    /*
        The number of columns (212) represents the long side of the InkyPhat which is
        conventionally the horizontal with the short side as the width but when viewed,
        is usually taken as the height.
     */

    // ========================================
    cout << endl << "Pin Setup" << endl;
    //GPIO.setup(command_pin, GPIO.OUT, initial=GPIO.LOW, pull_up_down=GPIO.PUD_OFF)
    cout << "   -> CommandPin:" << unsigned(command_pin) << endl;
    // Set the COMMAND_PIN as output
    cout << "      Mode:" << OUTPUT << endl;
    pinMode(command_pin, OUTPUT);
    // Set the pull-up-down resistor to off
    pullUpDnControl(command_pin, PUD_OFF);
    // Set the state to LOW
    cout << "      InitialState:" << LOW << endl;
    digitalWrite(command_pin, LOW);


    //GPIO.setup(reset_pin, GPIO.OUT, initial=GPIO.HIGH, pull_up_down=GPIO.PUD_OFF)
    cout << "   -> ResetPin:" << unsigned(reset_pin) << endl;
    // Set the RESET_PIN as output
    cout << "      Mode:" << OUTPUT << endl;
    pinMode(reset_pin, OUTPUT);
    // Set the pull-up-down resistor to off
    pullUpDnControl(reset_pin, PUD_OFF);
    // Set the state to HIGH
    cout << "      InitialState:" << HIGH << endl;
    digitalWrite(reset_pin, HIGH);
    
    //GPIO.setup(busy_pin, GPIO.IN, pull_up_down=GPIO.PUD_OFF)
    cout << "   -> BusyPin:" << unsigned(busy_pin) << endl;
    // Set the BUSY_PIN as input
    cout << "      Mode:" << INPUT << endl;
    pinMode(busy_pin, INPUT);
    // Set the pull-up-down resistor to off
    pullUpDnControl(reset_pin, PUD_OFF);

    
    //GPIO.output(reset_pin, GPIO.LOW) - Set reset pin to low
    digitalWrite(reset_pin, LOW);
    //sleep for 0.1ms - time.sleep(0.1)
    usleep(100);
    //GPIO.output(reset_pin, GPIO.HIGH) - set reset pin to high
    digitalWrite(reset_pin, HIGH);
    //sleep for 0.1ms - time.sleep(0.1)
    usleep(100);

    //if GPIO.input(busy_pin) == 1 : set_version(1)
    if(digitalRead(busy_pin) == HIGH)
    {
        cout << "InkyHat version:" << 1 << endl;
        //set_version(1);
        palette.push_back(BLACK);
        palette.push_back(WHITE);
        palette.push_back(RED);
    }
    else if(digitalRead(busy_pin) == LOW)
    {
        cout << "InkyHat version:" << 2 << endl;
        // set_version(2);
        palette.push_back(WHITE);
        palette.push_back(BLACK);
        palette.push_back(RED);
    }
}

// Display initialisation
int InkyPhat::_display_init()
{
    reset();

    _send_command(0x74, 0x54); // Set analog control block
    _send_command(0x75, 0x3b); // Sent by dev board but undocumented in datasheet

    // Driver output control
    vector<uint8_t> driverData{0xd3, 0x00, 0x00};
    _send_command(0x01, driverData);

    // Dummy line period
    // Default value: 0b-----011
    // See page 22 of datasheet
    _send_command(0x3a, 0x07);

    // Gate line width
    _send_command(0x3b, 0x04);

    // Data entry mode
    _send_command(0x11, 0x03);
    return 0;
}

// Display update
// self._display_update = self._v2_update
int InkyPhat::_display_update(vector<uint8_t> buf_black, vector<uint8_t> buf_red)
{
    vector<uint8_t> xRamData{0x00, 0x0c};
    _send_command(0x44, xRamData); // Set RAM X address
    vector<uint8_t> yRamData{0x00, 0x00, 0xD3, 0x00, 0x00};
    _send_command(0x45, yRamData); // Set RAM Y address + erroneous extra byte?

    vector<uint8_t> sourceDrivingVoltage{0x2d, 0xb2, 0x22};
    _send_command(0x04, sourceDrivingVoltage); // Source driving voltage control

    _send_command(0x2c, 0x3c); // VCOM register, 0x3c = -1.5v?

    // Border control
    _send_command(0x3c, 0x00);
    if( border == 0b11000000)
    {
        _send_command(0x3c, 0x00);
    }
    else if( border == 0b01000000)
    {
        _send_command(0x3c, 0x33);
    }
    else if( border == 0b10000000)
    {
        _send_command(0x3c, 0xFF);
    }

    // VSS  = 0b00;
    // VSH1 = 0b01;
    // VSL  = 0b10;
    // VSH2 = 0b11;
    // def l(a, b, c, d):
    //     return (a << 6) | (b << 4) | (c << 2) | d;

    //// Send LUTs
    vector<uint8_t> lookup_tables{
        // Phase 0     Phase 1     Phase 2     Phase 3     Phase 4     Phase 5     Phase 6
        // A B C D     A B C D     A B C D     A B C D     A B C D     A B C D     A B C D
        0b01001000, 0b10100000, 0b00010000, 0b00010000, 0b00010011, 0b00000000, 0b00000000, // 0b00000000, // LUT0 - Black
        0b01001000, 0b10100000, 0b10000000, 0b00000000, 0b00000011, 0b00000000, 0b00000000, // 0b00000000, // LUTT1 - White
        0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, // 0b00000000, // IGNORE
        0b01001000, 0b10100101, 0b00000000, 0b10111011, 0b00000000, 0b00000000, 0b00000000, // 0b00000000, // LUT3 - Red
        0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000, // 0b00000000, // LUT4 - VCOM
        //0xA5, 0x89, 0x10, 0x10, 0x00, 0x00, 0x00, // LUT0 - Black
        //0xA5, 0x19, 0x80, 0x00, 0x00, 0x00, 0x00, // LUT1 - White
        //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // LUT2 - Red - NADA!
        //0xA5, 0xA9, 0x9B, 0x9B, 0x00, 0x00, 0x00, // LUT3 - Red
        //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // LUT4 - VCOM

        //       Duration              |  Repeat
        //       A     B     C     D   |
        67, 10, 31, 10, 4, // 0 Flash
        16, 8, 4, 4, 6,    // 1 clear
        4, 8, 8, 32, 16,   // 2 bring in the black
        4, 8, 8, 64, 32,   // 3 time for red
        6, 6, 6, 2, 2,     // 4 final black sharpen phase
        0, 0, 0, 0, 0,     // 4
        0, 0, 0, 0, 0,     // 5
        0, 0, 0, 0, 0,     // 6
        0, 0, 0, 0, 0      // 7
    };
    _send_command(0x32, lookup_tables);

    _send_command(0x44, xRamData); // Set RAM X address
    vector<uint8_t> newYRamData{0x00, 0x00, 0xD3, 0x00};
    _send_command(0x45, newYRamData); // Set RAM Y address

    _send_command(0x4e, 0x00); // Set RAM X address counter
    vector<uint8_t> yRamAddressCounter{0x00, 0x00};
    _send_command(0x4f, yRamAddressCounter); // Set RAM Y address counter

    _send_command(0x24, buf_black);

    _send_command(0x44, xRamData); // Set RAM X address
    _send_command(0x45, newYRamData); // Set RAM Y address
    _send_command(0x4e, 0x00); // Set RAM X address counter
    _send_command(0x4f, yRamAddressCounter); // Set RAM Y address counter

    _send_command(0x26, buf_red);

    _send_command(0x22, 0xc7); // Display update setting
    _send_command(0x20); // Display update activate
    usleep(50);
    _busy_wait();

    return 0;
}

// Display finalisation
int InkyPhat::_display_fini()
{
    return 0;
}

InkyPhat::~InkyPhat()
{
    cout << "InkyPhat Destructor" << endl;
}

int InkyPhat::update(vector< vector< uint8_t > > pixels)
{
    _display_init();

    vector<uint8_t> red_buffer;
    vector<uint8_t> black_buffer;

    // For each row, create a single value
    for(vector< vector<uint8_t> >::iterator col_it = pixels.begin(); col_it != pixels.end(); col_it++ )
    {
        int count = 0;
        
        // Start with empty value
        uint8_t redValue = 0;
        uint8_t blackValue = 0;
        
        for (vector<uint8_t>::iterator row_it = (*col_it).begin(); row_it != (*col_it).end(); row_it++)
        {
            uint8_t value = *row_it;
            // If the value equals RED, it's considered TRUE otherwise FALSE
            if(value == RED)
            {
                // If RED, add one and shift left
                redValue <<= 1;
                redValue += 1;
            }
            else
            {
                // If not RED, just shift left
                redValue <<= 1;
            }
            if(value == BLACK)
            {
                // If BLACK, just shift left
                blackValue <<= 1;
            }
            else
            {
                // If not BLACK, shift left and add 1
                blackValue <<= 1;
                blackValue += 1;
            }

            if(++count == 8)
            {
                // Push the bytes into respective vectors
                red_buffer.push_back(redValue);
                black_buffer.push_back(blackValue);

                // Reset the bytes
                redValue = 0;
                blackValue = 0;
                count = 0;
            }
        }
    }

    cout << "BlackBuffer Length:" << black_buffer.size() << endl;
    cout << "RedBuffer Length:" << red_buffer.size() << endl;

    _display_update(black_buffer, red_buffer);

    _display_fini();
    return 0;
}

int InkyPhat::_busy_wait()
{
    //Wait for the e-paper driver to be ready to receive commands/data.
    int wait_for = LOW;
    
    while( digitalRead(busy_pin) != wait_for )
    {
        // Yes this is a tight loop that spins waiting for a pin to change value
    }
    return 0;
}

int InkyPhat::reset()
{
    //Send a reset signal to the e-paper driver.
    digitalWrite(reset_pin, LOW);
    usleep(100);
    digitalWrite(reset_pin, HIGH);
    usleep(100);

    _send_command(_V2_RESET);

    _busy_wait();
    return 0;
}

int InkyPhat::_send_command(uint8_t command)
{
    vector<uint8_t> commandArray{command};
    _spi_write(_SPI_COMMAND, commandArray);
    return 0;
}

int InkyPhat::_send_command(uint8_t command, uint8_t data)
{
    vector<uint8_t> commandArray{command};
    vector<uint8_t> dataArray{data};
    return _send_command(commandArray, dataArray);
}

int InkyPhat::_send_command(uint8_t command, vector<uint8_t> data)
{
    vector<uint8_t> commandArray{command};
    return _send_command(commandArray, data);
}

int InkyPhat::_send_command(vector<uint8_t> command, vector<uint8_t> data)
{
    _spi_write(_SPI_COMMAND, command);
    _send_data(data);
    return 0;
}

int InkyPhat::_send_data(vector<uint8_t> data)
{
    _spi_write(_SPI_DATA, data);
    return 0;
}

int InkyPhat::_spi_write(uint8_t level, vector<uint8_t> data)
{
    digitalWrite(command_pin, level);
    uint8_t arr[data.size()];
    copy(data.begin(), data.end(), arr);
    wiringPiSPIDataRW(CS0_PIN, arr, data.size());
    return 0;
}