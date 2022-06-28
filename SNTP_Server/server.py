from struct import pack, unpack
from time import time


class SNTPServer:
    stratum = 1
    mode = 4
    head_format = ">BBBBII4sQQQQ"
    leap_indicator = 0
    version_number = 4
    utc_offset = 2208988800

    def __init__(self, request_package, delay):
        self.offset = delay
        self.request_time = self.get_current_time()
        self.dispatch_time = self.get_dispatch_time(request_package)

    def build_package(self):
        return pack(
            self.head_format,
            self.leap_indicator << 6 | self.version_number << 3 | self.mode, self.stratum,
            0, 0, 0, 0, b'', 0, self.dispatch_time, self.request_time,
            self.get_current_time()
        )

    def get_dispatch_time(self, package):
        return unpack(self.head_format, package)[10]

    def get_current_time(self):
        time_with_offset = time() + self.utc_offset + self.offset
        return int(time_with_offset * (2 ** 32))
