#include "viewerwidget.h"

#include <QKeyEvent>
#include <QMouseEvent>
#include <QWheelEvent>

#include <cmath>

ViewerWidget::ViewerWidget(QWidget* parent) : QOpenGLWidget(parent)
{
    setFocusPolicy(Qt::StrongFocus);
    setMinimumSize(320, 240);
}

ViewerWidget::~ViewerWidget()
{
    // The renderer's buffers belong to this widget's context.
    makeCurrent();
    renderer_.upload(nullptr);
    doneCurrent();
}

void ViewerWidget::setModel(const MeshModel* model)
{
    model_ = model;
    uploaded_ = false;
    fit();
}

void ViewerWidget::setOptions(const RenderOptions& options)
{
    options_ = options;
    emit optionsChanged(options_);
    update();
}

void ViewerWidget::fit()
{
    if (model_ != nullptr)
        camera_.fit(model_->bounds());
    update();
}

void ViewerWidget::initializeGL()
{
    if (!renderer_.initialize())
        emit graphicsError(renderer_.error());
}

void ViewerWidget::paintGL()
{
    // Uploaded here, where the context is current: setModel may come before the widget is shown.
    if (!uploaded_) {
        renderer_.upload(model_);
        uploaded_ = true;
    }
    const qreal ratio = devicePixelRatioF();
    renderer_.render(camera_, options_, static_cast<int>(std::lround(width() * ratio)), static_cast<int>(std::lround(height() * ratio)));
}

void ViewerWidget::mousePressEvent(QMouseEvent* event)
{
    last_ = event->position().toPoint();
}

// Left button turns the mesh, right or middle button moves it.
void ViewerWidget::mouseMoveEvent(QMouseEvent* event)
{
    const QPoint delta = event->position().toPoint() - last_;
    last_ = event->position().toPoint();
    if (event->buttons() & Qt::LeftButton)
        camera_.orbit(-0.4f * static_cast<float>(delta.x()), 0.4f * static_cast<float>(delta.y()));
    else if (event->buttons() & (Qt::RightButton | Qt::MiddleButton))
        camera_.pan(static_cast<float>(delta.x()) / static_cast<float>(height()), static_cast<float>(delta.y()) / static_cast<float>(height()));
    update();
}

void ViewerWidget::wheelEvent(QWheelEvent* event)
{
    // One notch (120 units) comes 10 % closer.
    camera_.zoom(std::pow(0.9f, static_cast<float>(event->angleDelta().y()) / 120.0f));
    update();
}

// The keyboard does everything the mouse does: arrows turn, Shift+arrows move, + and - zoom.
void ViewerWidget::keyPressEvent(QKeyEvent* event)
{
    const bool shift = event->modifiers() & Qt::ShiftModifier;
    switch (event->key()) {
    case Qt::Key_Left: shift ? camera_.pan(-0.05f, 0) : camera_.orbit(10, 0); break;
    case Qt::Key_Right: shift ? camera_.pan(0.05f, 0) : camera_.orbit(-10, 0); break;
    case Qt::Key_Up: shift ? camera_.pan(0, -0.05f) : camera_.orbit(0, 10); break;
    case Qt::Key_Down: shift ? camera_.pan(0, 0.05f) : camera_.orbit(0, -10); break;
    case Qt::Key_Plus:
    case Qt::Key_Equal: camera_.zoom(0.9f); break;
    case Qt::Key_Minus: camera_.zoom(1 / 0.9f); break;
    default: QOpenGLWidget::keyPressEvent(event); return;
    }
    update();
}
