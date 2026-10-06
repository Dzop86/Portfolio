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

## F3. Image multi-étapes, non root, contexte `projects/`
**Choix :** le contexte de build est `projects/` pour compiler lib-c depuis ses sources ; `Dockerfile.dockerignore` n'envoie que lib-c et fastapi. L'étape finale (`python:3.13-slim`) ne reçoit que la roue de l'API et `libmesh.so`, tourne sous un utilisateur dédié, et `compose.yaml` monte son système de fichiers en lecture seule.
**Pourquoi :** image sans compilateur (213 Mo) ; un défaut dans le lecteur C exposé sur le réseau ne donne ni droits root ni écriture disque.
**Limite :** les versions de FastAPI et uvicorn ne sont fixées que par des intervalles ; un fichier de verrouillage serait nécessaire pour des images reproductibles.

## F4. Topologie par la même API C que le navigateur, sous verrou
**Choix :** `/v1/mesh/topology` appelle `libtopoc` (API C de la bibliothèque C++) par ctypes ; un `threading.Lock` sérialise les appels, car l'API garde un état global.
**Alternatives :** API C à poignées (réentrante), pool de processus.
**Limite :** les calculs de topologie passent un par un ; suffisant pour une démonstration, à revoir pour une charge réelle.
