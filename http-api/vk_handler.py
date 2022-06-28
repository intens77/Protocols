from os import path, mkdir
from pathlib import Path
from typing import List, Tuple, Dict
from tqdm import tqdm

import requests

from config import VK_CONFIG


class VkHandler:
    def __init__(self):
        self.__domain = VK_CONFIG['domain']
        self.__access_token = VK_CONFIG['access_token']
        self.__version = VK_CONFIG['version']

    def get_photos(self, post_id: str):
        description = self.__make_request_to_vk_API(method="wall.getById", post_id=post_id)['response'][0]
        photos = self.__extract_photos(description)
        downloads_path = path.join(str(Path.home() / "Downloads"), post_id)
        mkdir(downloads_path)

        for i in tqdm(range(len(photos)), ncols=100):
            photo_link = photos[i]
            self.__save_img_by_link(link=photo_link, path=path.join(downloads_path, f"photo{i}.jpg"))

    @staticmethod
    def __try_handle_an_exception(description):
        if 'error' in description:
            raise Exception(description['error']['error_msg'])

    @staticmethod
    def __extract_photos(description):
        return list(
            map(
                lambda p: p['photo']['sizes'][-1]['url'],
                filter(lambda a: a['type'] == 'photo', description['attachments'])
            )
        )

    def __make_request_to_vk_API(self, method: str, post_id: str) -> Dict:
        url = f'{self.__domain}/{method}?' \
              f'access_token={self.__access_token}' \
              f'&posts={post_id}' \
              f'&v={self.__version}'
        response = requests.get(url)
        description = response.json()
        self.__try_handle_an_exception(description)
        return description

    @staticmethod
    def __save_img_by_link(link: str, path: str) -> None:
        p = requests.get(link)
        out = open(path, "wb")
        out.write(p.content)
        out.close()

    @staticmethod
    def __display_information(information: List[Tuple[str, str]], id_indent=20):
        for e in information:
            print(f'Id: {e[0]:<{id_indent}} Name: {e[1]}')


if __name__ == '__main__':
    pass
