CC=g++
CXXFLAGS=Wall -g
BLINKT_DIR=Blinkt
BLINKT_DEPS=$(BLINKT_DIR)/clinkt.cpp $(BLINKT_DIR)/pixel.cpp $(BLINKT_DIR)/low_level.cpp
OBJDIR=build/

all: blinkt

blinkt:
	mkdir -p $(OBJDIR)
	$(CC) -$(CXXFLAGS) -std=c++11 $(BLINKT_DEPS) -l bcm2835 -fPIC -shared -o $(OBJDIR)/libblinkt.so

clean:
	rm $(OBJDIR)/*