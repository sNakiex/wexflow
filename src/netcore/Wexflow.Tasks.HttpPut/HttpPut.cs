﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.HttpPut
{
    public class HttpPut : Task
    {
        public string Url { get; private set; }
        public string Payload { get; private set; }
        public string AuthorizationScheme { get; private set; }
        public string AuthorizationParameter { get; private set; }
        public string Type { get; private set; }

        public HttpPut(XElement xe, Workflow wf) : base(xe, wf)
        {
            Url = GetSetting("url");
            Payload = GetSetting("payload");
            AuthorizationScheme = GetSetting("authorizationScheme");
            AuthorizationParameter = GetSetting("authorizationParameter");
            Type = GetSetting("type", "application/json");
        }

        public override TaskStatus Run()
        {
            Info("Executing PUT request...");
            var status = Status.Success;
            try
            {
                var putTask = Put(Url, AuthorizationScheme, AuthorizationParameter, Payload);
                putTask.Wait();
                var result = putTask.Result;
                var destFile = Path.Combine(Workflow.WorkflowTempFolder, string.Format("HttpPut_{0:yyyy-MM-dd-HH-mm-ss-fff}", DateTime.Now));
                File.WriteAllText(destFile, result);
                Files.Add(new FileInf(destFile, Id));
                InfoFormat("PUT request {0} executed whith success -> {1}", Url, destFile);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while executing the PUT request {0}: {1}", Url, e.Message);
                status = Status.Error;
            }
            Info("Task finished.");
            return new TaskStatus(status);
        }

        public async System.Threading.Tasks.Task<string> Put(string url, string authScheme, string authParam, string payload)
        {
            using (StringContent httpContent = new(payload, Encoding.UTF8, Type))
            using (HttpClient httpClient = new())
            {
                if (!string.IsNullOrEmpty(authScheme) && !string.IsNullOrEmpty(authParam))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, authParam);
                }

                var httpResponse = await httpClient.PutAsync(url, httpContent);
                if (httpResponse.Content != null)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    return responseContent;
                }
            }
            return string.Empty;
        }
    }
}
