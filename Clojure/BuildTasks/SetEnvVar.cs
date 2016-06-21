using System;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    public class SetEnvVar : Task
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(Name, Value);
            return true;
        }
    }
}
