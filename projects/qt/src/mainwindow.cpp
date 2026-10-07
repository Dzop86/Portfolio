#include "mainwindow.h"

#include "legendwidget.h"
#include "viewerwidget.h"

#include <QAction>
#include <QActionGroup>
#include <QApplication>
#include <QDockWidget>
#include <QDragEnterEvent>
#include <QDropEvent>
#include <QElapsedTimer>
#include <QFileDialog>
#include <QFileInfo>
#include <QFormLayout>
#include <QLabel>
#include <QLocale>
#include <QMenuBar>
#include <QMessageBox>
#include <QMimeData>
#include <QSignalBlocker>
#include <QStatusBar>
#include <QVBoxLayout>

#include <cmath>

const QStringList MainWindow::kFields = {
    QStringLiteral("vertices"), QStringLiteral("edges"), QStringLiteral("faces"), QStringLiteral("components"),
    QStringLiteral("boundary"), QStringLiteral("nonmanifold_edges"), QStringLiteral("nonmanifold_vertices"),
    QStringLiteral("manifold"), QStringLiteral("orientable"), QStringLiteral("euler"), QStringLiteral("genus"),
    QStringLiteral("gauss_bonnet")};

const QStringList MainWindow::kSamples = {QStringLiteral("torus"), QStringLiteral("sphere"), QStringLiteral("mobius"),
                                          QStringLiteral("saddle")};

