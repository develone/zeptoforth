\ Copyright (c) 2020-2024 Travis Bemann
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

\ Compile this to flash
compile-to-flash

begin-module disassemble-internal

  internal import

  defined? armv7m-fp [if]
    armv7m-fp::armv7m-fp-mask import
  [then]

  \ Disassemble for gas
  variable for-gas

  \ Local label count
  64 constant local-count

  \ Local label buffer
  local-count cells aligned-buffer: local-buffer

  \ Local label index
  variable local-index

  \ Does this follow an underscore
  variable prev-underscore

  \ Begin compressing compiled code in flash
  compress-flash

  \ Look up a local label
  : lookup-local ( addr -- label|0 )
    local-index @ 0 ?do
      i cells local-buffer + @ over = if
	drop i 1 + unloop exit
      then
    loop
    drop 0
  ;

  \ SB and SH
  char B negate constant SB
  char H negate constant SH
  
  \ Commit to flash
  commit-flash

  \ Type a condition
  : (cond.) ( cond -- )
    case
      %0000 of ." EQ" endof
      %0001 of ." NE" endof
      %0010 of ." HS" endof \ or ." CS"
      %0011 of ." LO" endof \ or ." CC"
      %0100 of ." MI" endof
      %0101 of ." PL" endof
      %0110 of ." VS" endof
      %0111 of ." VC" endof
      %1000 of ." HI" endof
      %1001 of ." LS" endof
      %1010 of ." GE" endof
      %1011 of ." LT" endof
      %1100 of ." GT" endof
      %1101 of ." LE" endof
      \    %1110 of ." AL" endof
    endcase
  ;

  \ Get whether the condition is null
  : null-cond? ( cond -- f ) 0< ;

  \ Type an signed decimal value
  : (dec.) ( u -- ) base @ >r decimal (.) r> base  ! ;

  \ Type an unsigned decimal value
  : (udec.) ( u -- ) base @ >r decimal (u.) r> base  ! ;

  \ Type a register
  : reg. ( u -- )
    case
      15 of ." PC" endof
      14 of ." LR" endof
      13 of ." SP" endof
      dup ." R" (udec.)
    endcase
  ;

  defined? float32 [if]

    \ Type a single-precision floating-point register
    : sreg. ( u -- )
      ." S" (udec.)
    ;
    
  [then]

  \ Type a value
  : val. ( u -- )
    for-gas @ if ." 0x" else ." $" then base @ >r hex (u.) r> base !
  ;

  \ Type a bit size
  : size. ( size -- )
    case
      0 of endof
      SB of ." SB" endof
      SH of ." SH" endof
      dup emit
    endcase
  ;

  \ Output data in hex and ASCII
  : data. { current end -- }
    begin current end u< while
      cr current h.8 space
      current 16 + end min current ?do i c@ h.2 space loop
      current 16 + end u> if
        current 16 + end ?do ."    " loop
      then
      ." |"
      current 16 + end min current ?do
        i c@ dup $20 < over $7E > or if drop [char] . then emit
      loop
      current 16 + end u> if
        current 16 + end ?do [char] . emit loop
      then
      ." |"
      16 +to current
    repeat
  ;
  
  \ Generate words with a given size specified
  : w-size ( xt size "name" -- )
    <builds , , does> 2@ execute
  ;

  \ Type a leading underscore
  : lead-underscore ( index char-count -- )
    swap 0<> prev-underscore @ not and if ." _" 1+ then
  ;

  \ Type a tailing underscore
  : tail-underscore ( count index char-count -- )
    -rot 1 + <> if ." _" 1+ true prev-underscore ! then
  ;

  \ Commit to flash
  commit-flash

  \ Convert type character case
  : convert-type-char-case
    case
      [char] ` of dup 6 lead-underscore ." bquote" tail-underscore endof
      [char] ~ of dup 5 lead-underscore ." tilde" tail-underscore endof
      [char] ! of dup 5 lead-underscore ." store" tail-underscore endof
      [char] @ of dup 5 lead-underscore ." fetch" tail-underscore endof
      [char] # of dup 4 lead-underscore ." num" tail-underscore endof
      [char] $ of dup 6 lead-underscore ." dollar" tail-underscore endof
      [char] % of dup 7 lead-underscore ." percent" tail-underscore endof
      [char] ^ of dup 5 lead-underscore ." caret" tail-underscore endof
      [char] & of dup 3 lead-underscore ." amp" tail-underscore endof
      [char] * of dup 4 lead-underscore ." star" tail-underscore endof
      $28 of dup 5 lead-underscore ." paren" tail-underscore endof
      [char] ) of dup 6 lead-underscore ." cparen" tail-underscore endof
      [char] - of 2drop 1 ." _" endof
      [char] = of dup 5 lead-underscore ." equal" tail-underscore endof
      [char] + of dup 4 lead-underscore ." plus" tail-underscore endof
      [char] [ of dup 7 lead-underscore ." bracket" tail-underscore endof
      [char] { of dup 5 lead-underscore ." brace" tail-underscore endof
      [char] ] of dup 8 lead-underscore ." cbracket" tail-underscore endof
      [char] } of dup 6 lead-underscore ." cbrace" tail-underscore endof
      $5C of dup 4 lead-underscore ." back" tail-underscore endof
      [char] | of dup 4 lead-underscore ." pipe" tail-underscore endof
      [char] ; of dup 4 lead-underscore ." semi" tail-underscore endof
      [char] : of dup 5 lead-underscore ." colon" tail-underscore endof
      [char] ' of dup 5 lead-underscore ." quote" tail-underscore endof
      [char] " of dup 6 lead-underscore ." dquote" tail-underscore endof
      [char] , of dup 5 lead-underscore ." comma" tail-underscore endof
      [char] < of dup 2 lead-underscore ." lt" tail-underscore endof
      [char] . of dup 3 lead-underscore ." dot" tail-underscore endof
      [char] > of dup 2 lead-underscore ." gt" tail-underscore endof
      [char] / of dup 5 lead-underscore ." slash" tail-underscore endof
      [char] ? of dup 4 lead-underscore ." ques" tail-underscore endof
      dup emit nip nip 1 swap false prev-underscore !
    endcase
  ;
  
  \ Convert and type a character meant for an assembler
  : convert-type-char ( b count index -- len )
    2 pick [char] - = 2 pick 1 = and over 0 = and if
      ." minus" 2drop drop
    else
      rot convert-type-char-case
    then
  ;

  \ Type out a label, with two different display modes depending on whether the
  \ target is a user or an assembler
  : label-type ( b-addr u -- )
    false prev-underscore !
    for-gas @ if
      dup 0 0 -rot ?do 2 pick i + c@ 2 pick i convert-type-char + loop nip nip
    else
      tuck type
    then
  ;

  \ Commit to flash
  commit-flash

  \ Print out an exclamation point if a register is not in a register list
  : not-in-reglist. ( list reg size -- )
    0 ?do over i rshift 1 and if dup i = if unloop exit then then loop
    2drop ." !"
  ;

  \ Print out a register list
  : reglist. ( list size -- )
    ." {" false swap 0 ?do
      over i rshift 1 and if
	dup if ." , " else drop true then
	i reg.
      then
    loop
    2drop ." }"
  ;

  \ Print out an absolute address
  : addr. ( op-addr ref-addr -- )
    dup find-by-xt ?dup if
      rot drop word-name count label-type drop
      for-gas @ if drop else space ." <" val. ." >" then
    else
      2dup <> if
	dup lookup-local ?dup if
	  (udec.) tuck > if ." B" else ." F" then
	  for-gas @ if drop else space ." <" val. ." >" then
	else
	  nip ." #" val.
	then
      else
	." ." for-gas @ if drop else space ." <" val. ." >" then drop
      then
    then
  ;

  \ Print out a label
  : label. ( addr -- )
    dup find-by-xt ?dup if
      nip word-name count for-gas @ if
	label-type ." :" 20 swap - 0 max 1 + 0 ?do space loop
      else
	20 min label-type ." :" 21 swap - 0 ?do space loop
      then
    else
      lookup-local ?dup if
	base @ >r 10 base ! 0 <# #s #> r> base ! for-gas @ if
	  tuck type ." :" 20 swap - 0 max 1 + 0 ?do space loop
	else
	  20 min tuck type ." :" 21 swap - 0 ?do space loop
	then
      else
	22 0 ?do space loop
      then
    then
  ;

  \ Separator
  : sep. ( -- ) ." , " ;

  \ Separator with immediate
  : sep-imm. ( -- ) ." , #" ;

  \ Type a 16-bit instruction in halfwords
  : instr16. ( h -- ) for-gas @ if drop else h.4 ." :      " then ;

  \ Type a 32-bit instruction in halfwords
  : instr32. ( low high -- )
    for-gas @ if 2drop else swap h.4 space h.4 ." : " then
  ;

  \ Sign extend a value of a certain size in bits
  : extend ( u bits -- ) 32 swap - tuck lshift swap arshift ;

  \ Match 16-bit
  : match16 ( u -- u f ) 2 + true ;

  \ Match 32-bit
  : match32 ( u -- u f ) 4 + true ;

  \ Not match 16-bit
  : not-match16 ( u u -- f ) 2drop false ;

  \ Not match 32-bit
  : not-match32 ( u u u -- f ) drop 2drop false ;

  \ Rotate a 7-bit value encoded within a 12-bit value
  : rotate7-in-12 ( u -- u )
    dup %1111111 and %10000000 or
    swap 7 rshift %11111 and 2dup rshift -rot 32 swap - lshift or
  ;

  \ Decode an imm12 constant
  : decode-const12 ( u -- )
    %111111111111 and
    dup 10 rshift 0 = if
      dup 8 rshift case
	%00 of (u.) endof
	%01 of
	  dup %11111111 and 0 = if
	    ." UNPREDICTABLE" drop
	  else
	    dup 16 lshift val.
	  then
	endof
	%10 of
	  dup %11111111 and 0 = if
	    ." UNPREDICTABLE" drop
	  else
	    dup 24 lshift swap 8 lshift or val.
	  then
	endof
	%11 of
	  dup %11111111 and 0 = if
	    ." UNPREDICTABLE" drop
	  else
	    dup 24 lshift over 16 lshift or over 8 lshift or or val.
	  then
	endof
      endcase
    else
      rotate7-in-12 val.
    then
  ;

  \ Decode an immediate shift
  : decode-imm-shift ( type u -- )
    swap case
      %00 of
	?dup if
	  ." , LSL #" (udec.)
	then
      endof
      %01 of
	?dup if
	  ." , LSR #" (udec.)
	else
	  ." , LSR #32"
	then
      endof
      %10 of
	?dup if
	  ." , ASR #" (udec.)
	else
	  ." , ASR #32"
	then
      endof
      %11 of
	?dup if
	  ." , ROR #" (udec.)
	else
	  ." , RRX #1"
	then
      endof
    endcase
  ;

  \ Extract a field
  : bitfield ( data shift bits -- )
    -rot rshift swap $FFFFFFFF 32 rot - rshift and
  ;

  \ Print out a condition if one is specified, otherwise do nothing
  : cond. ( cond -- )
    dup null-cond? not if
      (cond.)
    else
      drop
    then
  ;

  \ Print out a condition if one is specified, otherwise do print out 'S'
  : conds. ( cond -- )
    dup null-cond? not if
      (cond.)
    else
      drop ." S"
    then
  ;

  \ Print out 'S' if a bit is set
  : s?. ( low shift -- ) 1 bitfield if ." S" then ;

  \ Print out 'S' if bit 4 is set
  : 4s?. ( low -- ) 4 1 bitfield if ." S" then ;

  \ Type a PC-relative address
  : rel. ( pc rel extend -- ) rot dup >r 4 + -rot extend + r> swap addr. ;

  \ Type a 4-aligned PC-relative address
  : rel4. ( pc rel extend -- )
    rot dup >r 4 + 4 align -rot extend + r> swap addr.
  ;

  \ Type a non-sign-extended PC-relative address
  : nrel. ( pc rel -- ) swap dup >r 4 + swap + r> swap addr. ;

  \ Type a non-sign-extended 4-aligned PC-relative address
  : nrel4. ( pc rel -- ) swap dup >r 2 + 4 align swap + r> swap addr. ;

  \ Type out .W
  : .w ( -- ) ." .W " ;

  defined? float32 [if]

    \ Type out .F32
    : .f32 ( -- ) ." .F32 " ;

  [then]

  \ Type a constant at a non-sign-extended 4-aligned PC-relative address
  : nconst4. ( pc rel -- ) swap 2 + 4 align swap + @ space ." @ " h.8 ;

  \ Commit to flash
  commit-flash

  \ Type out a register and a separator
  : reg-sep. ( reg -- ) reg. sep. ;

  defined? float32 [if]

    \ Type out a single-precision floating-point register and a separator
    : sreg-sep. ( sreg -- ) sreg. sep. ;
    
  [then]
  
  \ Type out a register, a separator, and an immediate marker
  : reg-sep-imm. ( reg -- ) reg. sep-imm. ;

  \ Type out a condition with a space for 16-bit words
  : csp. ( cond low -- low ) swap cond. space ;

  \ Type out a condition or S with a space for 16-bit words
  : cssp. ( cond low -- low ) swap conds. space ;

  \ Type out the condition and .W for 32-bit words
  : c.w ( cond low high -- low high ) rot cond. .w ;

  \ Type out the condition and a space for 32-bit words
  : c.sp ( cond low high -- low high ) rot cond. space ;

  defined? float32 [if]

    \ Type out the condition, ".F32", and a space for 32-bit words
    : cf32.sp ( cond low high -- low high ) rot cond. .f32 ;
    
  [then]

  \ Type out the S, the condition, and .W for 32-bit words
  : 4sc.w ( cond low high -- low high ) over 4s?. rot cond. .w ;

  \ Type out the S and the condition followed by a space for 32-bit words
  : 4sc.sp ( cond low high -- low high ) over 4s?. rot cond. space ;

  \ 0 3 bitfield
  : 0_3_bf ( data -- field ) 0 3 bitfield ;

  \ 3 3 bitfield
  : 3_3_bf ( data -- field ) 3 3 bitfield ;

  \ 3 4 bitfield
  : 3_4_bf ( data -- field ) 3 4 bitfield ;

  \ 8 3 bitfield
  : 8_3_bf ( data -- field ) 8 3 bitfield ;

  \ 8 4 bitfield
  : 8_4_bf ( data -- field ) 8 4 bitfield ;

  \ 0 4 bitfield
  : 0_4_bf ( data -- field ) 0 4 bitfield ;

  \ 0 8 bitfield
  : 0_8_bf ( data -- field ) 0 8 bitfield ;

  \ 12 4 bitfield
  : 12_4_bf ( data -- field ) 12 4 bitfield ;

  \ 10 1 bitfield
  : 10_1_bf ( data -- field ) 10 1 bitfield ;

  \ 4 2 bitfield
  : 4_2_bf ( data -- field ) 4 2 bitfield ;

  \ 6 2 bitfield
  : 6_2_bf ( data -- field ) 6 2 bitfield ;

  \ 12 3 bitfield
  : 12_3_bf ( data -- field ) 12 3 bitfield ;

  \ 6 3 bitfield
  : 6_3_bf ( data -- field ) 6 3 bitfield ;

  \ 0 12 bitfield
  : 0_12_bf ( data -- field ) 0 12 bitfield ;

  defined? float32 [if]

    \ high 12 4 : low 6 bitfield
    : hi_12_4_lo_6_bf ( low high -- field )
      12 4 bitfield 1 lshift swap 6 rshift 1 and or
    ;

    \ high 0 4 : high 5 bitfield
    : hi_0_4_hi_5_bf ( low high -- field )
      nip dup 0 4 bitfield 1 lshift swap 5 rshift 1 and or
    ;

    \ low 0 4 : high 7 bitfield
    : lo_0_4_hi_7_bf ( low high -- field )
      7 rshift 1 and swap 0 4 bitfield 1 lshift or
    ;
    
  [then]
  
  \ Size bitshift
  : size-bitshift ( value type -- value' )
    case
      0 of 2 lshift endof
      [char] H of 1 lshift endof
      [char] B of endof
    endcase
  ;
  
  \ Commit to flash
  commit-flash

  \ Decode a 16-bit AND register instruction
  : decode-and-reg-16 ( low -- )
    dup 0_3_bf reg-sep. 3_3_bf reg.
  ;

  \ Decode a 32-bit AND register instruction
  : decode-and-reg-32 ( low high -- )
    dup 8_4_bf reg-sep. swap 0_4_bf reg-sep.
    dup 0_4_bf reg. dup 4_2_bf over 6_2_bf rot 12_3_bf 2 lshift or
    decode-imm-shift
  ;

  \ Decode a 16-bit ASR immediate instruction
  : decode-asr-imm-16 ( low -- )
    dup 0_3_bf reg-sep. dup 3_3_bf reg-sep-imm.  6 5 bitfield val.
  ;

  \ Decode a 32-bit ASR immediate instruction
  : decode-asr-imm-32 ( low high -- )
    dup 8_4_bf reg-sep. dup 0_4_bf reg-sep-imm.
    dup 6_2_bf swap 12_3_bf 2 lshift or (udec.)
  ;

  \ Decode a 32-bit ASR register instruction
  : decode-asr-reg-32 ( low high -- )
    dup 8_4_bf reg-sep. swap 0_4_bf reg-sep. 0_4_bf reg.
  ;

  \ Decode a 32-bit CMN immediate instruction
  : decode-cmn-imm-32 ( low high -- )
    over 0_4_bf reg-sep-imm.
    dup 0_8_bf swap 12_3_bf 8 lshift or swap 10_1_bf 11 lshift or decode-const12
  ;

  \ Decode a 32-bit CMN register instruction
  : decode-cmn-reg-32 ( low high -- )
    swap 0_4_bf reg-sep.
    dup 0_4_bf reg.
    dup 4_2_bf
    over 6_2_bf rot 12_3_bf 2 lshift or and decode-imm-shift
  ;

  \ Decode a 16-bit ADD immediate with a 3-bit unextended immediate
  : decode-add-imm-1 ( low -- )
    dup 0_3_bf reg-sep. dup 3_3_bf reg-sep-imm. 6_3_bf val.
  ;

  \ Decode a 16-bit ADD immediate with a 8-bit unextended immediate
  : decode-add-imm-2 ( low -- )
    dup 8_3_bf reg-sep-imm. 0_8_bf val.
  ;

  \ Decode a 32-bit ADD immediate or ADC immediate instruction
  : decode-add-imm-3 ( low high -- )
    dup 8_4_bf reg-sep.
    over 0_4_bf reg-sep-imm.
    dup 0_8_bf swap 12_3_bf 8 lshift or
    swap 10_1_bf 11 lshift or decode-const12
  ;

  \ Decode a 32-bit ADD immediate with a 12-bit unextended immediate
  : decode-add-imm-4 ( low high -- )
    dup 8_4_bf reg-sep.
    over 0_4_bf reg-sep-imm.
    dup 0_8_bf swap 12_4_bf 8 lshift or swap 10_1_bf 11 lshift or val.
  ;

  \ Decode the first kind of 16-bit ADD register instructions
  : decode-add-reg-16 ( low -- )
    dup 0_3_bf reg-sep. dup 3_3_bf reg-sep. 6_3_bf reg.
  ;

  \ Decode the second kind of 32-bit ADD register or ADC register instruction
  : decode-add-reg-32 ( low high --- )
    dup 8_4_bf reg-sep. decode-cmn-reg-32
  ;

  \ Decode a 16-bit immediate value 32-bit MOV immediate instruction
  : decode-mov-imm-32 ( low high -- )
    dup 8_4_bf reg-sep-imm. dup 0_8_bf swap 12_3_bf 8 lshift or
    over 10_1_bf 11 lshift or swap 0_4_bf 12 lshift or val.
  ;

  \ Decode a CBNZ/CBZ instruction
  : decode-cbz ( pc cond h -- )
    nip space dup 0_3_bf reg-sep.
    dup 3 5 bitfield swap 9 1 bitfield 5 lshift or 1 lshift nrel.
  ;

  \ Decode an SMULL instruction
  : decode-smull ( low high -- )
    dup 12_4_bf reg-sep. dup 8_4_bf reg-sep. swap 0_4_bf reg-sep. 0_4_bf reg.
  ;
  
  \ Decode an LDR immediate instruction
  : decode-ldr-imm-1
    >r dup 0_3_bf reg-sep. ." [" dup 3_3_bf reg.
    6 5 bitfield r> size-bitshift ?dup if
      sep-imm. val.
    then
    ." ]"
  ;

  \ Decode an LDR immediate instruction
  : decode-ldr-imm-2
    dup 8_3_bf reg-sep. ." [SP"
    0_8_bf 2 lshift ?dup if
      sep-imm. val.
    then
    ." ]"
  ;

  \ Decode an LDR immediate instruction
  : decode-ldr-imm-3
    dup 12_4_bf reg-sep. ." [" swap 0_4_bf reg.
    0 12 bitfield ?dup if
      2 lshift sep-imm. val.
    then
    ." ]"
  ;

  \ Decode an LDR immediate instruction
  : decode-ldr-imm-4
    dup 12_4_bf reg-sep. ." [" swap 0_4_bf reg.
    dup 10_1_bf if
      sep-imm. dup 0_8_bf over 9 1 bitfield 0= if negate then (dec.) ." ]"
      8 1 bitfield if ." !" then
    else
      ." ]" sep-imm. dup 0_8_bf swap 9 1 bitfield 0= if negate then (dec.)
    then
  ;

  \ Decode an LDR register instruction
  : decode-ldr-reg-1
    dup 0_3_bf reg. ." , [" dup 3_3_bf reg-sep.
    6_3_bf reg. ." ]"
  ;

  \ Decode an LDR register instruction
  : decode-ldr-reg-2
    dup 12_3_bf reg. ." , [" swap 0_3_bf reg-sep.
    0_3_bf reg. 4_2_bf ?dup if ." , LSL #" (udec.) then ." ]"
  ;

  defined? float32 [if]

    \ Deocde a one-argument single-precision floating-point instruction
    : decode-instr-sr ( addr low high -- )
      hi_12_4_lo_6_bf sreg. drop
    ;
    
    \ Decode a two-argument single-precision floating-point instruction
    : decode-instr-2*sr ( addr low high -- )
      2dup hi_12_4_lo_6_bf sreg-sep. hi_0_4_hi_5_bf sreg. drop
    ;
    
    \ Decode a three-argument single-precision floating-point instruction
    : decode-instr-3*sr ( addr low high -- )
      2dup hi_12_4_lo_6_bf sreg-sep. 2dup lo_0_4_hi_7_bf sreg-sep.
      hi_0_4_hi_5_bf sreg. drop
    ;

    \ Decode a one-argument single-precision floating-point instruction
    : decode-instr-sr-fract ( addr low high size -- )
      -rot 2dup hi_12_4_lo_6_bf dup sreg-sep. sreg-sep.
      hi_0_4_hi_5_bf - ." #" . drop
    ;

    \ Decode a single-precision load/store multiple instruction
    : decode-instr-sr-load/store-multi { addr low high update -- }
      low 0_4_bf reg. update if ." !" then ." , {"
      low high hi_12_4_lo_6_bf { vd }
      high 0_8_bf { count }
      count vd + vd ?do i sreg. i count vd + 1- <> if ." , " then loop
      ." }"
    ;

    \ Decode a single-precision floating-point load/store instruction
    : decode-instr-sr-load/store-imm ( addr low high -- )
      2dup hi_12_4_lo_6_bf sreg-sep. ." ["
      over 0_4_bf reg.
      0_8_bf swap 7 bit and 0= { imm8 sub }
      imm8 0<> sub or if
        sep. ." #" sub if ." -" then imm8 2 lshift (udec.)
      then
      ." ]" drop
    ;

    \ Decode a VMOV immediate instruction
    : decode-instr-sr-imm ( addr low high -- )
      2dup hi_12_4_lo_6_bf sreg-sep.
      ." #$" swap 0_4_bf 4 lshift swap 0_4_bf or h.2 drop
    ;

    \ Decode a core register/single-precision floating point register transfer
    \ instruction
    : decode-instr-cr-sr { addr low high rev -- }
      low high lo_0_4_hi_7_bf { sreg }
      high 12_4_bf { reg }
      rev if reg reg-sep. sreg sreg. else sreg sreg-sep. reg reg. then
    ;

    \ Decode a double core register/single-precision floating point register
    \ transfer instruction
    : decode-instr-2*cr-2*sr { addr low high rev -- }
      low high hi_0_4_hi_5_bf { vm }
      low 0_4_bf { rt2 }
      high 12_4_bf { rt }
      rev if
        rt reg-sep. rt2 reg-sep. vm sreg-sep. vm 1+ sreg.
      else
        vm sreg-sep. vm 1+ sreg-sep. rt reg-sep. rt2 reg.
      then
    ;

    \ Decode a core register to/from FPSCR transfer instruction
    : decode-instr-cr-fpscr { addr low high rev -- }
      high 12_4_bf { reg }
      rev if reg reg-sep. ." FPSCR" else ." FPSCR" reg reg. then
    ;

  [then]
  
  commit-flash

  thumb-2? [if]
    
  \ Parse an ADC immediate instruction
    : p-adc-imm
      ." ADC" 4sc.sp decode-add-imm-3 drop
    ;

  [then]

  \ Parse an ADC register instruction
  : p-adc-reg-1
    ." ADC" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an ADC register instruction
    : p-adc-reg-2
      ." ADC" 4sc.w decode-add-reg-32 drop
    ;

  [then]

  \ Parse an ADD immediate instruction
  : p-add-imm-1
    ." ADD" cssp. decode-add-imm-1 drop
  ;

  \ Parse an ADD immediate instruction
  : p-add-imm-2
    ." ADD" cssp. decode-add-imm-2 drop
  ;

  thumb-2? [if]
    
    \ Parse an ADD immediate instruction
    : p-add-imm-3
      ." ADD" 4sc.w decode-add-imm-3 drop
    ;
    
    \ Parse an ADD immediate instruction
    : p-add-imm-4
      ." ADDW" c.sp decode-add-imm-4 drop
    ;

  [then]

  \ Parse an ADD register instruction
  : p-add-reg-1
    ." ADD" cssp. decode-add-reg-16 drop
  ;

  \ Parse an ADD register instruction
  : p-add-reg-2
    ." ADD" csp.
    dup 0_3_bf over 7 1 bitfield 3 lshift or reg-sep. 3_4_bf reg. drop
  ;

  thumb-2? [if]

    \ Parse an ADD register instruction
    : p-add-reg-3
      ." ADD" 4sc.w decode-add-reg-32 drop
    ;

  [then]

  \ Parse an ADD SP to immediate instruction
  : p-add-sp-imm-1
    ." ADD" csp. dup 8_3_bf reg. ." , SP, #" 0_8_bf 2 lshift val. drop
  ;

  \ Parse an ADD immediate to SP instruction
  : p-add-sp-imm-2
    ." ADD" csp. ." SP, SP, #" 0 7 bitfield 2 lshift val. drop
  ;

  \ Parse an SUB immediate from  SP instruction
  : p-sub-sp-imm-1
    ." SUB" csp. ." SP, SP, #" 0 7 bitfield 2 lshift val. drop
  ;

  \ \ Parse an ADD SP to immediate instruction
  \ : p-add-sp-imm
  \   ." ADD" 4sc.sp
  \   dup 8_4_bf reg. ." , SP, #" dup 0_8_bf swap 12_4_bf 8 lshift or
  \   swap 10_1_bf 11 lshift or decode-const12 drop
  \ ;

  \ \ Parse an ADD SP to immediate instruction
  \ : p-add-sp-imm
  \   ." ADDW" c.sp
  \   dup 8_4_bf reg. ." , SP, #" dup 0_8_bf swap 12_4_bf 8 lshift or
  \   swap 10_1_bf 11 lshift or val. drop
  \ ;

  \ \ Parse an ADD SP to register instruction
  \ : p-add-sp-reg-1
  \   ." ADD" csp.
  \   dup 0_3_bf over 7 1 bitfield 3 lshift or dup reg. ." , SP, " reg. drop
  \ ;

  \ Parse an ADD SP to register instruction
  : p-add-sp-reg-2
    ." ADD" csp. ." SP, " 2 4 bitfield reg. drop
  ;
  
  \ \ Parse an ADD SP to register instruction
  \ : p-add-sp-reg-3
  \   ." ADD" swap 4s?. swap cond. .w
  \   dup 8_4_bf reg. ." , SP, " dup 0_4_bf reg. dup 4_2_bf
  \   over 6_2_bf rot 12_3_bf lshift 2 or decode-imm-shift drop
  \ ;

  \ Parse an ADR instruction
  : p-adr-1
    ." ADD" csp. dup 8_3_bf reg. ." , PC, #"
    0_8_bf 2 lshift val. drop
  ;

  thumb-2? [if]  

    \ Parse an ADR instruction
    : p-adr-2
      ." SUB" c.sp dup 8_4_bf reg. ." , PC, #"
      dup 0_8_bf swap 12_3_bf 8 lshift or swap 10_1_bf 12 lshift or val. drop
    ;
    
    \ Parse an ADR instruction
    : p-adr-3
      ." ADD" c.sp dup 8_4_bf reg. ." , PC, #"
      dup 0_8_bf swap 12_3_bf 8 lshift or swap 10_1_bf 12 lshift or val. drop
    ;
    
    \ Parse an AND immediate instruction
    : p-and-imm
      ." AND" 4sc.sp dup 8_4_bf reg-sep.
      over 0_4_bf reg-sep-imm. dup 0_8_bf swap 12_3_bf 8 lshift or
      swap 10_1_bf 12 lshift or decode-const12 drop
    ;

  [then]

  \ Parse an AND register instruction
  : p-and-reg-1
    ." AND" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an AND register instruction
    : p-and-reg-2
      ." AND" 4sc.w decode-and-reg-32 drop
    ;

  [then]

  \ Parse an ASR immediate instruction
  : p-asr-imm-1
    ." ASR" cssp. decode-asr-imm-16 drop
  ;
  
  thumb-2? [if]
    
    \ Parse an ASR immediate instruction
    : p-asr-imm-2
      ." ASR" swap 4s?. swap cond. .w decode-asr-imm-32 drop
    ;

  [then]

  \ Parse an ASR register instruction
  : p-asr-reg-1
    ." ASR" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an ASR register instruction
    : p-asr-reg-2
      ." ASR" 4sc.w decode-asr-reg-32 drop
    ;

  [then]

  \ Parse an SVC instruction
  : p-svc
    ." SVC" csp. 0_8_bf ." #" val. drop
  ;

  \ Parse an SXTB register instruction
  : p-sxtb-reg-1
    ." SXTB" csp. decode-and-reg-16 drop
  ;

  \ Parse an SXTH register instruction
  : p-sxth-reg-1
    ." SXTH" csp. decode-and-reg-16 drop
  ;

  \ Parse an UXTB register instruction
  : p-uxtb-reg-1
    ." UXTB" csp. decode-and-reg-16 drop
  ;

  \ Parse an UXTH register instruction
  : p-uxth-reg-1
    ." UXTH" csp. decode-and-reg-16 drop
  ;

  \ Parse a REV register instruction
  : p-rev-reg-1
    ." REV" csp. decode-and-reg-16 drop
  ;

  \ Parse a REV16 register instruction
  : p-rev16-reg-1
    ." REV16" csp. decode-and-reg-16 drop
  ;

  \ Parse a REVSH register instruction
  : p-revsh-reg-1
    ." REVSH" csp. decode-and-reg-16 drop
  ;

  \ Parse a ROR register instruction
  : p-ror-reg-1
    ." ROR" cssp. decode-and-reg-16 drop
  ;

  \ Parse a B instruction
  : p-b-1
    dup 8_4_bf $F = if
      p-svc
    else
      ." B" nip dup 8_4_bf cond. space 0_8_bf 1 lshift 9 rel.
    then
  ;

  \ Parse a B instruction
  : p-b-2
    ." B" csp. 0 11 bitfield 1 lshift 12 rel.
  ;
  
  thumb-2? [if]
    
    \ Parse a B instruction
    : p-b-3
      ." B" rot drop over 6 4 bitfield cond. .w
      dup 0 11 bitfield 2 pick 0 6 bitfield 11 lshift or
      over 13 1 bitfield 17 lshift or swap 11 1 bitfield 18 lshift or
      swap 10_1_bf 19 lshift or 1 lshift 21 rel.
    ;
    
    \ Parse a B instruction
    : p-b-4
      ." B" c.w
      dup 0 11 bitfield 2 pick 0 10 bitfield 11 lshift or
      over 11 1 bitfield 3 pick 10_1_bf xor not 1 and 21 lshift or
      swap 13 1 bitfield 2 pick 10_1_bf xor not 1 and 22 lshift or
      swap 10_1_bf 23 lshift or 1 lshift 25 rel.
    ;
    
    
    \ Parse a BFC instruction
    : p-bfc
      ." BFC" c.sp nip 8_4_bf reg-sep-imm.
      dup 6_2_bf over 12_3_bf 2 lshift or dup val. sep-imm.
      over 0 5 bitfield 1+ swap - val. drop
    ;
    
    \ Parse a BFI instruction
    : p-bfi
      ." BFI" c.sp dup 8_4_bf reg-sep. swap 0_4_bf reg-sep-imm.
      dup 6_2_bf over 12_3_bf 2 lshift or dup val. sep-imm.
      over 0 5 bitfield 1+ swap - val. drop
    ;

    \ Parse a BIC immediate instruction
    : p-bic-imm
      ." BIC" over 4s?. space decode-add-imm-3 drop
    ;

  [then]

  \ Parse a BIC register instruction
  : p-bic-reg-1
    ." BIC" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse a BIC register instruction
    : p-bic-reg-2
      ." BIC" 4sc.w decode-add-reg-32 drop
    ;

  [then]

  \ Parse a BKPT instruction
  : p-bkpt
    ." BKPT" space nip 0_8_bf ." #" val. drop
  ;

  \ Parse a BL immediate instruction
  : p-bl-imm
    ." BL" c.sp
    dup 0 11 bitfield
    2 pick 0 10 bitfield 11 lshift or
    over 11 1 bitfield 3 pick 10 1 bitfield xor not 1 and 21 lshift or
    swap 13 1 bitfield 2 pick 10 1 bitfield xor not 1 and 22 lshift or
    swap 10 1 bitfield 23 lshift or 1 lshift 25 rel.
  ;

  \ Parse a BLX register instruction
  : p-blx-reg
    ." BLX" csp. 3_4_bf reg. drop
  ;

  \ Parse a BX register instruction
  : p-bx
    ." BX" csp. 3_4_bf reg. drop
  ;

  \ Parse a CBNZ instruction
  : p-cbnz ." CBNZ" decode-cbz ;

  \ Parse a CBZ instruction
  : p-cbz ." CBZ" decode-cbz ;

  thumb-2? [if]
    
    \ Parse a CMN immediate instruction
    : p-cmn-imm
      ." CMN" csp. decode-cmn-imm-32 drop
    ;

  [then]

  \ Parse a CMN register instruction
  : p-cmn-reg-1
    ." CMN" csp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse a CMN register instruction
    : p-cmn-reg-2
      ." CMN" swap cond. .w decode-cmn-reg-32
    ;

  [then]

  \ Parse an CMP immediate instruction
  : p-cmp-imm-1
    ." CMP " nip decode-add-imm-2 drop
  ;

  thumb-2? [if]
    
    \ Parse a CMP immediate instruction
    : p-cmp-imm-2
      ." CMP" c.w decode-cmn-imm-32 drop
    ;

  [then]

  \ Parse a CMP register instruction
  : p-cmp-reg-1
    ." CMP" csp. decode-and-reg-16 drop
  ;

  \ Parse a CMP register instruction
  : p-cmp-reg-2
    ." CMP" csp. dup 0_3_bf over 7 1 bitfield 3 lshift or reg-sep.
    3_4_bf reg. drop
  ;

  thumb-2? [if]
    
    \ Parse a CMP register instruction
    : p-cmp-reg-3
      ." CMP" c.w decode-cmn-reg-32 drop
    ;

  [then]

  \ Parse a CPS instruction
  : p-cps-1
    ." CPS" nip dup 4 1 bitfield if [char] D else [char] E then emit space
    dup 2 1 bitfield if [char] A emit then
    dup 1 1 bitfield if [char] I emit then
    0 1 bitfield if [char] F emit then drop
  ;
  
  \ Parse a DMB instruction
  : p-dmb ." DMB" 2drop 2drop ;

  \ Parse a DSB instruction
  : p-dsb
    ." DSB" nip drop 0_4_bf case
      \ %1111 of endof
      %0111 of space ." UN" endof
      %1110 of space ." ST" endof
      %0110 of space ." UNST" endof
    endcase
    drop
  ;

  \ Parse an ISB instruction
  : p-isb ." ISB" 2drop 2drop ;

  thumb-2? [if]
    
    \ Parse an EOR immediate instruction
    : p-eor-imm
      ." EOR" rot drop over 4s?. decode-add-imm-3 drop
    ;

  [then]

  \ Parse an EOR register instruction
  : p-eor-reg-1
    ." EOR" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an EOR register instruction
    : p-eor-reg-2
      ." EOR" 4sc.w decode-add-reg-32 drop
    ;

  [then]

  \ Parse an LDMIA instruction
  : p-ldmia-1
    ." LDMIA" csp. dup 8_3_bf dup reg.
    swap 0_8_bf dup rot 8 not-in-reglist. sep. 8 reglist. drop
  ;

  \ Parse an LDR immediate instruction
  : p-ldr-imm-1
    ." LDR" dup >r size. csp. r> decode-ldr-imm-1 drop
  ;

  \ Parse an LDR immediate instruction
  : p-ldr-imm-2
    ." LDR" size. csp. decode-ldr-imm-2 drop
  ;

  thumb-2? [if]
    
    \ Parse an LDR immediate instruction
    : p-ldr-imm-3
      ." LDR" size. c.w decode-ldr-imm-3 drop
    ;
    
    \ Parse an LDR immediate instruction
    : p-ldr-imm-4
      ." LDR" size. c.sp decode-ldr-imm-4 drop
    ;

  [then]

  \ Parse an LDR literal instruction
  : p-ldr-lit-1
    ." LDR" size. csp. dup 8_3_bf reg-sep. 0_8_bf 2 lshift
    for-gas @ if ." [PC, #" (udec.) ." ]" drop else 2dup nrel4. nconst4. then
  ;

  thumb-2? [if]
    
    \ Parse an LDR literal instruction
    : p-ldr-lit-2
      ." LDR" size. c.w dup dup 12_4_bf reg-sep.
      0_12_bf swap 7 1 bitfield if negate then
      for-gas @ if ." [PC, #" (dec.) ." ]" drop else 2dup nrel4. nconst4. then
    ;

  [then]
  
  \ Parse an LDR register instruction
  : p-ldr-reg-1
    ." LDR" size. csp. decode-ldr-reg-1 drop
  ;

  thumb-2? [if]
    
    \ Parse an LDR register instruction
    : p-ldr-reg-2
      ." LDR" size. c.w decode-ldr-reg-2 drop
    ;

  [then]

  \ Parse an LSL immediate instruction
  : p-lsl-imm-1
    ." LSL" cssp. decode-asr-imm-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an LSL immediate instruction
    : p-lsl-imm-2
      ." LSL" 4sc.w decode-asr-imm-32 drop
    ;

  [then]

  \ Parse an LSL register instruction
  : p-lsl-reg-1
    ." LSL" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an LSL register instruction
    : p-lsl-reg-2
      ." LSL" 4sc.w decode-asr-reg-32 drop
    ;

  [then]
  
  \ Parse an LSR immediate instruction
  : p-lsr-imm-1
    ." LSR" cssp. decode-asr-imm-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an LSR immediate instruction
    : p-lsr-imm-2
      ." LSR" 4sc.w decode-asr-imm-32 drop
    ;

  [then]

  \ Parse an LSR register instruction
  : p-lsr-reg-1
    ." LSR" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an LSR register instruction
    : p-lsr-reg-2
      ." LSR" 4sc.w decode-asr-reg-32 drop
    ;

  [then]

  \ Parse an MLS instruction
  : p-mls
    ." MLS" rot drop space dup 8_4_bf reg-sep. swap 0_4_bf reg-sep.
    dup 0_4_bf reg-sep. 12_4_bf reg. drop
  ;

  \ Parse a MOV immediate instruction
  : p-mov-imm-1
    ." MOV" cssp. decode-add-imm-2 drop
  ;

  thumb-2? [if]
    
    \ Parse a MOV immediate instruction
    : p-mov-imm-2
      ." MOV" 4sc.w dup 8_4_bf reg-sep-imm.
      dup 0_8_bf swap 12_3_bf 8 lshift or
      swap 10_1_bf 11 lshift or decode-const12 drop
    ;
    
    \ Parse a MOV immediate instruction
    : p-mov-imm-3
      ." MOVW" c.sp decode-mov-imm-32 drop
    ;

  [then]

  \ Parse a MOV register instruction
  : p-mov-reg-1
    ." MOV" csp. dup 0_3_bf over 7 1 bitfield 3 lshift or reg-sep.
    3_4_bf reg. drop
  ;

  \ Parse a MOV register instruction
  : p-mov-reg-2
    ." MOVS" space nip decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse a MOV register instruction
    : p-mov-reg-3
      ." MOV" 4sc.w nip dup 8_4_bf reg-sep. 0_4_bf reg. drop
    ;

    \ Parse a MOVT instruction
    : p-movt
      ." MOVT" c.sp decode-mov-imm-32 drop
    ;

  [then]
  
  \ Parse a MUL instruction
  : p-mul-1
    ." MUL" cssp. decode-and-reg-16 drop
  ;
  
  thumb-2? [if]
    
    \ Parse a MUL instruction
    : p-mul-2
      ." MUL" over 4s?. space decode-asr-reg-32 drop
    ;

  [then]
    
  \ Parse an MVN register instruction
  : p-mvn-reg-1
    ." MVN" cssp. decode-and-reg-16 drop
  ;

  \ Parse a NOP instruction
  : p-nop-1
    ." NOP" drop cond. drop
  ;

  thumb-2? [if]
    
    \ Parse an ORR immediate instruction
    : p-orr-imm
      ." ORR" 4sc.sp decode-add-imm-3 drop
    ;

  [then]

  \ Parse an ORR register instruction
  : p-orr-reg-1
    ." ORR" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an ORR register instruction
    : p-orr-reg-2
      ." ORR" 4sc.w decode-and-reg-32 drop
    ;

  [then]

  \ Parse a POP instruction
  : p-pop-1
    ." POP" csp. dup 0_8_bf swap 8 1 bitfield 15 lshift or 16 reglist. drop
  ;

  thumb-2? [if]
    
    \ Parse a POP instruction
    : p-pop-2
      ." POP" c.w nip dup 0_12_bf swap 14 2 bitfield 14 lshift or 16 reglist.
      drop
    ;

  [then]

  \ Parse a PUSH instruction
  : p-push-1
    ." PUSH" csp. dup 0_8_bf swap 8 1 bitfield 14 lshift or 16 reglist. drop
  ;

  thumb-2? [if]  

    \ Parse a PUSH instruction
    : p-push-2
      ." PUSH" c.w nip dup 0_12_bf swap 14 1 bitfield 14 lshift or 16 reglist.
      drop
    ;

  [then]

  \ Parse an RSB immediate instruction
  : p-rsb-imm-1
    ." RSB " cssp. decode-and-reg-16 ." , #0" drop
  ;

  thumb-2? [if]
    
    \ Parse an SBC immediate instruction
    : p-sbc-imm
      ." SBC" 4sc.sp decode-add-imm-3 drop
    ;

  [then]

  \ Parse an SBC register instruction
  : p-sbc-reg-1
    ." SBC" cssp. decode-and-reg-16 drop
  ;

  thumb-2? [if]
    
    \ Parse an SBC register instruction
    : p-sbc-reg-2
      ." SBC" 4sc.w decode-and-reg-32 drop
    ;

    \ Parse an SDIV register instruction
    : p-sdiv
      ." SDIV" c.sp decode-asr-reg-32 drop
    ;
    
    \ Parse an SMULL instruction
    : p-smull
      ." SMULL" c.sp decode-smull drop
    ;

  [then]

  \ Parse an SEV instruction
  : p-sev-1
    ." SEV" swap cond. 2drop
  ;
  
  \ Parse an STR immediate instruction
  : p-str-imm-1
    ." STR" dup >r size. csp. r> decode-ldr-imm-1 drop
  ;

  \ Parse an STR immediate instruction
  : p-str-imm-2
    ." STR" size. csp. decode-ldr-imm-2 drop
  ;

  thumb-2? [if]

    \ Parse an STR immediate instruction
    : p-str-imm-3
      ." STR" size. c.w decode-ldr-imm-3 drop
    ;
    
    \ Parse an STR immediate instruction
    : p-str-imm-4
      ." STR" size. c.sp decode-ldr-imm-4 drop
    ;

  [then]

  \ Parse an STR register instruction
  : p-str-reg-1
    ." STR" size. csp. decode-ldr-reg-1 drop
  ;

  thumb-2? [if]

    \ Parse an STR register instruction
    : p-str-reg-2
      ." STR" size. c.w decode-ldr-reg-2 drop
    ;

  [then]
    
  \ Parse an SUB immediate instruction
  : p-sub-imm-1
    ." SUB" cssp. decode-add-imm-1 drop
  ;

  \ Parse an SUB immediate instruction
  : p-sub-imm-2
    ." SUB" cssp. decode-add-imm-2 drop
  ;

  thumb-2? [if]

    \ Parse an SUB immediate instruction
    : p-sub-imm-3
      ." SUB" 4sc.w decode-add-imm-3 drop
    ;
    
    \ Parse an SUB immediate instruction
    : p-sub-imm-4
      ." SUBW" c.sp decode-add-imm-4 drop
    ;

  [then]

  \ Parse an SUB register instruction
  : p-sub-reg-1
    ." SUB" cssp. decode-add-reg-16 drop
  ;

  thumb-2? [if]

    \ Parse an SUB register instruction
    : p-sub-reg-2
      ." SUB" 4sc.w decode-add-reg-32 drop
    ;
    
    \ Parse a TST immediate instruction
    : p-tst-imm
      ." TST" csp. decode-cmn-imm-32 drop
    ;

  [then]

  \ Parse a TST register instruction
  : p-tst-reg-1
    ." TST" csp. decode-and-reg-16 drop
  ;

  thumb-2? [if]

    \ Parse a CMP register instruction
    : p-tst-reg-2
      ." CMP" c.w decode-cmn-reg-32 drop
    ;
    
    \ Parse an UDIV register instruction
    : p-udiv
      ." UDIV" c.sp decode-asr-reg-32 drop
    ;
    
    \ Parse an UMULL instruction
    : p-umull
      ." UMULL" c.sp decode-smull drop
    ;

  [then]

  \ Parse an WFE instruction
  : p-wfe-1
    ." WFE" swap cond. 2drop
  ;
  
  \ Parse an WFI instruction
  : p-wfi-1
    ." WFI" swap cond. 2drop
  ;

  \ Parse a YIELD instruction
  : p-yield-1
    ." YIELD" swap cond. 2drop
  ;

  \ Parse a string
  : p-start-string
    ." STRING: " 2drop 2 + dup c@ 1+ over + 2 align data.
  ;

  defined? float32 [if]

    \ Parse a VABS.F32 instruction
    : p-vabs-f32
      ." VABS" cf32.sp decode-instr-2*sr
    ;

    \ Parse a VADD.F32 instruction
    : p-vadd-f32
      ." VADD" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VCMP.F32 instruction
    : p-vcmp-f32
      ." VCMP" cf32.sp decode-instr-2*sr
    ;

    \ Parse a VCMPE.F32 instruction
    : p-vcmpe-f32
      ." VCMPE" cf32.sp decode-instr-2*sr
    ;

    \ Parse a VCMP.F32 #0.0 instruction
    : p-vcmp-f32-#0.0
      ." VCMP" cf32.sp decode-instr-sr ." , #0.0"
    ;

    \ Parse a VCMPE.F32 #0.0 instruction
    : p-vcmpe-f32-#0.0
      ." VCMPE" cf32.sp decode-instr-sr ." , #0.0"
    ;

    \ Parse a VCVTA.S32.F32 instruction
    : p-vcvta-s32-f32
      ." VCVTA.S32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTA.U32.F32 instruction
    : p-vcvta-u32-f32
      ." VCVTA.U32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTN.S32.F32 instruction
    : p-vcvtn-s32-f32
      ." VCVTN.S32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTN.U32.F32 instruction
    : p-vcvtn-u32-f32
      ." VCVTN.U32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTP.S32.F32 instruction
    : p-vcvtp-s32-f32
      ." VCVTP.S32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTP.U32.F32 instruction
    : p-vcvtp-u32-f32
      ." VCVTP.U32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTM.S32.F32 instruction
    : p-vcvtm-s32-f32
      ." VCVTM.S32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTM.U32.F32 instruction
    : p-vcvtm-u32-f32
      ." VCVTM.U32.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VCVTR.S32.F32 instruction
    : p-vcvtr-s32-f32
      ." VCVTR" rot cond. ." .S32.F32 " decode-instr-2*sr
    ;

    \ Parse a VCVTR.U32.F32 instruction
    : p-vcvtr-u32-f32
      ." VCVTR" rot cond. ." .U32.F32 " decode-instr-2*sr
    ;

    \ Parse a VCVT.S32.F32 instruction
    : p-vcvt-s32-f32
      ." VCVT" rot cond. ." .S32.F32 " decode-instr-2*sr
    ;

    \ Parse a VCVT.U32.F32 instruction
    : p-vcvt-u32-f32
      ." VCVT" rot cond. ." .U32.F32 " decode-instr-2*sr
    ;

    \ Parse a VCVT.F32.S32 instruction
    : p-vcvt-f32-s32
      ." VCVT" rot cond. ." .F32.S32 " decode-instr-2*sr
    ;

    \ Parse a VCVT.F32.U32 instruction
    : p-vcvt-f32-u32
      ." VCVT" rot cond. ." .F32.U32 " decode-instr-2*sr
    ;

    \ Parse a VCVT.U16.F32 fixed-point instruction
    : p-vcvt-u16-f32-#
      ." VCVT" rot cond. ." .U16.F32 " 16 decode-instr-sr-fract
    ;

    \ Parse a VCVT.S16.F32 fixed-point instruction
    : p-vcvt-s16-f32-#
      ." VCVT" rot cond. ." .U16.F32 " 16 decode-instr-sr-fract
    ;

    \ Parse a VCVT.U32.F32 fixed-point instruction
    : p-vcvt-u32-f32-#
      ." VCVT" rot cond. ." .U32.F32 " 32 decode-instr-sr-fract
    ;

    \ Parse a VCVT.S32.F32 fixed-point instruction
    : p-vcvt-s32-f32-#
      ." VCVT" rot cond. ." .U32.F32 " 32 decode-instr-sr-fract
    ;

    \ Parse a VCVT.F32.U16 fixed-point instruction
    : p-vcvt-f32-u16-#
      ." VCVT" rot cond. ." .F32.U16 " 16 decode-instr-sr-fract
    ;

    \ Parse a VCVT.F32.S16 fixed-point instruction
    : p-vcvt-f32-s16-#
      ." VCVT" rot cond. ." .F32.U16 " 16 decode-instr-sr-fract
    ;

    \ Parse a VCVT.F32.U32 fixed-point instruction
    : p-vcvt-f32-u32-#
      ." VCVT" rot cond. ." .F32.U32 " 32 decode-instr-sr-fract
    ;

    \ Parse a VCVT.F32.S32 fixed-point instruction
    : p-vcvt-f32-s32-#
      ." VCVT" rot cond. ." .F32.U32 " 32 decode-instr-sr-fract
    ;

    \ Parse a VCVTB.F32.F16 instruction
    : p-vcvtb-f32-f16
      ." VCVTB" rot cond. ." .F32.F16 " decode-instr-2*sr
    ;
    
    \ Parse a VCVTT.F32.F16 instruction
    : p-vcvtt-f32-f16
      ." VCVTT" rot cond. ." .F32.F16 " decode-instr-2*sr
    ;
    
    \ Parse a VCVTB.F16.F32 instruction
    : p-vcvtb-f16-f32
      ." VCVTB" rot cond. ." .F16.F32 " decode-instr-2*sr
    ;
    
    \ Parse a VCVTT.F16.F32 instruction
    : p-vcvtt-f16-f32
      ." VCVTT" rot cond. ." .F16.F32 " decode-instr-2*sr
    ;

    \ Parse a VDIV.F32 instruction
    : p-vdiv-f32
      ." VDIV" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VFMA.F32 instruction
    : p-vfma-f32
      ." VFMA" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VFMS.F32 instruction
    : p-vfms-f32
      ." VFMS" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VFNMA.F32 instruction
    : p-vfnma-f32
      ." VFNMA" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VFNMS.F32 instruction
    : p-vfnms-f32
      ." VFNMS" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VLDMIA.F32 instruction without update
    : p-vldmia-f32
      ." VLDMIA" c.sp false decode-instr-sr-load/store-multi
    ;

    \ Parse a VLDMIA.F32 instruction with update
    : p-vldmia-f32-update
      ." VLDMIA" c.sp true decode-instr-sr-load/store-multi
    ;

    \ Parse a VLDMDB.F32 instruction with update
    : p-vldmdb-f32-update
      ." VLDMDB" c.sp true decode-instr-sr-load/store-multi
    ;

    \ Parse a VLDR instruction
    : p-vldr-f32
      ." VLDR" c.sp decode-instr-sr-load/store-imm
    ;

    \ Parse a VMAXNM.F32 instruction
    : p-vmaxnm-f32
      ." VMAXNM.F32" rot drop decode-instr-3*sr
    ;

    \ Parse a VMINNM.F32 instruction
    : p-vminnm-f32
      ." VMINNM.F32" rot drop decode-instr-3*sr
    ;

    \ Parse a VMLA.F32 instruction
    : p-vmla-f32
      ." VMLA" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VMLS.F32 instruction
    : p-vmls-f32
      ." VMLS" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VMOV.F32 immediate instruction
    : p-vmov-f32-imm
      ." VMOV" cf32.sp decode-instr-sr-imm
    ;

    \ Parse a VMOV.F32 instruction
    : p-vmov-f32
      ." VMOV" cf32.sp decode-instr-2*sr
    ;

    \ Parse a VMOV single-precision floating-point move to core instruction
    : p-vmov-cr-f32
      ." VMOV" c.sp true decode-instr-cr-sr
    ;

    \ Parse a VMOV core to single-precision floating-point move instruction
    : p-vmov-f32-cr
      ." VMOV" c.sp false decode-instr-cr-sr
    ;

    \ Parse a VMOV double single-precision floating-point to core transfer
    \ instruction
    : p-vmov-2*cr-2*f32
      ." VMOV" c.sp true decode-instr-2*cr-2*sr
    ;

    \ Parse a VMOV double core to single-precision floating-point transfer
    \ instruction
    : p-vmov-2*f32-2*cr
      ." VMOV" c.sp false decode-instr-2*cr-2*sr
    ;

    \ Parse a VMRS instruction
    : p-vmrs
      ." VMRS" c.sp true decode-instr-cr-fpscr
    ;

    \ Parse a VMSR instruction
    : p-vmsr
      ." VMSR" c.sp false decode-instr-cr-fpscr
    ;

    \ Parse a VMUL.F32 instruction
    : p-vmul-f32
      ." VMUL" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VNEG instruction
    : p-vneg-f32
      ." VNEG" cf32.sp decode-instr-2*sr
    ;

    \ Parse a VNMLA.F32 instruction
    : p-vnmla-f32
      ." VNMLA" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VNMLS.F32 instruction
    : p-vnmls-f32
      ." VNMLS" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VNMUL.F32 instruction
    : p-vnmul-f32
      ." VNMUL" cf32.sp decode-instr-3*sr
    ;

    \ Parse a VRINTA.F32 instruction
    : p-vrinta-f32
      ." VRINTA.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTN.F32 instruction
    : p-vrintn-f32
      ." VRINTN.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTP.F32 instruction
    : p-vrintp-f32
      ." VRINTP.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTM.F32 instruction
    : p-vrintm-f32
      ." VRINTM.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTX.F32 instruction
    : p-vrintx-f32
      ." VRINTX.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTZ.F32 instruction
    : p-vrintz-f32
      ." VRINTZ.F32 " rot drop decode-instr-2*sr
    ;

    \ Parse a VRINTR.F32 instruction
    : p-vrintr-f32
      ." VRINTR.F32 " rot drop decode-instr-2*sr
    ;
    
    \ Parse a VSELEQ.F32 instruction
    : p-vseleq-f32
      ." VSELEQ.F32 " rot drop decode-instr-3*sr
    ;

    \ Parse a VSELVS.F32 instruction
    : p-vselvs-f32
      ." VSELVS.F32 " rot drop decode-instr-3*sr
    ;

    \ Parse a VSELGE.F32 instruction
    : p-vselge-f32
      ." VSELGE.F32 " rot drop decode-instr-3*sr
    ;

    \ Parse a VSELGT.F32 instruction
    : p-vselgt-f32
      ." VSELGT.F32 " rot drop decode-instr-3*sr
    ;

    \ Parse a VSQRT.F32 instruction
    : p-vsqrt-f32
      ." VSELGT" cf32.sp decode-instr-2*sr
    ;
    
    \ Parse a VSTMIA.F32 instruction without update
    : p-vstmia-f32
      ." VSTMIA" c.sp false decode-instr-sr-load/store-multi
    ;

    \ Parse a VSTMIA.F32 instruction with update
    : p-vstmia-f32-update
      ." VSTMIA" c.sp true decode-instr-sr-load/store-multi
    ;

    \ Parse a VSTMDB.F32 instruction with update
    : p-vstmdb-f32-update
      ." VSTMDB" c.sp true decode-instr-sr-load/store-multi
    ;

    \ Parse a VSTR instruction
    : p-vstr-f32
      ." VSTR" c.sp decode-instr-sr-load/store-imm
    ;

    \ Parse a VSUB.F32 instruction
    : p-vsub-f32
      ." VSUB" cf32.sp decode-instr-3*sr
    ;
    
  [then]

  \ Commit to flash
  commit-flash

  0 ' p-ldr-imm-1 w-size p-ldr-imm-1-w
  0 ' p-ldr-imm-2 w-size p-ldr-imm-2-w

  thumb-2? [if]
  
    0 ' p-ldr-imm-3 w-size p-ldr-imm-3-w
    0 ' p-ldr-imm-4 w-size p-ldr-imm-4-w

  [then]
  
  0 ' p-ldr-lit-1 w-size p-ldr-lit-1-w

  thumb-2? [if]

    0 ' p-ldr-lit-2 w-size p-ldr-lit-2-w

  [then]
  
  0 ' p-ldr-reg-1 w-size p-ldr-reg-1-w

  thumb-2? [if]

    0 ' p-ldr-reg-2 w-size p-ldr-reg-2-w

  [then]
  
  char B ' p-ldr-imm-1 w-size p-ldr-imm-1-b

  thumb-2? [if]

    char B ' p-ldr-imm-3 w-size p-ldr-imm-3-b
    char B ' p-ldr-imm-4 w-size p-ldr-imm-4-b
  [then]
  
  thumb-2? [if]
    
    char B ' p-ldr-lit-2 w-size p-ldr-lit-2-b

  [then]
  
  char B ' p-ldr-reg-1 w-size p-ldr-reg-1-b

  thumb-2? [if]

    char B ' p-ldr-reg-2 w-size p-ldr-reg-2-b

  [then]
  
  char H ' p-ldr-imm-1 w-size p-ldr-imm-1-h

  thumb-2? [if]

    char H ' p-ldr-imm-3 w-size p-ldr-imm-3-h
    char H ' p-ldr-imm-4 w-size p-ldr-imm-4-h
    char H ' p-ldr-lit-2 w-size p-ldr-lit-2-h

  [then]
  
  char H ' p-ldr-reg-1 w-size p-ldr-reg-1-h

  thumb-2? [if]

    char H ' p-ldr-reg-2 w-size p-ldr-reg-2-h

  [then]

  SB ' p-ldr-reg-1 w-size p-ldr-reg-1-sb
  SH ' p-ldr-reg-1 w-size p-ldr-reg-1-sh
  
  0 ' p-str-imm-1 w-size p-str-imm-1-w
  0 ' p-str-imm-2 w-size p-str-imm-2-w

  thumb-2? [if]

    0 ' p-str-imm-3 w-size p-str-imm-3-w
    0 ' p-str-imm-4 w-size p-str-imm-4-w

  [then]
  
  0 ' p-str-reg-1 w-size p-str-reg-1-w

  thumb-2? [if]

    0 ' p-str-reg-2 w-size p-str-reg-2-w

  [then]
  
  char B ' p-str-imm-1 w-size p-str-imm-1-b

  thumb-2? [if]

    char B ' p-str-imm-3 w-size p-str-imm-3-b
    char B ' p-str-imm-4 w-size p-str-imm-4-b

  [then]
  
  char B ' p-str-reg-1 w-size p-str-reg-1-b

  thumb-2? [if]
  
    char B ' p-str-reg-2 w-size p-str-reg-2-b

  [then]
  
  char H ' p-str-imm-1 w-size p-str-imm-1-h

  thumb-2? [if]
  
    char H ' p-str-imm-3 w-size p-str-imm-3-h
    char H ' p-str-imm-4 w-size p-str-imm-4-h

  [then]
  
  char H ' p-str-reg-1 w-size p-str-reg-1-h

  thumb-2? [if]

    char H ' p-str-reg-2 w-size p-str-reg-2-h

  [then]

  \ Highest bit mask
  %1000000000000000 constant highest

  \ Commit to flash
  commit-flash

  \ All the 16-bit ops
  create all-ops16
  ' p-start-string , %1111111111111111 h, start-string h,
  ' p-adc-reg-1 ,    %1111111111000000 h, %0100000101000000 h,
  ' p-add-imm-1 ,    %1111111000000000 h, %0001110000000000 h,
  ' p-add-imm-2 ,    %1111100000000000 h, %0011000000000000 h,
  ' p-add-reg-1 ,    %1111111000000000 h, %0001100000000000 h,
  ' p-add-reg-2 ,    %1111111100000000 h, %0100010000000000 h,
  ' p-add-sp-imm-1 , %1111100000000000 h, %1010100000000000 h,
  ' p-add-sp-imm-2 , %1111111110000000 h, %1011000000000000 h,
  \ ' p-add-sp-reg-1 , %1111111101111000 h, %0100010001101000 h,
  ' p-add-sp-reg-2 , %1111111110000111 h, %0100010010000101 h,
  ' p-adr-1 ,        %1111000000000000 h, %1010000000000000 h,
  ' p-and-reg-1 ,    %1111111111000000 h, %0100000000000000 h,
  ' p-asr-imm-1 ,    %1111100000000000 h, %0001000000000000 h,
  ' p-asr-reg-1 ,    %1111111111000000 h, %0100000100000000 h,
  ' p-b-1 ,          %1111000000000000 h, %1101000000000000 h,
  ' p-b-2 ,          %1111100000000000 h, %1110000000000000 h,
  ' p-bic-reg-1 ,    %1111111111000000 h, %0100001110000000 h,
  ' p-bkpt ,         %1111111100000000 h, %1011111000000000 h,
  ' p-blx-reg ,      %1111111110000000 h, %0100011110000000 h,
  ' p-bx ,           %1111111110000000 h, %0100011100000000 h,
  ' p-cbnz ,         %1111110100000000 h, %1011100100000000 h,
  ' p-cbz ,          %1111110100000000 h, %1011000100000000 h,
  ' p-cmn-reg-1 ,    %1111111111000000 h, %0100001011000000 h,
  ' p-cmp-imm-1 ,    %1111100000000000 h, %0010100000000000 h,
  ' p-cmp-reg-1 ,    %1111111111000000 h, %0100001010000000 h,
  ' p-cmp-reg-2 ,    %1111111100000000 h, %0100010100000000 h,
  ' p-cps-1 ,        %1111111111101000 h, %1011011001100000 h,
  ' p-eor-reg-1 ,    %1111111111000000 h, %0100000001000000 h,
  ' p-ldmia-1 ,      %1111100000000000 h, %1100100000000000 h,
  ' p-ldr-imm-1-w ,  %1111100000000000 h, %0110100000000000 h,
  ' p-ldr-imm-2-w ,  %1111100000000000 h, %1001100000000000 h,
  ' p-ldr-lit-1-w ,  %1111100000000000 h, %0100100000000000 h,
  ' p-ldr-reg-1-w ,  %1111111000000000 h, %0101100000000000 h,
  ' p-ldr-imm-1-b ,  %1111100000000000 h, %0111100000000000 h,
  ' p-ldr-reg-1-b ,  %1111111000000000 h, %0101110000000000 h,
  ' p-ldr-imm-1-h ,  %1111100000000000 h, %1000100000000000 h,
  ' p-ldr-reg-1-h ,  %1111111000000000 h, %0101101000000000 h,
  ' p-ldr-reg-1-sb , %1111111000000000 h, $5600 h,
  ' p-ldr-reg-1-sh , %1111111000000000 h, $5E00 h,
  ' p-mov-reg-2 ,    %1111111111000000 h, %0000000000000000 h,
  ' p-lsl-imm-1 ,    %1111100000000000 h, %0000000000000000 h,
  ' p-lsl-reg-1 ,    %1111111111000000 h, %0100000010000000 h,
  ' p-lsr-imm-1 ,    %1111100000000000 h, %0000100000000000 h,
  ' p-lsr-reg-1 ,    %1111111111000000 h, %0100000011000000 h,
  ' p-mov-imm-1 ,    %1111100000000000 h, %0010000000000000 h,
  ' p-mov-reg-1 ,    %1111111100000000 h, %0100011000000000 h,
  ' p-mul-1 ,        %1111111111000000 h, %0100001101000000 h,
  ' p-mvn-reg-1 ,    %1111111111000000 h, %0100001111000000 h,
  ' p-nop-1 ,        %1111111111111111 h, %1011111100000000 h,
  ' p-orr-reg-1 ,    %1111111111000000 h, %0100001100000000 h,
  ' p-pop-1 ,        %1111111000000000 h, %1011110000000000 h,
  ' p-push-1 ,       %1111111000000000 h, %1011010000000000 h,
  ' p-rev-reg-1 ,    %1111111111000000 h, $BA00 h,
  ' p-rev16-reg-1 ,  %1111111111000000 h, $BA40 h,
  ' p-revsh-reg-1 ,  %1111111111000000 h, $BAC0 h,
  ' p-ror-reg-1 ,    %1111111111000000 h, $41C0 h,
  ' p-rsb-imm-1 ,    %1111111111000000 h, %0100001001000000 h,
  ' p-sbc-reg-1 ,    %1111111111000000 h, %0100000110000000 h,
  ' p-sev-1 ,        %1111111111111111 h, $BF40 h,
  ' p-str-imm-1-w ,  %1111100000000000 h, %0110000000000000 h,
  ' p-str-imm-2-w ,  %1111100000000000 h, %1001000000000000 h,
  ' p-str-reg-1-w ,  %1111111000000000 h, %0101000000000000 h,
  ' p-str-imm-1-b ,  %1111100000000000 h, %0111000000000000 h,
  ' p-str-reg-1-b ,  %1111111000000000 h, %0101010000000000 h,
  ' p-str-imm-1-h ,  %1111100000000000 h, %1000000000000000 h,
  ' p-str-reg-1-h ,  %1111111000000000 h, %0101001000000000 h,
  ' p-sub-imm-1 ,    %1111111000000000 h, %0001111000000000 h,
  ' p-sub-imm-2 ,    %1111100000000000 h, %0011100000000000 h,
  ' p-sub-reg-1 ,    %1111111000000000 h, %0001101000000000 h,
  ' p-sub-sp-imm-1 , %1111111110000000 h, %1011000010000000 h,
  ' p-svc ,          %1111111100000000 h, %1101111100000000 h,
  ' p-sxtb-reg-1 ,   %1111111111000000 h, $B240 h,
  ' p-sxth-reg-1 ,   %1111111111000000 h, $B200 h,
  ' p-tst-reg-1 ,    %1111111111000000 h, %0100001000000000 h,
  ' p-uxtb-reg-1 ,   %1111111111000000 h, $B2C0 h,
  ' p-uxth-reg-1 ,   %1111111111000000 h, $B280 h,
  ' p-wfe-1 ,        %1111111111111111 h, %1011111100100000 h,
  ' p-wfi-1 ,        %1111111111111111 h, %1011111100110000 h,
  ' p-yield-1 ,      %1111111111111111 h, %1011111100010000 h,
  0 ,

  \ All the 32-bit ops
  create all-ops32

  thumb-2? [if]
    
    ' p-adc-imm , %1111101111100000 h, highest h, %1111000101000000 h, 0 h,
    ' p-adc-reg-2 , %1111111111100000 h, 0 h, %1110101101000000 h, 0 h,
    ' p-add-imm-3 , %1111101111100000 h, highest h, %1111000100000000 h, 0 h,
    ' p-add-imm-4 , %1111101111110000 h, highest h, %1111001000000000 h, 0 h,
    ' p-add-reg-3 , %1111111111100000 h, 0 h, %1110101100000000 h, 0 h,
    \ ' p-add-sp-imm-3 , %1111101111101111 h, highest h, %1111000100001101 h, 0 h,
    \ ' p-add-sp-imm-4 , %1111101111111111 h, highest h, %1111001000001101 h, 0 h,
    \ ' p-add-sp-reg-3 , %1111111111101111 h, highest h, %1110101100001101 h, 0 h,
    ' p-adr-2 , %1111101111111111 h, highest h, %1111001010101111 h, 0 h,
    ' p-adr-3 , %1111101111111111 h, highest h, %1111001000001111 h, 0 h,
    ' p-and-imm , %1111101111100000 h, highest h, %1111000000000000 h, 0 h,
    ' p-and-reg-2 , %1111111111100000 h, 0 h, %1110101000000000 h, 0 h,
    ' p-asr-imm-2 , %1111111111101111 h, %0000000000110000 h,
    %1110101001001111 h, %0000000000100000 h,
    ' p-asr-reg-2 , %1111111111100000 h, %1111000011110000 h,
    %1111101001000000 h, %1111000000000000 h,
    ' p-b-3 , %1111100000000000 h, %1101000000000000 h,
    %1111000000000000 h, %1000000000000000 h,
    ' p-b-4 , %1111100000000000 h, %1101000000000000 h,
    %1111000000000000 h, %1001000000000000 h,
    ' p-bfc , %1111101111111111 h, highest h, %1111001101101111 h, 0 h,
    ' p-bfi , %1111101111110000 h, highest h, %1111001101100000 h, 0 h,
    ' p-bic-imm , %11111000111100000 h, highest h, %1111000000100000 h, 0 h,
    ' p-bic-reg-2 , %1111111111100000 h, 0 h, %1110101000100000 h, 0 h,

  [then]
    
  ' p-bl-imm , %1111100000000000 h, %1101000000000000 h,
  %1111000000000000 h, %1101000000000000 h,

  thumb-2? [if]
    
    \ ' p-cdp ,
    \ ' p-cdp2 ,
    \ ' p-clrex ,
    \ ' p-clz ,
    ' p-cmn-imm , %1111101111110000 h, %1000111100000000 h,
    %1111000100010000 h, %0000111100000000 h,
    ' p-cmn-reg-2 , %1111111111110000 h, %0000111100000000 h,
    %1110101100010000 h, %0000111100000000 h,
    ' p-cmp-imm-2 , %1111101111110000 h, %1000111100000000 h,
    %1111000110110000 h, %0000111100000000 h,
    ' p-cmp-reg-3 , %1111111111110000 h, %0000111100000000 h,
    %1110101110110000 h, %0000111100000000 h,
    \ ' p-cps-2 ,
    \ ' p-dbg ,

  [then]
  
  ' p-dmb , %1111111111110000 h, %1101000011110000 h,
  %1111001110110000 h, %1000000001010000 h,
  ' p-dsb , %1111111111110000 h, %1101000011110000 h,
  %1111001110110000 h, %1000000001000000 h,

  thumb-2? [if]
    
    ' p-eor-imm , %1111101111100000 h, highest h, %1111000010000000 h, 0 h,
    ' p-eor-reg-2 , %1111111111100000 h, 0 h, %1110101010000000 h, 0 h,

  [then]
  
  ' p-isb , %1111111111110000 h, %1101000011110000 h,
  %1111001110110000 h, %1000000001100000 h,

  thumb-2? [if]
    
    \ ' p-it ,
    \ ' p-ldc ,
    \ ' p-ldmdb ,
    \ ' p-ldmia-2 , %1111111111010000 h, 0 h, %1110100010010000 h, 0 h,
    ' p-ldr-imm-3-w , %1111111111110000 h, 0 h, %1111100011010000 h, 0 h,
    ' p-ldr-imm-4-w , %1111111111110000 h, %0000100000000000 h,
    %1111100001010000 h, %0000100000000000 h,
    ' p-ldr-lit-2-w , %1111111101111111 h, 0 h, %1111100001011111 h, 0 h,
    ' p-ldr-reg-2-w , %1111111111110000 h, %0000111111000000 h,
    %1111100001010000 h, 0 h,
    ' p-ldr-imm-3-b , %1111111111110000 h, 0 h, %1111100010010000 h, 0 h,
    ' p-ldr-imm-4-b , %1111111111110000 h, %0000100000000000 h,
    %1111100000010000 h, %0000100000000000 h,
    ' p-ldr-lit-2-b , %1111111101111111 h, 0 h, %1111100000011111 h, 0 h,
    ' p-ldr-reg-2-b , %1111111111110000 h, %0000111111000000 h,
    %1111100001010000 h, 0 h,
    \ ' p-ldrbt ,
    \ ' p-ldrd ,
    \ ' p-ldrex ,
    \ ' p-ldrexb ,
    \ ' p-ldrexd ,
    \ ' p-ldrexh ,
    ' p-ldr-imm-3-h , %1111111111110000 h, 0 h, %1111100010110000 h, 0 h,
    ' p-ldr-imm-4-h , %1111111111110000 h, %0000100000000000 h,
    %1111100000110000 h, %0000100000000000 h,
    ' p-ldr-lit-2-h , %1111111101111111 h, 0 h, %1111100000111111 h, 0 h,
    ' p-ldr-reg-2-h , %1111111111110000 h, %0000111111000000 h,
    %1111100000110000 h, 0 h,
    \ ' p-ldrht ,
    \ ' p-ldrsb ,
    \ ' p-ldrsbt ,
    \ ' p-ldrsh ,
    \ ' p-ldrsht ,
    \ ' p-ldrt ,
    ' p-lsl-imm-2 , %1111111111101111 h, %0000000000110000 h,
    %1110101001001111 h, 0 h,
    ' p-lsl-reg-2 , %1111111111100000 h, %1111000011110000 h,
    %1111101000000000 h, %1111000000000000 h,
    ' p-lsr-imm-2 , %1111111111101111 h, %0000000000110000 h,
    %1110101001001111 h, %0000000000010000 h,
    ' p-lsr-reg-2 , %1111111111100000 h, %1111000011110000 h,
    %1111101000100000 h, %1111000000000000 h,
    \ ' p-mcr ,
    \ ' p-mcrr ,
    \ ' p-mla ,
    ' p-mls , %1111111111110000 h, %0000000011110000 h,
    %1111101100000000 h, %0000000000010000 h,
    ' p-mov-imm-2 , %1111101111101111 h, highest h,
    %1111000001001111 h, 0 h,
    ' p-mov-imm-3 , %1111101111110000 h, highest h,
    %1111001001000000 h, 0 h,
    ' p-mov-reg-3 , %1111111111101111 h, %0111000011110000 h,
    %1110101001001111 h, 0 h,
    ' p-movt , %1111101111110000 h, highest h,
    %1111001011000000 h, 0 h,
    \ ' p-mrc ,
    \ ' p-mrrc ,
    \ ' p-mrs ,
    \ ' p-msr ,
    ' p-mul-2 , %1111111111110000 h, %1111000011110000 h,
    %1111101100000000 h, %1111000000000000 h,

    \ ' p-mvn-imm ,
    \ ' p-mvn-reg-2 , %1111111111101111 h, 0 h, %1110101001101111 h, 0 h,
    \ ' p-nop-3 ,
    \ ' p-orn-imm ,
    \ ' p-orn-reg ,
    ' p-orr-imm , %1111101111100000 h, highest h,
    %1111000001000000 h, 0 h,
    ' p-orr-reg-2 , %1111111111100000 h, 0 h, %1110101001000000 h, 0 h,
    \ ' p-pkh ,
    \ ' p-pld-imm ,
    \ ' p-pld-reg ,
    \ ' p-pli-imm ,
    \ ' p-pli-reg ,
    ' p-pop-2 , %1111111111111111 h, 0 h, %1110100010111101 h, 0 h,
    ' p-push-2 , %1111111111111111 h, 0 h, %1110100100101101 h, 0 h,
    \ ' p-qadd ,
    \ ' p-qadd16 ,
    \ ' p-qadd8 ,
    \ ' p-qasx ,
    \ ' p-qdadd ,
    \ ' p-qdsub ,
    \ ' p-qsax ,
    \ ' p-qsub ,
    \ ' p-qsub16 ,
    \ ' p-qsub8 ,
    \ ' p-rbit
    \ ' p-rev ,
    \ ' p-rev16 ,
    \ ' p-revsh
    \ ' p-rfe ,
    \ ' p-ror-imm ,
    \ ' p-ror-reg ,
    \ ' p-rrx ,
    \ ' p-rsb-imm-2 ,
    \ ' p-rsb-reg ,
    \ ' p-sadd16 ,
    \ ' p-sadd8 ,
    \ ' p-sasx .
    ' p-sbc-imm , %1111101111100000 h, highest h, %1111000101100000 h, 0 h,
    ' p-sbc-reg-2 , %1111111111100000 h, 0 h, %1110101101100000 h, 0 h,
    \ ' p-sbfx ,
    ' p-sdiv , %1111111111110000 h, %0000000011110000 h,
    %1111101110010000 h, %0000000011110000 h,
    \ ' p-sel ,
    \ ' p-setend ,
    \ ' p-sev ,
    \ ' p-shadd16 ,
    \ ' p-shadd8 ,
    \ ' p-shasx ,
    \ ' p-shsax ,
    \ ' p-shsub16 ,
    \ ' p-shsub8 ,
    \ ' p-smi ,
    \ ' p-smla* ,
    \ ' p-smlal* ,
    \ ' p-smlaw* ,
    \ ' p-smlsd ,
    \ ' p-smlsld ,
    \ ' p-smmla ,
    \ ' p-smmls ,
    \ ' p-smmul ,
    \ ' p-smuad ,
    \ ' p-smul* ,
    ' p-smull , %1111111111110000 h, %0000000011110000 h,
    %1111101110000000 h, 0 h,
    \ ' p-smulw* ,
    \ ' p-smusd ,
    \ ' p-srs* ,
    \ ' p-ssat ,
    \ ' p-ssat16 ,
    \ ' p-ssax ,
    \ ' p-ssub16 ,
    \ ' p-ssub8 ,
    \ ' p-stc ,
    \ ' p-stmdb ,
    \ ' p-stmia ,
    ' p-str-imm-3-w , %1111111111110000 h, 0 h, %1111100011000000 h, 0 h,
    ' p-str-imm-4-w , %1111111111110000 h, %0000100000000000 h,
    %1111100001000000 h, %0000100000000000 h,
    ' p-str-reg-2-w , %1111111111110000 h, %0000111111000000 h,
    %1111100001000000 h, 0 h,
    ' p-str-imm-3-b , %1111111111110000 h, 0 h, %1111100010000000 h, 0 h,
    ' p-str-imm-4-b , %1111111111110000 h, %0000100000000000 h,
    %1111100000000000 h, %0000100000000000 h,
    ' p-str-reg-2-b , %1111111111110000 h, %0000111111000000 h,
    %1111100000000000 h, 0 h,
    \ ' p-strbt ,
    \ ' p-strd ,
    \ ' p-strex ,
    \ ' p-strexb ,
    \ ' p-strexd ,
    \ ' p-strexh ,
    ' p-str-imm-3-h , %1111111111110000 h, 0 h, %1111100010100000 h, 0 h,
    ' p-str-imm-4-h , %1111111111110000 h, %0000100000000000 h,
    %1111100000100000 h, %0000100000000000 h,
    ' p-str-reg-2-h , %1111111111110000 h, %0000111111000000 h,
    %1111100000100000 h, 0 h,
    \ ' p-strht ,
    \ ' p-strt ,
    ' p-sub-imm-3 , %1111101111100000 h, highest h, %1111000110100000 h, 0 h,
    ' p-sub-imm-4 , %1111101111110000 h, highest h, %1111001010100000 h, 0 h,
    ' p-sub-reg-2 , %1111111111100000 h, 0 h, %1110101110100000 h, 0 h,
    \ ' p-sub-sp-imm ,
    \ ' p-sub-sp-reg ,
    \ ' p-sub-pc-lr ,
    \ ' p-svc ,
    \ ' p-sxtab ,
    \ ' p-sxtab16 ,
    \ ' p-sxtah ,
    \ ' p-sxtb ,
    \ ' p-sxtb16 ,
    \ ' p-sxth ,
    \ ' p-tbb ,
    \ ' p-tbh ,
    \ ' p-teq ,
    \ ' p-teqh ,
    ' p-tst-imm , %1111101111110000 h, %10001111000000 h,
    %1111000000010000 h, %00001111000000 h,
    ' p-tst-reg-2 , %1111111111110000 h, %0000111100000000 h,
    %1110101000010000 h, %0000111100000000 h,
    \ ' p-uadd16 ,
    \ ' p-uadd8 ,
    \ ' p-uasx ,
    \ ' p-ubfx ,
    ' p-udiv , %1111111111110000 h, %0000000011110000 h,
    %1111101110110000 h, %0000000011110000 h,
    \ ' p-uhadd16 ,
    \ ' p-uhadd8 ,
    \ ' p-uhasx ,
    \ ' p-uhsax ,
    \ ' p-uhsub16 ,
    \ ' p-uhsub8 ,
    \ ' p-umaal ,
    \ ' p-umlal ,
    ' p-umull , %1111111111110000 h, %0000000011110000 h,
    %1111101110100000 h, 0 h,
    \ ' p-uqadd16 ,
    \ ' p-uqadd8 ,
    \ ' p-uqasx ,
    \ ' p-uqsax ,
    \ ' p-uqsub16 ,
    \ ' p-uqsub8 ,
    \ ' p-usad8 ,
    \ ' p-usasa8 ,
    \ ' p-usat ,
    \ ' p-usat16 ,
    \ ' p-usax ,
    \ ' p-usub16 ,
    \ ' p-usub8 ,
    \ ' p-uxtab ,
    \ ' p-uxtab16
    \ ' p-uxtah ,
    \ ' p-uxtb ,
    \ ' p-uxtb16 ,
    \ ' p-uxth ,
    \ ' p-wfe ,
    \ ' p-wfi-2 ,
    \ ' p-yield ,

  [then]

  defined? float32 [if]

    ' p-vabs-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB0 h, $0AC0 h,
    ' p-vadd-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE30 h, $0A00 h,
    ' p-vcmp-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB4 h, $0A40 h,
    ' p-vcmpe-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB4 h, $0AC0 h,
    ' p-vcmp-f32-#0.0 , instr-sr-mask-0 h, instr-sr-mask-1 h, $EEB5 h, $0A40 h,
    ' p-vcmpe-f32-#0.0 , instr-sr-mask-0 h, instr-sr-mask-1 h, $EEB5 h, $0AC0 h,
    ' p-vcvta-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBC h, $0A40 h,
    ' p-vcvta-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBC h, $0AC0 h,
    ' p-vcvtn-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBD h, $0A40 h,
    ' p-vcvtn-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBD h, $0AC0 h,
    ' p-vcvtp-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBE h, $0A40 h,
    ' p-vcvtp-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBE h, $0AC0 h,
    ' p-vcvtm-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBF h, $0A40 h,
    ' p-vcvtm-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $FEBF h, $0AC0 h,
    ' p-vcvtr-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEBC h, $0AC0 h,
    ' p-vcvtr-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEBD h, $0AC0 h,
    ' p-vcvt-u32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEBC h, $0A40 h,
    ' p-vcvt-s32-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEBD h, $0A40 h,
    ' p-vcvt-f32-u32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB8 h, $0A40 h,
    ' p-vcvt-f32-s32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB8 h, $0AC0 h,
    ' p-vcvt-u16-f32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBF h, $0A40 h,
    ' p-vcvt-s16-f32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBE h, $0A40 h,
    ' p-vcvt-u32-f32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBF h, $0AC0 h,
    ' p-vcvt-s32-f32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBE h, $0AC0 h,
    ' p-vcvt-f32-u16-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBB h, $0A40 h,
    ' p-vcvt-f32-s16-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBA h, $0A40 h,
    ' p-vcvt-f32-u32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBB h, $0AC0 h,
    ' p-vcvt-f32-s32-# , instr-sr-fract-mask-0 h, instr-sr-fract-mask-1 h,
    $EEBA h, $0AC0 h,
    ' p-vcvtb-f32-f16 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB2 h, $0A40 h,
    ' p-vcvtt-f32-f16 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB2 h, $0AC0 h,
    ' p-vcvtb-f16-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB3 h, $0A40 h,
    ' p-vcvtt-f16-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h,
    $EEB3 h, $0AC0 h,
    ' p-vdiv-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE80 h, $0A00 h,
    ' p-vfma-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EEA0 h, $0A00 h,
    ' p-vfms-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EEA0 h, $0A40 h,
    ' p-vfnma-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE90 h, $0A00 h,
    ' p-vfnms-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE90 h, $0A40 h,
    ' p-vldmia-f32 ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $EC90 h, $0A00 h,
    ' p-vldmia-f32-update ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $ECB0 h, $0A00 h,
    ' p-vldmdb-f32-update ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $ED30 h, $0A00 h,
    ' p-vldr-f32 ,
    instr-sr-load/store-imm-mask-0 h, instr-sr-load/store-imm-mask-1 h,
    $ED10 h, $0A00 h,
    ' p-vmaxnm-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE80 h, $0A00 h,
    ' p-vminnm-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE80 h, $0A40 h,
    ' p-vmla-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE00 h, $0A40 h,
    ' p-vmls-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE00 h, $0A00 h,
    ' p-vmov-f32-imm , instr-sr-imm-mask-0 h, instr-sr-imm-mask-1 h,
    $EEB0 h, $0A00 h,
    ' p-vmov-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB0 h, $0A40 h,
    ' p-vmov-cr-f32 , instr-cr-sr-mask-0 h, instr-cr-sr-mask-1 h,
    $EE10 h, $0A10 h,
    ' p-vmov-f32-cr , instr-cr-sr-mask-0 h, instr-cr-sr-mask-1 h,
    $EE00 h, $0A10 h,
    ' p-vmov-2*cr-2*f32 , instr-2*cr-2*sr-mask-0 h, instr-2*cr-2*sr-mask-1 h,
    $EE50 h, $0A10 h,
    ' p-vmov-2*f32-2*cr , instr-2*cr-2*sr-mask-0 h, instr-2*cr-2*sr-mask-1 h,
    $EE40 h, $0A10 h,
    ' p-vmrs , instr-cr-fpscr-mask-0 h, instr-cr-fpscr-mask-1 h,
    $EEF1 h, $0A10 h,
    ' p-vmsr , instr-cr-fpscr-mask-0 h, instr-cr-fpscr-mask-1 h,
    $EEE1 h, $0A10 h,
    ' p-vmul-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE20 h, $0A00 h,
    ' p-vneg-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB1 h, $0A40 h,
    ' p-vnmla-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE10 h, $0A40 h,
    ' p-vnmls-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE10 h, $0A00 h,
    ' p-vnmul-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE20 h, $0A40 h,
    ' p-vrinta-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $FEB8 h, $0A40 h,
    ' p-vrintn-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $FEB9 h, $0A40 h,
    ' p-vrintp-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $FEBA h, $0A40 h,
    ' p-vrintm-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $FEBB h, $0A40 h,
    ' p-vrintx-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB7 h, $0A40 h,
    ' p-vrintz-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB6 h, $0AC0 h,
    ' p-vrintr-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB6 h, $0A40 h,
    ' p-vseleq-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE00 h, $0A00 h,
    ' p-vselvs-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE10 h, $0A00 h,
    ' p-vselge-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE20 h, $0A00 h,
    ' p-vselgt-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $FE30 h, $0A00 h,
    ' p-vsqrt-f32 , instr-2*sr-mask-0 h, instr-2*sr-mask-1 h, $EEB1 h, $0AC0 h,
    ' p-vstmia-f32 ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $EC80 h, $0A00 h,
    ' p-vstmia-f32-update ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $ECA0 h, $0A00 h,
    ' p-vstmdb-f32-update ,
    instr-sr-load/store-multi-mask-0 h, instr-sr-load/store-multi-mask-1 h,
    $ED20 h, $0A00 h,
    ' p-vstr-f32 ,
    instr-sr-load/store-imm-mask-0 h, instr-sr-load/store-imm-mask-1 h,
    $ED00 h, $0A00 h,
    ' p-vsub-f32 , instr-3*sr-mask-0 h, instr-3*sr-mask-1 h, $EE30 h, $0A40 h,

  [then]

  0 ,

  \ Commit to flash
  commit-flash

  \ Get condition
  : current-cond ( -- cond ) -1 ;

  \ Disassemble a 16-bit instruction
  : disassemble16 ( op-addr handler-addr -- match )
    over h@ over cell+ h@ and over 6 + h@ = if
      current-cond 2 pick h@ instr16.
      2 pick label. 2 pick h@ rot @ execute true
    else
      2drop false
    then
  ;

  \ Disassemble a 32-bit instruction
  : disassemble32 ( op-addr handler-addr -- match )
    over h@ over cell+ h@ and over 8 + h@ = if
      over 2 + h@ over 6 + h@ and over 10 + h@ = if
	current-cond 2 pick h@ 3 pick 2 + h@ instr32.
	2 pick label. 2 pick h@ 3 pick 2 + h@ 3 roll @ execute true
      else
	2drop false
      then
    else
      2drop false
    then
  ;

  \ Add a local label
  : add-local ( addr -- )
    local-index @ local-count < if
      local-index @ 0 ?do
	i cells local-buffer + @ over = if
	  drop unloop exit
	then
      loop
      local-index @ cells local-buffer + ! 1 local-index +!
    else
      drop
    then
  ;

  \ Parse a 16-bit instruction to find local labels
  : parse-local16 ( op-addr low -- )
    dup start-string <> if
      dup %1111000000000000 and %1101000000000000 = if
        0_8_bf 1 lshift swap 4 + swap 9 extend + add-local
      else
        dup %1111100000000000 and %1110000000000000 = if
          0 11 bitfield 1 lshift swap 4 + swap 12 extend + add-local
        else
          2drop
        then
      then
    else
      2drop
    then
  ;

  \ Parse a 32-bit instruction to find local labels
  : parse-local32 ( op-addr low high -- )
    over %1111100000000000 and %1111000000000000 = if
      dup %1101000000000000 and %1000000000000000 = if
	dup 0 11 bitfield 2 pick 0 6 bitfield 11 lshift or
	over 13 1 bitfield 17 lshift or swap 11 1 bitfield 18 lshift or
	swap 10_1_bf 19 lshift or 1 lshift swap 4 + swap 21 extend + add-local
      else
	dup %1101000000000000 and %1001000000000000 = if
	  dup 0 11 bitfield 2 pick 0 10 bitfield 11 lshift or
	  over 11 1 bitfield 3 pick 10_1_bf xor not 1 and 21 lshift or
	  swap 13 1 bitfield 2 pick 10_1_bf xor not 1 and 22 lshift or
	  swap 10_1_bf 23 lshift or 1 lshift swap 4 + swap 25 extend + add-local
	else
	  2drop drop
	then
      then
    else
      2drop drop
    then
  ;

  \ Find a local label for a 16-bit instruction
  : find-local16 ( op-addr handler-addr -- match )
    over h@ over cell+ h@ and over 6 + h@ = if
      drop dup h@ parse-local16 true
    else
      2drop false
    then
  ;

  \ Find a local label for a 32-bit instruction
  : find-local32 ( op-addr handler-addr -- match )
    over h@ over cell+ h@ and over 8 + h@ = if
      over 2 + h@ over 6 + h@ and over 10 + h@ = if
	drop dup h@ over 2 + h@ parse-local32 true
      else
	2drop false
      then
    else
      2drop false
    then
  ;

  \ Commit to flash
  commit-flash

  \ The body of finding local labels
  : find-local ( addr -- addr )
    all-ops16 begin
      dup @ 0<> if
	2dup find-local16 if
          drop
          dup h@ start-string <> if
            2 + true true
          else
            2 + dup c@ 1+ + 2 align true true
          then
	else
	  [ 2 cells ] literal + false
	then
      else
	drop false true
      then
    until
    not if
      all-ops32 begin
	dup @ 0<> if
	  2dup find-local32 if
	    drop 4 + true true
	  else
	    [ 3 cells ] literal + false
	  then
	else
	  drop false true
	then
      until
      not if
	2 +
      then
    then
  ;
  
  \ The body of disassembly
  : disassemble-main ( addr -- addr )
    for-gas @ not if dup h.8 space then
    all-ops16 begin
      dup @ 0<> if
	2dup disassemble16 if
          drop
          dup h@ start-string <> if
            2 + true true
          else
            2 + dup c@ 1+ + 2 align true true
          then
	else
	  [ 2 cells ] literal + false
	then
      else
	drop false true
      then
    until
    not if
      all-ops32 begin
	dup @ 0<> if
	  2dup disassemble32 if
	    drop 4 + true true
	  else
	    [ 3 cells ] literal + false
	  then
	else
	  drop false true
	then
      until
      not if
	dup h@ instr16. ." ????" 2 +
      then
    then
    cr
  ;

  \ Get whether an instruction is the end token
  : see-end? ( addr -- flag )
    dup h@ $003F <> if
      drop false
    else
      2 - h@ dup $FF00 and $BD00 = if
	drop true
      else
	$FF80 and $4700 =
      then
    then
  ;

  \ Commit to flash
  commit-flash
  
end-module> import
  
\ Disassemble instructions
: disassemble ( start end -- )
  false for-gas !
  0 local-index !
  2dup swap begin 2dup swap u< while find-local repeat 2drop
  cr swap begin 2dup swap u< while disassemble-main repeat 2drop
;

\ Disassemble instructions for GAS
: disassemble-for-gas ( start end -- )
  true for-gas !
  0 local-index !
  2dup swap begin 2dup swap u< while find-local repeat 2drop
  cr swap begin 2dup swap u< while disassemble-main repeat 2drop
;

\ Disassemble a word by its xt
: see-xt ( xt -- )
  false for-gas !
  0 local-index !
  dup begin dup see-end? not while find-local repeat drop
  cr begin dup see-end? not while disassemble-main repeat drop
;

\ Disassemble a word by its xt for GAS
: see-xt-for-gas ( xt -- )
  true for-gas !
  0 local-index !
  dup begin dup see-end? not while find-local repeat drop
  cr begin dup see-end? not while disassemble-main repeat drop
;

\ SEE a word
: see ( "name" -- ) token-word >xt see-xt ;

\ SEE a word for GAS
: see-for-gas ( "name" -- ) token-word >xt see-xt-for-gas ;

\ Finish compressing the code
end-compress-flash

\ Reboot
reboot
