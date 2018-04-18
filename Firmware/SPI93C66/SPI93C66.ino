//#include <SPI.h>

/* pin
  10 - CS
  11 - DI
  12 - DO
  13 - CLK
  VCC - ORG (16 bit mode)
*/

void spi_bit_tx ( uint8_t bit )
{
  digitalWrite ( 11, bit ? HIGH : LOW);
  digitalWrite ( 13, HIGH );
  delayMicroseconds ( 10 );

  digitalWrite ( 13, LOW );
  digitalWrite ( 11, 0 );
  delayMicroseconds ( 10 );
}

uint8_t spi_bit_rx ()
{
  uint8_t d;

  digitalWrite ( 13, HIGH );
  delayMicroseconds ( 10 );

  d = digitalRead ( 12 );

  digitalWrite ( 13, LOW );
  delayMicroseconds ( 10 );

  return d ? 1 : 0;
}

uint16_t data_read_bitbang ( uint8_t addr )
{
  int i;
  uint16_t rx = 0;

  digitalWrite ( 10, HIGH );

  spi_bit_tx ( 1 );

  spi_bit_tx ( 1 );
  spi_bit_tx ( 0 );

  for ( i = 7; i >= 0; i-- )
    spi_bit_tx ( (addr >> i ) & 1 );

  for ( i = 0; i < 16; i++ )
  {
    rx <<= 1;
    rx |= spi_bit_rx ();
  }

  digitalWrite ( 10, LOW );

  return rx;
}

/* enable erase/write */
void ewen_set_bitbang ()
{
  int i;

  digitalWrite ( 10, HIGH );

  spi_bit_tx ( 1 );

  spi_bit_tx ( 0 );
  spi_bit_tx ( 0 );

  for ( i = 0; i < 8; i++ )
    spi_bit_tx ( 1 );

  digitalWrite ( 10, LOW );

  delay ( 10 );
}

void data_write_bitbang ( uint8_t addr, uint16_t data )
{
  int i;

  digitalWrite ( 10, HIGH );

  spi_bit_tx ( 1 );

  spi_bit_tx ( 0 );
  spi_bit_tx ( 1 );

  for ( i = 7; i >= 0; i-- )
    spi_bit_tx ( (addr >> i ) & 1 );

  for ( i = 15; i >= 0; i-- )
    spi_bit_tx ( (data >> i ) & 1 );

  digitalWrite ( 10, LOW );

  /* fake clock */
  digitalWrite ( 13, HIGH );
  delayMicroseconds ( 10 );
  digitalWrite ( 13, LOW );
  delayMicroseconds ( 10 );

  digitalWrite ( 10, HIGH );

  /* clock hingga siap */
  digitalWrite ( 13, HIGH );
  delayMicroseconds ( 10 );
  while ( !digitalRead ( 12 ) )
  {
    digitalWrite ( 13, HIGH );
    delayMicroseconds ( 10 );
    digitalWrite ( 13, LOW );
    delayMicroseconds ( 10 );
  }

  digitalWrite ( 10, LOW );

}


void setup ()
{
  pinMode ( 10, OUTPUT ); /* SS */
  pinMode ( 11, OUTPUT ); /* MOSI */
  pinMode ( 13, OUTPUT );       /* SCK */

  /* clock phase rising (CPHA=0), clock idle at GND (CPOL=0) -> SPI_MODE0 */

  Serial.begin ( 57600 );
}
String a = "";

uint16_t d[256];

void loop() {
  while (Serial.available()) {
    a = Serial.readString(); // read the incoming data as string
  }

  if (a != "") {

    switch (a.charAt(0)) {
      case 'w':
        //        Serial.println("Sending data.....");
        ewen_set_bitbang ();
        delay ( 16 );
        for ( int i = 1; i < a.length() - 1; i += 4 )
        {
          char bufferStr [2];
          bufferStr[0] = a.charAt(i);
          bufferStr[1] = a.charAt(i + 1);
          unsigned char highNible = strtoul (bufferStr, NULL, 16);
          bufferStr[0] = a.charAt(i + 2);
          bufferStr[1] = a.charAt(i + 3);
          unsigned char lowNible = strtoul (bufferStr, NULL, 16);
          data_write_bitbang ( (i - 1) / 4, (highNible << 8) | lowNible );
          //Serial.print((i - 1) / 2, HEX);
          //Serial.write ( ':' );
          //Serial.print(highNible, HEX);
          //Serial.write ( '.' );
          //Serial.print(lowNible, HEX);
          //Serial.write ( '.' );
        }
        //Serial.println ( "done" );

        break;

      case 'r':
        for ( int i = 0; i < 128; i++ )
        {
          d[i] = data_read_bitbang ( i );
        }
        for ( int i = 0; i < 128; i++ )
        {
          Serial.print(d[i], HEX);
        }
        break;
    }


    a = "";
  }

}

