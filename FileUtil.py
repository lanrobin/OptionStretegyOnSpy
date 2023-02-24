from pathlib import Path
import os

def EnsurePathExists(path):
    Path(path).mkdir(parents=True, exist_ok=True)

def FolderExistsAndEmpty(path):
    # Getting the list of directories
    if(os.DirEntry.is_dir(path)):
        dir = os.scandir(path)
        return len(dir) == 0
    return False

def remove_prefix(text, prefix):
    if text.startswith(prefix):
        return text[len(prefix):]
    return text  # or whatever