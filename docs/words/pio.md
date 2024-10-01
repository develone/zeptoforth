# Programmable Input/Output Words

Programmable input/output (PIO) on the RP2040 (i.e. the Raspberry Pi Pico) and the RP2350 (i.e. the Raspberry Pi Pico 2) provides a means to input to or output from pins in a very high-speed fashion at some speed up to the system clock of 125 MHz, on the RP2040, or 150 MHz, on the RP2350. There are two PIO peripherals, `PIO0` and `PIO1`, on the RP2040, and a third PIO peripheral, `PIO2`, on the RP2350, each of which contain four *state machines*.

PIO's may have up to 32 PIO instructions in their memory, which are 16 bits in size each. PIO state machines may be set to wrap from a *top* instruction to a *bottom* instruction automatically, unless a `jmp` instruction is executed at the *top* address. Instructions may be loaded into a PIO's instruction memory with `pio-instr-mem!` or `pio-instr-relocate-mem!`. Instructions may also be fed into a state machine to be executed immediately with `sm-instr!`. The address to execute PIO instructions at may be set for a state machine with `sm-addr!`

Up to four PIO state machines may be enabled, disabled, or reset at a time with `sm-enable`, `sm-disable`, or `sm-restart` respectively. These take a bitset of four bits where the position of each bit corresponds to the index of the state machine to enable, disable, or restart.

Each PIO state machine has four 32-bit registers, an input shift register (ISR), an output shift register (OSR), an X register, and a Y register. They also have a 5-bit program counter (PC). These are all initialized to zero.

Each PIO state machine has an RX FIFO and a TX FIFO of four 32-bit values each. These are initialized to empty. Note that the RX FIFO and TX FIFO on a PIO state machine may be joined into a single unidirectional FIFO consisting of eight 32-bit values. The RX FIFO for a state machine may be pushed to from a state machine's ISR register, which is 32-bits in size. The TX FIFO for a state machine may be pulled from to a state machine's OSR register, which is also 32-bits in size.

PIO state machines may automatically *pull* from its TX FIFO after a threshold number of bits have been shifted out of its OSR register. They may also automatically *push* to its RX FIFO after a threshold number of bits have been shifted into its ISR register.

The clock divider for a state machine is set with `sm-clkdiv!`, which takes a fractional component (from 0 to 255) and an integral component (from 0 to 65536) to divide the system clock by for the clock rate of the state machine in question. Note that if the integral clock divisor is 0 it is treated as 65536, and in those cases the fractional clock divisor must be 0.

PIO state machines may have an optional delay in cycles associated with each PIO instruction and may have *sideset* enabled for up to five pins; the sideset pins belong to the upper bits in the delay/sideset fields for instructions that have them and the delay pins belong to the lower bits in said fields. Sideset pin bits set the state of pins each cycle simultaneous with whatever other operations they are carrying out. `sm-sideset-pins!` sets the number of bits that will be used for sideset. Note that the highest bit in the five-bit delay/sideset field may be optionally set to mean sideset enabled if `sm-sideset-high-enable!` is set, leaving four bits for delay and sideset.

The legacy way of defining a PIO program is simply to use `create` to give it a name, then lists the instructions one at a time.  This requires manually dealing with addresses in jumps, as well as program attributes such as the wrap points.

A more convenient way to define a PIO program is using the `:pio` defining word.  Following that, the instructions are given including some additional ones that work only in the context of a `:pio` and are defined in word list `pioasm`.  Such a PIO program is ended with `;pio`.  Space for a program can be allocated by `alloc-piomem`.  It can then be loaded and set up with `setup-prog`, which takes care of loading as well as setting transfer address and wrap in a single operation.  Similar to the `armv6m` module, in this mode jumps use "marks", which are cells on the data stack, for forward and backward references.

PIO assembler words compile PIO instructions to `here` as 16 bits per instruction. There are two different basic types of PIO instructions - instructions without an associated delay or sideset, and instructions with an associated delay and/or sideset. The latter kind of instruction is marked with an `+` in its assembling word.

### `pio`

The `pio` module contains the following words:

#### Constants

##### `on`
( -- state )

On state.

##### `off`
( -- state )

Off state.

