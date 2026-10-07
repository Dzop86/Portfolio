// MeshModel: what the viewer derives from lib-c's reading and topologie's analysis.
#include "meshmodel.h"

#include <QFile>
#include <QTemporaryFile>
#include <QTest>

#include <cmath>

class TestMeshModel : public QObject {
    Q_OBJECT

private:
    static MeshModel sample(const char* name)
    {
        QFile file(QStringLiteral(":/samples/%1.obj").arg(QLatin1String(name)));
        if (!file.open(QIODevice::ReadOnly))
            qFatal("missing sample %s", name);
        return MeshModel::fromData(file.readAll());
    }

private slots:
    void cubeFromLibC()
    {
        const MeshModel cube = MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/cube.obj"));
        QCOMPARE(cube.positions().size(), 8u);
        QCOMPARE(cube.mesh().face_count(), 12u);
        QCOMPARE(cube.edges().size(), 18u);
        QVERIFY(cube.boundaryEdges().empty());
        QCOMPARE(cube.invariants().euler_characteristic, 2);
        QCOMPARE(cube.invariants().genus.value(), 0);
        QCOMPARE(cube.bounds().center(), QVector3D(0.5f, 0.5f, 0.5f));
        QVERIFY(std::abs(cube.bounds().radius() - std::sqrt(3.0f) / 2) < 1e-6f);
    }

    void stlAndPlyToo()
    {
        QCOMPARE(MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/cube.stl")).invariants().euler_characteristic, 2);
        QCOMPARE(MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/tetrahedron.ply")).edges().size(), 6u);
    }

    void normalsAreUnitAndPointOutOfAConvexMesh()
    {
        const MeshModel cube = MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/cube.obj"));
        for (std::size_t v = 0; v < cube.positions().size(); ++v) {
            const QVector3D n = cube.normals()[v];
            QVERIFY(std::abs(n.length() - 1) < 1e-5f);
            QVERIFY(QVector3D::dotProduct(n, cube.positions()[v] - cube.bounds().center()) > 0);
        }
    }

    void samplesHaveTheirKnownTopology()
    {
        const MeshModel torus = sample("torus");
        QCOMPARE(torus.invariants().genus.value(), 1);
        QVERIFY(std::abs(torus.gaussBonnetRatio()) < 1e-9);

        const MeshModel sphere = sample("sphere");
        QCOMPARE(sphere.invariants().euler_characteristic, 2);
        QVERIFY(std::abs(sphere.gaussBonnetRatio() - 2) < 1e-9);

        const MeshModel mobius = sample("mobius");
        QVERIFY(!mobius.invariants().orientable);
        QCOMPARE(mobius.invariants().boundary_loops, 1u);
        QVERIFY(!mobius.boundaryEdges().empty());
        QVERIFY(!mobius.invariants().genus.has_value());
    }

    void mobiusNormalsDoNotCancelAtTheTwist()
    {
        // Opposite face normals used to cancel out across the twist, leaving a dark seam.
        const MeshModel mobius = sample("mobius");
        for (const QVector3D& n : mobius.normals())
            QVERIFY(std::abs(n.length() - 1) < 1e-4f);
    }

    void nonManifoldEdgesAreListed()
    {
        // Three triangles around the edge 0-1: a "book" with three pages.
        const MeshModel book(topo::Mesh({{0, 0, 0}, {0, 0, 1}, {1, 0, 0}, {0, 1, 0}, {-1, 0, 0}}, {{0, 1, 2}, {0, 1, 3}, {0, 1, 4}}));
        QCOMPARE(book.nonManifoldEdges().size(), 1u);
        QCOMPARE(book.nonManifoldEdges().front(), (Edge{0, 1}));
        QVERIFY(!book.invariants().manifold);
        QCOMPARE(book.boundaryEdges().size(), 6u);
    }

    void curvatureColoursRunFromBlueToRed()
    {
        const QColor blue = MeshModel::curvatureColor(-2, 2);
        const QColor flat = MeshModel::curvatureColor(0, 2);
        const QColor red = MeshModel::curvatureColor(2, 2);
        QVERIFY(blue.blue() > blue.red());
        QVERIFY(red.red() > red.blue());
        QVERIFY(flat.red() > 230 && flat.green() > 230 && flat.blue() > 230);
        QCOMPARE(MeshModel::curvatureColor(50, 2), red);   // clamped
        QCOMPARE(MeshModel::curvatureColor(1, 0), flat);   // no scale: everything flat
    }

    void curvatureScaleIgnoresTheFewSharpestVertices()
    {
        const MeshModel saddle = sample("saddle");
        double largest = 0;
        for (std::size_t v = 0; v < saddle.curvature().gaussian.size(); ++v)
            if (!saddle.curvature().boundary[v])
                largest = std::max(largest, std::abs(saddle.curvature().gaussian[v]));
        QVERIFY(saddle.curvatureScale() > 0);
        QVERIFY(saddle.curvatureScale() <= largest);
    }

    void readingErrorsKeepTheirLine()
    {
        QTemporaryFile file;
        QVERIFY(file.open());
        file.write("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 7\n");
        file.close();
        try {
            (void)MeshModel::fromFile(file.fileName());
            QFAIL("no error");
        } catch (const topo::LoadError& e) {
            QCOMPARE(e.line(), 4u);
        }
        QVERIFY_THROWS_EXCEPTION(topo::LoadError, (void)MeshModel::fromFile(QStringLiteral("no/such/file.obj")));
    }
};

QTEST_GUILESS_MAIN(TestMeshModel)
#include "test_meshmodel.moc"
