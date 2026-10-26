
#!/usr/bin/env python3

from sys import byteorder


def verify(name, actual, expected):

    if actual != expected:
        raise AssertionError(f"{name}: FAIL\nExpected {expected!r}\nActual {actual!r}")

    print(f"{name}: PASS")


def test_binary_operations():

    print("\n# Binary Operations #")

    buffer = bytes([
        0x01, 0x23, 0x45, 0x67,
        0x89, 0xAB, 0xCD, 0xEF
    ])

    verify("Read16", int.from_bytes(buffer[:2], "little"), 0x2301)
    verify("Read32", int.from_bytes(buffer[:4], "little"), 0x67452301)
    verify("Read64", int.from_bytes(buffer, "little"), 0xEFCDAB8967452301)

    verify("Write16", (0x2301).to_bytes(2, "little"), buffer[:2])
    verify("Write32", (0x67452301).to_bytes(4, "little"), buffer[:4])
    verify("Write64", (0xEFCDAB8967452301).to_bytes(8, "little"), buffer)


def test_fasthash():

    print("\n# FastHash #")

    buffer = (232, 126, 56, 93, 172, 17, 3)

    tail = 0

    tail ^= buffer[6] << 48
    tail ^= buffer[5] << 40
    tail ^= buffer[4] << 32
    tail ^= buffer[3] << 24
    tail ^= buffer[2] << 16
    tail ^= buffer[1] << 8
    tail ^= buffer[0]

    verify("FastHash Tail", tail, 0x0311AC5D387EE8)


def test_highwayhash():

    print("\n# HighwayHash #")

    buffer = (
        210, 83, 73, 5, 227, 27, 187, 194,
        146, 95, 250, 101, 181, 13, 103, 1,
        136, 25, 255, 78, 183, 0, 0, 84,
        122, 180, 25, 202, 41, 2, 73
    )

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

    expected = (
        0xC2BB1BE3054953D2,
        0x01670DB565FA5F92,
        0x540000B74EFF1988,
        0x490229CACA19B47A
    )

    for i in range(4):

        b = bytes(packet[i * 8:(i + 1) * 8])
        value = int.from_bytes(b, "little", signed=False)

        verify(f"HighwayHash K{i + 1}", value, expected[i])


def test_mirhash():

    print("\n# MirHash #")

    buffer = (87, 0, 133, 53, 12)

    tail = 0

    for i in range(len(buffer)):
        tail = (tail >> 8) | (buffer[i] << 56)

    verify("MirHash Tail", tail, 0x0C35850057000000)


def test_mumhash():

    print("\n# MumHash #")

    buffer = (16, 205, 33, 39, 6, 142, 199)

    tail = 0

    tail += int.from_bytes(buffer[0:4], "little", signed=False)
    tail |= buffer[6] << 48
    tail |= buffer[5] << 40
    tail |= buffer[4] << 32

    verify("MumHash Tail", tail, 0x00C78E062721CD10)


def main():

    header = f"# {byteorder.upper()}-ENDIAN TESTS #"
    header_length = len(header)

    print("#" * header_length)
    print(header)
    print("#" * header_length)

    test_binary_operations()
    test_fasthash()
    test_highwayhash()
    test_mirhash()
    test_mumhash()


if __name__ == "__main__":
    main()
