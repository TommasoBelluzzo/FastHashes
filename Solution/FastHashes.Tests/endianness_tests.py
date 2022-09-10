#!/usr/bin/env python3

from sys import byteorder

header = f'# {byteorder.upper()}-ENDIAN TESTS #'
header_length = len(header)

print('#' * header_length)
print(f'# {byteorder.upper()}-ENDIAN TESTS #')
print('#' * header_length)
print()

############
# FastHash #
############

buffer = (232, 126, 56, 93, 172, 17, 3)
tail = 0

tail ^= buffer[6] << 48
tail ^= buffer[5] << 40
tail ^= buffer[4] << 32
tail ^= buffer[3] << 24
tail ^= buffer[2] << 16
tail ^= buffer[1] << 8
tail ^= buffer[0]

print(f'FastHash: {tail}')
