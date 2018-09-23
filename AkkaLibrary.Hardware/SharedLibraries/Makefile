CC=g++
CXXFLAGS=Wall -g
INKY_DIR=Inky
BLINKT_DIR=Blinkt

#BLINKT_DEPS=$(BLINKT_DIR)/clinkt.cpp $(BLINKT_DIR)/pixel.cpp $(BLINKT_DIR)/low_level.cpp
#BLINKT_DEPS=$(BLINKT_DIR)/clinkt.cpp $(BLINKT_DIR)/pixel.cpp $(BLINKT_DIR)/low_level.cpp

OBJDIR=build/

all: blinkt inkyphat

blinkt: $(BLINKT_DIR)/*cpp
	mkdir -p $(OBJDIR)
	$(CC) -$(CXXFLAGS) -std=c++11 $^ -lbcm2835 -fPIC -shared -o $(OBJDIR)/libblinkt.so

inkyphat: $(INKY_DIR)/*cpp
	mkdir -p $(OBJDIR)
	$(CC) -$(CXXFLAGS) -std=c++11 $^ -lwiringPi -fPIC -shared -o $(OBJDIR)/libinkyphat.so

clean:
	rm $(OBJDIR)/*