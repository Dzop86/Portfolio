# Décisions, fastapi

## F1. lib-c par ctypes, pas d'extension compilée
**Choix :** lib-c est construite en bibliothèque partagée (`BUILD_SHARED_LIBS=ON`, symboles exportés sous Windows par `CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS`) et chargée par `ctypes` depuis le chemin `MESHLIB_PATH`.
**Pourquoi :** un seul lecteur (testé, fuzzé) pour le C, le navigateur et Python ; pas d'extension à recompiler pour chaque version de Python.
**Alternatives :** cffi, nanobind ou pybind11 (via la bibliothèque C++ de topologie), réécriture en Python.
**Limite :** les structures C sont recopiées à la main dans `libmesh.py` ; un changement de `mesh.h` doit y être reporté (les tests le détecteraient).

## F2. Fichier en corps brut, appel C hors de la boucle d'événements
**Choix :** `POST /v1/mesh/stats` lit le corps brut (`curl --data-binary`), en flux, et refuse au-delà de 32 Mio ; l'appel à lib-c passe par `run_in_threadpool`.
**Pourquoi :** pas de dépendance `python-multipart` ; un gros maillage (1 s) ne bloque pas les autres requêtes.
**Limite :** pas de formulaire HTML d'envoi (multipart) pour l'instant.
