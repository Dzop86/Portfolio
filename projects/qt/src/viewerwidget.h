// The 3D view: an OpenGL widget driven by the mouse and the keyboard.
#pragma once

#include "camera.h"
#include "meshmodel.h"
#include "picking.h"
#include "renderer.h"

#include <QOpenGLWidget>
#include <QPoint>

class ViewerWidget : public QOpenGLWidget {
    Q_OBJECT
public:
    explicit ViewerWidget(QWidget* parent = nullptr);
    ~ViewerWidget() override;

    // The widget does not own the model; nullptr shows an empty view.
    void setModel(const MeshModel* model);
    [[nodiscard]] const RenderOptions& options() const { return options_; }
    void setOptions(const RenderOptions& options);
    [[nodiscard]] const Camera& camera() const { return camera_; }
    void fit();
    [[nodiscard]] const Selection& selection() const { return selection_; }
    void setSelection(const Selection& selection);
    // Selects what lies under a point of the widget (a click, or the centre for the keyboard).
    void selectAt(QPointF position);

signals:
    void optionsChanged(const RenderOptions& options);
    void selectionChanged(const Selection& selection);
    // OpenGL 3.3 is missing or the shaders do not compile.
    void graphicsError(const QString& message);

protected:
    void initializeGL() override;
    void paintGL() override;
    void mousePressEvent(QMouseEvent* event) override;
    void mouseMoveEvent(QMouseEvent* event) override;
    void mouseReleaseEvent(QMouseEvent* event) override;
    void wheelEvent(QWheelEvent* event) override;
    void keyPressEvent(QKeyEvent* event) override;

private:
    const MeshModel* model_ = nullptr;
    Renderer renderer_;
    Camera camera_;
    RenderOptions options_;
    QPoint last_;
    QPoint pressed_;
    Selection selection_;
    bool uploaded_ = false;
    bool selection_dirty_ = false;
};
