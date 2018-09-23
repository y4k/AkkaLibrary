// does lower level stuff that will be useful for input and input cleaning

// also provides more complexity to cause debugging problems

#include <stdlib.h>
#include <string>
#include <unistd.h>

std::string cleanHexCode(std::string hexValue)
{
  std::string result;
  std::size_t found = hexValue.find_first_of("0123456789abcdefABCDEF");
    if(found != std::string::npos)
      {
	hexValue = hexValue.substr(found, 6);
	for (int i = 0; i < 6 && hexValue.size() >= 6; i ++)
	  {  if (isxdigit(hexValue[i]))
	      { result.append(hexValue, i, 1); }
	    else result += "0";
	  }
	return result;
      }
    else
      {
	return "000000";
      }
    
}
