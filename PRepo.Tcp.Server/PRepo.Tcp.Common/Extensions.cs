using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRepo.Tcp.Common
{
	public static class Extensions
	{
		public static bool IsNotNullOrEmpty<T>(this T array) where T : IList
		{
			return array?.Count > 0;
		}
	}
}
