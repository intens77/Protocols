import sys

import app

if __name__ == '__main__':
    try:
        app.run()
    except PermissionError:
        print("Попробуйте запустить с использованием sudo")
        sys.exit(7)
