using Naiad;
using Xunit;

namespace SafeQuery.Tests;

public class PngRenderTests
{
    [Fact]
    public void RendersErDiagramToPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"safequery-{Guid.NewGuid():N}.png");
        try
        {
            SkiaRenderer.RenderPng("erDiagram\n  A ||--o{ B : \"\"\n", path);

            var header = File.ReadAllBytes(path).Take(4).ToArray();
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, header);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
