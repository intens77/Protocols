# portscan
Автор: Кириенко Владислав

### Описание
Многопоточный сканер TCP-портов удалённого компьютера. 
Возможность определения прикладных протоколов (SMTP/POP3/IMAP/HTTP).

### Требования
* Python 3

### Состав
* Консольная версия: `portscan.py`
* Модули: `arguments_parser.py`, `scanner.py`

### Консольная версия
Справка по запуску: `./portscan.py --help` `./portscan.py -h`

Пример запуска: `./portscan.py -t 127.0.0.1`, `./portscan.py -t --ports 8000 13000 localhost`
