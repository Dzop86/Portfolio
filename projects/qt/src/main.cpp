// Mesh viewer: qtviewer [--lang fr|en] [--curvature] [--screenshot FILE --size WxH] [FILE | sample:NAME]
#include "mainwindow.h"
#include "renderer.h"
#include "viewerwidget.h"

#include <QApplication>
#include <QCommandLineParser>
#include <QSurfaceFormat>
#include <QTimer>

int main(int argc, char* argv[])
{
    // Before the application exists: every OpenGL widget then asks for a 3.3 core context.
    QSurfaceFormat::setDefaultFormat(Renderer::surfaceFormat());
    QApplication app(argc, argv);
    QApplication::setApplicationName(QStringLiteral("qtviewer"));
    QApplication::setApplicationVersion(QStringLiteral("0.1.0"));

    QCommandLineParser parser;
    parser.addHelpOption();
    parser.addVersionOption();
    const QCommandLineOption lang(QStringLiteral("lang"), QStringLiteral("Language: fr or en (default: the system's)."), QStringLiteral("code"));
    const QCommandLineOption curvature(QStringLiteral("curvature"), QStringLiteral("Colour the faces by Gaussian curvature."));
    const QCommandLineOption screenshot(QStringLiteral("screenshot"), QStringLiteral("Save the window to FILE and quit."), QStringLiteral("file"));
    const QCommandLineOption size(QStringLiteral("size"), QStringLiteral("Window size, WIDTHxHEIGHT."), QStringLiteral("size"), QStringLiteral("1280x720"));
    parser.addOptions({lang, curvature, screenshot, size});
    parser.addPositionalArgument(QStringLiteral("mesh"), QStringLiteral("An OBJ, PLY or STL file, or sample:torus, sample:sphere, sample:mobius, sample:saddle."));
    parser.process(app);

    MainWindow window;
    if (parser.isSet(lang))
        window.setLanguage(parser.value(lang));
    const QStringList files = parser.positionalArguments();
    const bool batch = parser.isSet(screenshot);
    window.setInteractive(!batch);
    if (!files.isEmpty()) {
        const QString& mesh = files.first();
        const bool ok = mesh.startsWith(QStringLiteral("sample:")) ? window.openSample(mesh.mid(7)) : window.open(mesh);
        if (!ok && batch) {
            qCritical("%s", qPrintable(window.lastError()));
            return 1;
        }
    }
    if (parser.isSet(curvature)) {
        RenderOptions options = window.viewer()->options();
        options.curvature = true;
        window.viewer()->setOptions(options);
    }
    const QStringList dimensions = parser.value(size).split(QLatin1Char('x'));
    if (dimensions.size() == 2)
        window.resize(dimensions[0].toInt(), dimensions[1].toInt());
    window.show();

    if (batch) {
        // A few frames for the window to be laid out and painted, then the picture.
        QTimer::singleShot(800, &window, [&] {
            const bool saved = window.grab().save(parser.value(screenshot));
            QCoreApplication::exit(saved && window.lastError().isEmpty() ? 0 : 1);
        });
    }
    return QApplication::exec();
}