##### `right`
( -- direction )

Right direction.

##### `left`
( -- direction )

Left direction.

##### `PIO0`
( -- pio )

PIO0.

##### `PIO1`
( -- pio )

PIO1.

##### `PIO2`
( -- pio )

PIO2 (`rp2350 only).

##### `IRQ0`
( -- irq )

IRQ0 index.

##### `IRQ1`
( -- irq )

IRQ1 index.

##### `PIO0_IRQ0`
( -- irq )

PIO0 IRQ0 index.

##### `PIO0_IRQ1`
( -- irq )

PIO0 IRQ1 index.

##### `PIO1_IRQ0`
( -- irq )

PIO1 IRQ0 index.

##### `PIO1_IRQ1`
( -- irq )

PIO1 IRQ1 index.

##### `PIO2_IRQ0`
( -- irq )

PIO2 IRQ0 index (`rp2350` only).

##### `PIO2_IRQ1`
( -- irq )

PIO2 IRQ1 index (`rp2350` only).

#### PIO Words

##### `pins-pio-alternate`
( pin-base pin-count pio -- )

Configure GPIO pins starting from *pin-base* of a count up to *pin-count* to be in an alternate function state such that the PIO may make use to them. Note that the pins wrap around, e.g. `29 3 PIO0 pins-pio-alternate` configures GPIO pins 29, 0, and 1.

**Note**: this was previously specified as not being needed with `sm-sideset-pins!`, `sm-set-pins!`, or `sm-out-pins!`, but as it turned out that this could not be implemented as intended with `sm-sideset-pins!`, this functionality was removed from `sm-sideset-pins!`, and for the sake of consistency, from `sm-set-pins!` and `sm-out-pins!` as well. Conversely, this is not needed for inputs, as the PIO does not require alternate functions to input from GPIO pins.

##### `pio-instr-mem!`
( addr count pio -- )

Write *count* halfwords starting at *addr* to instruction memory starting at instruction 0.

##### `pio-instr-relocate-mem!`
( addr count offset pio -- )

Write *count* halfwords starting at *addr* to instruction memory starting at instruction *offset*, relocating any JMP instructions in the process so that a destination of 0 is relocated to *offset* and so on.

##### `pio-interrupt-enable`
( interrupt-bits irq pio -- )

Enable interrupts.

##### `pio-interrupt-disable`
( interrupt-bits irq pio -- )

Disable interrupts.

##### `pio-interrupt-enable-force`
( interrupt-bits irq pio -- )

Enable forcing interrupts.

##### `pio-interrupt-disable-force`
( interrupt-bits irq pio -- )

Disable forcing interrupts.

##### `pio-interrupt-raw@`
( pio -- interrupt-bits )

Get raw interrupts.

##### `pio-interrupt@`
( irq pio -- interrupt-bits )

Get interrupt status.

##### `INT_SM`
( state-machine -- index )

Interrupt bits.

##### `INT_SM_TXNFULL`
( state-machine -- index )

TXN not full interupt bits.

##### `INT_SM_RXNEMPTY`
( state-machine -- index )

RXN not empty interrupt bits.

#### State Machine Words

##### `sm-enable`
( state-machine-bits pio -- )

Enable state machines.

##### `sm-disable`
( state-machine-bits pio -- )

Disable state machines.

##### `sm-restart`
( state-machine-bits pio -- )

Restart state machines.

##### `sm-clkdiv!`
( fractional integral state-machine pio -- )

Set the clock divisor for a state machine.

##### `sm-addr!`
( address state-machine pio -- )

Set the address for a state machine.

##### `sm-wrap!`
( bottom-address top-address state-machine pio -- )

Set the wrapping of a state machine.

##### `sm-out-sticky!`
( sticky state-machine pio -- )

Set the sticky state of a state machine.

##### `sm-sideset-high-enable!`
( on/off state-machine pio -- )

Enable using the highest bit in the delay/sideset fields of instructions as a sideset enable bit for a state machine. This leaves four bits in the delay/sideset fields of instructions for delay and sideset.

##### `sm-sideset-pindir!`
( on/off state-machine pio -- )

Set sideset data assserted to pin directions pit.

##### `sm-jmp-pin!`
( pin state-machine pio -- )

Set GPIO number to use as condition for JMP PIN.

##### `sm-inline-out-enable!`
( pin state-machine pio -- )

Set a bit of OUT data to use as an inline OUT enable.

##### `sm-inline-out-enable-clear`
( state-machine pio -- )

Disable using a bit of OUT data as an inline OUT enable.

##### `sm-pull-threshold!`
( threshold state-machine pio -- )

Set OSR threshold before autopull or conditional pull will take place; value may be from 1 to 32.

##### `sm-push-threshold!`
( threshold state-machine pio -- )

Set ISR threshold before autopush or conditional push will take place; value may be from 1 to 32.

##### `sm-in-count!`
( in-count state-machine pio -- )

Set number of pins which are not masked to 0 when read by an IN PINS, WAIT PIN, or MOV x, PINS instruction; value may be from 1 to 32 (`rp2350` only).

##### `sm-txf!`
( x state-machine pio -- )

Write to the TX FIFO of a state machine.

##### `sm-rxf@`
( state-machine pio -- x )

Read from the RX FIFO of a state machine.

##### `sm-pin!`
( on/off pin state-machine pio -- )

Set the on/off state of a pin for a state machine. Note that the state machine should be disabled.

##### `sm-pindir!`
( out/in pin state-machine pio -- )

State the out/in state of a pin for a state machine. Note that the state machine should be disabled.

##### `sm-sideset-pins!`
( pin-base pin-count state-machine pio -- )

Set the sideset pins for a state machine. Note that the state machine should be disabled.

##### `sm-set-pins!`
( pin-base pin-count state-machine pio -- )

Set the SET pins for a state machine. Note that the state machine should be disabled.

##### `sm-out-pins!`
( pin-base pin-count state-machine pio -- )

Set the OUT pins for a state machine. Note that the state machine should be disabled.

##### `sm-in-pin-base!`
( pin-base state-machine pio -- )

Set the IN pin base for a state machine. Note that the state machine should be disabled.

##### `sm-rx-fifo-level@`
( state-machine pio -- level )

Get a state machine RX FIFO level.

##### `sm-tx-fifo-level@`
( state-machine pio -- level )

Get a state machine TX FIFO level.

##### `sm-join-rx-fifo!`
( join? state-machine pio -- )

Set joining the RX FIFO and TX FIFO of a state machine into a single eight by 32-bit RX FIFO.

##### `sm-join-tx-fifo!`
( join? state-machine pio -- )

Set joining the RX FIFO and TX FIFO of a state machine into a single eight by 32-bit TX FIFO.

##### `sm-out-shift-dir`
( direction state-machine pio -- )

Set the output shift register direction.

##### `sm-in-shift-dir`
( direction state-machine pio -- )

Set the input shift register direction.

##### `sm-autopull!`
( on/off state-machine pio -- )

Set autopull on/off.

##### `sm-autopush!`
( on/off state-machine pio -- )

Set autopush on/off.

##### `sm-instr!`
( addr count state-machine pio -- )

Manually write instructions to a state machine.

#### Words for "new style" PIO programs

##### `:pio`
( "name" -- pio-mark )

Starts the definition of PIO program `name`.  This imports word list `pioasm` to enable words that are only valid within a PIO program.  End the definition with `;pio`.

##### `;pio`
( pio-mark -- )

Ends the definition of the PIO program begun with `:pio`.

##### `p-code`
( prog -- code )

The start of the code (PIO instructions) for the given PIO program.

##### `p-size`
( prog -- size )

Program size in instructions for the given PIO program.

##### `p-wrap-bot`
( prog -- wrap-bot )

Wrap bottom for the given PIO program.

##### `p-wrap-top`
( prog -- wrap-top )

Wrap top for the given PIO program.

##### `p-wrap`
( prog -- wrap-bot wrap-top )

Both wrap points for the given PIO program.

##### `p-transfer`
( prog -- transfer )

Transfer (start) address for the given PIO program.

##### `p-prog`
( prog -- code size )

Code address and program length for the given PIO program, suitable for `pio-instr-mem!`.

##### `alloc-piomem`
( pio size -- base )

Allocate space for a program of length `size` in the specified PIO (PIO0 or PIO1) and returns the base address.  Raises `x-pio-no-room` if there is no room for a program of that size in the specified PIO.

##### `free-piomem`
( pio base size -- )

Frees previously allocated PIO program space.

##### `setup-prog`
( state-machine pio prog base -- )

This loads the specified program (defined by `:pio`) into `pio` starting at `base` and sets up `state-machine` in that PIO to run it.  The starting address and wrap parameters are set from the program attributes, i.e., `sm-wrap!` and `sm-addr!` have been done.  Other state machine specific setup such as pin mappings, as well as FIFO and clock parameters, have to be done separately.

In some applications the same program is used in several state machines of the same PIO, which allows the same program words to drive both state machines.  The simplest way to deal with this is to invoke `setup-prog` once for each state machine, with the same `base` each time.  That will load the program words each time, but this is harmless.

#### PIO Assembler Words

##### `jmp,`
( address condition -- )

PIO JMP instruction. Branch, conditionally or unconditionally. *Condition* is the condition, with or without a decrement, to apply to whether to branch. *Address* is the PIO instruction address to branch to.

##### `wait,`
( index source polarity -- )

PIO WAIT instruction. Wait for a condition to be true. *Polarity* is whether to wait for a 1 or a 0. *Source* is whether to wait for an absolute GPIO pin, a pin relative to the absolute `IN` pin base, or a PIO IRQ index. *Index* is the pin or PIO IRQ index to wait for.

##### `in,`
( bit-count source -- )

PIO IN instruction. Input bits from a source into the current state machine's ISR register. *Source* is the source to shift bits into the ISR register for the current state machine into, according to the configured shift in direction. *Bit-count* is how many bits to shift, a value from 1 to 32. The ISR bit shift count is increased by the bit count, saturating at 32.

##### `out,`
( bit-count destination -- )

PIO OUT instruction. Output bits from the current state machine's OSR register to a destination. *Destination* is the destination to shift bits into from the OSR register for the current state machine, according to the configured shift out direction. *Bit-count* is how many bits to shift, value from 1 to 32. The OSR bit shift count is increased by the bit count, saturating at 32.

##### `push,`
( block if-full -- )

PIO PUSH instruction. Push the contents of the current state machine's ISR register into the current state machine's RX FIFO and reset the ISR register to all zeros. *If-full* is the condition as to whether to conditionally push. *Block* is whether to stall if pushing is not possible due to the RX FIFO being full.

##### `pull,`
( block if-empty -- )

PIO PULL instruction. Pull a 32-bit value from the current state machine's TX FIFO into the current state machine's OSR register. *If-empty* is the condition as to whether to conditionally pull. *Block* is whether to stall if pulling is not possible due to the TX FIFO being empty.

##### `mov,`
( source op destination -- )

PIO MOV instruction. Transfer a value from *source* to *destination*, applying *op* (which may be a no-op) to the value in the process.

##### `mov-isr-rx-idx-imm,`
( index -- )

PIO MOV ISR to RX instruction, with an immediate index. Transfer a value from the ISR to an immediate index in the RX FIFO.

##### `mov-isr-rx-idx-y,`
( -- )

PIO MOV ISR to RX instruction, indexed by the Y register. Transfer a value from the ISR to an index in the RX FIFO determined by the Y register.

##### `mov-rx-osr-idx-imm,`
( index -- )

PIO MOV RX to OSR instruction, with an immediate index. Transfer a value from an immediate index in the RX FIFO to the OSR.

##### `mov-rx-osr-idx-y,`
( -- )

PIO MOV RX to OSR instruction, indexed by the Y register. Transfer a value from an index in the RX FIFO determined by the Y register to the OSR.

##### `irq,`
( index set/wait -- )

PIO IRQ instruction. Set or wait on a PIO IRQ *index*, from 0 to 7 before `REL` is applied. Note that `REL` can be used to mark a PIO IRQ as relative to the current state machine ID by adding the state machine ID to the lower two bits of the IRQ index, by way of module-4 addition on the two LSB's.

##### `set,`
( data destination -- )

PIO SET instruction. Set *destination* to *data*, which is a value from $00 to $1F.

##### `jmp+,`
( address delay/side-set condition -- )

PIO JMP instruction with delay or side-set. Branch, conditionally or unconditionally. *Condition* is the condition, with or without a decrement, to apply to whether to branch. *Address* is the PIO instruction address to branch to. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `wait+,`
( index delay/side-set source polarity -- )

PIO WAIT instruction with delay or side-set. Wait for a condition to be true. *Polarity* is whether to wait for a 1 or a 0. *Source* is whether to wait for an absolute GPIO pin, a pin relative to the absolute `IN` pin base, or a PIO IRQ index. *Index* is the pin or PIO IRQ index to wait for. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `in+,`
( bit-count delay/side-set source -- )

PIO IN instruction with delay or side-set. Input bits from a source into the current state machine's ISR register. *Source* is the source to shift bits into the ISR register for the current state machine into, according to the configured shift in direction. *Bit-count* is how many bits to shift, a value from 1 to 32. The ISR bit shift count is increased by the bit count, saturating at 32. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `out+,`
( bit-count delay/side-set destination -- )

PIO OUT instruction with delay or side-set. Output bits from the current state machine's OSR register to a destination. *Destination* is the destination to shift bits into from the OSR register for the current state machine, according to the configured shift out direction. *Bit-count* is how many bits to shift, value from 1 to 32. The OSR bit shift count is increased by the bit count, saturating at 32. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `push+,`
( delay/side-set block if-full -- )

PIO PUSH instruction with delay or side-set. Push the contents of the current state machine's ISR register into the current state machine's RX FIFO and reset the ISR register to all zeros. *If-full* is the condition as to whether to conditionally push. *Block* is whether to stall if pushing is not possible due to the RX FIFO being full. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `pull+,`
( delay/side-set block if-empty -- )

PIO PULL instruction with delay or side-set. Pull a 32-bit value from the current state machine's TX FIFO into the current state machine's OSR register. *If-empty* is the condition as to whether to conditionally pull. *Block* is whether to stall if pulling is not possible due to the TX FIFO being empty.

##### `mov+,`
( source delay/side-set op destination -- )

PIO MOV instruction with delay or side-set. Transfer a value from *source* to *destination*, applying *op* (which may be a no-op) to the value in the process. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `mov-isr-rx-idx-imm+,`
( delay/side-set index -- )

PIO MOV ISR to RX instruction, with an immediate index, with delay or side-set. Transfer a value from the ISR to an immediate index in the RX FIFO.

##### `mov-isr-rx-idx-y+,`
( delay/side-set -- )

PIO MOV ISR to RX instruction, indexed by the Y register, with delay or side-set. Transfer a value from the ISR to an index in the RX FIFO determined by the Y register.

##### `mov-rx-osr-idx-imm+,`
( delay/side-set index -- )

PIO MOV RX to OSR instruction, with an immediate index, with delay or side-set. Transfer a value from an immediate index in the RX FIFO to the OSR.

##### `mov-rx-osr-idx-y+,`
( delay/side-set -- )

PIO MOV RX to OSR instruction, indexed by the Y register, with delay or side-set. Transfer a value from an index in the RX FIFO determined by the Y register to the OSR.

##### `irq+,`
( index delay/side-set set/wait -- )

PIO IRQ instruction with delay or side-set. Set or wait on a PIO IRQ *index*, from 0 to 7 before `REL` is applied. Note that `REL` can be used to mark a PIO IRQ as relative to the current state machine ID by adding the state machine ID to the lower two bits of the IRQ index, by way of module-4 addition on the two LSB's. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

##### `set+,`
( data delay/side-set destination -- )

PIO SET instruction with delay or side-set. Set *destination* to *data*, which is a value from $00 to $1F. *Delay/side-set* is either a number of state machine cycles to delay after the instruction is executed, or up to five bits to write to the pins configured to sideset, depending upon how the current state machine is configured.

#### Module `pioasm`

These words are useful only within the context of a `:pio` program definition.  They are in the `pioasm` word list, which is automatically imported by `:pio` and unimported by `;pio`.

##### `mark<`
( -- jmp-mark )

Mark a backward destination.

##### `>mark`
( jmp-mark -- )

Mark a forward destination.

##### `jmp>`
( condition -- jmp-mark )

Forward PIO JMP to next unmatched `>mark`, without delay/side-set.

##### `jmp<`
( jmp-mark condition -- )

Backward jump to preceding unmatched `mark<`, without delay or side-set.

##### `jmp+>`
( delay/side-set condition -- mark-add marker )

Forward PIO JMP to next unmatched `>mark` with delay or side-set.

##### `jmp+<`
( jmp-mark marker delay/side-set condition -- )

Backward jump to preceding unmatched `mark<` with delay or side-set.

##### `wrap<`
( -- )

Set wrap *bottom* at this line (defaults to first instruction).

##### `<wrap`
( -- )

Set wrap *top* at preceding line (defaults to end of program).

##### `start>`
( -- )

Set transfer address (program start) at this line (defaults to first instruction).

##### `COND_ALWAYS`
( -- condition )

Always jump.

##### `COND_X0=`
( -- condition )

Jump if scratch X is zero.

##### `COND_X1-`
( -- condition )

Jump if scratch X is non-zero, post-decrement.

##### `COND_Y0=`
( -- condition )

Jump if scratch Y is zero.

##### `COND_Y1-`
( -- condition )

Jump if scratch Y is non-zero, post-decrement.

##### `COND_XY<>`
( -- condition )

Jump if scratch X not equal scratch Y.

##### `COND_PIN`
( -- condition )

Jump on input pin.

##### `COND_IOSRE`
( -- condition )

Jump on output shift register not empty.

##### `WAIT_GPIO`
( -- wait )

Wait for GPIO.

##### `WAIT_PIN`
( -- wait )

Wait for a pin.

##### `WAIT_IRQ`
( -- wait )

Wait for an IRQ.

##### `WAIT_JMPPIN`
( -- wait )

Wait on the pin indexed by the PINCTRL_JMP_PIN configuration, plus an index in the range 0-3, modulo 32 (`rp2350` only).

##### `IN_PINS`
( -- in-source )

Pins input.

##### `IN_X`
( -- in-source )

Scratch register X input.

##### `IN_Y`
( -- in-source )

Scratch register Y input.

##### `IN_NULL`
( -- in-source )

NULL input (all zeros).

##### `IN_ISR`
( -- in-source )

ISR input.

##### `IN_OSR`
( -- in-source )

OSR input.

##### `OUT_PINS`
( -- out-destination )

Pins output.

##### `OUT_X`
( -- out-destination )

Scratch register X output.

##### `OUT_Y`
( -- out-destination )

Scratch register Y output.

##### `OUT_NULL`
( -- out-destination )

NULL output (discard data).

##### `OUT_PINDIRS`
( -- out-destination )

PINDIRs output.

##### `OUT_PC`
( -- out )

PC output (unconditional jump to shifted address).

##### `OUT_ISR`
( -- out )

ISR output (also sets ISR shift counter to bit count).

##### `OUT_EXEC`
( -- out )

Execute OSR shift data as instruction.

##### `PUSH_NOT_FULL`
( -- push-if-full-option )

Push data even if threshold is not met.

##### `PUSH_IF_FULL`
( -- push-if-full-option )

Do nothing unless the total input shift count has reached its threshold.

##### `PUSH_NO_BLOCK`
( -- push-block-option )

Do not stall execution if RX FIFO is full, instead drop data from ISR.

##### `PUSH_BLOCK`
( -- push-block-option )

Stall execution if RX FIFO is full.

##### `PULL_NOT_EMPTY`
( -- pull-if-full-option )

Pull data even if threshold is not met.

##### `PULL_IF_EMPTY`
( -- pull-if-full-option )

Do nothing unless the total output shift count has reached its threshold.

##### `PULL_NO_BLOCK`
( -- pull-block-option )

Do not stall execution if TX FIFO is empty, instead copy from scratch X.

##### `PULL_BLOCK`
( -- pull-block-option )

Stall execution if TX FIFO is empty.

##### `MOV_DEST_PINS`
( -- mov-destination )

Move to PINS.

##### `MOV_DEST_X`
( -- mov-destination )

Move to scratch register X.

##### `MOV_DEST_Y`
( -- mov-destination )

Move to scratch register Y.

##### `MOV_DEST_PINDIRS`
( -- mov-destination )

Move to PINDIRS (`rp2350` only).

##### `MOV_DEST_EXEC`
( -- mov-destination )

Move to EXEC (execute data as instruction).

##### `MOV_DEST_PC`
( -- mov-destination )

Move to PC (treat data as address for unconditional branch).

##### `MOV_DEST_ISR`
( -- mov-destination )

Move to ISR (input shift counter is reset to 0, i.e. empty).

##### `MOV_DEST_OSR`
( -- mov-destination )

Move to OSR (output shift counter is reset to 0, i.e. full).

##### `MOV_OP_NONE`
( -- mov-op )

Move operation none.

##### `MOV_OP_INVERT`
( -- mov-op )

Move operation invert.

##### `MOV_OP_REVERSE`
( -- mov-op )

Move operation bit-reverse.

##### `MOV_SRC_PINS`
( -- mov-op )

Move from PINS.

##### `MOV_SRC_X`
( -- mov-source )

Move from scratch register X.

##### `MOV_SRC_Y`
( -- mov-source )

Move from scratch register Y.

##### `MOV_SRC_NULL`
( -- mov-source )

Move from NULL.

##### `MOV_SRC_STATUS`
( -- mov-source )

Move from STATUS.

##### `MOV_SRC_ISR`
( -- mov-source )

Move from ISR.

##### `MOV_SRC_OSR`
( -- mov-source )

Move from OSR.

##### `IRQ_SET`
( -- irq-op )

Raise an IRQ.

##### `IRQ_CLEAR`
( -- irq-op )

Clear an IRQ.

##### `IRQ_WAIT`
( -- irq-op )

Wait for an IRQ to be lowered.

##### `SET_PINS`
( -- set-destination )

Set PINS.

##### `SET_X`
( -- set-destination )

Set scratch register X (5 LSBs are set to data, all others are cleared).

##### `SET_Y`
( -- set-destination )

Set scratch register Y (5 LSBs are set to data, all others are cleared).

##### `SET_PINDIRS`
( -- set-destination )

Set PINDIRS.

##### `PREV`
( irq -- irq-prev )

Reference an IRQ in the previous-numbered PIO, wrapping around (`rp2350` only).

##### `REL`
( irq -- irq-rel )

Add the state machine ID to the lower two bits of the IRQ index, by way of module-4 addition on the two LSB's.

##### `NEXT`
( irq -- irq-next )

Reference an IRQ in the next-numbered PIO, wrapping around (`rp2350` only).

#### Exceptions

##### `x-sm-out-of-range`
( -- )

State machine out of range exception.

##### `x-pio-out-of-range`
( -- )

PIO out of range exception.

##### `x-too-many-instructions`
( -- )

Too many instructions exception.

##### `x-address-out-of-range`
( -- )

Address out of range exception.

##### `x-too-many-pins`
( -- )

Too many pins exception.

##### `x-pin-out-of-range`
( -- )

Pin out of range exception.

##### `x-clkdiv-out-of-range`
( -- )

Clock divisor out of range exception.

##### `x-irq-out-of-range`
( -- )

IRQ out of range exception.

##### `x-interrupt-out-of-range`
( -- )

Interrupt out of range exception.

##### `x-threshold-out-of-range`
( -- )

Buffer threshold out of range exception.

##### `x-bit-out-of-range`
( -- )

Bit out of range exception.

##### `x-relocate-out-of-range`
( -- )

Relocation out of range exception.

##### `x-in-pio`
( -- )

`:pio` when inside a previous `:pio` exception.

##### `x-not-in-pio`
( -- )

`;pio` or other word valid only after `:pio` exception.

##### `x-incorrect-mark-type`
( -- )

Wrong mark for jump or mark exception.

##### `x-pio-no-room`
( -- )

`alloc-piomem` has no room for a program this big exception.

##### `x-invalid-size`
( -- )

`alloc-piomem` size argument > 32 exception.

##### `x-invalid-base`
( -- )

Invalid program base address in `setup-prog` or `free-piomem` exception.

##### `x-invalid-gpio-base`
( -- )

Invalid GPIO base (i.e. not 0 or 16) exception (`rp2350` only).
