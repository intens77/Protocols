from threading import Thread

from server import SNTPServer

BUFFER_SIZE = 4096


class SNTPWorker(Thread):
    def __init__(self, socket, delay):
        super().__init__()
        self.socket = socket
        self.delay = delay

    def run(self):
        request_package, address = self.socket.recvfrom(BUFFER_SIZE)
        print(" : ".join(map(lambda e: str(e), address)))
        package = SNTPServer(request_package, self.delay).build_package()
        self.socket.sendto(package, address)
