// Local headers
#include "libinkyphat.h"
#include<iostream>


InkyPhat *display;
bool initialised = false;

using namespace std;

extern "C"
{
    int init()
    {
        if(initialised)
        {
            #ifdef DEBUG
                cout << "init() called when already initialised" << endl;
            #endif
            return 3;
        }

        // Initialises wiringPi and uses the wiringPi pin numbering scheme
        if(wiringPiSetupGpio() == -1)
        {
            #ifdef DEBUG
                cout << "Failed to initialise WiringPi" << endl;
            #endif

            return 1;
        }
        #ifdef DEBUG
            cout << "WiringPi correctly initialised" << endl;
        #endif

        if(wiringPiSPISetup(CS0_PIN, 488000) == -1)
        {
            #ifdef DEBUG
                cout << "Could not initialise WiringPi SPI library" << endl;
            #endif

            return 2;
        }
        #ifdef DEBUG
            cout << "WiringPi SPI library initialised" << endl;
        #endif

        display = new InkyPhat();

        initialised = true;

        return 0;
    }

    const int valuesSize = WIDTH * HEIGHT; // 104 * 212;

    int draw(uint8_t values[])
    {
        if(!initialised)
        {
            #ifdef DEBUG
                cout << "InkyPhat set_pixels called when uninitialised" << endl;
            #endif
            return 1;
        }

        cout << "ValuesSize:" << valuesSize << endl;

        // // The data is expected as a single array that is 104x212 long
        // int length = sizeof(values) / sizeof(values[0]);
        // if (length != valuesSize)
        // {
        //     #ifdef DEBUG
        //         cout << "Values array passed to set_pixel() was not the correct length:" << length << endl;
        //     #endif
        //     return 2;
        // }

        //All values must be either 0, 1 or 2
        for(int i = 0; i < valuesSize; i++)
        {
            if(values[i] < 0 || values[i] > 2)
            {
                #ifdef DEBUG
                    cout << "Values array contained a value at index [" << i << "] that was {" << unsigned(values[i])
                    << "} which is not 0, 1 or 2" << endl;
                #endif
                return 3;
            }
        }
        
        #ifdef DEBUG
        cout << "InkyPhat set_pixels running" << endl;
        #endif

        // Partition the array into chunks of 104.
        vector< vector<uint8_t> > pixelValues;
        for (int col_index = 0; col_index < valuesSize; col_index += WIDTH)
        {
            vector<uint8_t> vec;
            for(int row_index = col_index; row_index < col_index + WIDTH; row_index++)
            {
                #ifdef DEBUG
                    cout << "Index:" << row_index << endl;
                #endif
                vec.push_back(values[row_index]);
            }
            pixelValues.push_back(vec);
        }

        display->update(pixelValues);
        
        return 0;
    }

    int shutdown()
    {
        if(!initialised)
        {
            #ifdef DEBUG
                cout << "InkyPhat library shutdown called when unitialised. Call init() to initialise." << endl;
            #endif
            return 1;
        }
        #ifdef DEBUG
            cout << "InkyPhat library shutdown. Call init() to re-initialise." << endl;
        #endif

        initialised = false;
        delete display;
        return 0;
    }
}
