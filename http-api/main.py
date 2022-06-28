import argparse

from vk_handler import VkHandler

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument("post_id", help="Vk post / wall id, example: wall423207183_1341", type=str)
    args = parser.parse_args()

    vk_handler = VkHandler()
    post_id = args.post_id[4:]
    try:
        vk_handler.get_photos(post_id=post_id)
    except Exception as e:
        print(str(e))
