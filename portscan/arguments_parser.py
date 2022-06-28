import argparse


def parse_arguments():
    parser = argparse.ArgumentParser()

    parser.add_argument('-t', action='store_true', dest='tcp_scan',
                        required=True, help='The program will scan tcp ports.')

    parser.add_argument('-p', '--ports', type=int, dest='port_range',
                        nargs=2, default=[1, 65535],
                        help='This port range will be scanned.')

    parser.add_argument('host', help='This host will be scanned.')

    return parser.parse_args()
