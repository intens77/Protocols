import threading
import socket
import queue


class Scanner:
    def __init__(self, host, port_range, timeout=0.25, workers=20):
        self.host = host
        self.ports = queue.Queue()
        for i in range(port_range[0], port_range[1] + 1):
            self.ports.put(i)

        socket.setdefaulttimeout(timeout)
        self.is_running = True

        self.threads = []
        for i in range(workers):
            self.threads.append(threading.Thread(target=self.run))

    def start(self):
        print('Scan started...')
        for thread in self.threads:
            thread.setDaemon(True)
            thread.start()

        for thread in self.threads:
            thread.join()

    def stop(self):
        self.is_running = False
        for thread in self.threads:
            thread.join()

    def run(self):
        while self.is_running:
            try:
                port = self.ports.get(block=False)
            except queue.Empty:
                break
            self.check_tcp_port(port)

    def check_tcp_port(self, port):
        sock = socket.socket()
        try:
            sock.connect((self.host, port))
        except socket.error or ConnectionResetError or ConnectionAbortedError:
            sock.close()
            return

        sock.send(b'aaa\r\n\r\n')
        try:
            data = sock.recv(1024)
            print(f'TCP {port} {self.define_protocol(data)}')
        except socket.timeout:
            pass
        except ConnectionResetError or ConnectionAbortedError:
            pass

        sock.close()

    @staticmethod
    def define_protocol(data):
        if b'SMTP' in data:
            return 'SMTP'
        if b'POP3' in data:
            return 'POP3'
        if b'IMAP' in data:
            return 'IMAP'
        if b'HTTP' in data:
            return 'HTTP'
        return ''
