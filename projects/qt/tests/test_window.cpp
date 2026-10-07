// MainWindow: opening files and samples, the topology panel, errors, languages, drag and drop.
#include "mainwindow.h"
#include "renderer.h"
#include "viewerwidget.h"

#include <QAction>
#include <QLabel>
#include <QLocale>
#include <QMenuBar>
#include <QMimeData>
#include <QSurfaceFormat>
#include <QTemporaryDir>
#include <QTemporaryFile>
#include <QTest>
#include <QUrl>

class TestWindow : public QObject {
    Q_OBJECT

private:
    static QString value(const MainWindow& window, const QString& key)
    {
        auto* label = window.findChild<QLabel*>(QStringLiteral("value_") + key);
        return label != nullptr ? label->text() : QStringLiteral("<missing>");
    }

    static QStringList menuTitles(const MainWindow& window)
    {
        QStringList titles;
        for (QAction* action : window.menuBar()->actions())
            titles << action->text();
        return titles;
    }

private slots:
    void initTestCase() { QSurfaceFormat::setDefaultFormat(Renderer::surfaceFormat()); }

    void aSampleFillsThePanel()
    {
        MainWindow window;
        window.setInteractive(false);
        window.setLanguage(QStringLiteral("en"));
        QVERIFY(window.openSample(QStringLiteral("torus")));
        QCOMPARE(value(window, QStringLiteral("euler")), QStringLiteral("0"));
        QCOMPARE(value(window, QStringLiteral("genus")), QStringLiteral("1"));
        QCOMPARE(value(window, QStringLiteral("boundary")), QStringLiteral("0"));
        QCOMPARE(value(window, QStringLiteral("orientable")), QStringLiteral("yes"));
        QCOMPARE(value(window, QStringLiteral("gauss_bonnet")), QStringLiteral("0.000000"));
        QCOMPARE(value(window, QStringLiteral("vertices")), QLocale(QLocale::English).toString(window.model()->mesh().vertex_count()));
        QCOMPARE(window.windowTitle(), QStringLiteral("Torus – Mesh viewer"));
    }

    void everyFieldHasAValue()
    {
        MainWindow window;
        window.setInteractive(false);
        QVERIFY(window.open(QStringLiteral(MESH_TEST_DATA "/tetrahedron.ply")));
        for (const QString& key : MainWindow::kFields)
            QVERIFY2(value(window, key) != QStringLiteral("—") && value(window, key) != QStringLiteral("<missing>"), qPrintable(key));
    }

    void aBadFileKeepsTheCurrentMeshAndSaysWhere()
    {
        MainWindow window;
        window.setInteractive(false);
        window.setLanguage(QStringLiteral("en"));
        QVERIFY(window.openSample(QStringLiteral("sphere")));
        const MeshModel* before = window.model();
        QTemporaryFile bad(QStringLiteral("XXXXXX.obj"));
        QVERIFY(bad.open());
        bad.write("v 0 0 0\nf 1 2 3\n");
        bad.close();
        QVERIFY(!window.open(bad.fileName()));
        QCOMPARE(window.model(), before);
        QVERIFY2(window.lastError().contains(QStringLiteral("(line 2)")), qPrintable(window.lastError()));
        QVERIFY(!window.openSample(QStringLiteral("teapot")));
    }

    void theLanguageSwitchesAtOnce()
    {
        MainWindow window;
        window.setInteractive(false);
        QVERIFY(window.openSample(QStringLiteral("mobius")));
        window.setLanguage(QStringLiteral("fr"));
        QCOMPARE(menuTitles(window), (QStringList{QStringLiteral("&Fichier"), QStringLiteral("&Affichage"), QStringLiteral("&Langue"), QStringLiteral("Aid&e")}));
        QCOMPARE(value(window, QStringLiteral("orientable")), QStringLiteral("non"));
        QCOMPARE(value(window, QStringLiteral("genus")), QStringLiteral("non défini"));
        QCOMPARE(value(window, QStringLiteral("edges")), QLocale(QLocale::French).toString(1140));
        QCOMPARE(window.windowTitle(), QStringLiteral("Ruban de Möbius – Visionneuse de maillages"));
        window.setLanguage(QStringLiteral("en"));
        QCOMPARE(menuTitles(window).first(), QStringLiteral("&File"));
        QCOMPARE(value(window, QStringLiteral("edges")), QStringLiteral("1,140"));
    }

    void aDroppedFileIsOpened()
    {
        MainWindow window;
        window.setInteractive(false);
        QMimeData text;
        text.setText(QStringLiteral("not a file"));
        QVERIFY(!MainWindow::canOpen(&text));
        QMimeData remote;
        remote.setUrls({QUrl(QStringLiteral("https://example.org/cube.obj"))});
        QVERIFY(!MainWindow::canOpen(&remote));
        QMimeData file;
        file.setUrls({QUrl::fromLocalFile(QStringLiteral(MESH_TEST_DATA "/cube.obj"))});
        QVERIFY(MainWindow::canOpen(&file));
        QVERIFY(window.openDropped(&file));
        QCOMPARE(window.model()->mesh().vertex_count(), 8u);
    }

