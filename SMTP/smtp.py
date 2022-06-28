import getpass
import json
import socket
import ssl
import base64
import os
import random
import sys
import urllib.request

from arguments_parser import parse_arguments

context = ssl.create_default_context()


class SMTP:
    def __init__(self, host, port, sender, receiver, subject, attachments,
                 ssl=False, auth=False, verbose=False, timeout=5):
        self.host = host
        self.port = port
        self.sender = sender
        self.receiver = receiver
        self.auth = auth
        self.verbose = verbose
        self.ssl = ssl
        self.subject = subject
        self.attachments = attachments

        socket.setdefaulttimeout(timeout)
        self.sock = None

    def start(self):
        self.connect()
        try:
            recv = self.sock.recv(1024)
        except socket.timeout:
            print(f'ERROR: Unable to connect to the server {self.host}:{self.port}')
            sys.exit(12)

        if self.verbose:
            print('s: ' + recv.decode())

        self.login_in()

        message = self.create_message_with_attachments(self.get_boundary())
        self.send_message(message)

    def close(self):
        self.sock.close()

    def connect(self):
        try:
            self.sock = socket.create_connection((self.host, self.port))
        except socket.timeout:
            print(f'ERROR: Unable to connect to the server {self.host}:{self.port}')
            sys.exit(12)
        if self.ssl:
            try:
                self.sock = context.wrap_socket(self.sock, server_hostname=self.host)
            except ssl.SSLError:
                print('Unable to establish a secure connection using SSL.')
                self.sock = socket.create_connection((self.host, self.port))

    def login_in(self):
        ip = urllib.request.urlopen('https://api.ipify.org').read().decode('utf8')
        recv = self.send_command(f'EHLO {ip}'.encode())
        if self.ssl and (b'STARTTLS' in recv):
            self.start_tls()

        while self.auth:
            login, password = self.ask_login_and_pass()
            self.send_command(b'AUTH LOGIN')
            self.send_command(base64.b64encode(login.encode()))
            recv = self.send_command(base64.b64encode(password.encode()))

            if b'235' in recv:
                break

    def start_tls(self):
        recv = self.send_command(b'STARTTLS')
        if b'220' in recv:
            try:
                self.sock = context.wrap_socket(self.sock, server_hostname=self.host)
            except ssl.SSLError:
                print('ERROR: Unable to establish a secure connection using STARTTLS.')
                sys.exit(13)

    @staticmethod
    def ask_login_and_pass():
        login = input('Enter login: ')
        password = getpass.getpass('Enter password: ')

        return login, password

    def send_command(self, command, show_command=True, buffer=1024):
        if self.verbose and show_command:
            print('c: ' + command.decode())

        self.sock.send(command + b'\r\n')
        recv = self.sock.recv(buffer)

        if self.verbose:
            print('s: ' + recv.decode())

        return recv

    def get_boundary(self):
        msg = self.create_message_with_attachments('')
        boundary = 'gc0p4Jq0M2Yt08jU534c0p'
        while boundary in msg:
            boundary += random.randint(10, 100)
        return boundary

    def create_message_with_attachments(self, boundary):
        attachments = f'--{boundary}\r\n' + f'--{boundary}\r\n'.join(self.attachments)
        return (
            f'To: {self.receiver}\r\n'
            f'From: {self.sender}\r\n'
            f'MIME-Version: 1.0\r\n'
            f'Subject: =?utf-8?B?{base64.b64encode(self.subject.encode()).decode()}?=\r\n'
            f'Content-Type: multipart/mixed; boundary="{boundary}"; charset=UTF-8\r\n\r\n'
            f'{attachments}--{boundary}--\r\n'
            f'.\r\n'
        )

    def send_message(self, message):
        recv = self.send_command(f'MAIL FROM: <{self.sender}>'.encode())
        if recv.decode().startswith('5'):
            print(f'ERROR: Invalid sender. {recv.decode()}')
            sys.exit(14)

        recv = self.send_command(f'RCPT TO: <{self.receiver}>'.encode())
        if recv.decode().startswith('5'):
            print(f'ERROR: Invalid receiver. {recv.decode()}')
            sys.exit(15)

        recv = self.send_command('DATA'.encode())
        if b'354' in recv:
            self.send_command(message.encode(), show_command=False)
        else:
            print(f'ERROR: Email not sent. {recv.decode()}')


def get_attachments(directory):
    if not os.path.exists(directory) or not os.path.isdir(directory):
        print('ERROR: Directory is not exists')
        sys.exit(16)

    if not os.path.exists(os.path.join('.', 'mime_types.json')) \
            or not os.path.isfile(os.path.join('.', 'mime_types.json')):
        print('ERROR: File "mime_types.json" not found.')
        sys.exit(17)

    with open(os.path.join('.', 'mime_types.json')) as f:
        mime_types = json.loads(f.read())

    attachment_files = []
    for item in os.listdir(directory):
        if not os.path.isfile(os.path.join(directory, item)):
            continue

        content_type = item.split('.')[-1]
        if content_type not in mime_types:
            continue

        with open(os.path.join(directory, item), 'rb') as f:
            file_content = f.read()

        content = base64.b64encode(file_content).decode()
        attachment_files.append((os.path.basename(item), content, content_type))

    return attachment_files


def format_attachments(files):
    attachments = []
    for file in files:
        content_filename = file[0]
        content_file = file[1]
        content_type = file[2]
        attachments.append(
            (
                f'Content-Type: {content_type}"\r\n'
                f'Content-Disposition: attachment; filename="{content_filename}"\r\n'
                f'Content-Transfer-Encoding: base64\r\n\r\n'
                f'{content_file}\r\n'
            )
        )
    return attachments


if __name__ == '__main__':
    args = parse_arguments()

    port = 25
    if ':' in args.server:
        port = int(args.server.split(':')[1])
        args.server = args.server.split(':')[0]

    files = get_attachments(args.directory)
    server = SMTP(args.server, port, args.sender, args.receiver, args.subject, format_attachments(files),
                  ssl=args.ssl, auth=args.auth, verbose=args.verbose)
    try:
        server.start()
    finally:
        server.close()
