"""Integration tests: the HTTP API over lib-c, through FastAPI's test client."""
from fastapi.testclient import TestClient

from meshapi.app import MAX_BYTES, app

client = TestClient(app)


def test_health_reports_the_library():
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json() == {"status": "ok", "libmesh": "loaded"}


def test_stats_of_an_uploaded_cube(sample):
    r = client.post("/v1/mesh/stats", content=sample("cube.obj"), headers={"content-type": "application/octet-stream"})
    assert r.status_code == 200
    body = r.json()
    assert body["format"] == "OBJ"
    assert (body["vertices"], body["triangles"], body["edges"], body["euler_characteristic"]) == (8, 12, 18, 2)
    assert body["bbox"] == {"min": [0.0, 0.0, 0.0], "max": [1.0, 1.0, 1.0]}


def test_stats_of_stl_and_ply(sample):
    assert client.post("/v1/mesh/stats", content=sample("cube.stl")).json()["format"] == "STL"
    assert client.post("/v1/mesh/stats", content=sample("tetrahedron.ply")).json()["vertices"] == 4


def test_a_broken_mesh_gives_422_with_its_line():
    r = client.post("/v1/mesh/stats", content=b"v 0 0 0\nv 1 0 0\nf 1 2 3\n")
    assert r.status_code == 422
    assert r.json() == {"detail": {"status": "index out of range", "line": 3}}


def test_a_file_over_the_limit_gives_413():
    r = client.post("/v1/mesh/stats", content=b"#" * (MAX_BYTES + 1))
    assert r.status_code == 413


def test_the_openapi_schema_documents_the_endpoint():
    schema = client.get("/openapi.json").json()
    assert "/v1/mesh/stats" in schema["paths"]
    assert "MeshStats" in schema["components"]["schemas"]
