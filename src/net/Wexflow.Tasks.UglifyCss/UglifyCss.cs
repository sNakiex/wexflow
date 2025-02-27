﻿using NUglify;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.UglifyCss
{
    public class UglifyCss : Task
    {
        public string SmbComputerName { get; private set; }
        public string SmbDomain { get; private set; }
        public string SmbUsername { get; private set; }
        public string SmbPassword { get; private set; }

        public UglifyCss(XElement xe, Workflow wf) : base(xe, wf)
        {
            SmbComputerName = GetSetting("smbComputerName");
            SmbDomain = GetSetting("smbDomain");
            SmbUsername = GetSetting("smbUsername");
            SmbPassword = GetSetting("smbPassword");
        }

        public override TaskStatus Run()
        {
            Info("Uglifying CSS files...");

            var success = true;
            var atLeastOneSuccess = false;

            try
            {
                if (!string.IsNullOrEmpty(SmbComputerName) && !string.IsNullOrEmpty(SmbUsername) && !string.IsNullOrEmpty(SmbPassword))
                {
                    using (NetworkShareAccesser.Access(SmbComputerName, SmbDomain, SmbUsername, SmbPassword))
                    {
                        success = UglifyCssFiles(ref atLeastOneSuccess);
                    }
                }
                else
                {
                    success = UglifyCssFiles(ref atLeastOneSuccess);
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while uglifying CSS files.", e);
                success = false;
            }

            var status = Status.Success;

            if (!success && atLeastOneSuccess)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status);
        }

        private bool UglifyCssFiles(ref bool atLeastOneSuccess)
        {
            var success = true;
            var cssFiles = SelectFiles();

            foreach (var cssFile in cssFiles)
            {
                try
                {
                    var source = File.ReadAllText(cssFile.Path);
                    var result = Uglify.Css(source);
                    if (result.HasErrors)
                    {
                        ErrorFormat("An error occured while uglifying the CSS file {0}: {1}", cssFile.Path, string.Concat(result.Errors.Select(e => e.Message + "\n").ToArray()));
                        success = false;
                        continue;
                    }

                    var destPath = Path.Combine(Workflow.WorkflowTempFolder, Path.GetFileNameWithoutExtension(cssFile.FileName) + ".min.css");
                    File.WriteAllText(destPath, result.Code);
                    Files.Add(new FileInf(destPath, Id));
                    InfoFormat("The CSS file {0} has been uglified -> {1}", cssFile.Path, destPath);
                    if (!atLeastOneSuccess)
                    {
                        atLeastOneSuccess = true;
                    }
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("An error occured while uglifying the CSS file {0}: {1}", cssFile.Path, e.Message);
                    success = false;
                }
            }

            return success;
        }
    }
}