MainWindow::MainWindow(QWidget* parent) : QMainWindow(parent)
{
    setAcceptDrops(true);
    viewer_ = new ViewerWidget(this);
    setCentralWidget(viewer_);
    // The menu follows the view's options, whoever changed them (menu, command line, tests).
    connect(viewer_, &ViewerWidget::optionsChanged, this, [this](const RenderOptions& options) {
        for (auto [action, on] : {std::pair{wireframe_, options.wireframe}, {curvature_, options.curvature}, {highlight_, options.highlight}}) {
            const QSignalBlocker blocker(action);
            action->setChecked(on);
        }
        refreshPanel();
    });
    connect(viewer_, &ViewerWidget::selectionChanged, this, [this] { refreshSelection(); });
    connect(viewer_, &ViewerWidget::graphicsError, this, [this](const QString& message) {
        fail(tr("OpenGL 3.3 is not available: %1").arg(message));
    });

    // Topology panel: one row per invariant, then the curvature legend.
    dock_ = new QDockWidget(this);
    dock_->setObjectName(QStringLiteral("topology"));
    dock_->setFeatures(QDockWidget::DockWidgetMovable | QDockWidget::DockWidgetFloatable);
    auto* panel = new QWidget(dock_);
    auto* column = new QVBoxLayout(panel);
    auto* form = new QFormLayout();
    for (const QString& key : kFields) {
        auto* name = new QLabel(panel);
        auto* value = new QLabel(QStringLiteral("—"), panel);
        value->setObjectName(QStringLiteral("value_") + key);
        value->setTextInteractionFlags(Qt::TextSelectableByMouse | Qt::TextSelectableByKeyboard);
        name->setBuddy(value);
        form->addRow(name, value);
        names_.insert(key, name);
        values_.insert(key, value);
    }
    column->addLayout(form);
    legend_title_ = new QLabel(panel);
    legend_ = new LegendWidget(panel);
    column->addSpacing(12);
    column->addWidget(legend_title_);
    column->addWidget(legend_);
    // What the click (or the keyboard) selected: one block of text, selectable and read by screen readers.
    selection_title_ = new QLabel(panel);
    selection_ = new QLabel(panel);
    selection_->setObjectName(QStringLiteral("selection"));
    selection_->setWordWrap(true);
    selection_->setTextInteractionFlags(Qt::TextSelectableByMouse | Qt::TextSelectableByKeyboard);
    selection_title_->setBuddy(selection_);
    column->addSpacing(12);
    column->addWidget(selection_title_);
    column->addWidget(selection_);
    column->addStretch();
    dock_->setWidget(panel);
    addDockWidget(Qt::RightDockWidgetArea, dock_);

    open_ = new QAction(this);
    open_->setShortcut(QKeySequence::Open);
    connect(open_, &QAction::triggered, this, [this] {
        const QString path = QFileDialog::getOpenFileName(this, tr("Open a mesh"), {}, tr("Meshes (*.obj *.ply *.stl);;All files (*)"));
        if (!path.isEmpty())
            open(path);
    });
    save_image_ = new QAction(this);
    save_image_->setShortcut(QKeySequence::Save);
    connect(save_image_, &QAction::triggered, this, [this] {
        const QString path = QFileDialog::getSaveFileName(this, tr("Save the view"), QStringLiteral("view.png"), tr("Images (*.png *.jpg)"));
        if (!path.isEmpty())
            saveImage(path);
    });
    quit_ = new QAction(this);
    quit_->setShortcut(QKeySequence::Quit);
    connect(quit_, &QAction::triggered, this, &QWidget::close);

    auto checkable = [this](const QString& key, bool on, auto setter) {
        auto* action = new QAction(this);
        action->setCheckable(true);
        action->setChecked(on);
        action->setShortcut(QKeySequence(key));
        connect(action, &QAction::toggled, this, [this, setter](bool checked) {
            RenderOptions options = viewer_->options();
            setter(options, checked);
            viewer_->setOptions(options);
            refreshPanel();
        });
        return action;
    };
    const RenderOptions defaults;
    wireframe_ = checkable(QStringLiteral("W"), defaults.wireframe, [](RenderOptions& o, bool on) { o.wireframe = on; });
    curvature_ = checkable(QStringLiteral("C"), defaults.curvature, [](RenderOptions& o, bool on) { o.curvature = on; });
    highlight_ = checkable(QStringLiteral("B"), defaults.highlight, [](RenderOptions& o, bool on) { o.highlight = on; });
    fit_ = new QAction(this);
    fit_->setShortcut(QKeySequence(QStringLiteral("F")));
    connect(fit_, &QAction::triggered, viewer_, &ViewerWidget::fit);

    auto* languages = new QActionGroup(this);
    french_ = new QAction(QStringLiteral("Français"), languages);
    english_ = new QAction(QStringLiteral("English"), languages);
    for (QAction* a : {french_, english_})
        a->setCheckable(true);
    connect(french_, &QAction::triggered, this, [this] { setLanguage(QStringLiteral("fr")); });
    connect(english_, &QAction::triggered, this, [this] { setLanguage(QStringLiteral("en")); });

    controls_ = new QAction(this);
    controls_->setShortcut(QKeySequence::HelpContents);
    connect(controls_, &QAction::triggered, this, [this] {
        QMessageBox::information(this, tr("Controls"),
            tr("Mouse: left button turns the mesh, right or middle button moves it, the wheel zooms, a click selects.\n"
               "Keyboard (click the view first): arrows turn, Shift+arrows move, + and - zoom, F frames the mesh, "
               "Space selects at the centre, ] and [ go from vertex to vertex, Escape clears the selection.\n"
               "W: edges, C: curvature colours, B: boundary and non-manifold edges."));
    });
    about_ = new QAction(this);
    connect(about_, &QAction::triggered, this, [this] {
        QMessageBox::about(this, tr("About"),
            tr("Mesh viewer, built with Qt %1, running on Qt %2.\nFiles are read by lib-c, the portfolio's C library; the half-edge structure, "
               "invariants and curvature come from its topologie project.").arg(QStringLiteral(QT_VERSION_STR), QString::fromLatin1(qVersion())));
    });

    file_menu_ = menuBar()->addMenu(QString());
    file_menu_->addAction(open_);
    samples_menu_ = file_menu_->addMenu(QString());
    for (const QString& sample : kSamples) {
        QAction* action = samples_menu_->addAction(QString());
        connect(action, &QAction::triggered, this, [this, sample] { openSample(sample); });
        sample_actions_.insert(sample, action);
    }
    file_menu_->addAction(save_image_);
    file_menu_->addSeparator();
    file_menu_->addAction(quit_);
    view_menu_ = menuBar()->addMenu(QString());
    view_menu_->addActions({wireframe_, curvature_, highlight_});
    view_menu_->addSeparator();
    view_menu_->addAction(fit_);
    language_menu_ = menuBar()->addMenu(QString());
    language_menu_->addActions({french_, english_});
    help_menu_ = menuBar()->addMenu(QString());
    help_menu_->addActions({controls_, about_});

    resize(1100, 700);
    setLanguage(QLocale::system().language() == QLocale::French ? QStringLiteral("fr") : QStringLiteral("en"));
}

MainWindow::~MainWindow() = default;

bool MainWindow::open(const QString& path)
{
    QElapsedTimer timer;
    timer.start();
    try {
        auto model = std::make_unique<MeshModel>(MeshModel::fromFile(path));
        return display(std::move(model), QFileInfo(path).fileName(), QString(), timer.elapsed());
    } catch (const topo::LoadError& e) {
        const QString where = e.line() > 0 ? tr(" (line %1)").arg(e.line()) : QString();
        fail(tr("Cannot read %1: %2%3").arg(QFileInfo(path).fileName(), QString::fromStdString(e.what()), where));
    } catch (const std::exception& e) {
        fail(tr("Cannot read %1: %2").arg(QFileInfo(path).fileName(), QString::fromStdString(e.what())));
    }
    return false;
}

