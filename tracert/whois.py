import socket


def whois(ip):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(("whois.arin.net", 43))
    sock.send((ip + "\r\n").encode())
    resp = get_response_from_socket(sock)
    sock.close()
    data = extract_data(resp, ['NetName', 'OriginAS', 'Country'])

    if 'PRIVATE-ADDRESS' in data['NetName'].upper():
        data = {'NetName': 'local', 'OriginAS': '', 'Country': ''}

    if data['Country'] == 'EU':
        data['Country'] = 'BE'

    if 'AS' in data['OriginAS'].upper():
        data['OriginAS'] = data['OriginAS'][2:]

    return data


def get_response_from_socket(sock):
    resp = b""
    while True:
        d = sock.recv(4096)
        resp += d
        if not d:
            break
    resp = resp.decode()
    return resp


def extract_data(inp_data, fields):
    data = inp_data.split('\n')
    data = list(filter(lambda line: line and line[0] != '#', data))
    data = list(map(lambda line: tuple(line.split(maxsplit=1)), data))
    out_data = {}
    for field in fields:
        out_data[field] = ''
    for el in data:
        if el[0][:-1] in out_data.keys() and len(el) > 1:
            out_data[el[0][:-1]] = el[1]

    return out_data
