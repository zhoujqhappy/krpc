using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    static class DocumentationExtensions
    {
        static IDictionary<string, XmlNode> documentation = new Dictionary<string, XmlNode> ();

        /// <summary>
        /// Remove indentiation from a multi-line string, where the first line
        /// is not indented, and following lines are indented.
        /// </summary>
        static string Dedent (String content)
        {
            var lines = content.Split ('\n');
            if (lines.Length == 1)
                return lines [0].Trim ();
            var indent = int.MaxValue;
            for (int i = 1; i < lines.Length; i++) {
                var newindex = 0;
                for (var j = 0; j < lines [i].Length; j++) {
                    if (lines [i] [j] != ' ') {
                        indent = Math.Min (indent, newindex);
                        break;
                    }
                    newindex++;
                }
            }
            if (indent == int.MaxValue)
                return content;
            lines [0] = new String (' ', indent) + lines [0];
            var result = String.Empty;
            foreach (var line in lines) {
                if (line.Length > indent)
                    result += line.Substring (indent);
                result += "\n";
            }
            return result.Trim ();
        }

        internal static String GetDocumentation (this MemberInfo member)
        {
            var membersNode = GetDocumentation (member.Module.Assembly.Location);
            if (membersNode == null)
                return String.Empty;
            var name = GetDocumentationName (member);
            var it = membersNode.ChildNodes.GetEnumerator ();
            while (it.MoveNext ()) {
                var node = (XmlNode)it.Current;
                var attr = node.Attributes.GetNamedItem ("name");
                if (attr != null && attr.Value == name) {
                    var content = String.Empty;
                    var descnode = node.GetEnumerator ();
                    while (descnode.MoveNext ())
                        content += Dedent (((XmlNode)descnode.Current).OuterXml.Replace ("\r\n", "\n")) + "\n";
                    return "<doc>\n" + content.TrimEnd () + "\n</doc>";
                }
            }
            return String.Empty;
        }

        [SuppressMessage ("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        static XmlNode GetDocumentation (string assemblyPath)
        {
            var path = Path.GetDirectoryName (assemblyPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (assemblyPath) + ".xml";
            if (!documentation.ContainsKey (path)) {
                if (!File.Exists (path)) {
                    Logger.WriteLine ("No documentation found for " + assemblyPath + " at " + path, Logger.Severity.Warning);
                    documentation [path] = null;
                } else {
                    Logger.WriteLine ("Loading documentation for " + assemblyPath + " from " + path);
                    var document = new XmlDocument ();
                    document.Load (path);
                    var membersNodes = document.DocumentElement.GetElementsByTagName ("members");
                    if (membersNodes.Count == 0)
                        Logger.WriteLine ("Failed to load documentation for " + assemblyPath + " from " + path);
                    documentation [path] = membersNodes.Item (0);
                }
            }
            return documentation [path];
        }

        [SuppressMessage ("Gendarme.Rules.Portability", "DoNotHardcodePathsRule")]
        internal static string GetDocumentationName (MemberInfo member)
        {
            char prefix;
            var memberType = member as Type;
            string name = memberType != null ? memberType.FullName : member.DeclaringType.FullName + "." + member.Name;
            name = name.Replace ('+', '.');
            name = Regex.Replace (name, @"\[\[.+\]\]", String.Empty);

            switch (member.MemberType) {

            case MemberTypes.NestedType:
            case MemberTypes.TypeInfo:
                prefix = 'T';
                break;

            case MemberTypes.Method:
                prefix = 'M';
                var parameters = String.Join (",", ((MethodBase)member).GetParameters ().Select (x => ParameterName (x)).ToArray ());
                if (!String.IsNullOrEmpty (parameters))
                    name += "(" + parameters + ")";
                break;

            case MemberTypes.Property:
                prefix = 'P';
                break;

            case MemberTypes.Field:
                prefix = 'F';
                break;

            default:
                throw new ArgumentException ("Unknown member type");
            }
            return prefix + ":" + name;
        }

        static string TypeName (Type type)
        {
            var name = type.FullName;
            name = name.Replace ('+', '.');
            if (!type.IsGenericType)
                return name;
            name = name.Remove (name.IndexOf ('`'));
            var typeArguments = "{" + String.Join (",", type.GetGenericArguments ().Select (x => TypeName (x)).ToArray ()) + "}";
            return name + typeArguments;
        }

        static string ParameterName (ParameterInfo parameter)
        {
            return TypeName (parameter.ParameterType);
        }
    }
}