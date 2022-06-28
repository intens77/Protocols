import sys
from arguments_parser import parse_arguments
from scanner import Scanner


def port_validation(port_range):
    for port in port_range:
        if port < 1 or port > 65535:
            raise IndexError()
    return sorted(port_range)


def main(host, port_range, tcp_scan):
    scanner = Scanner(host, port_range)
    try:
        scanner.start()
    except KeyboardInterrupt:
        scanner.stop()


if __name__ == '__main__':
    args = parse_arguments()
    try:
        port_range = port_validation(args.port_range)
    except IndexError:
        print('You entered an invalid port value.')
        sys.exit(10)

    try:
        main(args.host, port_range, args.tcp_scan)
    except PermissionError:
        print("Permission error. Use sudo.")
        sys.exit(11)
