import requests
import re
import os
import logging
from datetime import datetime

def setup_logging():
    log_dir = "./log"
    os.makedirs(log_dir, exist_ok=True)
    
    log_file = os.path.join(log_dir, "extractor.log")
    if os.path.exists(log_file):
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        os.rename(log_file, f"{log_file}.{timestamp}")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file),
            logging.StreamHandler()
        ]
    )

def extract_mod_ids():
    workshop_file = "./Saves_and_Output/WorkshopID.txt"
    output_file = "./Saves_and_Output/Aufstellung.txt"
    mod_file = "./Saves_and_Output/ModID.txt"
    base_url = "https://steamcommunity.com/sharedfiles/filedetails/?id={}"

    if not os.path.exists(workshop_file):
        logging.error(f"Datei '{workshop_file}' nicht gefunden.")
        return

    with open(workshop_file, "r") as file:
        content = file.read()

    match = re.search(r"WorkshopItems=\s*([\d;]+)", content)
    if not match:
        logging.error("Keine Workshop-IDs im angegebenen Format gefunden.")
        return

    workshop_ids = match.group(1).split(";")

    with open(output_file, "w") as file:
        file.write("")

    with open(mod_file, "w") as file:
        file.write("")

    results = []

    for workshop_id in workshop_ids:
        if not workshop_id.strip():
            continue

        url = base_url.format(workshop_id)
        try:
            response = requests.get(url)
            response.raise_for_status()
            
            mod_id_match = re.search(r"Mod ID:\s*([\w-]+)", response.text)
            if mod_id_match:
                mod_id = mod_id_match.group(1)
                results.append(f"{workshop_id} : {mod_id}")
                logging.info(f"Mod-ID gefunden: {workshop_id} : {mod_id}")
            else:
                logging.warning(f"Keine Mod-ID für Workshop-ID {workshop_id} gefunden.")
        except requests.RequestException as e:
            logging.error(f"Fehler beim Abrufen der URL {url}: {e}")

    with open(output_file, "w") as file:
        file.write("\n".join(results))

    mod_ids = []
    if os.path.exists(output_file):
        with open(output_file, "r") as file:
            for line in file:
                parts = line.split(":")
                if len(parts) == 2:
                    mod_ids.append(parts[1].strip())

        with open(mod_file, "w") as file:
            file.write(f"Mods= {';'.join(mod_ids)}")

        logging.info(f"Mod-IDs erfolgreich in '{mod_file}' gespeichert.")
    else:
        logging.error(f"Datei '{output_file}' nicht gefunden, Mod-IDs konnten nicht geschrieben werden.")

if __name__ == "__main__":
    setup_logging()
    extract_mod_ids()
