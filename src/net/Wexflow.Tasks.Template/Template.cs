﻿using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.Template
{
    public class Template : Task
    {
        public Template(XElement xe, Workflow wf)
            : base(xe, wf)
        {
            // Task settings goes here
        }

        public override TaskStatus Run()
        {
            try
            {
                // Task logic goes here

                return new TaskStatus(Status.Success);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
        }
    }
}