bool MainWindow::openSample(const QString& name)
{
    QFile file(QStringLiteral(":/samples/%1.obj").arg(name));
    if (!kSamples.contains(name) || !file.open(QIODevice::ReadOnly)) {
        fail(tr("Unknown sample: %1").arg(name));
        return false;
    }
    QElapsedTimer timer;
    timer.start();
    return display(std::make_unique<MeshModel>(MeshModel::fromData(file.readAll())), QString(), name, timer.elapsed());
}

bool MainWindow::display(std::unique_ptr<MeshModel> model, const QString& file, const QString& sample, qint64 milliseconds)
{
    // The view forgets the old model before it is destroyed.
    viewer_->setModel(model.get());
    model_ = std::move(model);
    file_ = file;
    sample_ = sample;
    load_ms_ = milliseconds;
    last_error_.clear();
    refreshPanel();
    return true;
}

void MainWindow::fail(const QString& message)
{
    last_error_ = message;
    statusBar()->showMessage(message);
    if (interactive_)
        QMessageBox::warning(this, tr("Mesh viewer"), message);
}

void MainWindow::setLanguage(const QString& code)
{
    language_ = code == QStringLiteral("fr") ? code : QStringLiteral("en");
    QLocale::setDefault(QLocale(language_ == QStringLiteral("fr") ? QLocale::French : QLocale::English));
    QCoreApplication::removeTranslator(&translator_);
    // English is the language of the source: only French needs a translation file.
    if (language_ == QStringLiteral("fr") && translator_.load(QStringLiteral(":/i18n/qtviewer_fr.qm")))
        QCoreApplication::installTranslator(&translator_);
    // The LanguageChange event only comes with the next turn of the event loop: this window does
    // not wait for it (the other widgets, such as the legend, do).
    retranslate();
    (language_ == QStringLiteral("fr") ? french_ : english_)->setChecked(true);
}

void MainWindow::changeEvent(QEvent* event)
{
    if (event->type() == QEvent::LanguageChange)
        retranslate();
    QMainWindow::changeEvent(event);
}

void MainWindow::retranslate()
{
    file_menu_->setTitle(tr("&File"));
    open_->setText(tr("&Open…"));
    save_image_->setText(tr("&Save the view as an image…"));
    samples_menu_->setTitle(tr("&Examples"));
    for (const QString& sample : kSamples)
        sample_actions_.value(sample)->setText(sampleTitle(sample));
    quit_->setText(tr("&Quit"));
    view_menu_->setTitle(tr("&View"));
    wireframe_->setText(tr("&Edges"));
    curvature_->setText(tr("&Curvature colours"));
    highlight_->setText(tr("&Boundary and non-manifold edges"));
    fit_->setText(tr("&Frame the mesh"));
    language_menu_->setTitle(tr("&Language"));
    help_menu_->setTitle(tr("&Help"));
    controls_->setText(tr("&Controls"));
    about_->setText(tr("&About"));
    dock_->setWindowTitle(tr("Topology"));
    const QStringList labels = {tr("Vertices"), tr("Edges"), tr("Faces"), tr("Connected components"), tr("Boundary loops"),
                                tr("Non-manifold edges"), tr("Non-manifold vertices"), tr("Manifold"), tr("Orientable"),
                                tr("Euler characteristic"), tr("Genus"), tr("Sum of angle defects / 2π")};
    for (qsizetype i = 0; i < kFields.size(); ++i)
        names_.value(kFields[i])->setText(labels[i]);
    legend_title_->setText(tr("Gaussian curvature"));
    selection_title_->setText(tr("Selection"));
    refreshPanel();
}

