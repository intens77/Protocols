import argparse


def parse_arguments():
    parser = argparse.ArgumentParser()

    parser.add_argument('-m', type=int, dest='max_hops', default=30,
                        help='Maximum number of hops to search for target.')

    parser.add_argument('-d', action='store_true', dest='no_hostname',
                        default=False,
                        help='Do not resolve addresses to hostname.')

    parser.add_argument('-w', '-timeout', type=float, dest='timeout',
                        default=4000.0,
                        help='Wait timeout milliseconds for each reply.')

    parser.add_argument('target_name', nargs=1)

    return parser.parse_args()
