﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.Http
{
    public class Http : Task
    {
        private const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
        private const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

        public string[] Urls { get; private set; }

        public Http(XElement xe, Workflow wf) : base(xe, wf)
        {
            Urls = GetSettings("url");
        }

        public override TaskStatus Run()
        {
            Info("Downloading files...");

            var success = true;
            var atLeastOneSucceed = false;

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = Tls12;

            foreach (var url in Urls)
            {
                try
                {
                    var fileName = Path.GetFileName(url) ?? throw new Exception("File name is null");
                    var destPath = Path.Combine(Workflow.WorkflowTempFolder, fileName);

                    DownloadFile(url, destPath).Wait();

                    InfoFormat("File {0} downlaoded as {1}", url, destPath);
                    Files.Add(new FileInf(destPath, Id));

                    if (!atLeastOneSucceed)
                    {
                        atLeastOneSucceed = true;
                    }
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("An error occured while downloading the file: {0}. Error: {1}", url, e.Message);
                    success = false;
                }
            }

            var status = Status.Success;

            if (!success && atLeastOneSucceed)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status, false);
        }

        private static async System.Threading.Tasks.Task DownloadFile(string url, string path)
        {
            using HttpClient client = new();
            var response = await client.GetAsync(url);
            using FileStream fs = new(path, FileMode.CreateNew);
            await response.Content.CopyToAsync(fs);
        }
    }
}
