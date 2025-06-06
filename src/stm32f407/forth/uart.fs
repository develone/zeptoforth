\ Copyright (c) 2013? Matthias Koch
\ Copyright (c) 2020-2023 Travis Bemann
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

\ Compile to flash
compile-to-flash

begin-module uart

  internal import
  interrupt import
  serial import
  serial-internal import
  pin import
  closure import
  
  \ Invalid UART exception
  : x-invalid-uart ( -- ) ." invalid UART" cr ;
  
  begin-module uart-internal

    \ Validate a USART
    : validate-uart ( uart -- )
      dup 1 >= swap 6 <= and averts x-invalid-uart
    ;
    
    \ Constant for number of bytes to buffer
    128 constant uart-rx-buffer-size
      
    \ Constant for number of bytes to buffer
    128 constant uart-tx-buffer-size
      
    begin-structure uart-size
    
      \ RX buffer read-index
      cfield: uart-rx-read-index
      
      \ RX buffer write-index
      cfield: uart-rx-write-index
      
      \ RX buffer
      uart-rx-buffer-size +field uart-rx-buffer
      
      \ TX buffer read-index
      cfield: uart-tx-read-index
      
      \ TX buffer write-index
      cfield: uart-tx-write-index
      
      \ TX buffer
      uart-tx-buffer-size +field uart-tx-buffer

    end-structure

    \ UART buffers
    uart-size 5 * buffer: usarts

    \ USART base register table
    create USART_Base_table
    $40011000 ,
    $40004400 ,
    $40004800 ,
    $40004C00 ,
    $40005000 ,
    $40011400 ,
    
    \ USART base
    : USART_Base ( usart -- addr ) 1- cells USART_Base_table + @ ;
    
    \ USART registers
    : USART_SR ( usart -- addr ) USART_Base $00 + ;
    : USART_DR ( usart -- addr ) USART_Base $04 + ;
    : USART_BRR ( usart -- addr ) USART_Base $08 + ;
    : USART_CR1 ( usart -- addr ) USART_Base $0C + ;
    : USART_CR2 ( usart -- addr ) USART_Base $10 + ;
    : USART_CR3 ( usart -- addr ) USART_Base $14 + ;
    : USART_GTPR ( usart -- addr ) USART_Base $18 + ;

    \ USART IRQ number
    : uart-irq ( usart -- irq )
      case
	1 of 37 endof
	2 of 38 endof
	3 of 39 endof
	4 of 52 endof
	5 of 53 endof
	6 of 71 endof
	['] x-invalid-uart ?raise
      endcase
    ;

    \ USART vector number
    : uart-vector ( usart -- vector ) uart-irq 16 + ;

    \ RCC base address
    $40023800 constant RCC_Base

    \ RCC registers
    RCC_Base $40 + constant RCC_APB1ENR ( RCC_APB1ENR )
    RCC_Base $44 + constant RCC_APB2ENR ( RCC_APB2ENR )
    RCC_Base $60 + constant RCC_APB1LPENR ( RCC_APB1LPENR )
    RCC_Base $64 + constant RCC_APB2LPENR ( RCC_APB2LPENR )

    \ RCC fields
    : RCC_APB1ENR_USART2EN 17 bit RCC_APB1ENR bis! ;
    : RCC_APB1ENR_USART2EN_Clear 17 bit RCC_APB1ENR bic! ;
    : RCC_APB1ENR_USART3EN 18 bit RCC_APB1ENR bis! ;
    : RCC_APB1ENR_USART3EN_Clear 18 bit RCC_APB1ENR bic! ;
    : RCC_APB1ENR_UART4EN 19 bit RCC_APB1ENR bis! ;
    : RCC_APB1ENR_UART4EN_Clear 19 bit RCC_APB1ENR bic! ;
    : RCC_APB1ENR_UART5EN 20 bit RCC_APB1ENR bis! ;
    : RCC_APB1ENR_UART5EN_Clear 20 bit RCC_APB1ENR bic! ;
    : RCC_APB2ENR_USART1EN 4 bit RCC_APB2ENR bis! ;
    : RCC_APB2ENR_USART1EN_Clear 4 RCC_APB2ENR bic! ;
    : RCC_APB2ENR_USART6EN 5 bit RCC_APB2ENR bis! ;
    : RCC_APB2ENR_USART6EN_Clear 5 RCC_APB2ENR bic! ;
    : RCC_APB1LPENR_USART2LPEN  17 bit RCC_APB1LPENR bis! ;
    : RCC_APB1LPENR_USART2LPEN_Clear 17 bit RCC_APB1LPENR bic! ;
    : RCC_APB1LPENR_USART3LPEN 18 bit RCC_APB1LPENR bis! ;
    : RCC_APB1LPENR_USART3LPEN_Clear 18 bit RCC_APB1LPENR bic! ;
    : RCC_APB1LPENR_UART4LPEN 19 bit RCC_APB1LPENR bis! ;
    : RCC_APB1LPENR_UART4LPEN_Clear 19 bit RCC_APB1LPENR bic! ;
    : RCC_APB1LPENR_UART5LPEN 20 bit RCC_APB1LPENR bis! ;
    : RCC_APB1LPENR_UART5LPEN_Clear 20 bit RCC_APB1LPENR bic! ;
    : RCC_APB2LPENR_USART1LPEN 4 bit RCC_APB2LPENR bis! ;
    : RCC_APB2LPENR_USART1LPEN_Clear 4 RCC_APB2LPENR bic! ;
    : RCC_APB2LPENR_USART6LPEN 5 bit RCC_APB2LPENR bis! ;
    : RCC_APB2LPENR_USART6LPEN_Clear 5 RCC_APB2LPENR bic! ;

    \ USART_CR1 fields
    : USART_CR1_TXEIE   %1 7 lshift swap USART_CR1 bis! ;  \ USART_CR1_TXEIE    interrupt enable
    : USART_CR1_RXNEIE   %1 5 lshift swap USART_CR1 bis! ;  \ USART_CR1_RXNEIE    RXNE interrupt enable
    : USART_CR1_TXEIE_Clear   %1 7 lshift swap USART_CR1 bic! ;  \ USART_CR1_TXEIE    interrupt disable
    : USART_CR1_RXNEIE_Clear   %1 5 lshift swap USART_CR1 bic! ;  \ USART_CR1_RXNEIE    RXNE interrupt enable

    $20 constant RXNE
    $80 constant TXE

    \ USART enable bits
    13 bit constant USART_CR1_UE
    3 bit constant USART_CR1_TE
    2 bit constant USART_CR1_RE

    \ Select USART structure
    : uart-select ( usart -- addr )
      dup 1 = if drop usarts else 2 - uart-size * usarts + then
    ;

    \ RX buffer read-index
    : uart-rx-read-index ( usart -- addr )
      uart-select uart-rx-read-index
    ;
      
    \ RX buffer write-index
    : uart-rx-write-index ( usart -- addr )
      uart-select uart-rx-write-index
    ;

    \ RX buffer
    : uart-rx-buffer ( usart -- addr )
      uart-select uart-rx-buffer
    ;
      
    \ TX buffer read-index
    : uart-tx-read-index ( usart -- addr )
      uart-select uart-tx-read-index
    ;
      
    \ TX buffer write-index
    : uart-tx-write-index ( usart -- addr )
      uart-select uart-tx-write-index
    ;

    \ TX buffer
    : uart-tx-buffer ( usart -- addr )
      uart-select uart-tx-buffer
    ;

    \ Get whether the rx buffer is full
    : uart-rx-full? ( usart -- f )
      dup uart-rx-write-index c@ swap uart-rx-read-index c@
      uart-rx-buffer-size 1- + uart-rx-buffer-size umod =
    ;

    \ Get whether the rx buffer is empty
    : uart-rx-empty? ( usart -- f )
      dup uart-rx-read-index c@ swap uart-rx-write-index c@ =
    ;

    \ Write a byte to the rx buffer
    : uart-write-rx ( c uart -- )
      [:
	dup uart-rx-full? not if
	  tuck dup uart-rx-write-index c@ swap uart-rx-buffer + c!
	  dup uart-rx-write-index c@ 1+ uart-rx-buffer-size mod
	  swap uart-rx-write-index c!
	else
	  2drop
	then
      ;] critical
    ;

    \ Read a byte from the rx buffer
    : uart-read-rx ( usart -- c )
      [:
	dup uart-rx-empty? not if
	  dup uart-rx-read-index c@ over uart-rx-buffer + c@
	  over uart-rx-read-index c@ 1+ uart-rx-buffer-size mod
	  rot uart-rx-read-index c!
	else
	  drop 0
	then
      ;] critical
    ;

    \ Get whether the tx buffer is full
    : uart-tx-full? ( usart -- f )
      dup uart-tx-write-index c@ swap uart-tx-read-index c@
      uart-tx-buffer-size 1- + uart-tx-buffer-size umod =
    ;

    \ Get whether the tx buffer is empty
    : uart-tx-empty? ( usart -- f )
      dup uart-tx-read-index c@ swap uart-tx-write-index c@ =
    ;

    \ Write a byte to the tx buffer
    : uart-write-tx ( c uart -- )
      [:
	dup uart-tx-full? not if
	  tuck dup uart-tx-write-index c@ swap uart-tx-buffer + c!
	  dup uart-tx-write-index c@ 1+ uart-tx-buffer-size mod
	  swap uart-tx-write-index c!
	else
	  2drop
	then
      ;] critical
    ;

    \ Read a byte from the tx buffer
    : uart-read-tx ( usart -- c )
      [:
	dup uart-tx-empty? not if
	  dup uart-tx-read-index c@ over uart-tx-buffer + c@
	  over uart-tx-read-index c@ 1+ uart-tx-buffer-size mod
	  rot uart-tx-read-index c!
	else
	  drop 0
	then
      ;] critical
    ;

    \ Handle IO for an USART
    : handle-uart-io ( usart -- )
      begin
	dup uart-rx-full? not if
	  dup USART_SR @ RXNE and if
	    dup USART_DR c@ over uart-write-rx false
	  else
	    true
	  then
	else
	  true
	then
      until
      dup uart-rx-full? if
	dup USART_CR1_RXNEIE_Clear
      then
      begin
	dup uart-tx-empty? not if
	  dup USART_SR @ TXE and if
	    dup uart-read-tx over USART_DR c! false
	  else
	    true
	  then
	else
	  true
	then
      until
      dup uart-tx-empty? if
	dup USART_CR1_TXEIE_Clear
      then
      uart-irq NVIC_ICPR_CLRPEND!
      wake
    ;

    \ USART interrupt handlers
    : handle-uart1-io ( -- ) 1 handle-uart-io ;
    : handle-uart3-io ( -- ) 3 handle-uart-io ;
    : handle-uart4-io ( -- ) 4 handle-uart-io ;
    : handle-uart5-io ( -- ) 5 handle-uart-io ;
    : handle-uart6-io ( -- ) 6 handle-uart-io ;
    
    \ Enable USART1 interrupt-driven IO
    : enable-uart1-int-io ( usart -- )
      disable-int
      0 1 uart-irq NVIC_IPR_IP!
      ['] handle-uart1-io 1 uart-vector vector!
      RCC_APB2LPENR_USART1LPEN
      1 uart-irq NVIC_ISER_SETENA!
      1 USART_CR1_RXNEIE
      enable-int
    ;

    \ Enable USART3 interrupt-driven IO
    : enable-uart3-int-io ( usart -- )
      disable-int
      0 3 uart-irq NVIC_IPR_IP!
      ['] handle-uart3-io 3 uart-vector vector!
      RCC_APB1LPENR_USART3LPEN
      3 uart-irq NVIC_ISER_SETENA!
      3 USART_CR1_RXNEIE
      enable-int
    ;

    \ Enable UART4 interrupt-driven IO
    : enable-uart4-int-io ( usart -- )
      disable-int
      0 4 uart-irq NVIC_IPR_IP!
      ['] handle-uart4-io 4 uart-vector vector!
      RCC_APB1LPENR_UART4LPEN
      4 uart-irq NVIC_ISER_SETENA!
      4 USART_CR1_RXNEIE
      enable-int
    ;

    \ Enable UART5 interrupt-driven IO
    : enable-uart5-int-io ( usart -- )
      disable-int
      0 5 uart-irq NVIC_IPR_IP!
      ['] handle-uart5-io 5 uart-vector vector!
      RCC_APB1LPENR_UART5LPEN
      5 uart-irq NVIC_ISER_SETENA!
      5 USART_CR1_RXNEIE
      enable-int
    ;

    \ Enable USART6 interrupt-driven IO
    : enable-uart6-int-io ( usart -- )
      disable-int
      0 6 uart-irq NVIC_IPR_IP!
      ['] handle-uart6-io 6 uart-vector vector!
      RCC_APB2LPENR_USART6LPEN
      6 uart-irq NVIC_ISER_SETENA!
      6 USART_CR1_RXNEIE
      enable-int
    ;

    \ Disable USART1 interrupt-driven IO
    : disable-uart1-int-io ( -- )
      disable-int
      ['] handle-null 1 uart-vector vector!
      1 USART_CR1_RXNEIE_Clear
      1 USART_CR1_TXEIE_Clear
      1 uart-irq NVIC_ICER_CLRENA!
      RCC_APB2LPENR_USART1LPEN_Clear
      enable-int
    ;

    \ Disable USART3 interrupt-driven IO
    : disable-uart3-int-io ( -- )
      disable-int
      ['] handle-null 3 uart-vector vector!
      3 USART_CR1_RXNEIE_Clear
      3 USART_CR1_TXEIE_Clear
      3 uart-irq NVIC_ICER_CLRENA!
      RCC_APB1LPENR_USART3LPEN_Clear
      enable-int
    ;

    \ Disable UART4 interrupt-driven IO
    : disable-uart4-int-io ( -- )
      disable-int
      ['] handle-null 4 uart-vector vector!
      4 USART_CR1_RXNEIE_Clear
      4 USART_CR1_TXEIE_Clear
      4 uart-irq NVIC_ICER_CLRENA!
      RCC_APB1LPENR_UART4LPEN_Clear
      enable-int
    ;
    
    \ Disable UART5 interrupt-driven IO
    : disable-uart5-int-io ( -- )
      disable-int
      ['] handle-null 5 uart-vector vector!
      5 USART_CR1_RXNEIE_Clear
      5 USART_CR1_TXEIE_Clear
      5 uart-irq NVIC_ICER_CLRENA!
      RCC_APB1LPENR_UART5LPEN_Clear
      enable-int
    ;

    \ Disable USART6 interrupt-driven IO
    : disable-uart6-int-io ( -- )
      disable-int
      ['] handle-null 6 uart-vector vector!
      6 USART_CR1_RXNEIE_Clear
      6 USART_CR1_TXEIE_Clear
      6 uart-irq NVIC_ICER_CLRENA!
      RCC_APB2LPENR_USART6LPEN_Clear
      enable-int
    ;

  end-module> import

  \ UART alternate function
  : uart-alternate ( uart -- alternate )
    dup validate-uart
    case
      1 of 7 endof
      2 of 7 endof
      3 of 7 endof
      4 of 8 endof
      5 of 8 endof
      6 of 8 endof
    endcase
  ;

  \ Get whether a USART is enabled
  : uart-enabled? ( usart -- flag )
    dup validate-uart
    USART_CR1_UE swap USART_CR1 bit@
  ;

  \ Enable a USART
  : enable-uart ( usart -- )
    dup validate-uart
    [ USART_CR1_UE USART_CR1_TE or USART_CR1_RE or ] literal swap USART_CR1 bis!
  ;

  \ Disable a USART
  : disable-uart ( usart -- )
    dup validate-uart
    [ USART_CR1_UE USART_CR1_TE or USART_CR1_RE or ] literal swap USART_CR1 bic!
  ;

  \ Carry out an operation with a USART disabled
  : with-uart-disabled ( xt usart -- )
    dup uart-enabled? 2dup if disable-uart else drop then
    2>r try 2r> if enable-uart else drop then ?raise
  ;

  \ Set a USART's baud
  : uart-baud! ( baud usart -- )
    [:
      swap dup 2 /
      2 pick case
	1 of 84000000 endof
	2 of 42000000 endof
	3 of 42000000 endof
	4 of 42000000 endof
	5 of 42000000 endof
	6 of 84000000 endof
      endcase
      + swap / swap USART_BRR !
    ;] over with-uart-disabled
  ;

  continue-module uart-internal

    \ Initialize interrupt-driven IO on USART1
    : init-uart1 ( -- )
      0 1 uart-rx-read-index c!
      0 1 uart-rx-write-index c!
      0 1 uart-tx-read-index c!
      0 1 uart-tx-write-index c!
      RCC_APB2ENR_USART1EN
      RCC_APB2LPENR_USART1LPEN
      115200 1 uart-baud!
      1 enable-uart
      enable-uart1-int-io
    ;
    
    \ Initialize interrupt-driven IO on USART3
    : init-uart3 ( -- )
      0 3 uart-rx-read-index c!
      0 3 uart-rx-write-index c!
      0 3 uart-tx-read-index c!
      0 3 uart-tx-write-index c!
      RCC_APB1ENR_USART3EN
      RCC_APB1LPENR_USART3LPEN
      115200 3 uart-baud!
      3 enable-uart
      enable-uart3-int-io
    ;
    
    \ Initialize interrupt-driven IO on UART4
    : init-uart4 ( -- )
      0 4 uart-rx-read-index c!
      0 4 uart-rx-write-index c!
      0 4 uart-tx-read-index c!
      0 4 uart-tx-write-index c!
      RCC_APB1ENR_UART4EN
      RCC_APB1LPENR_UART4LPEN
      115200 4 uart-baud!
      4 enable-uart
      enable-uart4-int-io
    ;
    
    \ Initialize interrupt-driven IO on UART5
    : init-uart5 ( -- )
      0 5 uart-rx-read-index c!
      0 5 uart-rx-write-index c!
      0 5 uart-tx-read-index c!
      0 5 uart-tx-write-index c!
      RCC_APB1ENR_UART5EN
      RCC_APB1LPENR_UART5LPEN
      115200 5 uart-baud!
      5 enable-uart
      enable-uart5-int-io
    ;

    \ Initialize interrupt-driven IO on USART6
    : init-uart6 ( -- )
      0 6 uart-rx-read-index c!
      0 6 uart-rx-write-index c!
      0 6 uart-tx-read-index c!
      0 6 uart-tx-write-index c!
      RCC_APB2ENR_USART6EN
      RCC_APB2LPENR_USART6LPEN
      115200 6 uart-baud!
      6 enable-uart
      enable-uart6-int-io
    ;
    
  end-module

  \ Emit a byte to a USART
  : >uart ( c usart -- )
    dup validate-uart
    dup 2 = if
      drop do-emit
    else
      [: dup uart-tx-full? not ;] wait
      tuck uart-write-tx
      USART_CR1_TXEIE
    then
  ; 

  \ Read a byte from a USART
  : uart> ( usart -- c )
    dup validate-uart
    dup 2 = if
      drop do-key
    else
      dup USART_CR1_RXNEIE
      [: dup uart-rx-empty? not ;] wait
      uart-read-rx
    then
  ;

  \ Get whether a USART is ready to emit a byte
  : >uart? ( usart -- flag )
    dup validate-uart
    dup 2 = if
      drop do-emit?
    else
      uart-tx-full? not
    then
  ;

  \ Get whether a byte is ready to be read from a USART
  : uart>? ( usart -- flag )
    dup validate-uart
    dup 2 = if
      drop do-key?
    else
      uart-rx-empty? not
    then
  ;

  \ Flush the transmit buffer for a USART
  : flush-uart ( usart -- )
    dup validate-uart
    dup 2 = if
      drop do-flush-console
    else
      [: dup uart-tx-empty? over TXE swap USART_SR bit@ and ;] wait drop
    then
  ;
  
  \ Enable interrupt-driven IO on a UART
  : enable-uart-int-io ( uart -- )
    dup validate-uart
    case
      1 of enable-uart1-int-io endof
      2 of enable-serial-int-io endof
      3 of enable-uart3-int-io endof
      4 of enable-uart4-int-io endof
      5 of enable-uart5-int-io endof
      6 of enable-uart6-int-io endof
    endcase
  ;

  \ Disable interrupt-drive IO on a UART
  : disable-uart-int-io ( uart -- )
    dup validate-uart
    case
      1 of disable-uart1-int-io endof
      2 of disable-serial-int-io endof
      3 of disable-uart3-int-io endof
      4 of disable-uart4-int-io endof
      5 of disable-uart5-int-io endof
      6 of disable-uart6-int-io endof
    endcase
  ;
  
  \ Set a pin to be a UART pin
  : uart-pin ( uart pin -- )
    swap uart-alternate swap alternate-pin
  ;
  
  continue-module uart-internal

    \ Data associated with UART input and output
    begin-structure console-uart-data-size
      
      \ A closure associated with KEY or EMIT
      closure-size +field console-io

      \ A closure associated with KEY? or EMIT?
      closure-size +field console-io?
      
      \ The uart associated with the input or output
      field: console-uart

    end-structure

    \ Data associated with UART output only
    begin-structure console-out-uart-data-size

      console-uart-data-size +field console-initial-part

      \ Flush console
      closure-size +field console-io-flush
      
    end-structure

    \ Initialize console UART data for input
    : init-console-uart-input { uart data -- }
      uart data console-uart !
      data data console-io [: { data }
        data console-uart @ uart>
      ;] bind
      data data console-io? [: { data }
        data console-uart @ uart>?
      ;] bind
    ;

    \ Initialize console UART data for output
    : init-console-uart-output { uart data -- }
      uart data console-uart !
      data data console-io [: { byte data }
        byte data console-uart @ >uart
      ;] bind
      data data console-io? [: { data }
        data console-uart @ >uart?
      ;] bind
      data data console-io-flush [: { data }
        data console-uart @ flush-uart
      ;] bind
    ;
    
  end-module

  \ Set the current input to a UART within an xt
  : with-uart-input ( uart xt -- )
    over validate-uart
    console-uart-data-size [: { data }
      swap data init-console-uart-input
      data console-io data console-io? rot console::with-input
    ;] with-aligned-allot
  ;

  \ Set the current output to a UART within an xt
  : with-uart-output ( uart xt -- )
    over validate-uart
    console-out-uart-data-size [: { data }
      swap data init-console-uart-output
      data console-io data console-io? rot data console-io-flush swap
      console::with-output
    ;] with-aligned-allot
  ;

  \ Set the current error output to a UART within an xt
  : with-uart-error-output ( uart xt -- )
    over validate-uart
    console-out-uart-data-size [: { data }
      swap data init-console-uart-output
      data console-io data console-io? rot data console-io-flush swap
      console::with-error-output
    ;] with-aligned-allot
  ;
  
end-module> import

\ Init
: init ( -- )
  init
  uart-internal::init-uart1
  uart-internal::init-uart3
  uart-internal::init-uart4
  uart-internal::init-uart5
  uart-internal::init-uart6
;

\ Reboot
reboot
