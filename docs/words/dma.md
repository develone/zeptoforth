# DMA support

There exists support for the DMA controller on the RP2040. There exist 12 DMA channels, 64 DREQ's/TREQ's, and 4 DMA timers. Register to register, buffer to register, register to buffer, and buffer to buffer transfers are supported; the main difference between registers and buffers is that buffers' addresses are updated with each transfer while registers' addresses are not. Transfer units of size 1 byte, 2 bytes, and 4 bytes are supported. DREQ's and TREQ's are used for synchronizing transfers with other peripherals.

There also exists a global DMA pool to simplify dynamically allocating and freeing DMA channels. It is recommended that one use this rather than manually selecting DMA channels.

### `dma`

The `dma` module contains the following words:

##### `x-out-of-range-dma-channel`
( -- )

Out of range DMA index exception.

##### `x-out-of-range-treq`
( -- )

Out of range transfer request exception.
  
##### `x-out-of-range-timer`
( -- )

Out of range timer exception.
  
##### `x-invalid-transfer-size`
( -- )

Invalid transfer size exception.

##### `x-out-of-range-timer-value`
( -- )

Out of range timer dividend or divisor

##### `DREQ_PIO_TX`
( sm pio -- dreq )

PIO TX DREQ for *pio* block state machine *sm* transmit.

##### `DREQ_PIO_RX`
( sm pio -- dreq )

PIO RX DREQ for *pio* block state machine *sm* receive.

##### `DREQ_SPI_TX`
( spi -- dreq )

SPI TX DREQ for *spi* peripheral transmit.

##### `DREQ_SPI_RX`
( spi -- dreq )

SPI RX DREQ for *spi* peripheral receive.

##### `DREQ_UART_TX`
( uart -- dreq )

UART TX DREQ for *uart* peripheral transmit.

##### `DREQ_UART_RX`
( uart -- dreq )

UART RX DREQ for *uart* peripheral receive.

##### `DREQ_PWM_WRAP`
( pwm -- dreq )

PWM DREQ for *pwm* slice.

##### `DREQ_I2C_TX`
( i2c -- dreq )

I2C TX DREQ for *i2c* peripheral transmit.

##### `DREQ_I2C_RX`
( i2c -- dreq )

I2C RX DREQ for *i2c* peripheral receive.

##### `DREQ_ADC`
( -- dreq )

ADC DREQ.

##### `DREQ_XIP_STREAM`
( -- dreq )

XIP STREAM DREQ.

##### `DREQ_XIP_SSITX`
( -- dreq )

XIP SSITX DREQ (`rp2040` only).

##### `DREQ_XIP_SSIRX`
( -- dreq )

XIP SSIRX DREQ (`rp2040` only).

##### `DREQ_XIP_QMITX`
( -- dreq )

XIP QMITX DREQ (`rp2350` only).

##### `DREQ_XIP_QMIRX`
( -- dreq )

XIP QMIRX DREQ (`rp2350` only).

##### `DREQ_HSTX`
( -- dreq )

HSTX DREQ (`rp2350` only).

##### `DREQ_CORESIGHT`
( -- dreq )

Coresight DREQ (`rp2350` only).

##### `DREQ_SHA256`
( -- dreq )

SHA256 DREQ (`rp2350` only).

##### `TREQ_TIMER`
( timer -- treq )

DMA timer *timer*, from 0 to 3, as TREQ.

##### `TREQ_UNPACED`
( -- treq )

Unpaced transfer TREQ.

