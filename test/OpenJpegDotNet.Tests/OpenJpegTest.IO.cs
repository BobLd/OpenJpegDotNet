﻿using System.Drawing;
using System.IO;
using OpenJpegDotNet.IO;
using Xunit;

namespace OpenJpegDotNet.Tests
{
    public sealed partial class OpenJpegTest
    {

        #region Functions

        [Fact]
        public void Writer()
        {
            const string testImage = "obama-240p.jpg";
            var path = Path.GetFullPath(Path.Combine(TestImageDirectory, testImage));
            using var bitmap = System.Drawing.Image.FromFile(path) as Bitmap;

            using var writer = new Writer(bitmap);
            var output = writer.Encode();
            Assert.True(output != null);

            var outputPath = Path.Combine(ResultDirectory, nameof(this.Writer), $"{Path.GetFileNameWithoutExtension(testImage)}.j2k");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, output);
        }

        #endregion Functions

    }
}