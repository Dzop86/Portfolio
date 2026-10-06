package portfolio.naval.ui;

import java.io.ByteArrayOutputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.util.zip.CRC32;
import java.util.zip.DeflaterOutputStream;

import javafx.scene.image.Image;
import javafx.scene.image.PixelReader;

/** Minimal PNG writer (8-bit RGB) for the screenshots, so neither AWT nor javafx-swing is needed. */
public final class Png {
    private Png() { }

    public static void write(Image image, OutputStream out) throws IOException {
        int w = (int) image.getWidth(), h = (int) image.getHeight();
        PixelReader px = image.getPixelReader();
        ByteArrayOutputStream raw = new ByteArrayOutputStream();
        try (DeflaterOutputStream z = new DeflaterOutputStream(raw)) {
            for (int y = 0; y < h; y++) {
                z.write(0); // filter: none
                for (int x = 0; x < w; x++) {
                    int argb = px.getArgb(x, y);
                    z.write((argb >> 16) & 0xff);
                    z.write((argb >> 8) & 0xff);
                    z.write(argb & 0xff);
                }
            }
        }
        DataOutputStream data = new DataOutputStream(out);
        data.write(new byte[] { (byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1a, '\n' });
        ByteArrayOutputStream header = new ByteArrayOutputStream();
        DataOutputStream hd = new DataOutputStream(header);
        hd.writeInt(w);
        hd.writeInt(h);
        hd.write(new byte[] { 8, 2, 0, 0, 0 }); // 8 bits, RGB, deflate, no filter, no interlace
        chunk(data, "IHDR", header.toByteArray());
        chunk(data, "IDAT", raw.toByteArray());
        chunk(data, "IEND", new byte[0]);
        data.flush();
    }

    private static void chunk(DataOutputStream out, String type, byte[] body) throws IOException {
        byte[] t = type.getBytes(java.nio.charset.StandardCharsets.US_ASCII);
        out.writeInt(body.length);
        out.write(t);
        out.write(body);
        CRC32 crc = new CRC32();
        crc.update(t);
        crc.update(body);
        out.writeInt((int) crc.getValue());
    }
}
