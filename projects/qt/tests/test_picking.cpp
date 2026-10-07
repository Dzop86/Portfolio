// Picking: rays through pixels, the nearest triangle crossed, vertex or face under the cursor.
#include "picking.h"

#include <QFile>
#include <QTest>

#include <cmath>
#include <numeric>

namespace {

const QSize kViewport(400, 300);

MeshModel cube() { return MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/cube.obj")); }

MeshModel sample(const char* name)
{
    QFile file(QStringLiteral(":/samples/%1.obj").arg(QLatin1String(name)));
    if (!file.open(QIODevice::ReadOnly))
        qFatal("missing sample %s", name);
    return MeshModel::fromData(file.readAll());
}

}  // namespace

class TestPicking : public QObject {
    Q_OBJECT

private slots:
    void theCentralRayGoesFromTheEyeToTheTarget()
    {
        Camera camera;
        camera.fit(cube().bounds());
        const Ray ray = picking::rayThrough(camera, QPointF(200, 150), kViewport);
        const QVector3D towards = (camera.target() - camera.eye()).normalized();
        QVERIFY(QVector3D::dotProduct(ray.direction, towards) > 0.9999f);
        QVERIFY(std::abs(ray.direction.length() - 1) < 1e-5f);
        // A pixel to the right gives a ray leaning to the right of the view.
        const Ray right = picking::rayThrough(camera, QPointF(390, 150), kViewport);
        QVERIFY(picking::project(camera, right.origin + 10 * right.direction, kViewport).x() > 380);
    }

    void projectAndRayThroughAgree()
    {
        Camera camera;
        camera.fit(cube().bounds());
        camera.orbit(20, -10);
        const QPointF pixel = picking::project(camera, QVector3D(1, 1, 1), kViewport);
        const Ray ray = picking::rayThrough(camera, pixel, kViewport);
        const QVector3D toCorner = (QVector3D(1, 1, 1) - ray.origin).normalized();
        QVERIFY(QVector3D::dotProduct(toCorner, ray.direction) > 0.99999f);
    }

    void aRayHitsTheNearestFace()
    {
        const MeshModel model = cube();
        // From above the cube, straight down: the top face (y = 1) at distance 4.
        const std::optional<Hit> hit = picking::intersect(model, {{0.5f, 5, 0.5f}, {0, -1, 0}});
        QVERIFY(hit.has_value());
        QVERIFY(std::abs(hit->distance - 4) < 1e-5f);
        QVERIFY(std::abs(hit->point.y() - 1) < 1e-5f);
        QVERIFY(std::abs(model.faceNormal(hit->face).y()) > 0.999f);
        QVERIFY(!picking::intersect(model, {{3, 5, 3}, {0, -1, 0}}).has_value());
        QVERIFY(!picking::intersect(model, {{0.5f, 5, 0.5f}, {0, 1, 0}}).has_value()); // behind the origin
    }

    void aClickNearAVertexSelectsIt_elsewhereTheFace()
    {
        const MeshModel model = sample("sphere");
        Camera camera;
        camera.fit(model.bounds());
        const Selection middle = picking::select(model, camera, QPointF(200, 150), kViewport);
        QVERIFY(middle.kind != Selection::Kind::None);
        // Exactly on the projection of one of the face's vertices: that vertex.
        const std::optional<Hit> hit = picking::intersect(model, picking::rayThrough(camera, QPointF(200, 150), kViewport));
        const uint32_t corner = model.mesh().triangles()[hit->face][0];
        const QPointF onVertex = picking::project(camera, model.positions()[corner], kViewport);
        QCOMPARE(picking::select(model, camera, onVertex, kViewport), (Selection{Selection::Kind::Vertex, corner}));
        // At the face's centre, far from its corners with a small radius: the face.
        QVector3D centre;
        for (uint32_t v : model.mesh().triangles()[hit->face])
            centre += model.positions()[v] / 3;
        QCOMPARE(picking::select(model, camera, picking::project(camera, centre, kViewport), kViewport, 1.0),
                 (Selection{Selection::Kind::Face, hit->face}));
        // Outside the sphere: nothing.
        QCOMPARE(picking::select(model, camera, QPointF(2, 2), kViewport).kind, Selection::Kind::None);
    }

    void localPropertiesAreConsistent()
    {
        const MeshModel model = cube();
        const auto& valence = model.valence();
        QCOMPARE(std::accumulate(valence.begin(), valence.end(), 0u), static_cast<uint32_t>(2 * model.edges().size()));
        double area = 0;
        for (uint32_t f = 0; f < model.mesh().face_count(); ++f)
            area += model.faceArea(f);
        QVERIFY(std::abs(area - 6) < 1e-9); // a unit cube
        // A regular torus grid: every vertex has six neighbours.
        const MeshModel torus = sample("torus");
        for (uint32_t v : torus.valence())
            QCOMPARE(v, 6u);
    }
};

QTEST_GUILESS_MAIN(TestPicking)
#include "test_picking.moc"
