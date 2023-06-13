﻿using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.SshCmd
{
    public partial class SshCmd : Task
    {
        public static readonly Regex Prompt = MyRegex();
        public static readonly Regex PwdPrompt = MyRegex1();
        public static readonly Regex PromptOrPwd = new($"{Prompt}|{PwdPrompt}", RegexOptions.Compiled);

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Cmd { get; private set; }
        public double Timeout { get; private set; }
        public TimeSpan ExpectTimeout { get; private set; }

        public SshCmd(XElement xe, Workflow wf) : base(xe, wf)
        {
            Host = GetSetting("host");
            Port = int.Parse(GetSetting("port", "22"));
            Username = GetSetting("username");
            Password = GetSetting("password");
            Cmd = GetSetting("cmd");
            Timeout = double.Parse(GetSetting("timeout", "60"));
            ExpectTimeout = TimeSpan.FromSeconds(Timeout);
        }

        public override TaskStatus Run()
        {
            Info("Running SSH command...");

            var success = true;
            ShellStream stream = null;

            try
            {
                ConnectionInfo connectionInfo = new(Host, Port, Username, new PasswordAuthenticationMethod(Username, Password));
                SshClient sshclient = new(connectionInfo);
                sshclient.Connect();
                Dictionary<TerminalModes, uint> modes = new() { { TerminalModes.ECHO, 53 } };
                stream = sshclient.CreateShellStream("xterm", 80, 24, 800, 600, 4096, modes);
                var result = stream.Expect(Prompt, ExpectTimeout);

                if (result == null)
                {
                    Error($"Timeout {Timeout} seconds reached while connecting.");
                    return new TaskStatus(Status.Error);
                }

                foreach (var line in result.GetLines())
                {
                    Info(line);
                }

                SendCommand(stream, Cmd);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Error while running SSH command: {0}\n", e.Message);
                success = false;
            }
            finally
            {
                stream?.Close();
            }
            var status = Status.Success;

            if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status);
        }

        public void SendCommand(ShellStream stream, string cmd)
        {
            stream.WriteLine(cmd);
            var result = stream.Expect(PromptOrPwd, ExpectTimeout);

            if (result == null)
            {
                Error($"Timeout {Timeout} seconds reached executing {cmd}");
                return;
            }

            if (PwdPrompt.IsMatch(result))
            {
                stream.WriteLine(Password);
                var res = stream.Expect(Prompt, ExpectTimeout);

                if (res == null)
                {
                    Error($"Timeout {Timeout} seconds reached executing {cmd}");
                    return;
                }

                result += res;
            }

            var echoCmd = "echo $?";
            stream.WriteLine(echoCmd);
            var errorCode = stream.Expect(Prompt, ExpectTimeout);

            if (errorCode == null)
            {
                Error($"Timeout {Timeout} seconds reached executing {echoCmd}");
                return;
            }

            if (errorCode.Contains(echoCmd))
            {
                errorCode = errorCode.StringAfter(echoCmd);
            }
            errorCode = errorCode.TrimStart('\r', '\n');
            if (!string.IsNullOrEmpty(errorCode))
            {
                errorCode = errorCode.First().ToString();
            }

            if (errorCode == "0")
            {
                foreach (var line in result.GetLines())
                {
                    Info(line);
                }
            }
            else if (result.Length > 0)
            {
                foreach (var line in result.GetLines())
                {
                    Error(line);
                }
            }
        }

        [GeneratedRegex("[a-zA-Z0-9_.-]*\\@[a-zA-Z0-9_.-]*\\:\\~[#$] ", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        [GeneratedRegex("password for .*\\:", RegexOptions.Compiled)]
        private static partial Regex MyRegex1();
    }
}
