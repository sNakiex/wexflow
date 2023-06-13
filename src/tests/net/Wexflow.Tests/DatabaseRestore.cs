﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Wexflow.Tests
{
    [TestClass]
    [Ignore]
    public class DatabaseRestore
    {
        private static readonly string SuccessFile = @"C:\WexflowTesting\DatabaseRestore\file1.txt";

        [TestInitialize]
        public void TestInitialize()
        {
            if (File.Exists(SuccessFile))
            {
                File.Delete(SuccessFile);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            File.Delete(SuccessFile);
        }

        [TestMethod]
        public void DatabaseRestoreTest()
        {
            Assert.IsFalse(File.Exists(SuccessFile));
            _ = Helper.StartWorkflow(86);
            Assert.IsTrue(File.Exists(SuccessFile));
        }
    }
}
