using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EFIngresProvider
{
    /// <summary>
    /// EFIngresProviderInstaller provides methods that put the entries required to use EFIngresProvider
    /// into configuration files
    /// </summary>
    public static class EFIngresProviderInstaller
    {
        public const string ProviderName = "Ingres Entity Framework Provider";
        public const string ProviderInvariantName = "EFIngresProvider";
        public const string Description = ".Net Framework Data Provider for Ingres";

        public static string GetFactoryTypeName(bool assemblyQualifiedName)
        {
            if (assemblyQualifiedName)
            {
                return typeof(EFIngresProviderFactory).AssemblyQualifiedName;
            }
            return $"{typeof(EFIngresProviderFactory).FullName}, {typeof(EFIngresProviderFactory).Assembly.GetName().Name}";
        }

        /// <summary>
        /// Gets the path to the configuration file for the running application.
        /// <para>F.eks. Web.config</para>
        /// </summary>
        /// <returns>The path to the configuration file for the running application</returns>
        public static string GetCurrentConfigFilePath()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
        }

        public static bool Install(bool assemblyQualifiedName)
        {
            return Install(GetCurrentConfigFilePath(), true, assemblyQualifiedName);
        }

        public static bool Install(string filename, bool removeFirst, bool assemblyQualifiedName)
        {
            var doc = XDocument.Load(filename, LoadOptions.PreserveWhitespace);
            var changed = Install(filename, doc, removeFirst, assemblyQualifiedName);

            if (changed)
            {
                doc.Save(filename, SaveOptions.DisableFormatting);
            }

            return changed;
        }

        public static bool Install(string filename, XDocument doc, bool removeFirst, bool assemblyQualifiedName)
        {
            var changed = false;

            var configurationElement = doc.Element("configuration");
            if (configurationElement == null)
            {
                throw new ArgumentException("File is not a configuration file", filename);
            }

            var systemDataElement = configurationElement.GetOrCreateElement("system.data", ref changed);
            var dbProviderFactoriesElement = systemDataElement.GetOrCreateElement("DbProviderFactories", ref changed);

            var removeElements = dbProviderFactoriesElement
                .Elements("remove")
                .Where(x => x.AttributeValue("invariant") == ProviderInvariantName);

            if (removeElements.Any())
            {
                if (removeFirst)
                {
                    changed = RemoveElements(removeElements.Skip(1)) || changed;
                    changed = ReplaceElement(removeElements.First(), CreateRemoveEFIngresNode()) || changed;
                }
                else
                {
                    changed = RemoveElements(removeElements) || changed;
                }
            }
            else
            {
                if (removeFirst)
                {
                    dbProviderFactoriesElement.AddFirstElement(CreateRemoveEFIngresNode());
                    changed = true;
                }
            }

            var addElements = dbProviderFactoriesElement
                .Elements("add")
                .Where(x => x.AttributeValue("invariant") == ProviderInvariantName);

            if (addElements.Any())
            {
                changed = RemoveElements(addElements.Skip(1)) || changed;
                changed = ReplaceElement(addElements.First(), CreateAddEFIngresNode(assemblyQualifiedName)) || changed;
            }
            else
            {
                dbProviderFactoriesElement.AddLastElement(CreateAddEFIngresNode(assemblyQualifiedName));
                changed = true;
            }

            return changed;
        }

        public static bool Uninstall()
        {
            return Uninstall(GetCurrentConfigFilePath());
        }

        public static bool Uninstall(string filename)
        {
            var doc = XDocument.Load(filename, LoadOptions.PreserveWhitespace);
            var changed = Uninstall(filename, doc);

            if (changed)
            {
                doc.Save(filename, SaveOptions.DisableFormatting);
            }

            return changed;
        }

        public static bool Uninstall(string filename, XDocument doc)
        {
            var changed = false;

            var configurationElement = doc.Element("configuration");
            if (configurationElement == null)
            {
                throw new ArgumentException("File is not a configuration file", filename);
            }

            var systemDataElement = configurationElement.Element("system.data");
            if (systemDataElement == null)
            {
                return false;
            }

            var dbProviderFactoriesElement = systemDataElement.Element("DbProviderFactories");
            if (dbProviderFactoriesElement == null)
            {
                return false;
            }

            var removeElements = dbProviderFactoriesElement
                .Elements("remove")
                .Where(x => x.AttributeValue("invariant") == ProviderInvariantName);

            if (removeElements.Any())
            {
                changed = RemoveElements(removeElements) || changed;
            }

            var addElements = dbProviderFactoriesElement
                .Elements("add")
                .Where(x => x.AttributeValue("invariant") == ProviderInvariantName);

            if (addElements.Any())
            {
                changed = RemoveElements(addElements) || changed;
            }

            if (changed)
            {
                if (!dbProviderFactoriesElement.Elements().Any())
                {
                    RemoveElement(dbProviderFactoriesElement);
                }
                if (!systemDataElement.Elements().Any())
                {
                    RemoveElement(systemDataElement);
                }
            }

            return changed;
        }

        private static XElement CreateRemoveEFIngresNode()
        {
            // <add name="Ingres Entity Framework Provider"
            //      invariant="EFIngresProvider"
            //      description=".Net Framework Data Provider for Ingres"
            //      type="EFIngresProvider.EFIngresProviderFactory, EFIngresProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=99a505ee199e6b8a"
            // />
            return new XElement("remove",
                new XAttribute("invariant", ProviderInvariantName)
            );
        }

        private static XElement CreateAddEFIngresNode(bool assemblyQualifiedName)
        {
            // <add name="Ingres Entity Framework Provider"
            //      invariant="EFIngresProvider"
            //      description=".Net Framework Data Provider for Ingres"
            //      type="EFIngresProvider.EFIngresProviderFactory, EFIngresProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=99a505ee199e6b8a"
            // />
            return new XElement("add",
                new XAttribute("name", ProviderName),
                new XAttribute("invariant", ProviderInvariantName),
                new XAttribute("description", Description),
                new XAttribute("type", GetFactoryTypeName(assemblyQualifiedName))
            );
        }

        #region XML Utils

        private static string AttributeValue(this XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute == null)
            {
                return null;
            }
            return attribute.Value;
        }

        private static XElement GetOrCreateElement(this XElement parent, XName name, ref bool created)
        {
            var element = parent.Element(name);
            if (element == null)
            {
                element = parent.AddLastElement(new XElement(name));
                var indent = GetIndent(parent);
                if (indent != null)
                {
                    element.Add(new XText(indent));
                }
                created = true;
            }
            return element;
        }

        private static bool RemoveElements(IEnumerable<XElement> elements)
        {
            var anyRemoved = false;
            foreach (var element in elements.Reverse())
            {
                RemoveElement(element);
                anyRemoved = true;
            }
            return anyRemoved;
        }

        private static void RemoveElement(XElement element)
        {
            if (element.PreviousNode.IsWhitespace())
            {
                element.PreviousNode.Remove();
            }
            element.Remove();
        }

        private static XElement AddFirstElement(this XContainer parent, XElement element)
        {
            var indent = GetIndent(parent);
            parent.AddFirst(element);
            if (indent != null)
            {
                parent.AddFirst(new XText(indent));
            }
            return element;
        }

        private static XElement AddLastElement(this XContainer parent, XElement element)
        {
            var indent = GetIndent(parent);
            if (parent.LastNode.IsWhitespace())
            {
                if (indent != null)
                {
                    parent.LastNode.AddBeforeSelf(new XText(indent));
                }
                parent.LastNode.AddBeforeSelf(element);
            }
            else
            {
                if (indent != null)
                {
                    parent.Add(new XText(indent));
                }
                parent.Add(element);
            }
            return element;
        }

        private static bool IsWhitespace(this XNode node)
        {
            var textNode = node as XText;
            if (textNode == null)
            {
                return false;
            }
            return string.IsNullOrWhiteSpace(textNode.Value);
        }

        private static string GetIndent(XContainer parent)
        {
            var firstNode = parent.FirstNode as XText;
            if (firstNode != null)
            {
                if (firstNode.NextNode == null)
                {
                    return firstNode.Value + "  ";
                }
                else
                {
                    return firstNode.Value;
                }
            }
            return null;
        }

        private static bool ReplaceElement(XElement oldElement, XElement newElement)
        {
            if (!XElementsEqual(oldElement, newElement))
            {
                oldElement.ReplaceWith(newElement);
                return true;
            }
            return false;
        }

        private static bool XAttributesEqual(XAttribute node1, XAttribute node2)
        {
            return (node1.Name == node2.Name) && (node1.Value == node2.Value);
        }

        private static bool XNodesEqual(XNode node1, XNode node2)
        {
            if (node1.GetType() != node2.GetType())
            {
                return false;
            }
            if (node1 is XDocument)
            {
                return XDocumentsEqual((XDocument)node1, (XDocument)node2);
            }
            if (node1 is XElement)
            {
                return XElementsEqual((XElement)node1, (XElement)node2);
            }
            if (node1 is XText)
            {
                return XTextEqual((XText)node1, (XText)node2);
            }
            if (node1 is XComment)
            {
                return XCommentsEqual((XComment)node1, (XComment)node2);
            }
            return false;
        }

        private static bool XContainersEqual(XContainer node1, XContainer node2)
        {
            if (node1.Nodes().Count() != node2.Nodes().Count())
            {
                return false;
            }
            if (!node1.Nodes()
                      .Zip(node2.Nodes(), (n1, n2) => XNodesEqual(n1, n2))
                      .All(x => x))
            {
                return false;
            }
            return true;
        }

        private static bool XDocumentsEqual(XDocument node1, XDocument node2)
        {
            return XContainersEqual(node1, node2);
        }

        private static bool XElementsEqual(XElement node1, XElement node2)
        {
            if (node1.Name != node2.Name)
            {
                return false;
            }
            if (node1.Attributes().Count() != node2.Attributes().Count())
            {
                return false;
            }
            if (!node1.Attributes()
                        .Zip(node2.Attributes(), (a1, a2) => XAttributesEqual(a1, a2))
                        .All(x => x))
            {
                return false;
            }
            return XContainersEqual(node1, node2);
        }

        private static bool XTextEqual(XText node1, XText node2)
        {
            return node1.Value == node2.Value;
        }

        private static bool XCommentsEqual(XComment node1, XComment node2)
        {
            return node1.Value == node2.Value;
        }

        #endregion
    }
}
