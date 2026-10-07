// The French translation is complete and keeps every placeholder and keyboard accelerator.
#include <QFile>
#include <QRegularExpression>
#include <QSet>
#include <QTest>
#include <QTranslator>
#include <QXmlStreamReader>

class TestTranslations : public QObject {
    Q_OBJECT

private:
    struct Message {
        QString source;
        QString translation;
        bool finished = true;
    };

    static QList<Message> read()
    {
        QFile file(QStringLiteral(VIEWER_SOURCE_DIR "/i18n/qtviewer_fr.ts"));
        if (!file.open(QIODevice::ReadOnly))
            qFatal("cannot read the .ts file");
        QList<Message> messages;
        QXmlStreamReader xml(&file);
        while (!xml.atEnd()) {
            if (!xml.readNextStartElement())
                continue;
            if (xml.name() == QStringLiteral("message"))
                messages.append(Message{});
            else if (xml.name() == QStringLiteral("source"))
                messages.last().source = xml.readElementText();
            else if (xml.name() == QStringLiteral("translation")) {
                messages.last().finished = xml.attributes().value(QStringLiteral("type")).isEmpty();
                messages.last().translation = xml.readElementText();
            }
        }
        if (xml.hasError())
            qFatal("%s", qPrintable(xml.errorString()));
        return messages;
    }

    static QSet<QString> placeholders(const QString& text)
    {
        static const QRegularExpression pattern(QStringLiteral("%\\d"));
        QSet<QString> found;
        for (const QRegularExpressionMatch& m : pattern.globalMatch(text))
            found.insert(m.captured());
        return found;
    }

private slots:
    void everyMessageIsTranslated()
    {
        const QList<Message> messages = read();
        QVERIFY(messages.size() > 40);
        for (const Message& m : messages) {
            QVERIFY2(m.finished && !m.translation.isEmpty(), qPrintable(m.source));
        }
    }

    void placeholdersAndAcceleratorsSurvive()
    {
        for (const Message& m : read()) {
            QCOMPARE(placeholders(m.translation), placeholders(m.source));
            QCOMPARE(m.translation.count(QLatin1Char('&')) > 0, m.source.count(QLatin1Char('&')) > 0);
        }
    }

    void theCompiledTranslationIsBuiltIn()
    {
        QTranslator translator;
        QVERIFY(translator.load(QStringLiteral(":/i18n/qtviewer_fr.qm")));
        QCOMPARE(translator.translate("MainWindow", "Vertices"), QStringLiteral("Sommets"));
    }
};

QTEST_GUILESS_MAIN(TestTranslations)
#include "test_translations.moc"
