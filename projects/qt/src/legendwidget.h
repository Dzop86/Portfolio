// The curvature colour scale, from -scale (blue) to +scale (red).
#pragma once

#include <QWidget>

class LegendWidget : public QWidget {
    Q_OBJECT
public:
    explicit LegendWidget(QWidget* parent = nullptr);
    void setScale(double scale);
    [[nodiscard]] double scale() const { return scale_; }
    [[nodiscard]] QSize sizeHint() const override;

protected:
    void paintEvent(QPaintEvent* event) override;
    void changeEvent(QEvent* event) override;

private:
    void updateAccessibleText();
    double scale_ = 1.0;
};
