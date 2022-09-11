#!/usr/bin/env python3

from sys import byteorder

header = f'# {byteorder.upper()}-ENDIAN TESTS #'
header_length = len(header)

print('#' * header_length)
print(header)
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

print(f'FastHash Tail: {tail}')
print()

###############
# HighwayHash #
###############

buffer = (210, 83, 73, 5, 227, 27, 187, 194, 146, 95, 250, 101, 181, 13, 103, 1, 136, 25, 255, 78, 183, 0, 0, 84, 122, 180, 25, 202, 41, 2, 73)

offset = 0
remainder = len(buffer) & 31

diff4 = remainder & ~3
mod4 = remainder & 3
packet = [0] * 32

if diff4 > 0:
    packet[0:diff4] = buffer[offset:diff4]
    offset += diff4

if (remainder & 16) > 0:
    for i in range(4):
        packet[28 + i] = buffer[offset + (i + mod4 - 4)]
elif mod4 > 0:
    packet[16] = buffer[offset]
    packet[17] = buffer[offset + (mod4 >> 1)]
    packet[18] = buffer[offset + (mod4 - 1)]

for i in range(0,32,8):
    pi = packet[i:i+8]
    ki = int.from_bytes(pi, 'little', signed=False)
    print(f'HighwayHash P{i + 1}: {pi}')
    print(f'HighwayHash K{i + 1}: {ki}')

print()

###########
# MirHash #
###########

buffer = (87, 0, 133, 53, 12)
tail = 0

for i in range(len(buffer)):
    tail = (tail >> 8) | (buffer[i] << 56)

print(f'MirHash Tail: {tail}')
print()

###########
# MumHash #
###########

buffer = (16, 205, 33, 39, 6, 142, 199)
tail = 0

tail += int.from_bytes(buffer[0:4], 'little', signed=False)
tail |= buffer[6] << 48
tail |= buffer[5] << 40
tail |= buffer[4] << 32

print(f'MumHash Tail: {tail}')
print()
