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
