using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PRepo.Tcp.Common
{
	public static class XmlUtils
	{
		public static string GetAttributeValue(this XElement xElement, string attributeName)
		{
			string attrValue = string.Empty;
			try
			{
				if (xElement != null)
				{
					XAttribute attribute = xElement.Attribute(attributeName);
					if (attribute != null)
					{
						attrValue = attribute.Value;
					}
				}
			}
			catch { }
			return attrValue;
		}
	}
}
