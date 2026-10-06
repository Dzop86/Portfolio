# API image. Build context: projects/ (lib-c is compiled from source here).
#   docker build -f fastapi/Dockerfile -t meshapi projects

# Stage 1: lib-c as a shared library, and the API as a wheel.
FROM python:3.13-slim AS build
RUN apt-get update && apt-get install -y --no-install-recommends gcc libc6-dev cmake make \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY lib-c ./lib-c
RUN cmake -S lib-c -B /build/lib-c -DBUILD_SHARED_LIBS=ON -DMESH_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=Release \
    && cmake --build /build/lib-c --parallel
COPY fastapi ./fastapi
RUN pip wheel --no-cache-dir --no-deps -w /wheels ./fastapi

# Stage 2: runtime only, no compiler, unprivileged user.
FROM python:3.13-slim
RUN useradd --system --no-create-home --uid 10001 meshapi
COPY --from=build /wheels /wheels
RUN pip install --no-cache-dir /wheels/*.whl && rm -rf /wheels
COPY --from=build /build/lib-c/libmesh.so /usr/local/lib/libmesh.so
ENV MESHLIB_PATH=/usr/local/lib/libmesh.so PYTHONUNBUFFERED=1
USER meshapi
EXPOSE 8000
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=2)"
CMD ["uvicorn", "meshapi.app:app", "--host", "0.0.0.0", "--port", "8000"]