    void theMenuFollowsTheViewOptions()
    {
        MainWindow window;
        window.setInteractive(false);
        window.setLanguage(QStringLiteral("en"));
        QVERIFY(window.openSample(QStringLiteral("saddle")));
        RenderOptions options = window.viewer()->options();
        options.curvature = true;
        window.viewer()->setOptions(options);
        QAction* curvature = nullptr;
        for (QAction* a : window.findChildren<QAction*>())
            if (a->text() == QStringLiteral("&Curvature colours"))
                curvature = a;
        QVERIFY(curvature != nullptr);
        QVERIFY(curvature->isChecked());
        curvature->trigger();
        QVERIFY(!window.viewer()->options().curvature);
    }

    void theSelectionFillsItsPanel()
    {
        MainWindow window;
        window.setInteractive(false);
        window.setLanguage(QStringLiteral("en"));
        auto* panel = window.findChild<QLabel*>(QStringLiteral("selection"));
        QVERIFY(panel != nullptr);
        QVERIFY(window.openSample(QStringLiteral("torus")));
        QVERIFY(panel->text().startsWith(QStringLiteral("Click the mesh")));
        window.viewer()->setSelection({Selection::Kind::Vertex, 0});
        QVERIFY2(panel->text().startsWith(QStringLiteral("Vertex 0\nPosition: (")), qPrintable(panel->text()));
        QVERIFY(panel->text().contains(QStringLiteral("Valence: 6")));
        QVERIFY(panel->text().contains(QStringLiteral("On the boundary: no")));
        window.viewer()->setSelection({Selection::Kind::Face, 5});
        QVERIFY(panel->text().startsWith(QStringLiteral("Face 5\nVertices: ")));
        QVERIFY(panel->text().contains(QStringLiteral("Area: ")));
        window.setLanguage(QStringLiteral("fr"));
        // French puts a no-break space before the colon.
        const QString french = QStringLiteral("Face 5\nSommets") + QChar(0x00a0) + QStringLiteral(": ");
        QVERIFY2(panel->text().startsWith(french), qPrintable(panel->text()));
        // Out of range, or a new mesh: no selection.
        window.viewer()->setSelection({Selection::Kind::Vertex, 999999});
        QCOMPARE(window.viewer()->selection().kind, Selection::Kind::None);
        window.viewer()->setSelection({Selection::Kind::Vertex, 3});
        QVERIFY(window.openSample(QStringLiteral("sphere")));
        QCOMPARE(window.viewer()->selection().kind, Selection::Kind::None);
    }

    void theKeyboardWalksTheVertices()
    {
        MainWindow window;
        window.setInteractive(false);
        QVERIFY(window.openSample(QStringLiteral("saddle")));
        ViewerWidget* view = window.viewer();
        QTest::keyClick(view, Qt::Key_BracketRight);
        QCOMPARE(view->selection(), (Selection{Selection::Kind::Vertex, 0}));
        QTest::keyClick(view, Qt::Key_BracketRight);
        QCOMPARE(view->selection().index, 1u);
        QTest::keyClick(view, Qt::Key_BracketLeft);
        QTest::keyClick(view, Qt::Key_BracketLeft);
        QCOMPARE(view->selection().index, window.model()->mesh().vertex_count() - 1);
        QTest::keyClick(view, Qt::Key_Escape);
        QCOMPARE(view->selection().kind, Selection::Kind::None);
    }

    void theViewIsSavedAsAnImage()
    {
        MainWindow window;
        window.setInteractive(false);
        QVERIFY(window.openSample(QStringLiteral("sphere"))); // the torus has a hole in the middle
        window.resize(800, 500);
        window.show();
        QVERIFY(QTest::qWaitForWindowExposed(&window));
        QTemporaryDir dir;
        const QString path = dir.filePath(QStringLiteral("view.png"));
        QVERIFY(window.saveImage(path));
        const QImage image(path);
        QCOMPARE(image.size(), window.viewer()->size() * window.viewer()->devicePixelRatio());
        QVERIFY(image.pixelColor(image.width() / 2, image.height() / 2) != Renderer::kBackground);
        QVERIFY(image.pixelColor(2, 2) == Renderer::kBackground);
        QVERIFY(!window.saveImage(dir.filePath(QStringLiteral("no/such/folder/view.png"))));
    }

    void theKeyboardTurnsTheView()
    {
        MainWindow window;
        window.setInteractive(false);
        QVERIFY(window.openSample(QStringLiteral("torus")));
        ViewerWidget* view = window.viewer();
        const float yaw = view->camera().yaw();
        const float distance = view->camera().distance();
        QTest::keyClick(view, Qt::Key_Left);
        QCOMPARE(view->camera().yaw(), yaw + 10);
        QTest::keyClick(view, Qt::Key_Plus);
        QVERIFY(view->camera().distance() < distance);
        const QVector3D target = view->camera().target();
        QTest::keyClick(view, Qt::Key_Right, Qt::ShiftModifier);
        QVERIFY(view->camera().target() != target);
    }
};

QTEST_MAIN(TestWindow)
#include "test_window.moc"
