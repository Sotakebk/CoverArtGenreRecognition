import os
import glob
from pathlib import Path
from PIL import Image as pimg
pimg.LOAD_TRUNCATED_IMAGES = True

source_path = 'downloadedCovers/'
target_path = 'resizedDownloadedCovers/'
files = glob.glob(source_path + '\*.*')

if not os.path.isdir(target_path):
    os.mkdir(target_path)

counter = 0
for file in files:
    try:
        im = pimg.open(file)
        resized = im.resize((150, 150))
        name = Path(file).stem
        resized.save(os.path.join(target_path, name + '.png'))
        counter = counter + 1
    except Exception as e:
        print(f'Failed to load file {file}, exception: {e}')

print(f'Processed {counter} images successfully')
