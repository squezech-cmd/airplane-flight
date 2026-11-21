#!/usr/bin/env python3
# make_setup.py — сборка self-extracting установщика «под ключ» (Windows).
# Склеивает: 7zSD.sfx (модуль 7-Zip SFX) + UTF-8 конфиг + 7z-архив → один Setup.exe,
# который при запуске сам распаковывается в %LOCALAPPDATA%\DearPassengers, ставит
# ярлыки и запускает игру. Работает локально и в CI (нужен python3 + py7zr).
#
# Использование:
#   python3 installer/make_setup.py --mode game   --payload builds --out DearPassengers_Setup.exe
#     (payload = папка СОБРАННОЙ игры: DearPassengers.exe + DearPassengers_Data)
#   python3 installer/make_setup.py --mode source --payload <папка с исходниками> --out DearPassengers_Setup.exe
#     (payload = исходники проекта + Install.bat; игра соберётся на машине пользователя)

import argparse
import io
import os
import sys

try:
    import py7zr
except ImportError:
    print("Ошибка: нужен py7zr (pip install py7zr)", file=sys.stderr)
    sys.exit(2)

HERE = os.path.dirname(os.path.abspath(__file__))
SFX = os.path.join(HERE, "7zSD.sfx")
INSTALL_TITLE = "Dear Passengers"


def config_text(mode: str) -> str:
    """Конфигурация 7zSD-модуля."""
    if mode == "game":
        # Игра уже собрана: распаковать и запустить.
        return (
            ";!@Install@!UTF-8!\n"
            f'Title="{INSTALL_TITLE}"\n'
            'BeginPrompt="Установить и запустить Dear Passengers? '
            '(18+ | Создана в развлекательных целях. Не повторяйте в реальной жизни.)"\n'
            'RunProgram="GameSetup.bat"\n'
            'InstallPath="%LOCALAPPDATA%\\\\DearPassengers"\n'
            'ExtractDialogText="Распаковка Dear Passengers…"\n'
            'GUIMode="2"\n'
            ";!@InstallEnd@!\n"
        )
    # Исходники: распаковать и собрать силами Unity на машине игрока.
    return (
        ";!@Install@!UTF-8!\n"
        f'Title="{INSTALL_TITLE} — установка"\n'
        'BeginPrompt="Установить Dear Passengers (18+)?\n'
        "Файлы будут распакованы, при необходимости поставится Unity 2022.3 (нужен один вход в Unity ID),\\n"
        'затем игра соберётся и запустится."\n'
        'RunProgram="Install.bat"\n'
        'InstallPath="%LOCALAPPDATA%\\\\DearPassengers"\n'
        'ExtractDialogText="Распаковка Dear Passengers…"\n'
        'GUIMode="2"\n'
        ";!@InstallEnd@!\n"
    )


def make_7z(payload_dir: str) -> bytes:
    """Упаковать содержимое папки в 7z (LZMA — для совместимости со старыми SFX)."""
    buf = io.BytesIO()
    try:
        filters = [{"id": py7zr.FILTER_LZMA, "preset": 7}]
        archive = py7zr.SevenZipFile(buf, "w", filters=filters)
    except Exception:
        archive = py7zr.SevenZipFile(buf, "w")
    with archive as z:
        for name in sorted(os.listdir(payload_dir)):
            path = os.path.join(payload_dir, name)
            if os.path.isdir(path):
                z.writeall(path, name)
            else:
                z.write(path, name)
    return buf.getvalue()


def verify(exe_path: str, sfx_len: int, cfg_len: int) -> list:
    """Проверка: после заглушки и конфига лежит читаемый 7z-архив. Возвращает имена файлов."""
    import tempfile
    import shutil
    with open(exe_path, "rb") as f:
        f.seek(sfx_len + cfg_len)
        data = f.read()
    tmp = tempfile.mkdtemp(prefix="dp_verify_")
    try:
        with py7zr.SevenZipFile(io.BytesIO(data), "r") as z:
            names = z.getnames()
            z.extractall(path=tmp)  # тест полной распаковки
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    return names


def main():
    ap = argparse.ArgumentParser(description="Сборка DearPassengers_Setup.exe")
    ap.add_argument("--mode", choices=["game", "source"], required=True)
    ap.add_argument("--payload", required=True, help="папка с содержимым установщика")
    ap.add_argument("--out", required=True, help="выходной .exe")
    args = ap.parse_args()

    if not os.path.isfile(SFX):
        print(f"Ошибка: не найден {SFX}", file=sys.stderr)
        sys.exit(2)

    cfg = config_text(args.mode).encode("utf-8")
    payload = make_7z(args.payload)

    with open(SFX, "rb") as f:
        sfx = f.read()

    with open(args.out, "wb") as f:
        f.write(sfx)
        f.write(cfg)
        f.write(payload)

    names = verify(args.out, len(sfx), len(cfg))
    size_mb = os.path.getsize(args.out) / 1024 / 1024
    print(f"OK: {args.out} ({size_mb:.2f} МБ, {len(names)} файлов/папок в архиве)")
    for n in names[:5]:
        print("   •", n)
    if len(names) > 5:
        print(f"   … и ещё {len(names) - 5}")


if __name__ == "__main__":
    main()
