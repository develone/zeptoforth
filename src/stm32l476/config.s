@ Copyright (c) 2019-2024 Travis Bemann
@
@ Permission is hereby granted, free of charge, to any person obtaining a copy
@ of this software and associated documentation files (the "Software"), to deal
@ in the Software without restriction, including without limitation the rights
@ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
@ copies of the Software, and to permit persons to whom the Software is
@ furnished to do so, subject to the following conditions:
@ 
@ The above copyright notice and this permission notice shall be included in
@ all copies or substantial portions of the Software.
@ 
@ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
@ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
@ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
@ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
@ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
@ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
@ SOFTWARE.

	.equ thumb2, 1
        .equ chip0, 0x73746D
        .equ chip1, 0x6C0001DC
        .equ cortex_m7, 0
        .equ cortex_m33, 0
	.equ ram_start, 0x20000000
	.equ ram_end, 0x20018000
	.equ rstack_size, 0x0400
	.equ rstack_top, ram_end
	.equ stack_size, 0x0200
	.equ stack_top, ram_end - rstack_size
	.equ vector_count, 112
	.equ vector_table_size, vector_count * 4 @ in bytes
	.equ VTOR_value, vector_table | (1 << 29)
	.equ VTOR, 0xE000ED08
	.equ flash_buffers_top, stack_top - stack_size
	.equ flash_block_size, 16 @ in bytes
	.equ flash_buffer_count, 32
	.equ flash_buffer_size, flash_block_size + 8
	.equ flash_buffer_space, flash_block_size
	.equ flash_buffer_addr, flash_block_size + 4
	.equ flash_min_address, 0x00008000
	.equ flash_start, 0x00000000
	.equ flash_dict_start, 0x00008000
	.equ flash_dict_end, 0x00100000
	.equ input_buffer_size, 255
	.equ pad_offset, 128
	.equ flash_mini_dict_size, 0 @ in bytes
	.equ cpu_count, 1
	.equ syntax_stack_size, 64 @ in bytes
