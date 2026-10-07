// The application window: the 3D view, the topology panel, menus, drag and drop, two languages.
#pragma once

#include "meshmodel.h"

#include <QHash>
#include <QMainWindow>
#include <QTranslator>

#include <memory>

class LegendWidget;
class QAction;
class QActionGroup;
class QDockWidget;
class QLabel;
class QMenu;
class QMimeData;
class ViewerWidget;

class MainWindow : public QMainWindow {
    Q_OBJECT
public:
    // The rows of the topology panel; their value labels are named "value_<key>" for the tests.
    static const QStringList kFields;
    static const QStringList kSamples;

    explicit MainWindow(QWidget* parent = nullptr);
    ~MainWindow() override;

    // Reads a mesh file; on failure, keeps the current mesh and reports the error (a dialog when interactive).
    bool open(const QString& path);
    // One of kSamples, built into the application.
    bool openSample(const QString& name);
    // What a drag and drop brings: the first URL, when it is a local file.
    [[nodiscard]] static bool canOpen(const QMimeData* mime);
    bool openDropped(const QMimeData* mime);
    // The 3D view as a PNG (or any format Qt writes, from the file's extension).
    bool saveImage(const QString& path);
    // "fr" or "en": menus, panel and numbers switch at once.
    void setLanguage(const QString& code);
    [[nodiscard]] QString language() const { return language_; }
    // Tests turn the error dialogs off.
    void setInteractive(bool interactive) { interactive_ = interactive; }
    [[nodiscard]] QString lastError() const { return last_error_; }
    [[nodiscard]] const MeshModel* model() const { return model_.get(); }
    [[nodiscard]] ViewerWidget* viewer() const { return viewer_; }

protected:
    void changeEvent(QEvent* event) override;
    void dragEnterEvent(QDragEnterEvent* event) override;
    void dropEvent(QDropEvent* event) override;

private:
    // A sample is named by its key, translated when shown; a file by its name.
    bool display(std::unique_ptr<MeshModel> model, const QString& file, const QString& sample, qint64 milliseconds);
    [[nodiscard]] QString meshName() const;
    void fail(const QString& message);
    void retranslate();
    void refreshPanel();
    void refreshSelection();
    [[nodiscard]] QString sampleTitle(const QString& name) const;

    std::unique_ptr<MeshModel> model_;
    QString file_;
    QString sample_;
    qint64 load_ms_ = 0;
    QString language_;
    QString last_error_;
    bool interactive_ = true;
    QTranslator translator_;

    ViewerWidget* viewer_ = nullptr;
    QDockWidget* dock_ = nullptr;
    LegendWidget* legend_ = nullptr;
    QLabel* legend_title_ = nullptr;
    QLabel* selection_title_ = nullptr;
    QLabel* selection_ = nullptr;
    QHash<QString, QLabel*> names_;
    QHash<QString, QLabel*> values_;
    QMenu* file_menu_ = nullptr;
    QMenu* samples_menu_ = nullptr;
    QMenu* view_menu_ = nullptr;
    QMenu* language_menu_ = nullptr;
    QMenu* help_menu_ = nullptr;
    QAction* open_ = nullptr;
    QAction* save_image_ = nullptr;
    QAction* quit_ = nullptr;
    QAction* wireframe_ = nullptr;
    QAction* curvature_ = nullptr;
    QAction* highlight_ = nullptr;
    QAction* fit_ = nullptr;
    QAction* french_ = nullptr;
    QAction* english_ = nullptr;
    QAction* controls_ = nullptr;
    QAction* about_ = nullptr;
    QHash<QString, QAction*> sample_actions_;
};
