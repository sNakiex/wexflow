﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.IO;

namespace Wexflow.NetCore.Tests
{
    [TestClass]
    public class ImagesCropper
    {
        private static readonly string Dest1 = @"C:\WexflowTesting\ImagesCropperDest\image1.jpg";
        private static readonly string Dest2 = @"C:\WexflowTesting\ImagesCropperDest\image2.jpg";
        private static readonly string Dest3 = @"C:\WexflowTesting\ImagesCropperDest\image3.jpg";

        [TestInitialize]
        public void TestInitialize()
        {
            if (File.Exists(Dest1)) File.Delete(Dest1);
            if (File.Exists(Dest2)) File.Delete(Dest2);
            if (File.Exists(Dest3)) File.Delete(Dest3);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (File.Exists(Dest1)) File.Delete(Dest1);
            if (File.Exists(Dest2)) File.Delete(Dest2);
            if (File.Exists(Dest3)) File.Delete(Dest3);
        }

        [TestMethod]
        public void ImagesCropperTest()
        {
            Assert.AreEqual(false, File.Exists(Dest1));
            Assert.AreEqual(false, File.Exists(Dest2));
            Assert.AreEqual(false, File.Exists(Dest3));
            Helper.StartWorkflow(74);
            Assert.AreEqual(true, File.Exists(Dest1));
            Assert.AreEqual(true, File.Exists(Dest2));
            Assert.AreEqual(true, File.Exists(Dest3));

            // Checking the image size
            CheckImageSize(Dest1);
            CheckImageSize(Dest2);
            CheckImageSize(Dest3);
        }

        private void CheckImageSize(string path)
        {
            using (Image image = Image.FromFile(path))
            {
                Assert.AreEqual(512, image.Width);
                Assert.AreEqual(384, image.Height);
            }
        }

    }
}
