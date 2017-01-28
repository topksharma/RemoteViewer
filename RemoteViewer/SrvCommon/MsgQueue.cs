using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrvCommon
{
	public class MsgQueue<T>
	{
		private Queue<T> _msgQueue = new Queue<T>();
		private readonly object _objLocker = new object();
	}
}
