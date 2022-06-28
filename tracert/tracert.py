import socket
import struct
import sys
import time
from arguments_parser import parse_arguments
from ICMP import ConstICMP, build_packet, parse_header
from whois import whois

PORT = 0
BUFFER = 1024


def get_route(hostname, mode, max_hops, timeout):
    sequence = 1
    for ttl in range(1, max_hops):
        print(f'{ttl: >3}', end='  ', flush=True)

        dest_addr = socket.gethostbyname(hostname)
        sock = get_socket(timeout, ttl)
        sequence += 1

        trace_complete = make_iteration(dest_addr, sock, sequence, mode)
        if trace_complete:
            print('Trace complete', end='\r\n')
            return


def make_iteration(dest_addr, sock, sequence, mode):
    sock.sendto(build_packet(sequence), (dest_addr, PORT))
    start_time = time.time()
    try:
        recv_packet, addr = sock.recvfrom(BUFFER)
        time_received = time.time()
    except socket.timeout:
        print_timeout()
        return False

    header = parse_header(recv_packet)
    processed_types = (ConstICMP.TIME_EXCEEDED, ConstICMP.DEST_UNREACHABLE,
                       ConstICMP.ECHO_REPLY)
    if header.request_type in processed_types:
        res = None
        if not mode:
            res = whois(addr[0])
        print_output(addr[0], time_received - start_time, res)
    if header.request_type == ConstICMP.ECHO_REPLY:
        return True

    return False


def get_socket(timeout, ttl):
    sock = socket.socket(family=socket.AF_INET, type=socket.SOCK_RAW,
                         proto=socket.IPPROTO_ICMP)
    sock.settimeout(timeout / 1000)
    sock.setsockopt(socket.IPPROTO_IP, socket.IP_TTL, struct.pack('I', ttl))
    return sock


def print_output(addr, rtt, info):
    rtt = round(rtt * 1000)
    if not info:
        print(f'{addr : <17}{rtt : >4} ms', flush=True, end='\r\n')
    else:
        try:
            print(f'{addr : <17}{rtt : >4} ms', flush=True, end='\r\n')
            info_str = ", ".join(list(filter(lambda el: el, info.values())))
            if info_str:
                print(f'     {info_str}', flush=True, end='\r\n')
        except socket.herror:
            print(f'{addr : <17}{rtt : >4} ms', flush=True, end='\r\n')
    print(end='\r\n')


def print_timeout():
    print('    *    ', flush=True, end='\r\n\r\n')


if __name__ == '__main__':
    args = parse_arguments()
    try:
        addr = socket.gethostbyname(args.target_name[0])
    except socket.gaierror:
        print(f'{args.target_name[0]} is invalid')
        sys.exit(10)

    print(f'Tracing rout to {addr}\r\n'
          f'over a maximum of {args.max_hops} hops:', end='\r\n')
    try:
        get_route(args.target_name[0], args.no_hostname, args.max_hops,
                  args.timeout)
    except PermissionError:
        print("Permission error. Use sudo")
        sys.exit(11)
