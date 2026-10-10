# The documentation assistant's API. Build context: the repository root, whose documentation it indexes.
#   docker build -f projects/rag/Dockerfile -t docrag .
FROM python:3.13-slim
RUN useradd --system --no-create-home --uid 10001 docrag
COPY projects/rag/pyproject.toml /src/pyproject.toml
COPY projects/rag/src /src/src
RUN pip install --no-cache-dir "/src[embed]" && rm -rf /src
# The embedding model, downloaded once at build time: the container needs no network to search.
ENV FASTEMBED_CACHE_PATH=/opt/fastembed PYTHONUNBUFFERED=1
RUN python -c "from docrag.embed import FastEmbedder; FastEmbedder()" && chmod -R a+rX /opt/fastembed
# The documentation: only the files the corpus reads (Dockerfile.dockerignore keeps nothing else).
COPY README.md PLAN.md DECISIONS.md /corpus/
COPY projects /corpus/projects
ENV DOCRAG_CORPUS=/corpus
USER docrag
EXPOSE 8000
HEALTHCHECK --interval=10s --timeout=3s --start-period=60s \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=2)"
CMD ["uvicorn", "docrag.api:app", "--host", "0.0.0.0", "--port", "8000"]
