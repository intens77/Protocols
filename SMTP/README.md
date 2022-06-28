# SMTP
Автор: Кириенко Владислав

### Описание
Скрипт, который отправляет получателю все картинки из указанного (или рабочего) каталога в качестве вложения.


### Требования
* Python 3

### Состав
* Консольная версия: `smtp.py`
* Модули: `arguments_parser.py`
* Перечень типов: `mime_types.json`

### Консольная версия
Справка по запуску: `./smtp.py --help` `./smtp.py -h`

Примеры запуска: 
* `./smtp.py --ssl --auth --verbose -s smtp.mail.ru:465 -t your-mail@gmail.com -f your-mail@mail.ru -d .\imgs`, 
* `./smtp.py --ssl --auth -s smtp.yandex.ru:465 -t your-mail@mail.ru -f your-mail@ya.ru --subject "Привет"`
