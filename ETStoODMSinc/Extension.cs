using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
    class Extension
    {
        private string definition;
        private string details;

        public string Definition
        {
            get { return definition; }
        }
        public string Details
        {
            get { return details; }
        }

        public Extension(string definition, string details)
        {
            this.definition = definition;
            this.details = details;  //For a new kind of type or if class inheriting from another class

        }
    }
}
