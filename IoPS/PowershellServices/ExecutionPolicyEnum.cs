using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoPS
{
	public enum ExecutionPolicy
	{
		Unrestricted, 
		RemoteSigned, 
		AllSigned, 
		Restricted, 
		Default, 
		Bypass, 
		Undefined
	}
}
