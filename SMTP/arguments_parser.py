import argparse


def parse_arguments():
    parser = argparse.ArgumentParser(
        description="SMTP Client that send images from directory as an attachment to mail")

    parser.add_argument('--ssl', action='store_true',
                        help='Enable ssl connection, if server supports it')
    parser.add_argument('-s', '--server', action='store', required=True,
                        help='Address (or domain name) of the SMTP Server in the format address [:port]')
    parser.add_argument('-t', '--to', action='store', dest='receiver',
                        required=True, help='Receiver email')
    parser.add_argument('-f', '--from', action='store', dest='sender',
                        default='', help='Sender email. By default is <>')
    parser.add_argument('--subject', action='store', default='Happy Pictures',
                        help='Email subject')
    parser.add_argument('--auth', action='store_true',
                        help='Request authorization')
    parser.add_argument('-v', '--verbose', action='store_true',
                        help='Display of the protocol of work')
    parser.add_argument('-d', '--directory', action='store', default='.',
                        help='Directory with images to send')

    return parser.parse_args()
