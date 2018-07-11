#ifndef LIBINKYPHAT_H
#define LIBINKYPHAT_H

#include "inkyphat.h"
#include "constants.h"

extern "C"
{
    int init();
    int draw(uint8_t values[]);
    int shutdown();
}

#endif