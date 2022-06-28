import socket, struct, sys, time

NTP_SERVER = 'localhost'
TIME1970 = 2208988800
PORT = 123


def sntp_client():
    client = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    data = '\x1b' + 47 * '\0'
    client.sendto(data.encode('utf-8'), (NTP_SERVER, PORT))
    data, address = client.recvfrom(1024)
    if data: print('Response received from:', address)
    t = struct.unpack('!12I', data)[10] - TIME1970
    print('\tTime = %s' % time.ctime(t))


if __name__ == '__main__':
    try:
        sntp_client()
    except PermissionError:
        print("Попробуйте запустить с использованием sudo")
