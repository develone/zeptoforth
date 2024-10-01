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

continue-module forth

  armv6m import
  internal import
  
  continue-module internal
    
    \ Fill memory with bytes, optimized for speed
    : fill-bytes ( c-addr u c -- )
      code[
      tos r0 movs_,_
      cortex-m7? cortex-m33? or [if]
        0 dp r1 ldr_,[_,#_]
        4 dp r2 ldr_,[_,#_]
        8 dp tos ldr_,[_,#_]
        12 dp adds_,#_
      [else]
        tos r2 r1 3 dp ldm
      [then]
      mark<
      0 r1 cmp_,#_
      eq bc>
      1 r1 subs_,#_
      r1 r2 r0 strb_,[_,_]
      2swap b<
      >mark
      ]code
    ;

    \ Fill memory with words, optimized for speed
    : fill-cells ( addr u x -- )
      code[
      tos r0 movs_,_
      cortex-m7? cortex-m33? or [if]
        0 dp r1 ldr_,[_,#_]
        4 dp r2 ldr_,[_,#_]
        8 dp tos ldr_,[_,#_]
        12 dp adds_,#_
      [else]
        tos r2 r1 3 dp ldm
      [then]
      mark<
      0 r1 cmp_,#_
      eq bc>
      4 r1 subs_,#_
      r1 r2 r0 str_,[_,_]
      2swap b<
      >mark
      ]code
    ;

    \ Convert a byte to a cell
    : byte>cell ( c -- x )
      dup over 8 lshift or over 16 lshift or swap 24 lshift or
    ;

  end-module> import

  \ Fill memory, optimized for speed
  : fill ( c-addr u c -- )
    over 0> if
      2 pick 3 and 0= if
        over 3 and 0= if
          byte>cell fill-cells
        else
          3dup swap dup 3 and - swap byte>cell fill-cells
          swap >r r@ dup 3 and - rot + swap r> 3 and swap fill-bytes
        then
      else
        over cell <= if
          fill-bytes
        else
          3dup nip over cell align 2 pick - dup >r swap fill-bytes
          rot r@ + rot r> - rot recurse
        then
      then
    else
      2drop drop
    then
  ;

  \ Go to a word, dropping any local variables or loop variables and pop the
  \ current word's return address
  : goto ( "target" -- )
    [immediate] [compile-only]
    token-word
    undefer-lit
    word-exit-hook @ ?execute
    [ armv6m-instr import ]
    r0 1 pop
    r0 lr mov4_,4_
    >xt
    dup here 4 + - 1 arshift
    dup 1024 < over -1025 > and if
      nip $7FF and $E000 or h, \ Unconditional branch
    else
      drop 1 or r0 literal,
      r0 bx_
    then
    [ armv6m-instr unimport ]
  ;

  \ Compile a word that implements a jump table
  : begin-jumptable ( "name" -- )
    token
    dup averts x-token-expected
    start-compile-no-push
    [ armv6m-instr import ]
    r6 r1 movs_,_
    r6 1 r7 ldm
    [ armv6m-instr unimport ]
  ;

  \ End a jump table
  : end-jumptable ( -- )
    word-exit-hook @ ?execute
    word-end-hook @ ?execute
    [ armv6m-instr import ]
    lr bx_
    [ armv6m-instr unimport ]
    $003F h,
    visible
    finalize,
  ;

  \ Compile a jump table entry
  : => ( x "target" -- )
    token-word swap
    undefer-lit
    [ armv6m-instr import ]
    dup 0 >= over 255 <= and if
      r1 cmp_,#_
    else
      r0 literal,
      r0 r1 cmp_,_
    then
    ne bc> rot
    word-exit-hook @ ?execute
    >xt
    dup here 4 + - 1 arshift
    dup 1024 < over -1025 > and if
      nip $7FF and $E000 or h,
    else
      drop 1 or r0 literal,
      r0 bx_
    then
    >mark
    [ armv6m-instr unimport ]
  ;

  \ Compile a default jump table entry
  : default=> ( "target" -- )
    token-word
    undefer-lit
    [ armv6m-instr import ]
    word-exit-hook @ ?execute
    >xt
    dup here 4 + - 1 arshift
    dup 1024 < over -1025 > and if
      nip $7FF and $E000 or h,
    else
      drop 1 or r0 literal,
      r0 bx_
    then
    [ armv6m-instr unimport ]
  ;

end-module

compile-to-ram
