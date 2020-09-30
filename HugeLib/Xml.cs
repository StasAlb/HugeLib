using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace HugeLib
{
    public static class XmlClass
    {
        public static string GetDataXml(XmlDocument xd, string address, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = xd.DocumentElement.SelectSingleNode(address, xnm);

                if (xn == null)
                    return "";
                return xn.InnerText;
            }
            catch { return ""; }
        }
        public static void SetDataXml(XmlDocument xd, string address, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNode xn = xd.DocumentElement.SelectSingleNode(address, xnm);

                if (xn == null)
                {
                    XmlNode newNode = xd.CreateElement("Admin");
                    newNode.InnerText = value;
                    xd.DocumentElement.AppendChild(newNode);
                }
                else  
                    xn.InnerText = value;
            }
            catch {}
        }
        public static void SetDataXml(XmlDocument xd, string address, string TagName, string TagValue, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                for (int i = 0; i < xnl.Count; i++)
                {
                    if (xnl[i].Attributes[TagName].Value == TagValue)
                    {
                        xnl[i].InnerText = value;
                        return;
                    }
                }
                return;
            }
            catch (Exception x)
            {
                return;
            }
        }
        public static void SetXmlAttribute(XmlDocument xd, string address, string AttributeName, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNode xn = xd.DocumentElement.SelectSingleNode(address, xnm);
                if (xn == null)
                    return;
                try
                {
                    xn.Attributes[AttributeName].Value = value;
                }
                catch
                {
                    XmlAttribute xa = xd.CreateAttribute(AttributeName);
                    xa.Value = value;
                    xn.Attributes.Append(xa);
                }
                return;
            }
            catch (Exception x)
            {
                return;
            }
        }
        public static void SetXmlAttribute(XmlDocument xd, string address, string TagName, string TagValue, string AttributeName, XmlNamespaceManager xnm, string value)
        {
            try
            {
                bool found = false;
                XmlNodeList xnl = xd.DocumentElement?.SelectNodes(address, xnm);
                for (int i = 0; i < xnl?.Count; i++)
                {
                    if (xnl[i].Attributes[TagName].Value == TagValue)
                    {
                        //XmlNode xn = xnl[i].SelectSingleNode(ElementName);
                        try
                        {
                            xnl[i].Attributes[AttributeName].Value = value;
                            found = true;
                        }
                        catch
                        {
                            XmlAttribute xa = xd.CreateAttribute(AttributeName);
                            xa.Value = value;
                            xnl[i].Attributes.Append(xa);
                        }
                        return;
                    }
                }
                if (!found)
                {
                    XmlNode xn = xd.CreateNode(XmlNodeType.Element, address, "");
                    XmlAttribute xa1 = xd.CreateAttribute(TagName);
                    xa1.Value = TagValue;
                    xn?.Attributes?.Append(xa1);
                    XmlAttribute xa2 = xd.CreateAttribute(AttributeName);
                    xa2.Value = value;
                    xn?.Attributes?.Append(xa2);
                    xd?.DocumentElement?.AppendChild(xn);
                }
                return;
            }
            catch (Exception x)
            {
                return;
            }
        }
        public static string GetDataXml(XmlDocument xd, string address, string ValueName, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = null;
                if (address.Length == 0)
                    xn = xd.FirstChild;
                else
                    xn = xd.DocumentElement.SelectSingleNode(address, xnm);
                if (xn == null)
                    return "";
                return xn.Attributes[ValueName].Value;
            }
            catch { return ""; }
        }
        public static int GetDataXmlInt(XmlDocument xd, string address, string ValueName, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = null;
                if (address.Length == 0)
                    xn = xd.FirstChild;
                else
                    xn = xd.DocumentElement.SelectSingleNode(address, xnm);
                if (xn == null)
                    return 0;
                return Convert.ToInt32(xn.Attributes[ValueName].Value);
            }
            catch { return 0; }
        }
        public static int GetDataXmlInt(XmlDocument xd, string address, string ValueName, int defaultValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = null;
                if (address.Length == 0)
                    xn = xd.FirstChild;
                else
                    xn = xd.DocumentElement.SelectSingleNode(address, xnm);
                if (xn == null)
                    return defaultValue;
                return Convert.ToInt32(xn.Attributes[ValueName].Value);
            }
            catch { return defaultValue; }
        }
        public static string GetXmlAttribute(XmlDocument xd, string address, string TagName, string TagValue, string AttributeName, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null)
                    return "";
                foreach (XmlNode xn in xnl)
                {
                    if (xn.Attributes[TagName].Value == TagValue)
                        return xn.Attributes[AttributeName].Value;
                }
                return "";
            }
            catch { return ""; }
        }
        public static string GetDataXml(XmlDocument xd, string address, string TagName, string TagValue, string AttributeName, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null)
                    return "";
                foreach (XmlNode xn in xnl)
                {
                    for (int i = 0; i < xn.ChildNodes.Count; i++)
                    {
                        if (xn.ChildNodes[i].Attributes[TagName].Value == TagValue)
                            return xn.ChildNodes[i].Attributes[AttributeName].Value;
                    }
                }
                return "";
            }
            catch { return ""; }
        }
        //начиная с этого - новая версия
        public static int GetXmlNodeCount(XmlDocument xd, string address, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = null;
                if (address.Length == 0)
                {
                    int ret = 0;
                    xnl = xd.DocumentElement.ChildNodes;
                    foreach (XmlNode xn in xnl)
                        if (xn.NodeType == XmlNodeType.Element)
                            ret++;
                    return ret;
                }
                    
                xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null)
                    return 0;
                return xnl.Count;
            }
            catch { return 0; }
        }
        public static XmlDocument GetXmlNode(XmlDocument xd, string address, int index, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = null;
                XmlDocument res = null;
                if (address == "")
                {
                    xnl = xd.DocumentElement.ChildNodes;
                    int cnt = 0;
                    for(int i=0;i<xnl.Count;i++)
                    {
                        if (xnl[i].NodeType == XmlNodeType.Element)
                        {
                            if (cnt == index)
                            {
                                res = new XmlDocument();
                                res.AppendChild(res.ImportNode(xnl[i], true));
                                return res;
                            }
                            cnt++;
                        }
                    }
                    return null;
                }
                xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null || xnl.Count <= index)
                    return null;
                res = new XmlDocument();
                res.AppendChild(res.ImportNode(xnl[index], true));
                return res;
            }
            catch { return null; }
        }
        public static void DeleteXmlNode(XmlDocument xd, string address, int index, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = null;
                if (address == "")
                {
                    xnl = xd.DocumentElement.ChildNodes;
                    int cnt = 0;
                    for (int i = 0; i < xnl.Count; i++)
                    {
                        if (xnl[i].NodeType == XmlNodeType.Element)
                        {
                            if (cnt == index)
                            {
                                xnl[i].ParentNode.RemoveChild(xnl[i]);
                                return;
                            }
                            cnt++;
                        }
                    }
                    return;
                }
                xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null || xnl.Count <= index)
                    return;
                xnl[index].ParentNode.RemoveChild(xnl[index]);
                return;
            }
            catch { return; }
        }
        public static XmlNode GetNode(XmlDocument xd, string address, int index, XmlNamespaceManager xnm)
        {
            return GetNode(xd.DocumentElement, address, index, xnm);
        }
        public static XmlNode GetNode(XmlNode xd, string address, int index, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = null;
                if (address == "")
                {
                    xnl = xd.ChildNodes;
                    int cnt = 0;
                    for (int i = 0; i < xnl.Count; i++)
                    {
                        if (xnl[i].NodeType == XmlNodeType.Element)
                        {
                            if (cnt == index)
                                return xnl[i];
                            cnt++;
                        }
                    }
                    return null;
                }
                xnl = xd.SelectNodes(address, xnm);
                if (xnl == null || xnl.Count <= index)
                    return null;
                return xnl[index];
            }
            catch { return null; }
        }
        public static XmlDocument GetXmlNode(XmlDocument xd, string address, string AttributeName, string AttributeValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                for(int i=0;i<xnl.Count;i++)
                {
                    if (xnl[i].Attributes[AttributeName].Value == AttributeValue)
                    {
                        XmlDocument res = new XmlDocument();
                        res.AppendChild(res.ImportNode(xnl[i], true));
                        return res;
                    }
                }
                return null;
            }
            catch { return null; }
        }
        public static XmlNode GetNode(XmlDocument xd, string address, string AttributeName, string AttributeValue, XmlNamespaceManager xnm)
        {
            return GetNode(xd.DocumentElement, address, AttributeName, AttributeValue, xnm);
        }
        public static XmlNode GetNode(XmlNode xd, string address, string AttributeName, string AttributeValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = xd.SelectNodes(address, xnm);
                for (int i = 0; i < xnl.Count; i++)
                {
                    if (xnl[i].Attributes[AttributeName].Value == AttributeValue)
                        return xnl[i];
                }
                return null;
            }
            catch { return null; }
        }
        

        public static void AddXmlNode(XmlDocument xd, string address, XmlNode addNode, XmlNamespaceManager xnm)
        {
            XmlNodeList xnl = null;
            if (address == "")
            {
                xd.DocumentElement.AppendChild(addNode);
                return;
            }
            xnl = xd.DocumentElement.SelectNodes(address, xnm);
            if (xnl == null)
                return;
            xnl[0].AppendChild(addNode);
            return;
        }
        public static string GetAttribute(XmlDocument xd, string address, string attributeName, XmlNamespaceManager xnm)
        {
            return GetAttribute(xd, address, attributeName, "", xnm);
        }
        public static string GetAttribute(XmlDocument xd, string address, string attributeName, string defaultValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = null;
                if (address.Length > 0)
                    xn = xd.DocumentElement.SelectSingleNode(address);
                else
                    xn = xd.DocumentElement;
                if (xn == null)
                    return defaultValue;
                return xn.Attributes[attributeName].Value;
            }
            catch
            {
                return defaultValue;
            }
        }
        public static string GetTag(XmlDocument xd, string address, XmlNamespaceManager xnm)
        {
            return GetTag(xd, address, "", xnm);
        }
        public static string GetTag(XmlDocument xd, string address, string defaultValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNode xn = null;
                if (address.Length > 0)
                    xn = xd.DocumentElement.SelectSingleNode(address, xnm);
                else
                    xn = xd.DocumentElement;
                if (xn == null)
                    return defaultValue;
                return xn.InnerText;
            }
            catch { return defaultValue; }
        }
        public static string GetTag(XmlDocument xd, string address, string AttributeName, string AttributeValue, XmlNamespaceManager xnm)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                if (xnl == null)
                    return "";
                foreach (XmlNode xn in xnl)
                {
                    try {
                        if (xn.Attributes[AttributeName].Value == AttributeValue)
                            return xn.InnerText;
                    }
                    catch{ }
                }
                return "";
            }
            catch { return ""; }
        }
        public static void SetTag(XmlDocument xd, string address, string AttributeName, string AttributeValue, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNodeList xnl = xd.DocumentElement.SelectNodes(address, xnm);
                for (int i = 0; i < xnl.Count; i++)
                {
                    if (xnl[i].Attributes[AttributeName].Value == AttributeValue)
                    {
                        xnl[i].InnerText = value;
                        return;
                    }
                }
                return;
            }
            catch
            {
                return;
            }
        }
        public static void SetAttribute(XmlNode xn, string address, string AttributeName, string AttributeValue, string AttributeSet, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNodeList xnl = null;
                if (address.Length > 0)
                    xnl = xn.SelectNodes(address, xnm);
                else
                    xnl = xn.ChildNodes;
                if (xnl == null)
                    return;
                for (int i = 0; i < xnl.Count; i++)
                {
                    if (xnl[i].Attributes[AttributeName].Value == AttributeValue)
                    {
                        xnl[i].Attributes[AttributeSet].Value = value;
                        return;
                    }
                }
                return;
            }
            catch
            {
                return;
            }
        }
        public static void SetAttribute(XmlNode xn, string address, string AttributeSet, XmlNamespaceManager xnm, string value)
        {
            try
            {
                XmlNode xnl = xn.SelectSingleNode(address, xnm);                
                if (xnl == null)
                    return;
                xnl.Attributes[AttributeSet].Value = value;
            }
            catch
            {
                return;
            }
        }
    }
}
