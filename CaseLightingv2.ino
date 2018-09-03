#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
  #include <avr/power.h>
#endif

#define PIN 3
#define INPUT_SIZE 30

Adafruit_NeoPixel strip = Adafruit_NeoPixel(9, PIN, NEO_GRB + NEO_KHZ800);

//serial input handling variables
String inputString = "";
bool stringComplete = false;

//storage variables
int sC = 0;
int sI = 0;
int sR = 0;
int sG = 0;
int sB = 0;
int sfilled = false;

void setup() {
  Serial.begin(250000);
  Serial.setTimeout(50);
  inputString.reserve(200);

  strip.begin();
  strip.show(); // Initialize all pixels to 'off'
  for(int i =0; i < strip.numPixels(); i++){
    strip.setPixelColor(i, 255,255,255);
    strip.show();
  }

}


void loop() {
  // Get next command from Serial (add 1 for final 0)
  char input[INPUT_SIZE + 1];
  byte size = Serial.readBytes(input, INPUT_SIZE);
  // Add the final 0 to end the C string
  input[size] = 0;
  
  // Read each command pair 
  char* command = strtok(input, "&");
  while (command != 0)
  {
      
      // Split the command in two values
      char* separator = strchr(command, ':');
      if (separator != 0)
      {
          // Actually split the string in 2: replace ':' with 0
          *separator = 0;
          int inC = atoi(command);
          ++separator;
          int inI = atoi(separator);

          // c = 0 == command
          // c = 1 == lednr
          // c = 2 == r
          // c = 3 == g
          // c = 4 == b

          switch(inC){
            case 0:
              sC = inI;
            break;
            case 1:
              sI = inI;
            break;
            case 2:
              sR = inI;
            break;
            case 3:
              sG = inI;
            break;
            case 4:
              sB = inI;
              sfilled = true;
            break;
          }  
      }
      // Find the next command in input string
      command = strtok(0, "&");
  }

  if(sfilled == true){
    if(sC == 0){
      setAll(sR,sG,sB);
    }
    if(sC == 1){
      setSingle(sI,sR,sG,sB);
    }
    sfilled = false;
  }
}


void setSingle(int i, int r, int g, int b){
   strip.setPixelColor(i, r, g, b);
   strip.show();
}

void setAll(int r, int g, int b){
  for(int i =0; i < strip.numPixels(); i++){
    strip.setPixelColor(i, r, g, b);
    strip.show();
  }
}

