Автор: Кириенко Владислав
### Подготовка к запуску:
Заполнение конфиг-файла(config.py):
* 'app_id' - id вашего приложения. Узнать, как его получить, можно, например, [здесь](https://www.pandoge.com/socialnye-seti-i-messendzhery/poluchenie-klyucha-dostupa-access_token-dlya-api-vkontakte).
* 'access_token' - токен доступа вашего приложения. Сделать это можно с помощью файла access_token.py. Инструкция:
   1) Запуск скрипта:
      ```
      python access_token.py
      ```
   2) Далее появится окно, в котором нужно со всем согласиться.
   3) Вас переадресует на страницу. В ссылке вам нужно скопировать значение заголовка "access_token", его вам и нужно записать в конфиг. 

Установка требований:
```
pip install -r requirements.txt
```

### Запуск: 
```
Шаблон: 
        python main.py post_id
Полная справка в help: 
        python main.py -h
```
