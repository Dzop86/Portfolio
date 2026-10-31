namespace Rpg.Client.Tests;

/// <summary>The tree's camera (sprint 176, D87, <c>docs/arbre-lisible.md</c> §7.4): a zoom around the mouse, its bounds, the overview, the frame on the build, a click told from a drag.</summary>
public class TreeCameraTests
{
    private static TreeCamera Camera() => new(1800, 780, 932, 644);

    [Fact]
    public void ThePointUnderTheMouse_StaysThere_WhenZooming()
    {
        TreeCamera camera = Camera();
        camera.Centre(900, 390);
        (float wx, float wy) = camera.ToWorld(300, 200);
        camera.ZoomAt(300, 200, 1);
        (float x, float y) = camera.ToView(wx, wy);
        Assert.Equal(300, x, 0.01f);
        Assert.Equal(200, y, 0.01f);
        Assert.Equal(camera.MinZoom * TreeCamera.Step, camera.Zoom, 0.0001f);
    }

    [Fact]
    public void TheZoom_StaysBetweenTheOverviewAndAHundredAndFifty()
    {
        TreeCamera camera = Camera();
        Assert.Equal(Math.Min(932f / 1800, 644f / 780), camera.MinZoom, 0.0001f);
        camera.ZoomAt(400, 300, 50);
        Assert.Equal(TreeCamera.MaxZoom, camera.Zoom);
        camera.ZoomAt(400, 300, -50);
        Assert.Equal(camera.MinZoom, camera.Zoom);
        // The overview holds the whole world.
        camera.Fit();
        (float left, float top) = camera.ToView(0, 0);
        (float right, float bottom) = camera.ToView(1800, 780);
        Assert.True(left >= -0.01f && top >= -0.01f && right <= 932.01f && bottom <= 644.01f);
    }

    [Fact]
    public void TheTree_NeverLeavesMoreThanHalfTheView()
    {
        TreeCamera camera = Camera();
        camera.ZoomAt(466, 322, 3);
        camera.PanBy(10_000, 10_000);
        Assert.Equal((466f, 322f), camera.ToView(0, 0));
        camera.PanBy(-20_000, -20_000);
        Assert.Equal((466f, 322f), camera.ToView(1800, 780));
    }

    [Fact]
    public void TheFrame_HoldsTheBuild_AtEightyPercentAtLeast()
    {
        TreeCamera camera = Camera();
        Box[] build = [new(400, 100, 56, 56), new(700, 400, 64, 64)];
        camera.Frame(build);
        Assert.InRange(camera.Zoom, TreeCamera.FrameZoom, TreeCamera.MaxZoom);
        Assert.All(build, b =>
        {
            (float x, float y) = camera.ToView(b.X, b.Y);
            (float r, float d) = camera.ToView(b.X + b.W, b.Y + b.H);
            Assert.True(x >= 0 && y >= 0 && r <= 932 && d <= 644, b.ToString());
        });
        // A single node: no closer than 150 %; nothing: the overview.
        camera.Frame([new Box(100, 100, 56, 56)]);
        Assert.Equal(TreeCamera.MaxZoom, camera.Zoom);
        camera.Frame([]);
        Assert.Equal(camera.MinZoom, camera.Zoom);
        // Too wide for 80 %: centred at 80 %.
        camera.Frame([new Box(0, 0, 56, 56), new Box(1700, 700, 56, 56)]);
        Assert.Equal(TreeCamera.FrameZoom, camera.Zoom);
    }

    [Fact]
    public void FivePixels_IsAClick_SevenADrag()
    {
        Assert.False(TreeCamera.IsDrag(3, 4));
        Assert.True(TreeCamera.IsDrag(7, 0));
        Assert.True(TreeCamera.IsDrag(0, -7));
    }
}
