\ Copyright (c) 2022-2024 Travis Bemann
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

armv6m import

continue-module internal

  \ Local buffer size
  512 constant local-buf-size

  \ Local buffer
  local-buf-size buffer: local-buf
  
  \ Current top of the local buffer
  variable local-buf-top

  \ Current bottom of the local buffer
  variable local-buf-bottom

  \ Block locals size
  64 constant block-locals-size

  \ Block locals
  block-locals-size buffer: block-locals

  \ Current top of the block locals
  variable block-locals-top

  \ Local variable types
  0 constant cell-local
  1 constant cell-addr-local
  2 constant double-local
  3 constant double-addr-local
  4 constant do-i-local
  5 constant do-limit-local
  6 constant do-leave-local
  
  \ Out of local space exception
  : x-out-of-locals ( -- ) ." out of local space" cr ;

  \ Loop variable not found
  : x-loop-var-not-found ( -- ) ." loop variable not found" cr ;

  \ Find a loop variable
  : find-loop-var ( i type addr -- i' addr' )
    begin dup local-buf-bottom @ < while
      dup c@ 2 pick = if nip exit then
      dup c@ dup double-local = swap double-addr-local = or if
        1+ dup c@ 1+ + rot 2 + -rot
      else
        1+ dup c@ 1+ + rot 1+ -rot
      then
    repeat
    drop 2drop ['] x-loop-var-not-found ?raise
  ;

  \ Find the i loop variable
  : find-i-var ( -- i )
    0 do-i-local local-buf-top @ find-loop-var drop
  ;

  \ Find the limit loop variable
  : find-limit-var ( -- i )
    0 do-limit-local local-buf-top @ find-loop-var drop
  ;

  \ Find the leave loop variable
  : find-leave-var ( -- i )
    0 do-leave-local local-buf-top @ find-loop-var drop
  ;

  \ Find the j loop variable
  : find-j-var ( -- i )
    0 do-i-local local-buf-top @ find-loop-var 2 + swap 1+ swap
    do-i-local swap find-loop-var drop
  ;
  
  \ Reset the local buffer
  : reset-local ( -- )
    local-buf local-buf-size + dup local-buf-top ! local-buf-bottom !
    block-locals block-locals-size + block-locals-top !
  ;

  \ Get the current word's local count
  : local-count ( -- u )
    0 local-buf-top @ begin dup local-buf-bottom @ < while
      dup c@ dup double-local = swap double-addr-local = or if
        1+ dup c@ 1+ + swap 2 + swap
      else
        1+ dup c@ 1+ + swap 1+ swap
      then
    repeat
    drop
  ;

  \ Add a local
  : add-local ( type c-addr u -- )
    dup 1+ local-buf-top @ swap - local-buf >= averts x-out-of-locals
    dup 1+ negate local-buf-top +!
    dup local-buf-top @ c!
    local-buf-top @ 1+ swap move
    -1 local-buf-top +!
    dup local-buf-top @ c!
    dup double-local = swap double-addr-local = or if 2 else 1 then
    block-locals-top @ c+!
  ;

  \ Add an anomymous local
  : add-noname-local ( type -- )
    local-buf-top @ 1- local-buf >= averts x-out-of-locals
    -1 local-buf-top +!
    0 local-buf-top @ c!
    -1 local-buf-top +!
    dup local-buf-top @ c!
    dup double-local = swap double-addr-local = or if 2 else 1 then
    block-locals-top @ c+!
  ;

  \ Fill the current set of locals
  : fill-locals ( u -- )
    dup 0> if
      undefer-lit
      dup 128 < if
        [ armv6m-instr import ]
        dup 4 * subsp,sp,#_
        0 ?do
          i 4 * tos str_,[sp,#_]
          tos 1 dp ldm
        loop
        [ armv6m-instr unimport ]
      else
        0 swap 1- ?do
          i lit, postpone roll
          [ armv6m-instr import ]
          tos 1 push
          tos 1 dp ldm
          [ armv6m-instr unimport ]
        -1 +loop
      then
    else
      drop
    then
  ;

  \ Push a new set of locals
  : push-locals ( -- )
    local-buf-top @ 1- local-buf > averts x-out-of-locals
    local-buf-top @ local-buf local-buf-size + < if
      local-buf-bottom @ local-buf-top @ - local-buf-top @ 1- c!
      local-buf-top @ 1- dup local-buf-bottom ! local-buf-top !
    then
    block-locals-top @ block-locals 2 + >= averts x-out-of-locals
    -1 block-locals-top +! $FF block-locals-top @ c!
    -1 block-locals-top +! 0 block-locals-top @ c!
  ;

  \ Push a new block's set of locals
  : push-block-locals ( -- )
    block-locals-top @ block-locals > averts x-out-of-locals
    -1 block-locals-top +! 0 block-locals-top @ c!
  ;
    
  \ Drop the current word's locals
  : drop-locals ( -- )
    local-count ?dup if
      undefer-lit
      [ armv6m-instr import ]
      dup 128 < if
        4 * addsp,sp,#_
      else
        4 * r0 literal,
        r0 addsp,sp,4_
      then
      [ armv6m-instr unimport ]
    then
  ;

  \ Drop the current block's locals
  : drop-block-locals ( -- )
    block-locals-top @ block-locals block-locals-size + < if
      block-locals-top @ c@ dup $FF <> if
        ?dup if
          undefer-lit
          [ armv6m-instr import ]
          dup 128 < if
            4 * addsp,sp,#_
          else
            4 * r0 literal,
            r0 addsp,sp,4_
          then
          [ armv6m-instr unimport ]
        then
      else
        drop
      then
    then
  ;

  \ Drop the current loop's locals
  : drop-loop-locals ( -- )
    find-leave-var 1+
    undefer-lit
    [ armv6m-instr import ]
    dup 128 < if
      4 * addsp,sp,#_
    else
      4 * r0 literal,
      r0 addsp,sp,4_
    then
    [ armv6m-instr unimport ]
  ;
  
  \ Clear the current word's locals
  : clear-locals ( -- )
    local-buf-bottom @ local-buf local-buf-size + < if
      local-buf-bottom @ 1+ local-buf-top !
      local-buf-bottom @ c@ local-buf-top @ + local-buf-bottom !
    else
      local-buf-bottom @ local-buf-top !
    then
    block-locals-top @ block-locals block-locals-size + < if
      begin block-locals-top @ c@ $FF = 1 block-locals-top +! until
    then
  ;

  \ Clear the current block's locals
  : clear-block-locals ( -- )
    block-locals-top @ c@ dup $FF <> if
      begin ?dup while
        local-buf-top @ dup c@ >r
        1+ c@ 2 + local-buf-top +!
        r@ double-local = r> double-addr-local = or if 2 - else 1- then
      repeat
      1 block-locals-top +!
    else
      drop
    then
  ;

  \ Ignore a local comment
  : ignore-local-comment ( "..." "}" -- )
    begin
      token dup if
        s" }" equal-strings?
      else
        2drop eval-eof @ ?dup if execute else true then
        if clear-locals true else display-prompt refill false then
      then
    until
  ;

  \ Compile getting a cell variable
  : compile-get-cell-local ( u -- )
    undefer-lit
    6 push,
    [ armv6m-instr import ]
    dup 128 < if
      4 * r6 ldr_,[sp,#_]
    else
      4 * r0 literal,
      r0 add4_,sp
      0 r0 r6 ldr_,[_,#_]
    then
    [ armv6m-instr unimport ]
  ;

  \ Compile getting a double cell variable
  : compile-get-double-local ( u -- )
    undefer-lit
    6 push,
    [ armv6m-instr import ]
    dup 127 < if
      dup 1+ 4 * r6 ldr_,[sp,#_]
    else
      dup 1+ 4 * r0 literal,
      r0 add4_,sp
      0 r0 r6 ldr_,[_,#_]
    then
    [ armv6m-instr unimport ]
    6 push,
    [ armv6m-instr import ]
    dup 128 < if
      4 * r6 ldr_,[sp,#_]
    else
      4 * r0 literal,
      r0 add4_,sp
      0 r0 r6 ldr_,[_,#_]
    then
    [ armv6m-instr unimport ]
  ;

  \ Compile getting a cell or double cell variable address
  : compile-get-cell-addr-local ( u -- )
    undefer-lit
    6 push,
    [ armv6m-instr import ]
    dup 128 < if
      4 * r6 add_,sp,#_
    else
      4 * r6 literal,
      r6 add4_,sp
    then
    [ armv6m-instr unimport ]
  ;

  \ Parse a local variable
  : parse-get-local ( c-addr u -- match? )
    2>r 0 local-buf-top @ begin dup local-buf-bottom @ < while
      dup 1+ count 2r@ equal-case-strings? if
        rdrop rdrop c@ case
          cell-local of compile-get-cell-local endof
          cell-addr-local of compile-get-cell-addr-local endof
          double-local of compile-get-double-local endof
          double-addr-local of compile-get-cell-addr-local endof \ This is correct
        endcase
        true exit
      else
        dup c@ dup double-local = swap double-addr-local = or if
          1+ dup c@ 1+ + swap 2 + swap
        else
          1+ dup c@ 1+ + swap 1+ swap
        then
      then
    repeat
    rdrop rdrop 2drop false
  ;

  \ Compile setting a cell variable
  : compile-set-cell-local ( u -- )
    undefer-lit
    [ armv6m-instr import ]
    dup 128 < if
      4 * tos str_,[sp,#_]
    else
      4 * r0 literal,
      r0 add4_,sp
      0 r0 tos str_,[_,#_]
    then
    [ armv6m-instr unimport ]
    6 pull,
  ;

  \ Compile setting a double cell variable
  : compile-set-double-local ( u -- )
    [ armv6m-instr import ]
    dup 128 < if
      dup 4 * tos str_,[sp,#_]
    else
      dup 4 * r0 literal,
      r0 add4_,sp
      0 r0 tos str_,[_,#_]
    then
    [ armv6m-instr unimport ]
    6 pull,
    [ armv6m-instr import ]
    dup 127 < if
      1+ 4 * tos str_,[sp,#_]
    else
      1+ 4 * r0 literal,
      r0 add4_,sp
      0 r0 tos str_,[_,#_]
    then
    [ armv6m-instr unimport ]
    6 pull,
  ;

  \ Set a local variable
  : parse-set-local ( c-addr u -- match? )
    2>r 0 local-buf-top @ begin dup local-buf-bottom @ < while
      dup 1+ count 2r@ equal-case-strings? if
        rdrop rdrop c@ case
          cell-local of compile-set-cell-local endof
          cell-addr-local of compile-set-cell-local endof
          double-local of compile-set-double-local endof
          double-addr-local of compile-set-double-local endof
        endcase
        true exit
      else
        dup c@ dup double-local = swap double-addr-local = or if
          1+ dup c@ 1+ + swap 2 + swap
        else
          1+ dup c@ 1+ + swap 1+ swap
        then
      then
    repeat
    rdrop rdrop 2drop false
  ;

  \ Compile adding to a cell variable
  : compile-add-cell-local ( u -- )
    undefer-lit
    [ armv6m-instr import ]
    dup 128 < if
      dup 4 * r0 ldr_,[sp,#_]
      r0 tos tos adds_,_,_
      4 * tos str_,[sp,#_]
    else
      4 * r0 literal,
      r0 add4_,sp
      0 r0 r1 ldr_,[_,#_]
      r1 tos tos adds_,_,_
      0 r0 tos str_,[_,#_]
    then
    [ armv6m-instr unimport ]
    6 pull,
  ;

  \ Compile adding to a double cell variable
  : compile-add-double-local ( u -- )
    undefer-lit
    [ armv6m-instr import ]
    dup 127 < if
      dup 1+ 4 * r1 ldr_,[sp,#_]
      dup 4 * r2 ldr_,[sp,#_]
      r3 1 dp ldm
      r3 r1 r1 adds_,_,_
      tos r2 adcs_,_
      dup 1+ 4 * r1 str_,[sp,#_]
      4 * r2 str_,[sp,#_]
      tos 1 dp ldm
    else
      4 * r0 literal,
      r0 add4_,sp
      4 r0 r1 ldr_,[_,#_]
      0 r0 r2 ldr_,[_,#_]
      r3 1 dp ldm
      r3 r1 r1 adds_,_,_
      tos r2 adcs_,_
      4 r0 r1 str_,[_,#_]
      0 r0 r2 str_,[_,#_]
      tos 1 dp ldm
    then
    [ armv6m-instr unimport ]
  ;
  
  \ Add to a local variable
  : parse-add-local ( c-addr u -- match? )
    2>r 0 local-buf-top @ begin dup local-buf-bottom @ < while
      dup 1+ count 2r@ equal-case-strings? if
        rdrop rdrop c@ case
          cell-local of compile-add-cell-local endof
          cell-addr-local of compile-add-cell-local endof
          double-local of compile-add-double-local endof
          double-addr-local of compile-add-double-local endof
        endcase
        true exit
      else
        dup c@ dup double-local = swap double-addr-local = or if
          1+ dup c@ 1+ + swap 2 + swap
        else
          1+ dup c@ 1+ + swap 1+ swap
        then
      then
    repeat
    rdrop rdrop 2drop false
  ;

  \ Get whether a value is a 2VALUE
  : 2value? ( xt -- 2value? ) 2 + h@ $3F08 = ;

  \ Get the address of a VALUE's field
  : value-field@ ( value-xt -- addr ) dup h@ $FF and 2 lshift 2 + + 4 align ;

  \ Get a VALUE's address
  : value-addr@ ( value-xt -- addr ) value-field@ @ ;
  
  \ Get a VALUE's initial value
  : value-init@ ( value-xt -- x ) value-field@ cell+ @ ;

  \ Get a 2VALUE's initial value
  : 2value-init@ ( 2value-xt -- d ) value-field@ cell+ 2@ ;
  
  \ Compile setting a VALUE
  : compile-to-value ( xt -- )
    undefer-lit
    dup 2value? if
      value-addr@
      0 literal,
      [ armv6m-instr import ]
      0 r0 r6 str_,[_,#_]
      6 pull,
      4 r0 r6 str_,[_,#_]
      6 pull,
      [ armv6m-instr unimport ]
    else
      value-addr@
      0 literal,
      [ armv6m-instr import ]
      0 r0 r6 str_,[_,#_]
      6 pull,
      [ armv6m-instr unimport ]
    then
  ;

  \ Compile adding to a VALUE
  : compile-+to-value ( xt -- )
    undefer-lit
    dup 2value? if
      value-addr@
      0 literal,
      [ armv6m-instr import ]
      4 r0 r1 ldr_,[_,#_]
      0 r0 r2 ldr_,[_,#_]
      r3 1 dp ldm
      r3 r1 r1 adds_,_,_
      tos r2 adcs_,_
      4 r0 r1 str_,[_,#_]
      0 r0 r2 str_,[_,#_]
      tos 1 dp ldm
      [ armv6m-instr unimport ]
    else
      value-addr@
      0 literal,
      [ armv6m-instr import ]
      0 r0 r1 ldr_,[_,#_]
      tos r1 r1 adds_,_,_
      0 r0 r1 str_,[_,#_]
      tos 1 dp ldm
      [ armv6m-instr unimport ]
    then
  ;

  \ Initialize VALUE's in flash
  : init-flash-values ( -- )
    flash-latest begin ?dup while
      dup word-flags h@ init-value-flag and if
        dup >xt dup 2value? if
          dup 2value-init@ rot value-addr@ 2!
        else
          dup value-init@ swap value-addr@ !
        then
      then
      next-word @
    repeat
  ;
  
  \ The saved word begin hook
  variable saved-word-begin-hook

  \ The saved word exit hook
  variable saved-word-exit-hook

  \ The saved word end hook
  variable saved-word-end-hook

  \ The saved word reset hook
  variable saved-word-reset-hook

  \ The saved block begin hook
  variable saved-block-begin-hook

  \ The saved block exit hook
  variable saved-block-exit-hook

  \ The saved block end hook
  variable saved-block-end-hook

  \ The saved parse hook
  variable saved-parse-hook
  
  \ Initialize values
  : init-values ( -- )
    word-begin-hook @ saved-word-begin-hook !
    word-exit-hook @ saved-word-exit-hook !
    word-end-hook @ saved-word-end-hook !
    word-reset-hook @ saved-word-reset-hook !
    block-begin-hook @ saved-block-begin-hook !
    block-exit-hook @ saved-block-exit-hook !
    block-end-hook @ saved-block-end-hook !
    parse-hook @ saved-parse-hook !
    [: saved-word-begin-hook @ ?execute push-locals ;] word-begin-hook !
    [: drop-locals saved-word-exit-hook @ ?execute ;] word-exit-hook !
    [: clear-locals saved-word-end-hook @ ?execute ;] word-end-hook !
    [: saved-word-reset-hook @ ?execute reset-local ;] word-reset-hook !
    [: saved-block-begin-hook @ ?execute push-block-locals ;] block-begin-hook !
    [: drop-block-locals saved-block-exit-hook @ ?execute ;] block-exit-hook !
    [: clear-block-locals saved-block-end-hook @ ?execute ;] block-end-hook !
    [:
      state @ if
        2dup parse-get-local not if
          saved-parse-hook @ ?dup if execute else 2drop false then
        else
          2drop true
        then
      else
        saved-parse-hook @ ?dup if execute else 2drop false then
      then
    ;] parse-hook !
    reset-local
    init-flash-values
  ;

  \ Parse a local variable with a type
  : parse-local-with-type ( type -- )
    begin
      token dup if
        add-local true
      else
        2drop eval-eof @ ?dup if
          execute display-prompt refill false
        else
          drop clear-locals ['] x-token-expected ?raise
        then
      then
    until
  ;
  
end-module> import

\ Declare a VALUE
: value ( x "name" -- )
  token
  dup 0= triggers x-token-expected
  compiling-to-flash? not if
    rot here cell align cell allot tuck ! -rot
  then
  start-compile-no-push
  visible
  compiling-to-flash? if init-value then
  hreserve
  6 push,
  [ armv6m-instr import ]
  0 r0 r6 ldr_,[_,#_]
  r14 bx_
  [ armv6m-instr unimport ]
  word-exit-hook @ ?execute
  word-end-hook @ ?execute
  $003F h,
  compiling-to-flash? if
    next-ram-space cell align, dup >r
  else
    swap
  then
  4 align, here swap , 0 rot ldr-pc!
  compiling-to-flash? if , then
  finalize,
  compiling-to-flash? if r> cell+ set-next-ram-space then
;

\ Declare a 2VALUE
: 2value ( d "name" -- )
  token
  dup 0= triggers x-token-expected
  compiling-to-flash? not if
    2swap here cell align 2 cells allot dup >r 2! r> -rot
  then
  start-compile-no-push
  visible
  compiling-to-flash? if init-value then
  hreserve
  [ armv6m-instr import ]
  8 dp subs_,#_
  4 dp tos str_,[_,#_]
  4 r0 r1 ldr_,[_,#_]
  0 dp r1 str_,[_,#_]
  0 r0 tos ldr_,[_,#_]
  r14 bx_
  [ armv6m-instr unimport ]
  word-exit-hook @ ?execute
  word-end-hook @ ?execute
  $003F h,
  compiling-to-flash? if
    next-ram-space cell align dup >r
  else
    swap
  then
  4 align, here swap , 0 rot ldr-pc!
  compiling-to-flash? if 2, then
  finalize,
  compiling-to-flash? if r> 2 cells + set-next-ram-space then
;

\ Set a local or a VALUE
: to ( [x/d] "name" -- )
  [immediate]
  token dup 0<> averts x-token-expected
  state @ if
    2dup parse-set-local not if
      find dup 0<> averts x-unknown-word >xt compile-to-value
    else
      2drop
    then
  else
    find dup 0<> averts x-unknown-word >xt
    dup 2value? if value-addr@ 2! else value-addr@ ! then
  then
;

\ Add to a local or a VALUE
: +to ( [x/d] "name" -- )
  [immediate]
  token dup 0<> averts x-token-expected
  state @ if
    2dup parse-add-local not if
      find dup 0<> averts x-unknown-word >xt compile-+to-value
    else
      2drop
    then
  else
    find dup 0<> averts x-unknown-word >xt
    dup 2value? if value-addr@ 2+! else value-addr@ +! then
  then
;

\ Declare locals
: { ( ... "name" ... -- )
  [immediate]
  [compile-only]
  block-locals-top @ c@
  begin
    token dup if
      2dup s" --" equal-strings? if
        2drop block-locals-top @ c@ swap - fill-locals ignore-local-comment true
      else
        2dup s" }" equal-strings? if
          2drop block-locals-top @ c@ swap - fill-locals true
        else
          2dup s" W:" equal-case-strings? if
            2drop cell-local parse-local-with-type false
          else
            2dup s" W^" equal-case-strings? if
              2drop cell-addr-local parse-local-with-type false
            else
              2dup s" D:" equal-case-strings? if
                2drop double-local parse-local-with-type false
              else
                2dup s" D^" equal-case-strings? if
                  2drop double-addr-local parse-local-with-type false
                else
                  2dup s" C:" equal-case-strings? if
                    2drop cell-local parse-local-with-type false
                  else
                    2dup s" C^" equal-case-strings? if
                      2drop cell-addr-local parse-local-with-type false
                    else
                      cell-local -rot add-local false
                    then
                  then
                then
              then
            then
          then
        then
      then
    else
      2drop eval-eof @ ?dup if execute else true then
      if
        drop clear-locals ['] x-token-expected ?raise
      else
        display-prompt refill false
      then
    then
  until
;

\ Start a DO LOOP
: do ( compile: -- loop-addr leave-addr ) ( runtime: limit begin -- )
  [immediate] [compile-only]
  undefer-lit
  begin-block
  do-leave-local add-noname-local
  do-limit-local add-noname-local
  do-i-local add-noname-local
  reserve-literal
  [ armv6m-instr import ]
  tos r2 movs_,_
  [ cortex-m7? cortex-m33? or ] [if]
    r3 1 dp ldm
    tos 1 dp ldm
  [else]
    tos r3 2 dp ldm
  [then]
  12 subsp,sp,#_
  8 r0 str_,[sp,#_]
  0 r2 str_,[sp,#_]
  4 r3 str_,[sp,#_]
  [ armv6m-instr unimport ]
  here swap
  begin-block
  internal::syntax-do internal::push-syntax
;

\ Start a ?DO LOOP
: ?do ( compile: -- loop-addr leave-addr ) ( runtime: limit begin -- )
  [immediate] [compile-only]
  undefer-lit
  begin-block
  do-leave-local add-noname-local
  do-limit-local add-noname-local
  do-i-local add-noname-local
  reserve-literal
  [ armv6m-instr import ]
  tos r2 movs_,_
  [ cortex-m7? cortex-m33? or ] [if]
    r3 1 dp ldm
    tos 1 dp ldm
  [else]
    tos r3 2 dp ldm
  [then]
  r3 r2 cmp_,_
  ne bc>
  r0 bx_
  >mark
  12 subsp,sp,#_
  8 r0 str_,[sp,#_]
  0 r2 str_,[sp,#_]
  4 r3 str_,[sp,#_]
  [ armv6m-instr unimport ]
  here swap
  begin-block
  internal::syntax-do internal::push-syntax
;

\ Close a DO LOOP
: loop ( compile: loop-addr leave-addr -- ) ( runtime: -- )
  [immediate] [compile-only]
  undefer-lit
  internal::syntax-do internal::verify-syntax internal::drop-syntax
  end-block
  swap
  [ armv6m-instr import ]
  find-i-var 4 *
  dup 128 < if
    r0 ldr_,[sp,#_]
  else
    r2 literal,
    r2 add4_,sp
    0 r2 r0 ldr_,[_,#_]
  then
  1 r0 adds_,#_
  find-limit-var 4 *
  dup 128 < if
    r1 ldr_,[sp,#_]
  else
    r2 literal,
    r2 add4_,sp
    0 r2 r1 ldr_,[_,#_]
  then
  r1 r0 cmp_,_
  eq bc>
  find-i-var 4 *
  dup 128 < if
    r0 str_,[sp,#_]
  else
    r2 literal,
    r2 add4_,sp
    0 r2 r0 str_,[_,#_]
  then
  rot branch,
  >mark
  [ armv6m-instr unimport ]
  end-block
  here 1 or 0 rot literal!
;

\ Close a DO +LOOP
: +loop ( compile: loop-addr leave-addr -- ) ( runtime: -- )
  [immediate] [compile-only]
  undefer-lit
  internal::syntax-do internal::verify-syntax internal::drop-syntax
  end-block
  swap
  [ armv6m-instr import ]
  find-i-var 4 *
  dup 128 < if
    r0 ldr_,[sp,#_]
  else
    r3 literal,
    r3 add4_,sp
    0 r3 r0 ldr_,[_,#_]
  then
  r0 r2 movs_,_
  tos r0 r0 adds_,_,_
  find-i-var 4 *
  dup 128 < if
    r0 str_,[sp,#_]
  else
    r3 literal,
    r3 add4_,sp
    0 r3 r0 str_,[_,#_]
  then
  find-limit-var 4 *
  dup 128 < if
    r1 ldr_,[sp,#_]
  else
    r3 literal,
    r3 add4_,sp
    0 r3 r1 ldr_,[_,#_]
  then

  0 tos cmp_,#_
  lt bc>

  tos 1 dp ldm
  
  r1 r2 cmp_,_
  le bc>
  4 pick branch,
  >mark

  r0 r1 cmp_,_
  le bc>
  4 pick branch,

  2swap >mark

  tos 1 dp ldm

  r1 r2 cmp_,_
  ge bc>
  4 pick branch,
  >mark
  
  r0 r1 cmp_,_
  gt bc>
  4 pick branch,

  >mark
  >mark

  drop
  
  [ armv6m-instr unimport ]
  end-block
  here 1 or 0 rot literal!
;

\ Get the I loop variable
: i ( -- x )
  [immediate] [compile-only]
  undefer-lit
  6 push,
  [ armv6m-instr import ]
  find-i-var 4 *
  dup 128 < if
    tos ldr_,[sp,#_]
  else
    r0 literal,
    r0 add4_,sp
    0 r0 tos ldr_,[_,#_]
  then
  [ armv6m-instr unimport ]
;

\ Get the J loop variable
: j ( -- x )
  [immediate] [compile-only]
  undefer-lit
  6 push,
  [ armv6m-instr import ]
  find-j-var 4 *
    dup 128 < if
    tos ldr_,[sp,#_]
  else
    r0 literal,
    r0 add4_,sp
    0 r0 tos ldr_,[_,#_]
  then
  [ armv6m-instr unimport ]
;

\ Leave a loop
: leave ( -- )
  [immediate] [compile-only]
  undefer-lit
  6 push,
  [ armv6m-instr import ]
  find-leave-var 4 *
  dup 128 < if
    tos ldr_,[sp,#_]
  else
    r0 literal,
    r0 add4_,sp
    0 r0 tos ldr_,[_,#_]
  then
  drop-loop-locals
  tos r0 movs_,_
  tos 1 dp ldm
  r0 bx_
  [ armv6m-instr unimport ]
;

\ Remove the loop variables (note that this is a no-op since EXIT does this now)
: unloop ( -- ) ;

\ Initialize
: init ( -- ) init init-values ;

reboot
