﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.ProcessLauncher
{
    public partial class ProcessLauncher : Task
    {
        public string ProcessPath { get; set; }
        public string ProcessCmd { get; set; }
        public bool HideGui { get; set; }
        public bool GeneratesFiles { get; set; }
        public bool LoadAllFiles { get; set; }

        private const string VarFilePath = "$filePath";
        private const string VarFileName = "$fileName";
        private const string VarFileNameWithoutExtension = "$fileNameWithoutExtension";
        private const string VarOutput = "$output";

        public ProcessLauncher(XElement xe, Workflow wf)
            : base(xe, wf)
        {
            ProcessPath = GetSetting("processPath");
            ProcessCmd = GetSetting("processCmd");
            HideGui = bool.Parse(GetSetting("hideGui"));
            GeneratesFiles = bool.Parse(GetSetting("generatesFiles"));
            LoadAllFiles = bool.Parse(GetSetting("loadAllFiles", "false"));
        }

        public override TaskStatus Run()
        {
            Info("Launching process...");

            if (GeneratesFiles && !(ProcessCmd.Contains(VarFileName) && ProcessCmd.Contains(VarOutput) && (ProcessCmd.Contains(VarFileName) || ProcessCmd.Contains(VarFileNameWithoutExtension))))
            {
                Error("Error in process command. Please read the documentation.");
                return new TaskStatus(Status.Error, false);
            }

            var success = true;
            var atLeastOneSucceed = false;

            if (!GeneratesFiles)
            {
                return StartProcess(ProcessPath, ProcessCmd, HideGui);
            }

            foreach (var file in SelectFiles())
            {
                string cmd;
                string outputFilePath;

                try
                {
                    cmd = ProcessCmd.Replace(string.Format("{{{0}}}", VarFilePath), string.Format("\"{0}\"", file.Path));

                    var outputRegex = OutputRegex();
                    var m = outputRegex.Match(cmd);

                    if (m.Success)
                    {
                        var val = m.Value;
                        outputFilePath = val;
                        if (outputFilePath.Contains(VarFileNameWithoutExtension))
                        {
                            outputFilePath = outputFilePath.Replace(VarFileNameWithoutExtension, Path.GetFileNameWithoutExtension(file.FileName));
                        }
                        else if (outputFilePath.Contains(VarFileName))
                        {
                            outputFilePath = outputFilePath.Replace(VarFileName, file.FileName);
                        }
                        outputFilePath = outputFilePath.Replace($"{{{VarOutput}:", Workflow.WorkflowTempFolder.Trim('\\') + "\\");
                        outputFilePath = outputFilePath.Trim('}');

                        cmd = cmd.Replace(val, $"\"{outputFilePath}\"");
                    }
                    else
                    {
                        Error("Error in process command. Please read the documentation.");
                        return new TaskStatus(Status.Error, false);
                    }
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("Error in process command. Please read the documentation. Error: {0}", e.Message);
                    return new TaskStatus(Status.Error, false);
                }

                if (StartProcess(ProcessPath, cmd, HideGui).Status == Status.Success)
                {
                    Files.Add(new FileInf(outputFilePath, Id));

                    if (LoadAllFiles)
                    {
                        var files = Directory.GetFiles(Workflow.WorkflowTempFolder, "*.*", SearchOption.AllDirectories);

                        foreach (var f in files)
                        {
                            if (f != outputFilePath)
                            {
                                Files.Add(new FileInf(f, Id));
                            }
                        }
                    }

                    if (!atLeastOneSucceed)
                    {
                        atLeastOneSucceed = true;
                    }
                }
                else
                {
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

        private TaskStatus StartProcess(string processPath, string processCmd, bool hideGui)
        {
            try
            {
                ProcessStartInfo startInfo = new(processPath, processCmd)
                {
                    CreateNoWindow = hideGui,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new() { StartInfo = startInfo };
                process.OutputDataReceived += OutputHandler;
                process.ErrorDataReceived += ErrorHandler;
                _ = process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return new TaskStatus(Status.Success, false);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while launching the process {0}", e, processPath);
                return new TaskStatus(Status.Error, false);
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            InfoFormat("{0}", outLine.Data);
        }

        private void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            ErrorFormat("{0}", outLine.Data);
        }

        [GeneratedRegex("{\\$output:(?:\\$fileNameWithoutExtension|\\$fileName)(?:[a-zA-Z0-9._-]*})")]
        private static partial Regex OutputRegex();
    }
}
