import webbrowser
from config import VK_CONFIG


def get_access_token() -> None:
    assert VK_CONFIG['app_id'] != '', 'You need to register your app'
    url = f'https://oauth.vk.com/authorize?client_id={VK_CONFIG["app_id"]}' \
          f'&redirect_uri=https://oauth.vk.com/blank.hmtl&scope=wall' \
          f'&response_type=token' \
          f'&display=page'
    webbrowser.open_new_tab(url)


if __name__ == "__main__":
    get_access_token()
