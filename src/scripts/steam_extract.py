import os
import re
import logging
import subprocess
from pathlib import Path

# Logging einrichten
def setup_logging():
    log_dir = Path(__file__).resolve().parent / "../../log"
    log_dir.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s - %(levelname)s - %(message)s",
        handlers=[
            logging.FileHandler(log_dir / "steam_extract.log", encoding="utf-8"),
            logging.StreamHandler()
        ]
    )

# Workshop-IDs auslesen
def read_workshop_ids(file_path):
    if not os.path.exists(file_path):
        logging.error(f"Datei '{file_path}' nicht gefunden.")
        return []
    with open(file_path, "r", encoding="utf-8") as file:
        content = file.read().strip()

    match = re.search(r"WorkshopItems\s*=\s*([\d\s;]+)", content)
    if match:
        return [id.strip() for id in match.group(1).split(";") if id.strip()]
    
    logging.error("Keine Workshop-IDs im angegebenen Format gefunden.")
    return []

# Überprüfen, ob Mod bereits heruntergeladen wurde
def is_mod_downloaded(workshop_id, workshop_dir):
    mod_path = Path(workshop_dir) / workshop_id
    if mod_path.exists() and any(mod_path.iterdir()):
        logging.info(f"Mod für Workshop-ID {workshop_id} bereits heruntergeladen. Überspringe Download.")
        return True
    return False

# SteamCMD ausführen
def download_with_steamcmd(workshop_id, steamcmd_path, output_dir):
    try:
        logging.info(f"Starte Download für Workshop-ID: {workshop_id}")
        result = subprocess.run(
            [
                steamcmd_path,
                "+login", "anonymous",
                "+@nCSClientRateLimitKbps", "50000",
                "+force_install_dir", output_dir,
                "+workshop_download_item", "108600", workshop_id,
                "validate",
                "+quit"
            ],
            cwd=output_dir,
            capture_output=True,
            text=True
        )
        
        if result.returncode != 0:
            logging.error(f"Fehler beim Download der Workshop-ID {workshop_id}: {result.stderr}")
            return False
        
        logging.info(f"Download abgeschlossen für Workshop-ID: {workshop_id}")
        return True
    except Exception as e:
        logging.error(f"Fehler beim Ausführen von SteamCMD: {e}")
        return False

# Mod-IDs aus info.txt extrahieren
def extract_mod_ids(workshop_id, workshop_dir):
    mod_info_path = Path(workshop_dir) / workshop_id / "mods"
    if not mod_info_path.exists():
        logging.warning(f"Mod-Ordner für Workshop-ID {workshop_id} nicht gefunden.")
        return []

    mod_ids = []
    for root, _, files in os.walk(mod_info_path):
        for file in files:
            if file.lower() == "mod.info":
                with open(os.path.join(root, file), "r", encoding="utf-8") as f:
                    content = f.read()
                match = re.search(r"id=([\w_]+)", content)
                if match:
                    mod_ids.append(match.group(1))

    if not mod_ids:
        logging.warning(f"Keine gültigen Mod-IDs in mod.info für Workshop-ID {workshop_id} gefunden.")
    return mod_ids

# Ergebnisse speichern
def save_results(file_path, results):
    with open(file_path, "w", encoding="utf-8") as file:
        file.write("\n".join(results))
    logging.info(f"Ergebnisse gespeichert in '{file_path}'.")


def save_mod_ids(file_path, mod_ids):
    # Dateiinhalt sicher überschreiben
    with open(file_path, "w", encoding="utf-8") as file:
        # `Mods=` nur einmal hinzufügen
        file.write(f"Mods={';'.join(mod_ids)}")
    logging.info(f"Mod-IDs gespeichert in '{file_path}'.")

# Hauptfunktion
def main():
    setup_logging()

    # Pfade relativ zum Skript
    base_path = Path(__file__).resolve().parent.parent
    workshop_file = base_path / "../Saves_and_Output/WorkshopID.txt"
    steamcmd_path = base_path / "steamcmd/steamcmd.exe"
    output_dir = base_path / "../Workshop_Files/"
    workshop_dir = output_dir / "steamapps/workshop/content/108600"  # Verzeichnis für Mods
    aufstellung_file = base_path / "../Saves_and_Output/Aufstellung_deepsearch.txt"
    modid_file = base_path / "../Saves_and_Output/ModID_deepsearch.txt"

    # Debugging: Pfade ausgeben
    logging.info(f"WorkshopID.txt Pfad: {workshop_file}")
    logging.info(f"SteamCMD Pfad: {steamcmd_path}")
    logging.info(f"Output-Verzeichnis: {output_dir}")

    # Workshop-IDs auslesen
    workshop_ids = read_workshop_ids(workshop_file)
    if not workshop_ids:
        logging.error("Keine Workshop-IDs gefunden. Beende.")
        return

    results_aufstellung = []
    all_mod_ids = []

    # Für jede Workshop-ID den Prozess durchführen
    for workshop_id in workshop_ids:
        if not workshop_id.strip():
            continue

        # Überprüfen, ob Mod bereits heruntergeladen wurde
        if is_mod_downloaded(workshop_id, workshop_dir):
            continue

        # Mod herunterladen
        if not download_with_steamcmd(str(workshop_id), str(steamcmd_path), str(output_dir)):
            continue

    # Mod-IDs extrahieren
    for workshop_id in workshop_ids:
        mod_ids = extract_mod_ids(str(workshop_id), str(workshop_dir))
        if mod_ids:
            results_aufstellung.append(f"{workshop_id} :")
            results_aufstellung.extend([f"  {mod_id}" for mod_id in mod_ids])
            all_mod_ids.extend(mod_ids)
        else:
            results_aufstellung.append(f"{workshop_id} : Fehler beim Auslesen")

    # Ergebnisse speichern
    save_results(str(aufstellung_file), results_aufstellung)
    save_mod_ids(str(modid_file), all_mod_ids)

if __name__ == "__main__":
    main()
