﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Wexflow.Tests
{
    [TestClass]
    public class MailsSender
    {
        private static readonly string MailsSenderFolder = @"C:\WexflowTesting\MailsSender\";

        [TestInitialize]
        public void TestInitialize()
        {
            Helper.DeleteFiles(MailsSenderFolder);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Helper.DeleteFiles(MailsSenderFolder);
        }

        [TestMethod]
        public void MailsSenderTest()
        {
            string[] files = GetFiles();
            Assert.AreEqual(0, files.Length);
            Helper.StartWorkflow(9);
            files = GetFiles();
            Assert.AreEqual(2, files.Length);
        }

        private string[] GetFiles()
        {
            return Directory.GetFiles(MailsSenderFolder, "*.txt");
        }
    }
}