void MainWindow::refreshPanel()
{
    refreshSelection();
    const bool showLegend = model_ != nullptr && curvature_->isChecked();
    legend_title_->setVisible(showLegend);
    legend_->setVisible(showLegend);
    if (model_ == nullptr) {
        setWindowTitle(tr("Mesh viewer"));
        statusBar()->showMessage(tr("Open a mesh (OBJ, PLY, STL) or an example from the File menu, or drop a file here."));
        return;
    }
    const QLocale locale;
    const topo::Invariants& inv = model_->invariants();
    const topo::Mesh& mesh = model_->mesh();
    auto count = [&locale](auto n) { return locale.toString(static_cast<qulonglong>(n)); };
    auto yesNo = [](bool b) { return b ? tr("yes") : tr("no"); };
    values_.value(QStringLiteral("vertices"))->setText(count(mesh.vertex_count()));
    values_.value(QStringLiteral("edges"))->setText(count(mesh.edge_count()));
    values_.value(QStringLiteral("faces"))->setText(count(mesh.face_count()));
    values_.value(QStringLiteral("components"))->setText(count(inv.components));
    values_.value(QStringLiteral("boundary"))->setText(count(inv.boundary_loops));
    values_.value(QStringLiteral("nonmanifold_edges"))->setText(count(inv.non_manifold_edges));
    values_.value(QStringLiteral("nonmanifold_vertices"))->setText(count(inv.non_manifold_vertices));
    values_.value(QStringLiteral("manifold"))->setText(yesNo(inv.manifold));
    values_.value(QStringLiteral("orientable"))->setText(yesNo(inv.orientable));
    values_.value(QStringLiteral("euler"))->setText(locale.toString(static_cast<qlonglong>(inv.euler_characteristic)));
    values_.value(QStringLiteral("genus"))->setText(inv.genus ? locale.toString(static_cast<qlonglong>(*inv.genus)) : tr("undefined"));
    // Rounded first: -0.0000000001 would print as "-0.000000".
    const double ratio = std::round(model_->gaussBonnetRatio() * 1e6) / 1e6;
    values_.value(QStringLiteral("gauss_bonnet"))->setText(locale.toString(ratio == 0 ? 0.0 : ratio, 'f', 6));
    legend_->setScale(model_->curvatureScale());
    setWindowTitle(tr("%1 – Mesh viewer").arg(meshName()));
    statusBar()->showMessage(tr("%1: %2 vertices, %3 faces, read in %4 ms").arg(meshName(), count(mesh.vertex_count()), count(mesh.face_count()), count(load_ms_)));
}

void MainWindow::refreshSelection()
{
    const Selection& s = viewer_->selection();
    selection_->setEnabled(model_ != nullptr);
    if (model_ == nullptr || s.kind == Selection::Kind::None) {
        selection_->setText(tr("Click the mesh (or press Space) to select a vertex or a face."));
        return;
    }
    const QLocale locale;
    auto number = [&locale](double x) { return locale.toString(x, 'g', 4); };
    auto vector = [&number](const QVector3D& v) {
        return QStringLiteral("(%1 ; %2 ; %3)").arg(number(v.x()), number(v.y()), number(v.z()));
    };
    QStringList lines;
    if (s.kind == Selection::Kind::Vertex) {
        const topo::GaussianCurvature& k = model_->curvature();
        lines << tr("Vertex %1").arg(locale.toString(s.index))
              << tr("Position: %1").arg(vector(model_->positions()[s.index]))
              << tr("Valence: %1").arg(locale.toString(model_->valence()[s.index]))
              << tr("Gaussian curvature: %1").arg(number(k.gaussian[s.index]))
              << tr("Angle defect: %1 rad").arg(number(k.angle_defect[s.index]))
              << tr("On the boundary: %1").arg(k.boundary[s.index] ? tr("yes") : tr("no"));
    } else {
        const topo::Triangle& t = model_->mesh().triangles()[s.index];
        lines << tr("Face %1").arg(locale.toString(s.index))
              << tr("Vertices: %1, %2, %3").arg(locale.toString(t[0]), locale.toString(t[1]), locale.toString(t[2]))
              << tr("Area: %1").arg(number(model_->faceArea(s.index)))
              << tr("Normal: %1").arg(vector(model_->faceNormal(s.index)));
    }
    selection_->setText(lines.join(QLatin1Char('\n')));
}

bool MainWindow::saveImage(const QString& path)
{
    if (!viewer_->grabFramebuffer().save(path)) {
        fail(tr("Cannot save %1").arg(QFileInfo(path).fileName()));
        return false;
    }
    statusBar()->showMessage(tr("View saved to %1").arg(QFileInfo(path).fileName()));
    return true;
}

QString MainWindow::meshName() const
{
    return sample_.isEmpty() ? file_ : sampleTitle(sample_);
}

QString MainWindow::sampleTitle(const QString& name) const
{
    if (name == QStringLiteral("torus"))
        return tr("Torus");
    if (name == QStringLiteral("sphere"))
        return tr("Sphere");
    if (name == QStringLiteral("mobius"))
        return tr("Möbius strip");
    return tr("Saddle");
}

bool MainWindow::canOpen(const QMimeData* mime)
{
    return mime != nullptr && mime->hasUrls() && !mime->urls().isEmpty() && mime->urls().first().isLocalFile();
}

bool MainWindow::openDropped(const QMimeData* mime)
{
    return canOpen(mime) && open(mime->urls().first().toLocalFile());
}

void MainWindow::dragEnterEvent(QDragEnterEvent* event)
{
    if (canOpen(event->mimeData()))
        event->acceptProposedAction();
}

void MainWindow::dropEvent(QDropEvent* event)
{
    if (openDropped(event->mimeData()))
        event->acceptProposedAction();
}
