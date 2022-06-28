import socket
import struct
from collections import namedtuple


class ConstICMP:
    ICMP_ECHO_REQUEST = 8
    ICMP_ECHO_REQUEST_CODE = 0
    ECHO_REPLY = 0
    TIME_EXCEEDED = 11
    DEST_UNREACHABLE = 3


def build_packet(sequence):
    checksum = 0
    ID = socket.htons(1)
    header = struct.pack("bbHHh", ConstICMP.ICMP_ECHO_REQUEST,
                         ConstICMP.ICMP_ECHO_REQUEST_CODE,
                         checksum, ID, sequence)
    data = struct.pack("qqqqqqqq", 0, 0, 0, 0, 0, 0, 0, 0)
    checksum = socket.htons(get_checksum(header + data))
    header = struct.pack("bbHHh", ConstICMP.ICMP_ECHO_REQUEST,
                         ConstICMP.ICMP_ECHO_REQUEST_CODE,
                         checksum, ID, sequence)

    packet = header + data
    return packet


def get_checksum(string):
    string = bytearray(string)
    csum = 0
    count_to = (len(string) // 2) * 2

    for count in range(0, count_to, 2):
        this_val = string[count + 1] * 256 + string[count]
        csum = csum + this_val
        csum = csum & 0xffffffff

    if count_to < len(string):
        csum = csum + string[-1]
        csum = csum & 0xffffffff

    csum = (csum >> 16) + (csum & 0xffff)
    csum = csum + (csum >> 16)
    answer = ~csum
    answer = answer & 0xffff
    answer = answer >> 8 | (answer << 8 & 0xff00)
    return answer


def parse_header(recv_packet):
    icmp_header = recv_packet[20:28]
    request_type, code, checksum, packet_id, sequence = \
        struct.unpack('bbHHh', icmp_header)
    Header = namedtuple('Header', ['request_type', 'code', 'checksum',
                                   'packet_id', 'sequence'])
    return Header(request_type, code, checksum, packet_id, sequence)
