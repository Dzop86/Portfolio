#include "legendwidget.h"

#include "meshmodel.h"

#include <QEvent>
#include <QLinearGradient>
#include <QLocale>
#include <QPainter>

LegendWidget::LegendWidget(QWidget* parent) : QWidget(parent)
{
    updateAccessibleText();
}

void LegendWidget::setScale(double scale)
{
    scale_ = scale;
    updateAccessibleText();
    update();
}

QSize LegendWidget::sizeHint() const
{
    return {220, fontMetrics().height() * 2 + 18};
}

void LegendWidget::changeEvent(QEvent* event)
{
    if (event->type() == QEvent::LanguageChange)
        updateAccessibleText();
    QWidget::changeEvent(event);
}

void LegendWidget::updateAccessibleText()
{
    setAccessibleName(tr("Curvature scale"));
    setAccessibleDescription(tr("From %1 (blue, saddle) to %2 (red, dome); white is flat.")
                                 .arg(QLocale().toString(-scale_, 'g', 3), QLocale().toString(scale_, 'g', 3)));
}

void LegendWidget::paintEvent(QPaintEvent*)
{
    QPainter painter(this);
    const int barHeight = 14;
    const QRect bar(0, 0, width() - 1, barHeight);
    QLinearGradient gradient(bar.topLeft(), bar.topRight());
    for (int i = 0; i <= 10; ++i) {
        const double t = i / 10.0;
        gradient.setColorAt(t, MeshModel::curvatureColor((2 * t - 1) * scale_, scale_));
    }
    painter.fillRect(bar, gradient);
    painter.setPen(palette().color(QPalette::WindowText));
    const QRect labels(0, barHeight + 4, width(), height() - barHeight - 4);
    painter.drawText(labels, Qt::AlignLeft | Qt::AlignTop, QLocale().toString(-scale_, 'g', 3));
    painter.drawText(labels, Qt::AlignHCenter | Qt::AlignTop, QStringLiteral("0"));
    painter.drawText(labels, Qt::AlignRight | Qt::AlignTop, QLocale().toString(scale_, 'g', 3));
}