##### `TRANS_COUNT_MODE_NORMAL`
( count -- count' )

Set a transfer count to be normal (`rp2350` only).

##### `TRANS_COUNT_MODE_TRIGGER_SELF`
( count -- count' )

Set a transfer count to be trigger-self (`rp2350` only).

##### `TRANS_COUNT_MODE_ENDLESS`
( count -- count' )

Set a transfer count to be endless (`rp2350` only).

##### `REGISTER_READ`
( -- mode )

Register source mode

##### `REGISTER_WRITE`
( -- mode )

Register destination mode

##### `INCR_BUFFER_READ`
( -- mode )

Incrementing buffer source mode

##### `INCR_BUFFER_WRITE`
( -- mode )

Incrementing buffer destination mode

##### `DECR_BUFFER_READ`
( -- mode )

Decrementing buffer source mode (`rp2350` only)

##### `DECR_BUFFER_WRITE`
( -- mode )

Decrementing buffer destination mode (`rp2350` only)

##### `INCR_RING_BUFFER_READ`
( low-ring-bits -- mode )

Incrementing ring buffer source mode for a given number of low bits in an address

##### `INCR_RING_BUFFER_WRITE`
( low-ring-bits -- mode )

Incrementing ring buffer destination mode for a given number of low bits in an addres

##### `DECR_RING_BUFFER_READ`
( low-ring-bits -- mode )

Decrementing ring buffer source mode for a given number of low bits in an address (`rp2350` only)

##### `DECR_RING_BUFFER_WRITE`
( low-ring-bits -- mode )

Decrementing ring buffer destination mode for a given number of low bits in an addres (`rp2350` only)

##### `start-dma`
( src dest src-mode dest-mode count size treq channel -- )

Start transfer of *count* units of *size* bytes on DMA *channel* with starting source address *src* and starting destination address *dest* with source mode *src-mode* and destination mode *dest-mode* synchronized by DREQ/TREQ *treq*.

##### `prepare-dma`
( src dest src-mode dest-mode count size treq channel -- )

Prepare transfer of *count* units of *size* bytes on DMA *channel* with starting source address *src* and starting destination address *dest* with source mode *src-mode* and destination mode *dest-mode* synchronized by DREQ/TREQ *treq*.

##### `start-dma-with-chain`
( chain-to src dest src-mode dest-mode count size treq channel -- )

Start transfer of *count* units of *size* bytes on DMA *channel* with starting source address *src* and starting destination address *dest* with source mode *src-mode* and destination mode *dest-mode* synchronized by DREQ/TREQ *treq* chained to DMA channel *chain-to*.

##### `prepare-dma-with-chain`
( chain-to src dest src-mode dest-mode count size treq channel -- )

Prepare transfer of *count* units of *size* bytes on DMA *channel* with starting source address *src* and starting destination address *dest* with source mode *src-mode* and destination mode *dest-mode* synchronized by DREQ/TREQ *treq* chained to DMA channel *chain-to*.

##### `start-register>register-dma`
( src dest count size treq channel -- )

Start register to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `start-register>buffer-dma`
( src dest count size treq channel -- )

Start register to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `start-buffer>register-dma`
( src dest count size treq channel -- )

Start buffer to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `start-buffer>buffer-dma`
( src dest count size treq channel -- )

Start buffer to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `prepare-register>register-dma`
( src dest count size treq channel -- )

Prepare without starting register to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `prepare-register>buffer-dma`
( src dest count size treq channel -- )

Prepare without starting register to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `prepare-buffer>register-dma`
( src dest count size treq channel -- )

Prepare without starting buffer to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `prepare-buffer>buffer-dma`
( src dest count size treq channel -- )

Prepare without starting buffer to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq*.

##### `start-register>register-dma-with-chain`
( chain-to src dest count size treq channel -- )

Start register to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `start-register>buffer-dma-with-chain`
( chain-to src dest count size treq channel -- )

Start register to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `start-buffer>register-dma-with-chain`
( chain-to src dest count size treq channel -- )

Start buffer to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `start-buffer>buffer-dma-with-chain`
( chain-to src dest count size treq channel -- )

Start buffer to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `prepare-register>register-dma-with-chain`
( chain-to src dest count size treq channel -- )

Prepare without starting register to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `prepare-register>buffer-dma-with-chain`
( chain-to src dest count size treq channel -- )

Prepare without starting register to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `prepare-buffer>register-dma-with-chain`
( chain-to src dest count size treq channel -- )

Prepare without starting buffer to register transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `prepare-buffer>buffer-dma-with-chain`
( chain-to src dest count size treq channel -- )

Prepare without starting buffer to buffer transfer of *count* units of *size* bytes on DMA *channel* with source address *src* and destination address *dest* synchronized by DREQ/TREQ *treq* with chaining to DMA channel *chain-to*. Note that if *channel* is the same as *chain-to* no chaining will take place.

##### `dma-timer!`
( dividend divisor timer -- )

Set DMA *timer* clock *dividend* and *divisor*.

##### `spin-wait-dma`
( channel -- )

Spin wait for DMA *channel* completion

##### `wait-dma`
( channel -- )

Non-busy wait for DMA *channel* completion

##### `halt-dma`
( channel -- )

Halt DMA *channel*

##### `abort-dma`
( channel -- )

Abort DMA *channel*

##### `dma-src-addr@`
( channel -- addr )

Get DMA *channel* source address

##### `dma-dest-addr@`
( channel -- addr )

Get DMA *channel* destination address

##### `dma-remaining@`
( channel -- remaining )

Get outstanding bytes transferred

### `dma-pool`

The `dma-pool` module contains the following words:

##### `x-no-dma-channels-available`
( -- )

No DMA channels available exception.

##### `x-dma-channel-already-free`
( -- )

DMA channel is already free.

##### `x-no-dma-timers-available`
( -- )

No DMA timers available exception.

##### `x-dma-timer-already-free`
( -- )

DMA timer is already free.

##### `allocate-dma`
( -- channel )

Allocate a DMA channel.

##### `free-dma`
( channel -- )

Free a DMA channel

##### `allocate-timer`
( -- timer )

Allocate a DMA timer.

##### `free-timer`
( timer -- )

Free a DMA timer
