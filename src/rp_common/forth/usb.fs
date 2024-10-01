\ Copyright (c) 2023-2024 Travis Bemann
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

compile-to-flash

begin-module usb

  armv6m import
  interrupt import
  task import
  core-lock import
  console import

  \ Are USB special characters enabled
  variable usb-special-enabled
  
  begin-module usb-internal

    \ Invalid packet size exception
    : x-invalid-pkt-size ( -- ) ." invalid USB packet size" cr ;

    \ Convert byte endianness of a word
    : rev ( x -- x' ) [inlined] code[ tos tos rev_,_ ]code ;

    \ Convert halfword endianness of a word
    : rev16 ( x -- x' ) [inlined] code[ tos tos rev16_,_ ]code ;

    \ Write a halfword to the dictionary without concern for endianness
    : 2c, ( h -- ) dup c, 8 rshift c, ;

    \ Saved reboot hook
    variable saved-reboot-hook
    
    \ USB IRQ index
    rp2040? [if]
      5 constant usbctrl-irq
    [then]
    rp2350? [if]
      14 constant usbctrl-irq
    [then]

    \ USB vector
    usbctrl-irq 16 + constant usbctrl-vector

    \ USB registers base address
    $50110000 constant USB_Base

    \ USBCTRL reset bit
    rp2040? [if]
      24 bit constant RESETS_USBCTRL
    [then]
    rp2350? [if]
      28 bit constant RESETS_USBCTRL
    [then]

    \ Reset base
    rp2040? [if]
      $4000C000 constant RESETS_BASE
    [then]
    rp2350? [if]
      $40020000 constant RESETS_BASE
    [then]

    \ Set reset
    RESETS_BASE 2 12 lshift or constant RESETS_RESET_Set

    \ Clear reset
    RESETS_BASE 3 12 lshift or constant RESETS_RESET_Clr

    \ Reset done
    RESETS_BASE 8 + constant RESETS_RESET_DONE

    \ Reset USB peripheral
    : reset-usb ( -- )
      RESETS_USBCTRL RESETS_RESET_Set !
      RESETS_USBCTRL RESETS_RESET_Clr !
      begin RESETS_RESET_DONE @ not RESETS_USBCTRL and while repeat
    ;

    \ USB device address and endpoint control register
    USB_Base $00 + constant USB_ADDR_ENDP

    \ Main control register
    USB_Base $40 + constant USB_MAIN_CTRL

    \ SIE control register
    USB_Base $4C + constant USB_SIE_CTRL

    \ SIE status register
    USB_Base $50 + constant USB_SIE_STATUS

    \ Interrupt endpoint control register
    USB_Base $54 + constant USB_INT_EP_CTRL

    \ Buffer status register
    USB_Base $58 + constant USB_BUFF_STATUS

    \ USB endpoint abort register
    USB_Base $60 + constant USB_EP_ABORT

    \ USB endpoint abort done register
    USB_Base $64 + constant USB_EP_ABORT_DONE
    
    \ Stall armed register
    USB_Base $68 + constant USB_EP_STALL_ARM

    \ Where to connect the USB controller
    USB_Base $74 + constant USB_USB_MUXING

    \ Overrides for the power signals in the event that the VBUS signals are
    \ not hooked up to GPIO
    USB_Base $78 + constant USB_USB_PWR

    \ Interupt enable
    USB_Base $90 + constant USB_INTE

    \ Interrupt status after masking and forcing
    USB_Base $98 + constant USB_INTS

    \ Enable controller
    0 bit constant USB_MAIN_CTRL_CONTROLLER_EN

    \ Set bit in BUFF_STATUS for every buffer completed on EP0
    29 bit constant USB_SIE_CTRL_EP0_INT_1BUF

    \ Enable pull up resistor
    16 bit constant USB_SIE_CTRL_PULLUP_EN

    \ Bus reset received clear bit
    19 bit constant USB_SIE_STATUS_BUS_RESET
    
    \ Setup packet received clear bit
    17 bit constant USB_SIE_STATUS_SETUP_REC

    \ Data sequence error bit
    31 bit constant USB_SIE_STATUS_DATA_SEQ_ERROR

    \ Receive timeout bit
    27 bit constant USB_SIE_STATUS_RX_TIMEOUT

    \ Bit stuff error bit
    25 bit constant USB_SIE_STATUS_BIT_STUFF_ERROR

    \ CRC error bit
    24 bit constant USB_SIE_STATUS_CRC_ERROR

    \ Enable interrupt endpoint bit
    : USB_INT_EP_CTRL_INT_EP_ACTIVE ( endpoint -- ) [inlined] bit ;
    
    \ Buffer status endpoint in bit
    : USB_BUFF_STATUS_EP_IN ( endpoint -- ) [inlined] 1 lshift bit ;

    \ Buffer status endpoint out bit
    : USB_BUFF_STATUS_EP_OUT ( endpoint -- ) [inlined] 1 lshift 1+ bit ;

    \ USB muxing bits
    0 bit constant USB_USB_MUXING_TO_PHY
    3 bit constant USB_USB_MUXING_SOFTCON

    \ USB power signal override bits
    2 bit constant USB_USB_PWR_VBUS_DETECT
    3 bit constant USB_USB_PWR_VBUS_DETECT_OVERRIDE_EN

    \ USB interrupt bits
    4 bit constant USB_INTS_BUFF_STATUS
    12 bit constant USB_INTS_BUS_RESET
    16 bit constant USB_INTS_SETUP_REQ
    
    \ USB DPRAM base
    $50100000 constant USB_DPRAM_Base

    \ USB setup packet address
    USB_DPRAM_Base constant USB_SETUP_PACKET

    \ USB endpoint in endpoint control
    : USB_EP_IN_ENDPOINT_CONTROL ( endpoint -- addr )
      3 lshift USB_DPRAM_Base +
    ;

    \ USB endpoint out endpoint control
    : USB_EP_OUT_ENDPOINT_CONTROL ( endpoint -- addr )
      3 lshift [ USB_DPRAM_Base cell+ ] literal +
    ;

    \ USB endpoint in buffer control
    : USB_EP_IN_BUFFER_CONTROL ( endpoint -- addr )
      3 lshift [ USB_DPRAM_Base $80 + ] literal +
    ;

    \ USB endpoint out buffer control
    : USB_EP_OUT_BUFFER_CONTROL ( endpoint -- addr )
      3 lshift [ USB_DPRAM_Base $80 + cell+ ] literal +
    ;

    \ USB endpoint 0 in/out buffer
    USB_DPRAM_Base $100 + constant USB_EP0_BUFFER

    \ USB data buffer base
    USB_DPRAM_Base $180 + constant USB_BUFFER_Base

    \ Endpoint control bits
    
    \ USB endpoint enable
    31 bit constant USB_EP_ENABLE

    \ USB double-buffered
    30 bit constant USB_EP_DOUBLE_BUFFER

    \ USB enable interrupt for every transferred buffer
    29 bit constant USB_EP_ENABLE_INTERRUPT_PER_BUFFER

    \ USB enable interrupt for every 2 transferred buffers (double-buffer only)
    28 bit constant USB_EP_ENABLE_INTERRUPT_PER_2_BUFFERS

    \ USB endpoint type bitshift
    26 constant USB_EP_ENDPOINT_TYPE_LSB

    \ USB endpoint interrupt on stall
    17 bit constant USB_EP_INTERRUPT_ON_STALL

    \ USB endpoint interrupt on NAK
    16 bit constant USB_EP_INTERRUPT_ON_NAK

    \ USB endpoint data buffer address bitshift
    0 constant USB_EP_ADDRESS_BASE_OFFSET_LSB

    \ Buffer control bits

    \ USB buffer 0 full bit
    15 bit constant USB_BUF_CTRL_FULL

    \ USB last buffer of transfer for buffer 0
    14 bit constant USB_BUF_CTRL_LAST

    \ USB buffer data pid 1
    13 bit constant USB_BUF_CTRL_DATA1_PID

    \ USB buffer data pid 0
    0 constant USB_BUF_CTRL_DATA0_PID

    \ USB reset buffer select to buffer 0
    12 bit constant USB_BUF_CTRL_SEL

    \ USB buffer stall
    11 bit constant USB_BUF_CTRL_STALL

    \ USB buffer is available for transfer
    10 bit constant USB_BUF_CTRL_AVAIL

    \ USB buffer 0 transfer length
    0 constant USB_BUF_CTRL_LEN

    \ USB buffer 0 transfer length mask
    $3FF constant USB_BUF_CTRL_LEN_MASK
    
    \ USB dual-ported RAM size
    4096 constant dpram-size

    \ Descriptor types
    1 constant USB_DT_DEVICE
    2 constant USB_DT_CONFIG
    3 constant USB_DT_STRING
    4 constant USB_DT_INTERFACE
    5 constant USB_DT_ENDPOINT

    \ Requests
    0 constant USB_REQUEST_GET_STATUS
    1 constant USB_REQUEST_CLEAR_FEATURE
    3 constant USB_REQUEST_SET_FEATURE
    5 constant USB_REQUEST_SET_ADDRESS
    6 constant USB_REQUEST_GET_DESCRIPTOR
    7 constant USB_REQUEST_SET_DESCRIPTOR
    8 constant USB_REQUEST_GET_CONFIGURATION
    9 constant USB_REQUEST_SET_CONFIGURATION
    10 constant USB_REQUEST_GET_INTERFACE
    11 constant USB_REQUEST_SET_INTERFACE
    12 constant USB_REQUEST_SYNC_FRAME
    $22 constant USB_REQUEST_SET_LINE

    \ Endpoint types
    0 constant USB_EP_TYPE_CONTROL
    1 constant USB_EP_TYPE_ISO
    2 constant USB_EP_TYPE_BULK
    3 constant USB_EP_TYPE_INTERRUPT

    \ Configuration attributes
    7 bit constant USB_CONFIG_BUS_POWERED
    6 bit constant USB_CONFIG_SELF_POWERED

    \ Configuration directions
    $00 constant USB_DIR_OUT
    $80 constant USB_DIR_IN
    
    \ Endpoint out address
    : USB_EP_OUT_ADDR ( u -- addr ) ;

    \ Endpoint in address
    : USB_EP_IN_ADDR ( u -- addr ) 128 + ;

    \ Device descriptor data
    create device-data
    
    \ Device descriptor
    \ Descriptor length
    18 c,
    \ Descriptor type
    USB_DT_DEVICE c,
    \ USB specification number
    $0200 2c,
    \ Device class
    2 c,
    \ Device subclass
    0 c,
    \ Device protocol
    0 c,
    \ Maximum packet size
    64 c,
    \ Vendor ID
    $0483 2c,
    \ Product ID
    $5740 2c,
    \ Device release number
    $0200 2c,
    \ Manufacturer string descriptor index (0 = none)
    0 c,
    \ Product string descriptor index (0 = none)
    0 c,
    \ Serial number string descriptor index (0 = none)
    0 c,
    \ Number of possible configurations
    1 c,
    
    here device-data - cell align, constant device-data-size

    \ Configuration data
    create config-data

    \ Configuration descriptor
    \ Descriptor length
    9 c,
    \ Descriptor type
    USB_DT_CONFIG c,
    \ Total length of all descriptors
    67 2c,
    \ Number of interfaces
    2 c,
    \ Configuration index
    1 c,
    \ Configuration string descriptor index (0 = none)
    0 c,
    \ Attribute bitmap
    USB_CONFIG_BUS_POWERED USB_CONFIG_SELF_POWERED or c,
    \ Maximum power in steps of 2 mA
    100 2 / c,

    \ Interface descriptor
    \ Descriptor length
    9 c,
    \ Descriptor type
    USB_DT_INTERFACE c,
    \ Interface number
    0 c,
    \ Alternate setting
    0 c,
    \ Number of endpoints
    1 c,
    \ Class code
    2 c,
    \ Subclass code
    2 c,
    \ Protocol code
    0 c,
    \ Interface string descriptor index (0 = none)
    0 c,

    \ Header functional
    5 c, 36 c, 0 c, 16 c, 1 c,
    \ Call mangaement functional
    5 c, 36 c, 1 c, 0 c, 1 c,
    \ ACM functional
    4 c, 36 c, 2 c, 2 c,
    \ Union functional
    5 c, 36 c, 6 c, 0 c, 1 c,

    \ Endpoint descriptor
    \ Descriptor length
    7 c,
    \ Descriptor type
    USB_DT_ENDPOINT c,
    \ Endpoint address
    2 USB_EP_IN_ADDR c,
    \ Attributes
    USB_EP_TYPE_INTERRUPT c,
    \ Maximum packet size
    8 2c,
    \ Polling interval in ms (slow?)
    255 c,

    \ Interface descriptor
    \ Descriptor length
    9 c,
    \ Descriptor type
    USB_DT_INTERFACE c,
    \ Interface number
    1 c,
    \ Alternate setting
    0 c,
    \ Number of endpoints
    2 c,
    \ Class code
    10 c,
    \ Subclass code
    0 c,
    \ Protocol code
    0 c,
    \ Interface string descriptor (0 = none)
    0 c,

    \ Endpoint descriptor
    \ Descriptor length
    7 c,
    \ Descriptor type
    USB_DT_ENDPOINT c,
    \ Endpoint address
    1 USB_EP_OUT_ADDR c,
    \ Attributes
    USB_EP_TYPE_BULK c,
    \ Maximum packet size
    64 2c,
    \ Polling interval (ignored)
    0 c,

    \ Endpoint descriptor
    \ Descriptor length
    7 c,
    \ Descriptor type
    USB_DT_ENDPOINT c,
    \ Endpoint address
    1 USB_EP_IN_ADDR c,
    \ Attributes
    USB_EP_TYPE_BULK c,
    \ Maximum packet size
    64 2c,
    \ Polling interval
    0 c,

    here config-data - cell align, constant config-data-size

    \ Global device address
    variable usb-device-addr

    \ Device address should be set
    variable set-usb-device-addr

    \ Current DTR
    variable usb-dtr?

    \ Device is configured
    variable usb-device-configd?

    \ Prepare for device to be configured
    variable prepare-usb-device-configd?
    
    \ Basic endpoint control structure
    begin-structure basic-endpoint-size
      
      \ Endpoint buffer address
      field: endpoint-buffer

      \ Endpoint control register address
      field: endpoint-endpoint-control

      \ Endpoint buffer control register address
      field: endpoint-buffer-control

      \ Endpoint next PID
      field: endpoint-next-pid

      \ Endpoint maximum packet size
      field: endpoint-max-packet-size

      \ Is endpoint transmit
      field: endpoint-tx?

    end-structure

    \ Endpoint control structure
    begin-structure endpoint-size

      basic-endpoint-size +field endpoint-shared-info

      \ Endpoint tranfer data
      field: endpoint-data-buffer

      \ Endpoint transfer length
      field: endpoint-data-len

      \ Endpoint total length
      field: endpoint-data-total-len
      
    end-structure

    \ Setup packet structure
    begin-structure setup-pkt-size

      \ Setup packet request type
      cfield: setup-pkt-request-type

      \ Setup packet request
      cfield: setup-pkt-request

      \ Setup packet value
      hfield: setup-pkt-value

      \ Setup packet index
      hfield: setup-pkt-index

      \ Setup packet length
      hfield: setup-pkt-length
      
    end-structure

    \ Endpoint 0 out
    endpoint-size buffer: endpoint0-out

    \ Endpoint 0 in
    endpoint-size buffer: endpoint0-in

    \ Endpoint 1 out
    basic-endpoint-size buffer: endpoint1-out

    \ Endpoint 1 in
    basic-endpoint-size buffer: endpoint1-in

    \ Endpoint 2 in
    endpoint-size buffer: endpoint2-in
    
    \ USB handled interrupts
    variable usb-handled-interrupts

    \ Endpoint 1 out ready
    variable endpoint1-out-ready?

    \ Endpoint 1 in ready
    variable endpoint1-in-ready?

    \ Endpoint 1 in refill
    variable endpoint1-in-refill?

    \ USB receive pending operation
    pending-op-size buffer: usb-rx-pending-op

    \ USB transmit pending operation
    pending-op-size buffer: usb-tx-pending-op

    \ USB receiving pending operation enabled
    variable usb-rx-pending-op-enabled?

    \ USB pending operation priority
    0 constant usb-pending-op-priority

    \ USB out core lock
    core-lock-size buffer: usb-out-core-lock

    \ USB in core lock
    core-lock-size buffer: usb-in-core-lock

    \ Control-C
    $03 constant ctrl-c

    \ Control-T
    $14 constant ctrl-t
    
    \ RAM variable for rx buffer read-index
    variable rx-read-index

    \ RAM variable for rx buffer write-index
    variable rx-write-index

    \ Constant for number of bytes to buffer
    128 constant rx-buffer-size

    \ Rx buffer
    rx-buffer-size buffer: rx-buffer

    \ RAM variable for tx buffer read-index
    variable tx-read-index

    \ RAM variable for tx buffer write-index
    variable tx-write-index

    \ Constant for number of bytes to buffer
    128 constant tx-buffer-size

    \ Tx buffer
    tx-buffer-size buffer: tx-buffer
    
    \ Get whether the rx buffer is full
    : rx-full? ( -- f )
      rx-read-index @ 1- $7F and rx-write-index @ =
    ;

    \ Get whether the rx buffer is empty
    : rx-empty? ( -- f )
      rx-read-index @ rx-write-index @ =
    ;

    \ Get number of bytes available in the rx buffer
    : rx-count ( -- u )
      rx-read-index @ { read-index }
      rx-write-index @ { write-index }
      read-index write-index <= if
        write-index read-index -
      else
        rx-buffer-size read-index - write-index +
      then
    ;

    \ Write a byte to the rx buffer
    : write-rx ( c -- )
      rx-full? not if
        rx-write-index @ rx-buffer + c!
        rx-write-index @ 1+ $7F and rx-write-index !
      else
        drop
      then
    ;

    \ Read a byte from the rx buffer
    : read-rx ( -- c )
      rx-empty? not if
        rx-read-index @ rx-buffer + c@
        rx-read-index @ 1+ $7F and rx-read-index !
      else
        0
      then
    ;

    \ Get whether the tx buffer is full
    : tx-full? ( -- f )
      tx-read-index @ 1- $7F and tx-write-index @ =
    ;

    \ Get whether the tx buffer is empty
    : tx-empty? ( -- f )
      tx-read-index @ tx-write-index @ =
    ;

    \ Get number of bytes available in the tx buffer
    : tx-count ( -- u )
      tx-read-index @ { read-index }
      tx-write-index @ { write-index }
      read-index write-index <= if
        write-index read-index -
      else
        tx-buffer-size read-index - write-index +
      then
    ;

    \ Write a byte to the tx buffer
    : write-tx ( c -- )
      tx-full? not if
        tx-write-index @ tx-buffer + c!
        tx-write-index @ 1+ $7F and tx-write-index !
      else
        drop
      then
    ;

    \ Read a byte from the tx buffer
    : read-tx ( -- c )
      tx-empty? not if
        tx-read-index @ tx-buffer + c@
        tx-read-index @ 1+ $7F and tx-read-index !
      else
        0
      then
    ;

    \ Get the offset in USB dual-ported RAM
    : usb-buffer-offset ( addr -- addr' ) USB_DPRAM_Base - ;

    \ Initialize a USB endpoint
    : init-usb-endpoint0
      { buffer-ctrl max-pkt-size tx? endpoint -- }
      0 endpoint endpoint-endpoint-control !
      buffer-ctrl endpoint endpoint-buffer-control !
      1 endpoint endpoint-next-pid !
      max-pkt-size endpoint endpoint-max-packet-size !
      0 buffer-ctrl !
      USB_EP0_BUFFER endpoint endpoint-buffer !
      0 endpoint endpoint-data-buffer !
      0 endpoint endpoint-data-len !
      0 endpoint endpoint-data-total-len !
      tx? endpoint endpoint-tx? !
    ;

    \ Initialize a console USB endpoint
    : init-usb-console-endpoint
      { ep-ctrl buffer-ctrl data-buf max-pkt-size ep-type tx? endpoint -- }
      ep-ctrl endpoint endpoint-endpoint-control !
      buffer-ctrl endpoint endpoint-buffer-control !
      0 endpoint endpoint-next-pid !
      max-pkt-size endpoint endpoint-max-packet-size !
      data-buf endpoint endpoint-buffer !
      0 buffer-ctrl !
      [ USB_EP_ENABLE USB_EP_ENABLE_INTERRUPT_PER_BUFFER or ] literal
      ep-type USB_EP_ENDPOINT_TYPE_LSB lshift or
      data-buf usb-buffer-offset or
      ep-ctrl !
      tx? endpoint endpoint-tx? !
    ;

    \ Initialize a USB endpoint
    : init-usb-endpoint
      { ep-ctrl buffer-ctrl data-buf max-pkt-size ep-type tx? endpoint -- }
      ep-ctrl endpoint endpoint-endpoint-control !
      buffer-ctrl endpoint endpoint-buffer-control !
      0 endpoint endpoint-next-pid !
      max-pkt-size endpoint endpoint-max-packet-size !
      data-buf endpoint endpoint-buffer !
      0 endpoint endpoint-data-buffer !
      0 endpoint endpoint-data-len !
      0 endpoint endpoint-data-total-len !
      0 buffer-ctrl !
      [ USB_EP_ENABLE USB_EP_ENABLE_INTERRUPT_PER_BUFFER or ] literal
      ep-type USB_EP_ENDPOINT_TYPE_LSB lshift or
      data-buf usb-buffer-offset or
      ep-ctrl !
      tx? endpoint endpoint-tx? !
    ;

    \ Initialize USB endpoints
    : init-usb-endpoints ( -- )
      0 USB_EP_IN_BUFFER_CONTROL 64 true endpoint0-in init-usb-endpoint0
      0 USB_EP_OUT_BUFFER_CONTROL 8 false endpoint0-out init-usb-endpoint0
      1 USB_EP_IN_ENDPOINT_CONTROL 1 USB_EP_IN_BUFFER_CONTROL
      USB_BUFFER_Base $00 + 64 USB_EP_TYPE_BULK true endpoint1-in
      init-usb-console-endpoint
      1 USB_EP_OUT_ENDPOINT_CONTROL 1 USB_EP_OUT_BUFFER_CONTROL
      USB_BUFFER_Base $40 + 64 USB_EP_TYPE_BULK false endpoint1-out
      init-usb-console-endpoint
      2 USB_EP_IN_ENDPOINT_CONTROL 2 USB_EP_IN_BUFFER_CONTROL
      USB_BUFFER_Base $80 + 8 USB_EP_TYPE_INTERRUPT true endpoint2-in
      init-usb-endpoint
    ;

    \ Close USB endpoint
    : close-usb-endpoint { endpoint -- }
      endpoint endpoint-endpoint-control @ ?dup if 0 swap ! then
      endpoint endpoint-buffer-control @ ?dup if 0 swap ! then
    ;

    \ Send empty reply to endpoint1-in
    : empty-endpoint-reply { endpoint -- }
      USB_BUF_CTRL_SEL USB_BUF_CTRL_FULL or USB_BUF_CTRL_LAST or
      endpoint endpoint-next-pid @ if
        USB_BUF_CTRL_DATA1_PID or
      else
        USB_BUF_CTRL_DATA0_PID or
      then
      dup endpoint endpoint-buffer-control @ !
      code[ b> >mark b> >mark b> >mark b> >mark b> >mark b> >mark ]code
      USB_BUF_CTRL_AVAIL or endpoint endpoint-buffer-control @ !
    ;

    \ Close USB endpoints
    : close-usb-endpoints ( -- )
      $1F USB_EP_ABORT !
      begin USB_EP_ABORT_DONE @ $1F and $1F = until
      endpoint1-in-ready? @ if endpoint1-in empty-endpoint-reply then
      endpoint1-out-ready? @ if endpoint1-out empty-endpoint-reply then
      endpoint2-in close-usb-endpoint
      endpoint1-out close-usb-endpoint
      endpoint1-in close-usb-endpoint
      endpoint0-out close-usb-endpoint
      endpoint0-in close-usb-endpoint
    ;

    \ Get console endpoint data available
    : usb-console-count { endpoint -- count }
      endpoint endpoint-tx? @ if tx-count else rx-buffer-size rx-count - then
    ;

    \ Update buffer control
    : usb-update-buffer-control { buffer-control-val endpoint -- }
      endpoint endpoint-buffer-control @ { buffer-control }
      buffer-control-val USB_BUF_CTRL_AVAIL and if
        buffer-control-val USB_BUF_CTRL_AVAIL bic buffer-control !
        code[ b> >mark b> >mark b> >mark b> >mark b> >mark b> >mark ]code
      then
      buffer-control-val buffer-control !
    ;
    
    \ Carry out next transfer for console IO
    : usb-console-continue-transfer { endpoint -- }
      endpoint usb-console-count endpoint endpoint-max-packet-size @
      min { bytes }
      endpoint endpoint-tx? @ bytes 0<> or if
        bytes [ USB_BUF_CTRL_AVAIL USB_BUF_CTRL_SEL or ] literal or
        { buffer-control-val }
        endpoint endpoint-tx? @ if
          endpoint endpoint-buffer @ bytes over + swap ?do
            read-tx i c!
          loop
          buffer-control-val USB_BUF_CTRL_FULL or to buffer-control-val
        then
        endpoint endpoint-next-pid @ if
          USB_BUF_CTRL_DATA1_PID
        else
          USB_BUF_CTRL_DATA0_PID
        then
        buffer-control-val or to buffer-control-val
        USB_BUF_CTRL_LAST buffer-control-val or to buffer-control-val
        endpoint endpoint-next-pid @ 1 xor endpoint endpoint-next-pid !
        buffer-control-val endpoint usb-update-buffer-control
      then
    ;

    \ Receive data for console IO
    : usb-console-rx ( endpoint -- )
      [: { endpoint }
        endpoint endpoint-buffer-control @ @ { buffer-control-val }
        [ USB_SIE_STATUS_BIT_STUFF_ERROR
        USB_SIE_STATUS_CRC_ERROR or
        ] literal USB_SIE_STATUS bit@ not if
          buffer-control-val USB_BUF_CTRL_FULL and if
            buffer-control-val USB_BUF_CTRL_LEN_MASK and { bytes }
            endpoint endpoint-buffer @ bytes over + swap ?do
              i c@
              usb-special-enabled @ if
                dup ctrl-c = if
                  drop reboot
                else
                  attention? @ if
                    usb-out-core-lock release-core-lock
                    [: attention-hook @ execute ;] try
                    usb-out-core-lock claim-core-lock
                    ?raise
                  else
                    dup ctrl-t = if
                      usb-out-core-lock release-core-lock
                      drop [: attention-start-hook @ execute ;] try
                      usb-out-core-lock claim-core-lock
                      ?raise
                    else
                      write-rx
                    then
                  then
                then
              else
                write-rx
              then
            loop
          then
        then
        USB_SIE_STATUS @ [ USB_SIE_STATUS_DATA_SEQ_ERROR
        USB_SIE_STATUS_RX_TIMEOUT or
        USB_SIE_STATUS_BIT_STUFF_ERROR or
        USB_SIE_STATUS_CRC_ERROR or ] literal and USB_SIE_STATUS !
      ;] usb-out-core-lock with-core-lock
    ;

    \ Start a USB transfer for console IO
    : usb-console-start-transfer { endpoint -- }
      endpoint endpoint-tx? @ if usb-in-core-lock else usb-out-core-lock then
      endpoint [: { endpoint }
        endpoint usb-console-continue-transfer
      ;] rot with-core-lock
    ;

    \ Carry out next transfer
    : usb-continue-transfer { endpoint -- }
      endpoint endpoint-data-len @ endpoint endpoint-max-packet-size @
      min { bytes }
      bytes [ USB_BUF_CTRL_AVAIL USB_BUF_CTRL_SEL or ] literal or
      { buffer-control-val }
      endpoint endpoint-tx? @ if
        endpoint endpoint-data-buffer @ if
          endpoint endpoint-data-buffer @
          endpoint endpoint-buffer @ bytes move
          bytes endpoint endpoint-data-buffer +!
        then
        buffer-control-val USB_BUF_CTRL_FULL or to buffer-control-val
        bytes negate endpoint endpoint-data-len +!
      then
      endpoint endpoint-next-pid @ if
        USB_BUF_CTRL_DATA1_PID
      else
        USB_BUF_CTRL_DATA0_PID
      then
      buffer-control-val or to buffer-control-val
      endpoint endpoint-data-len @ 0= if
        USB_BUF_CTRL_LAST buffer-control-val or to buffer-control-val
      then
      endpoint endpoint-next-pid @ 1 xor endpoint endpoint-next-pid !
      buffer-control-val endpoint usb-update-buffer-control
    ;

    \ Receive data
    : usb-rx { endpoint -- }
      endpoint endpoint-buffer-control @ @ { buffer-control-val }
      buffer-control-val USB_BUF_CTRL_LEN_MASK and { bytes }
      endpoint endpoint-tx? @ not if
        endpoint endpoint-data-buffer @ if
          endpoint endpoint-buffer @
          endpoint endpoint-data-buffer @
          bytes move
          bytes endpoint endpoint-data-buffer +!
        then
        bytes endpoint endpoint-data-total-len +!
        bytes negate endpoint endpoint-data-len +!
      then
    ;

    \ Handle a USB endpoint
    : usb-handle { endpoint -- }
      endpoint usb-rx
      endpoint endpoint-data-len @ 0> if
        endpoint usb-continue-transfer
      then
    ;
    
    \ Start a USB transfer
    : usb-start-transfer { addr bytes endpoint -- }
      addr endpoint endpoint-data-buffer !
      bytes endpoint endpoint-data-len !
      endpoint usb-continue-transfer
    ;

    \ Send a USB acknowledge out request
    : usb-ack-out-request ( -- )
      0 0 endpoint0-in usb-start-transfer
    ;

    \ Set a USB device address
    : usb-set-device-addr ( -- )
      USB_SETUP_PACKET setup-pkt-value h@ usb-device-addr !
      true set-usb-device-addr !
      usb-ack-out-request
    ;

    \ Set a USB device configuration
    : usb-set-device-config ( -- )
      true prepare-usb-device-configd? !
      usb-ack-out-request
    ;

    \ Set DTR
    : usb-set-line ( -- )
      USB_SETUP_PACKET setup-pkt-value h@ 1 and 0<> usb-dtr? !
      usb-ack-out-request
    ;

    \ Transfer the device descriptors
    : usb-handle-device-descr ( -- )
      device-data USB_SETUP_PACKET setup-pkt-length h@ device-data-size min
      endpoint0-in usb-start-transfer
    ;
    
    \ Transfer the configuration, interface, and endpoint descriptors
    : usb-handle-config-descr ( -- )
      config-data
      USB_SETUP_PACKET setup-pkt-length h@ 9 = if 9 else config-data-size then
      endpoint0-in usb-start-transfer
    ;

    \ Transfer the string descriptors
    : usb-handle-string-descr ( -- )
      usb-ack-out-request
    ;

    \ Handle a USB setup packet
    : usb-handle-setup-pkt ( -- )
      \ Reset PID to 1
      1 endpoint0-in endpoint-next-pid !
      USB_SETUP_PACKET setup-pkt-request-type c@ case
        USB_DIR_OUT of
          USB_SETUP_PACKET setup-pkt-request c@ case
            USB_REQUEST_SET_ADDRESS of usb-set-device-addr endof
            USB_REQUEST_SET_CONFIGURATION of usb-set-device-config endof
            usb-ack-out-request
          endcase
        endof
        %00100001 of
          USB_SETUP_PACKET setup-pkt-request c@ case
            USB_REQUEST_SET_LINE of usb-set-line endof
            usb-ack-out-request
          endcase
        endof
        USB_DIR_IN of
          USB_SETUP_PACKET setup-pkt-request c@ case
            USB_REQUEST_GET_DESCRIPTOR of
              USB_SETUP_PACKET setup-pkt-value h@ 8 rshift case
                USB_DT_DEVICE of usb-handle-device-descr endof
                USB_DT_CONFIG of usb-handle-config-descr endof
                USB_DT_STRING of usb-handle-string-descr endof
                usb-ack-out-request
              endcase
            endof
            usb-ack-out-request
          endcase
        endof
        usb-ack-out-request
      endcase
    ;

    \ Handle bus reset
    : usb-handle-bus-reset ( -- )
      0 USB_ADDR_ENDP !
      0 usb-device-addr !
      false set-usb-device-addr !
      false prepare-usb-device-configd? !
      false usb-device-configd? !
      false endpoint1-out-ready? !
      false endpoint1-in-ready? !
      true endpoint1-in-refill? !
      false usb-rx-pending-op-enabled? !
      false usb-dtr? !
      0 rx-read-index !
      0 rx-write-index !
      0 tx-read-index !
      0 tx-write-index !
    ;

    \ USB receive pending operation
    : usb-partial-rx ( -- )
      usb-rx-pending-op-enabled? @ if
        false usb-rx-pending-op-enabled? !
        rx-count rx-buffer-size 64 - < endpoint1-out-ready? @ and if
          false endpoint1-out-ready? !
          endpoint1-out usb-console-start-transfer
        then
      then
    ;
    
    \ Partially send data
    defer usb-partial-tx
    :noname
      tx-empty? not if
        usb-dtr? @ if
          endpoint1-in-ready? @ if
            false endpoint1-in-ready? !
            endpoint1-in usb-console-start-transfer
            true endpoint1-in-refill? !
          else
            ['] usb-partial-tx usb-tx-pending-op set-pending-op
          then
        else
          ['] usb-partial-tx usb-tx-pending-op set-pending-op
        then
      then
    ; is usb-partial-tx
    
    \ Attempt to receive data
    : usb-attempt-rx ( -- )
      rx-count rx-buffer-size 64 - < endpoint1-out-ready? @ and if
        true usb-rx-pending-op-enabled? !
        ['] usb-partial-rx usb-rx-pending-op set-pending-op
      then
    ;

    \ Attempt to send data
    : usb-attempt-tx ( -- )
      ['] usb-partial-tx usb-tx-pending-op set-pending-op
    ;

    \ Handle USB interrupt
    : handle-usb-irq ( -- )
      USB_BUFF_STATUS @ dup { buff-status } USB_BUFF_STATUS !
      USB_INTS @ { ints }
      buff-status 1 USB_BUFF_STATUS_EP_OUT and if
        endpoint1-out usb-console-rx
        true endpoint1-out-ready? !
        usb-attempt-rx
      then
      buff-status 1 USB_BUFF_STATUS_EP_IN and if
        true endpoint1-in-ready? !
        usb-attempt-tx
      then
      buff-status 2 USB_BUFF_STATUS_EP_IN and if
        endpoint2-in usb-handle
      then
      buff-status 0 USB_BUFF_STATUS_EP_IN and if
        endpoint0-in usb-handle
        prepare-usb-device-configd? @ if
          true usb-device-configd? !
          false prepare-usb-device-configd? !
          true endpoint1-out-ready? !
          true endpoint1-in-ready? !
          usb-attempt-rx
          usb-attempt-tx
          0 0 endpoint0-out usb-start-transfer
        else
          set-usb-device-addr @ if
            usb-device-addr @ USB_ADDR_ENDP !
            false set-usb-device-addr !
          else
            0 0 endpoint0-out usb-start-transfer
          then
        then
      then
      ints USB_INTS_SETUP_REQ and if
        USB_SIE_STATUS_SETUP_REC USB_SIE_STATUS !
        usb-handle-setup-pkt
      then
      ints USB_INTS_BUS_RESET and if
        USB_SIE_STATUS_BUS_RESET USB_SIE_STATUS !
        usb-handle-bus-reset
      then
    ;
    
    \ Initialize USB
    : init-usb ( -- )
      true usb-special-enabled !
      
      usb-out-core-lock init-core-lock
      usb-in-core-lock init-core-lock

      0 rx-read-index !
      0 rx-write-index !
      0 tx-read-index !
      0 tx-write-index !

      reset-usb

      \ Clear the DPRAM just because
      USB_DPRAM_Base dpram-size 0 fill

      0 USB_ADDR_ENDP !
      0 usb-device-addr !
      false set-usb-device-addr !
      false prepare-usb-device-configd? !
      false usb-device-configd? !
      false endpoint1-out-ready? !
      false endpoint1-in-ready? !
      true endpoint1-in-refill? !
      false usb-dtr? !
      
      false usb-rx-pending-op-enabled? !
      usb-pending-op-priority usb-rx-pending-op register-pending-op
      usb-pending-op-priority usb-tx-pending-op register-pending-op

      0 usbctrl-irq NVIC_IPR_IP!
      ['] handle-usb-irq usbctrl-vector vector!
      usbctrl-irq NVIC_ISER_SETENA!

      [ USB_USB_MUXING_TO_PHY USB_USB_MUXING_SOFTCON or ] literal
      USB_USB_MUXING !
      [ USB_USB_PWR_VBUS_DETECT USB_USB_PWR_VBUS_DETECT_OVERRIDE_EN or ] literal
      USB_USB_PWR !
      USB_MAIN_CTRL_CONTROLLER_EN USB_MAIN_CTRL !
      USB_SIE_CTRL_EP0_INT_1BUF USB_SIE_CTRL !
      [ USB_INTS_BUFF_STATUS USB_INTS_BUS_RESET or USB_INTS_SETUP_REQ or ]
      literal USB_INTE !

      init-usb-endpoints

      [ USB_SIE_CTRL_EP0_INT_1BUF USB_SIE_CTRL_PULLUP_EN or ] literal
      USB_SIE_CTRL !

      reboot-hook @ saved-reboot-hook !
      [:
        [:
          [:
            saved-reboot-hook @ execute
            0 internal::in-critical !
            pause
            100000 0 do loop
            \        in-interrupt? not if flush-console then
            close-usb-endpoints
            100000 0 do loop
          ;] usb-out-core-lock with-core-lock
        ;] usb-in-core-lock with-core-lock
      ;] reboot-hook !
    ;

    \ Get whether a byte is ready to be emitted
    : usb-emit? ( -- emit? )
      usb-dtr? @ usb-device-configd? @ and tx-full? not and
    ;
    
    \ Emit a byte
    : usb-emit ( c -- )
      begin
        [:
          [:
            usb-dtr? @ usb-device-configd? @ and if
              tx-full? not if
                write-tx true
              else
                false endpoint1-in-refill? ! false
              then
              usb-attempt-tx
            else
              false
            then
          ;] critical
        ;] usb-in-core-lock with-core-lock
        dup not if begin endpoint1-in-refill? @ not while pause repeat then
      until
    ;

    \ Get whether a byte is ready to be read
    : usb-key? ( -- key? )
      usb-device-configd? @ rx-empty? not and
    ;

    \ Read a byte
    : usb-key ( -- c )
      begin
        [:
          [:
            usb-device-configd? @ if
              rx-empty? not if
                read-rx true
              else
                false
              then
              usb-attempt-rx
            else
              false
            then
          ;] critical
        ;] usb-out-core-lock with-core-lock
        dup not if pause then
      until
    ;

    \ Flush the console
    : usb-flush-console ( -- )
      begin
        tx-empty? endpoint1-in-ready? @ and
        dup not if
          usb-dtr? @ usb-device-configd? @ and if usb-attempt-tx then
          pause
        then
      until
      true endpoint1-in-refill? !
    ;

  end-module> import

  \ Enable the usb console
  : usb-console
    ['] usb-key? key?-hook !
    ['] usb-key key-hook !
    ['] usb-emit? emit?-hook !
    ['] usb-emit emit-hook !
    ['] usb-emit? error-emit?-hook !
    ['] usb-emit error-emit-hook !
    ['] usb-flush-console flush-console-hook !
    ['] usb-flush-console error-flush-console-hook !
  ;

  \ Set the curent input to usb within an xt
  : with-usb-input ( xt -- )
    ['] usb-key ['] usb-key? rot with-input
  ;

  \ Set the current output to usb within an xt
  : with-usb-output ( xt -- )
    ['] usb-emit ['] usb-emit? rot ['] usb-flush-console swap with-output
  ;

  \ Set the current error output to usb within an xt
  : with-usb-error-output ( xt -- )
    ['] usb-emit ['] usb-emit? rot ['] usb-flush-console swap with-error-output
  ;
  
end-module> import

\ Initialize
: init ( -- )
  init
  usb-internal::init-usb
  usb-console
;

compile-to-ram
