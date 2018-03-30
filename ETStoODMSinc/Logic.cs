using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
	class Logic
	{
		string attribute;
		string logicExpression;

		public Logic(string attribute, string logicExpression)
		{
			this.attribute = attribute;
			this.logicExpression = logicExpression;
		}
	}
}