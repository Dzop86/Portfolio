// Renderer: draws into an off-screen framebuffer, then the picture is read back pixel by pixel.
#include "renderer.h"

#include <QFile>
#include <QOffscreenSurface>
#include <QOpenGLContext>
#include <QOpenGLFramebufferObject>
#include <QTest>

#include <functional>
#include <memory>

namespace {

MeshModel sample(const char* name)
{
    QFile file(QStringLiteral(":/samples/%1.obj").arg(QLatin1String(name)));
    if (!file.open(QIODevice::ReadOnly))
        qFatal("missing sample %s", name);
    return MeshModel::fromData(file.readAll());
}

bool near(QRgb pixel, const QColor& colour, int tolerance = 12)
{
    return std::abs(qRed(pixel) - colour.red()) <= tolerance && std::abs(qGreen(pixel) - colour.green()) <= tolerance
        && std::abs(qBlue(pixel) - colour.blue()) <= tolerance;
}

int count(const QImage& image, const std::function<bool(QRgb)>& wanted)
{
    int n = 0;
    for (int y = 0; y < image.height(); ++y)
        for (int x = 0; x < image.width(); ++x)
            n += wanted(image.pixel(x, y)) ? 1 : 0;
    return n;
}

}  // namespace

class TestRender : public QObject {
    Q_OBJECT

private:
    QOffscreenSurface surface_;
    QOpenGLContext context_;
    static constexpr int kSize = 200;

    QImage draw(const MeshModel* model, const RenderOptions& options)
    {
        Camera camera;
        if (model != nullptr)
            camera.fit(model->bounds());
        QOpenGLFramebufferObject fbo(kSize, kSize, QOpenGLFramebufferObject::CombinedDepthStencil);
        fbo.bind();
        Renderer renderer;
        if (!renderer.initialize())
            qFatal("%s", qPrintable(renderer.error()));
        renderer.upload(model);
        renderer.render(camera, options, kSize, kSize);
        QImage image = fbo.toImage();
        fbo.release();
        return image;
    }

private slots:
    void initTestCase()
    {
        context_.setFormat(Renderer::surfaceFormat());
        QVERIFY2(context_.create(), "no OpenGL context");
        surface_.setFormat(context_.format());
        surface_.create();
        QVERIFY(context_.makeCurrent(&surface_));
        const QSurfaceFormat format = context_.format();
        QVERIFY2(format.majorVersion() * 10 + format.minorVersion() >= 33, "OpenGL 3.3 needed");
        Renderer probe;
        QVERIFY(probe.initialize());
        qInfo("OpenGL %s", qPrintable(probe.glVersion()));
    }

    void anEmptyViewIsTheBackground()
    {
        const QImage image = draw(nullptr, {});
        QCOMPARE(count(image, [](QRgb p) { return !near(p, Renderer::kBackground, 0); }), 0);
    }

    void aCubeFillsTheMiddleAndNotTheCorners()
    {
        const MeshModel cube = MeshModel::fromFile(QStringLiteral(MESH_TEST_DATA "/cube.obj"));
        const QImage image = draw(&cube, {});
        QVERIFY(near(image.pixel(0, 0), Renderer::kBackground, 0));
        QVERIFY(near(image.pixel(kSize - 1, kSize - 1), Renderer::kBackground, 0));
        QVERIFY(!near(image.pixel(kSize / 2, kSize / 2), Renderer::kBackground));
        // Lit grey faces: brighter than the background, never brighter than the surface colour.
        const QRgb middle = image.pixel(kSize / 2, kSize / 2);
        QVERIFY(qGray(middle) > qGray(Renderer::kBackground.rgb()) && qGray(middle) <= qGray(Renderer::kSurface.rgb()) + 1);
    }

    void edgesAreDrawnOnlyWhenAsked()
    {
        const MeshModel torus = sample("torus");
        auto dark = [](QRgb p) { return near(p, Renderer::kWire, 8); };
        RenderOptions plain;
        plain.wireframe = false;
        const int without = count(draw(&torus, plain), dark);
        const int with = count(draw(&torus, {}), dark);
        QVERIFY2(with > without + 500, qPrintable(QStringLiteral("%1 dark pixels with edges, %2 without").arg(with).arg(without)));
    }

    void curvatureColoursShowDomesInRedAndSaddlesInBlue()
    {
        RenderOptions options;
        options.curvature = true;
        options.wireframe = false;
        auto redder = [](QRgb p) { return qRed(p) > qBlue(p) + 30; };
        auto bluer = [](QRgb p) { return qBlue(p) > qRed(p) + 30; };
        const MeshModel sphere = sample("sphere");
        const QImage dome = draw(&sphere, options);
        QVERIFY(count(dome, redder) > 1000);
        QCOMPARE(count(dome, bluer), 0);
        const MeshModel saddle = sample("saddle");
        const QImage pass = draw(&saddle, options);
        QVERIFY(count(pass, bluer) > count(pass, redder));
        // Without the option, the same saddle is grey.
        options.curvature = false;
        QCOMPARE(count(draw(&saddle, options), bluer), 0);
    }

    void theBoundaryIsHighlightedInPistachio()
    {
        const MeshModel mobius = sample("mobius");
        auto pistachio = [](QRgb p) { return near(p, Renderer::kBoundary, 20); };
        QVERIFY(count(draw(&mobius, {}), pistachio) > 100);
        RenderOptions options;
        options.highlight = false;
        QCOMPARE(count(draw(&mobius, options), pistachio), 0);
        // A closed surface has no boundary to highlight.
        const MeshModel sphere = sample("sphere");
        QCOMPARE(count(draw(&sphere, {}), pistachio), 0);
    }

    void cleanupTestCase() { context_.doneCurrent(); }
};

QTEST_MAIN(TestRender)
#include "test_render.moc"
