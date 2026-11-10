"""Read the .NET #US heap without loading or executing a mod assembly."""

import struct


def user_strings(path):
    data = path.read_bytes()
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError("Not a PE file")
    sections, optional_size = struct.unpack_from("<H12xH", data, pe + 6)
    optional = pe + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    directories = optional + (96 if magic == 0x10B else 112)
    section_table = optional + optional_size

    def offset(rva):
        for index in range(sections):
            entry = section_table + index * 40
            virtual_size, address, raw_size, pointer = struct.unpack_from("<IIII", data, entry + 8)
            if address <= rva < address + max(virtual_size, raw_size):
                return pointer + rva - address
        raise ValueError(f"Unmapped RVA: {rva}")

    clr_rva = struct.unpack_from("<I", data, directories + 14 * 8)[0]
    if not clr_rva:
        return []
    metadata_rva = struct.unpack_from("<I", data, offset(clr_rva) + 8)[0]
    metadata = offset(metadata_rva)
    if data[metadata:metadata + 4] != b"BSJB":
        raise ValueError("Missing CLR metadata signature")
    version_length = struct.unpack_from("<I", data, metadata + 12)[0]
    cursor = (metadata + 16 + version_length + 3) & ~3
    streams = struct.unpack_from("<H", data, cursor + 2)[0]
    cursor += 4
    heap = None
    for _ in range(streams):
        start, size = struct.unpack_from("<II", data, cursor)
        end = data.index(b"\0", cursor + 8)
        name = data[cursor + 8:end].decode("ascii")
        cursor = (end + 4) & ~3
        if name == "#US":
            heap = data[metadata + start:metadata + start + size]
    if heap is None:
        return []
    cursor = 1
    result = []
    while cursor < len(heap):
        first = heap[cursor]
        cursor += 1
        if first < 0x80:
            length = first
        elif first < 0xC0:
            length = ((first & 0x3F) << 8) | heap[cursor]
            cursor += 1
        elif first < 0xE0:
            length = ((first & 0x1F) << 24) | int.from_bytes(heap[cursor:cursor + 3], "big")
            cursor += 3
        else:
            raise ValueError("Invalid compressed string length")
        if not length:
            continue
        end = cursor + length
        if end > len(heap):
            raise ValueError("String extends beyond #US heap")
        result.append(heap[cursor:end - 1].decode("utf-16-le"))
        cursor = end
    return result
