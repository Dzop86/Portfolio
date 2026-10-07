// Camera: framing, orbit, zoom and pan, checked with the projection matrices.
#include "camera.h"

#include <QTest>

#include <cmath>

namespace {

Bounds box() { return {{-1, -2, -3}, {3, 2, 1}}; }

// Normalised device coordinates of a world point.
QVector3D ndc(const Camera& camera, const QVector3D& p, float aspect = 1.0f)
{
    return (camera.projection(aspect) * camera.view()).map(p);
}

bool close(float a, float b, float tolerance = 1e-4f) { return std::abs(a - b) <= tolerance; }

}  // namespace

class TestCamera : public QObject {
    Q_OBJECT

private slots:
    void fitCentresTheMeshAndShowsAllOfIt()
    {
        Camera camera;
        camera.fit(box());
        QCOMPARE(camera.target(), QVector3D(1, 0, -1));
        const QVector3D centre = ndc(camera, box().center());
        QVERIFY(close(centre.x(), 0) && close(centre.y(), 0));
        // Every corner of the box lies inside the screen and between the near and far planes.
        for (float x : {-1.0f, 3.0f})
            for (float y : {-2.0f, 2.0f})
                for (float z : {-3.0f, 1.0f}) {
                    const QVector3D p = ndc(camera, {x, y, z});
                    QVERIFY2(std::abs(p.x()) < 1 && std::abs(p.y()) < 1 && std::abs(p.z()) < 1, "corner off screen");
                }
    }

    void orbitKeepsTheDistanceAndClampsThePitch()
    {
        Camera camera;
        camera.fit(box());
        const float distance = camera.distance();
        camera.orbit(123, 40);
        QVERIFY(close((camera.eye() - camera.target()).length(), distance));
        camera.orbit(0, 500);
        QCOMPARE(camera.pitch(), Camera::kMaxPitch);
        camera.orbit(0, -1000);
        QCOMPARE(camera.pitch(), -Camera::kMaxPitch);
        // The target stays in the middle of the screen whatever the angle.
        const QVector3D centre = ndc(camera, camera.target());
        QVERIFY(close(centre.x(), 0) && close(centre.y(), 0));
    }

    void zoomStaysWithinItsLimits()
    {
        Camera camera;
        camera.fit(box());
        const float radius = box().radius();
        camera.zoom(0.5f);
        QVERIFY(camera.distance() < 2 * radius / std::sin(qDegreesToRadians(Camera::kFieldOfView / 2)));
        for (int i = 0; i < 100; ++i)
            camera.zoom(0.5f);
        QVERIFY(close(camera.distance(), radius / 20));
        for (int i = 0; i < 100; ++i)
            camera.zoom(2.0f);
        QVERIFY(close(camera.distance(), radius * 50, 1e-3f));
    }

    void panMovesTheTargetAcrossTheView()
    {
        Camera camera;
        camera.fit(box());
        const QVector3D before = camera.target();
        const QVector3D forward = (camera.target() - camera.eye()).normalized();
        camera.pan(0.1f, 0);
        const QVector3D moved = camera.target() - before;
        QVERIFY(moved.length() > 0);
        QVERIFY(close(QVector3D::dotProduct(moved.normalized(), forward), 0));
        // Dragging to the right moves the mesh right: the old target now lies right of the centre.
        QVERIFY(ndc(camera, before).x() > 0);
    }
};

QTEST_GUILESS_MAIN(TestCamera)
#include "test_camera.moc"
